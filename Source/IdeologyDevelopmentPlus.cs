using System;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace IdeologyDevelopmentPlus
{
    public class IdeologyDevelopmentPlus : GameComponent
    {
        internal const string Name = "Ideology Development+";

        static Harmony harmony;
        static int points;
        static string explanation;

        /// <summary>
        /// Applies Harmony patches
        /// </summary>
        public IdeologyDevelopmentPlus(Game game)
        {
            if (harmony != null)
                return;

            if (!ModsConfig.IdeologyActive)
            {
                LogUtility.Log($"Ideology DLC is required for Ideology Development+!", LogLevel.Error);
                return;
            }

            harmony = new Harmony("Garwel.IdeologyDevelopmentPlus");
            Type type = typeof(IdeologyDevelopmentPlus);

            void LogPatchError(string methodName) => LogUtility.Log($"Error patching {methodName}.", LogLevel.Error);

            void Patch(string className, string methodName, bool patchPrefix = true, bool patchPostfix = false)
            {
                try
                {
                    if (harmony.Patch(
                        AccessTools.Method($"RimWorld.{className}:{methodName}"),
                        patchPrefix ? new HarmonyMethod(type.GetMethod($"{className}_{methodName}")) : null,
                        patchPostfix ? new HarmonyMethod(type.GetMethod($"{className}_{methodName}")) : null) == null)
                        LogPatchError($"{className}.{methodName}");
                }
                catch (Exception ex)
                { LogUtility.Log($"Exception while patching {className}.{methodName}: {ex}"); }
            }

            Patch("IdeoDevelopmentUtility", "ConfirmChangesToIdeo");
            Patch("IdeoDevelopmentTracker", "TryAddDevelopmentPoints");
            Patch("IdeoDevelopmentTracker", "ResetDevelopmentPoints");
            Patch("Dialog_ReformIdeo", "DoWindowContents", false, true);
            if (harmony.Patch(
                AccessTools.Method("RimWorld.RitualOutcomeEffectWorker_FromQuality:ApplyDevelopmentPoints"),
                prefix: new HarmonyMethod(type.GetMethod("RitualOutcomeEffectWorker_FromQuality_ApplyDevelopmentPoints_Prefix")),
                finalizer: new HarmonyMethod(type.GetMethod("RitualOutcomeEffectWorker_FromQuality_ApplyDevelopmentPoints_Finalizer"))) == null)
                LogPatchError("RitualOutcomeEffectWorker_FromQuality.ApplyDevelopmentPoints");
            if (harmony.Patch(
                AccessTools.PropertyGetter(typeof(IdeoDevelopmentTracker), "NextReformationDevelopmentPoints"),
                new HarmonyMethod(type.GetMethod("IdeoDevelopmentTracker_NextReformationDevelopmentPoints"))) == null)
                LogPatchError("IdeoDevelopmentTracker.NextReformationDevelopmentPoints");
            LogUtility.Log($"Initialization complete.");
        }

        /// <summary>
        /// Shows dialog to make player ideo fluid
        /// </summary>
        public override void FinalizeInit()
        {
            if (harmony == null)
                return;
            Ideo ideo = IdeoUtility.PlayerIdeo;
            if (ideo != null && !ideo.Fluid)
                if (Prefs.DevMode)
                    IdeoUtility.MakeIdeoFluid();
                else Find.WindowStack.Add(new Dialog_MessageBox(
                    $"Do you want to make {ideo.name.Colorize(ideo.TextColor)} ideoligion fluid to allow its development?",
                    "OK".Translate(),
                    IdeoUtility.MakeIdeoFluid,
                    "Cancel".Translate(),
                    acceptAction: IdeoUtility.MakeIdeoFluid,
                    title: Name));
        }

        #region HARMONY PATCHES

        /// <summary>
        /// Adds a check whether we have enough dev points for reform, when the user clicks Done in the reform dialog
        /// </summary>
        public static bool IdeoDevelopmentUtility_ConfirmChangesToIdeo(Ideo ideo, Ideo newIdeo)
        {
            points = IdeoUtility.GetPoints(ideo, newIdeo, out explanation);
            LogUtility.Log($"Available dev points: {IdeoUtility.PlayerIdeoPoints.ToStringCached()}");
            if (IdeoUtility.PlayerIdeoPoints < points)
            {
                Messages.Message($"Can't reform ideoligion: {points.ToStringCached()} development points needed.", MessageTypeDefOf.RejectInput, false);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Replaces RimWorld.IdeoDevelopmentTracker.TryAddDevelopmentPoints so that dev points aren't capped by the amount needed for development
        /// </summary>
        public static bool IdeoDevelopmentTracker_TryAddDevelopmentPoints(IdeoDevelopmentTracker __instance, ref bool __result, int pointsToAdd)
        {
            LogUtility.Log($"IdeoDevelopmentTracker_TryAddDevelopmentPoints({__instance.ideo}, {pointsToAdd.ToStringCached()})");
            bool canReformNow = __instance.CanReformNow;
            __instance.points += Mathf.RoundToInt(pointsToAdd * Settings.DevPointsMultiplier);
            if (!canReformNow && __instance.CanReformNow)
                Find.LetterStack.ReceiveLetter("LetterLabelReformIdeo".Translate(), "LetterTextReformIdeo".Translate(__instance.ideo), LetterDefOf.PositiveEvent);
            __result = true;
            return false;
        }

        /// <summary>
        /// Replaces RimWorld.IdeoDevelopmentTracker.ResetDevelopmentPoints to only remove the spent number of dev points
        /// </summary>
        public static bool IdeoDevelopmentTracker_ResetDevelopmentPoints(IdeoDevelopmentTracker __instance)
        {
            __instance.points -= points;
            points = 0;
            return false;
        }

        // The following members are a hack to make sure that a call to IdeoDevelopmentTracker.NextReformationDevelopmentPoints from
        // RitualOutcomeEffectWorker_FromQuality.ApplyDevelopmentPoints returns an invalid (very large) value. It is used to fix a bug where rituals don't
        // award any dev points if current dev points is exactly the base reform cost.
        static bool inApplyDevelopmentPoints = false;

        public static void RitualOutcomeEffectWorker_FromQuality_ApplyDevelopmentPoints_Prefix() => inApplyDevelopmentPoints = true;

        public static void RitualOutcomeEffectWorker_FromQuality_ApplyDevelopmentPoints_Finalizer() => inApplyDevelopmentPoints = false;

        /// <summary>
        /// Replaces IdeoDevelopmentTracker.NextReformationDevelopmentPoints to change dev points requirements
        /// </summary>
        public static bool IdeoDevelopmentTracker_NextReformationDevelopmentPoints(IdeoDevelopmentTracker __instance, ref int __result)
        {
            if (inApplyDevelopmentPoints)
            {
                inApplyDevelopmentPoints = false;
                __result = int.MaxValue;
            }
            else __result = IdeoUtility.BaseReformCost;
            return false;
        }

        /// <summary>
        /// Displays current and needed dev points
        /// </summary>
        public static void Dialog_ReformIdeo_DoWindowContents(Dialog_ReformIdeo __instance, Rect inRect, Ideo ___ideo, Ideo ___newIdeo, ref IdeoReformStage ___stage)
        {
            points = IdeoUtility.GetPoints(___ideo, ___newIdeo, out explanation, false);
            int availablePoints = IdeoUtility.PlayerIdeoPoints;

            // Surprise Precepts Mode
            if (Settings.RandomizePrecepts && ___stage == IdeoReformStage.PreceptsNarrativeAndDeities)
            {
                LogUtility.Log("Memes selected in Surprise Precepts Mode.");
                ___stage = IdeoReformStage.MemesAndStyles;
                if (availablePoints < points)
                {
                    LogUtility.Log($"Not enough dev points ({points.ToStringCached()} needed, {availablePoints.ToStringCached()} available).");
                    Messages.Message($"Can't reform ideoligion: {points.ToStringCached()} development points needed.", MessageTypeDefOf.RejectInput, false);
                    return;
                }
                ___newIdeo.foundation.RandomizePrecepts(true, new IdeoGenerationParms(Faction.OfPlayer.def));
                LogUtility.Log($"Added precepts: {IdeoUtility.GetAddedPrecepts(___ideo, ___newIdeo).Select(precept => precept.def.defName).ToCommaList()}");
                LogUtility.Log($"Removed precepts: {IdeoUtility.GetRemovedPrecepts(___ideo, ___newIdeo).Select(precept => precept.def.defName).ToCommaList()}");
                LogUtility.Log($"Changed issues: {IdeoUtility.GetChangedIssues(___ideo, ___newIdeo).Select(issue => issue.defName).ToCommaList()}");
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                    "In Surprise Precepts Mode, your precepts are randomly generated based on your chosen memes. You can't manually set or preview them, but they don't cost development points. If you are unhappy with the precepts, you will have to reform again. You can disable Surprise Precepts Mode in ID+ settings.\n\nDo you want to apply the changes?",
                    () => IdeoDevelopmentUtility.ConfirmChangesToIdeo(___ideo, ___newIdeo, () =>
                    {
                        IdeoDevelopmentUtility.ApplyChangesToIdeo(___ideo, ___newIdeo);
                        __instance.Close();
                    }),
                    title: Name));
            }

            // Displaying points and Reset button
            if (availablePoints < points)
                GUI.color = Color.red;
            else GUI.color = Color.white;
            float y = inRect.y;
            Widgets.Label(inRect.x + inRect.width - 100, ref y, 100, $"Points: {points.ToStringCached()} / {availablePoints.ToStringCached()}", explanation);
            GUI.color = Color.white; 
            if (Widgets.ButtonText(new Rect(inRect.x + inRect.width - 100, y, 100, 40), "Reset"))
            {
                LogUtility.Log($"Resetting the ideo.");
                ___ideo.CopyTo(___newIdeo);
            }
        }

        #endregion HARMONY PATCHES
    }
}
