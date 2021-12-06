using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace IdeologyDevelopmentPlus
{
    public class IdeologyDevelopmentPlus : GameComponent
    {
        static Harmony harmony;

        static int points;
        static string explanation = "";

        public IdeologyDevelopmentPlus(Game game)
        {
            Harmony.DEBUG = Prefs.DevMode;

            if (harmony != null)
                return;

            harmony = new Harmony("Garwel.IdeologyDevelopmentPlus");
            Type type = typeof(IdeologyDevelopmentPlus);

            void Patch(string methodToPatch, string prefix = null, string postfix = null)
            {
                MethodInfo patchInfo = null;
                try
                {
                    patchInfo = harmony.Patch(AccessTools.Method(methodToPatch),
                        prefix != null ? new HarmonyMethod(type.GetMethod(prefix)) : null,
                        postfix != null ? new HarmonyMethod(type.GetMethod(postfix)) : null);
                }
                catch (Exception ex)
                { LogUtility.Log($"Exception while patching {methodToPatch}: {ex}"); }
                if (patchInfo == null)
                    LogUtility.Log($"Error patching {methodToPatch}.", LogLevel.Error);
            }

            Patch("RimWorld.IdeoDevelopmentUtility:ConfirmChangesToIdeo", "IdeoDevelopmentUtility_ConfirmChangesToIdeo");
            Patch("RimWorld.IdeoDevelopmentTracker:TryAddDevelopmentPoints", "IdeoDevelopmentTracker_TryAddDevelopmentPoints");
            Patch("RimWorld.IdeoDevelopmentTracker:ResetDevelopmentPoints", "IdeoDevelopmentTracker_ResetDevelopmentPoints");
            Patch("RimWorld.Dialog_ReformIdeo:DoWindowContents", postfix: "Dialog_ReformIdeo_DoWindowContents");
            harmony.Patch(
                AccessTools.PropertyGetter(typeof(IdeoDevelopmentTracker), "NextReformationDevelopmentPoints"),
                new HarmonyMethod(type.GetMethod("IdeoDevelopmentTracker_NextReformationDevelopmentPoints")));
            LogUtility.Log($"Inititalization complete.");
        }

        public override void FinalizeInit()
        {
            Ideo ideo = IdeoUtility.PlayerIdeo;
            if (ideo != null && !ideo.Fluid)
                if (Prefs.DevMode)
                    MakeIdeoFluid();
                else Find.WindowStack.Add(new Dialog_MessageBox(
                    $"Do you want to make {ideo.name.Colorize(ideo.TextColor)} ideoligion fluid to allow its development?",
                    "OK".Translate(),
                    MakeIdeoFluid,
                    "Cancel".Translate(),
                    acceptAction: MakeIdeoFluid,
                    title: "Ideology Development+"));
        }

        public static void MakeIdeoFluid() => IdeoUtility.PlayerIdeo.Fluid = true;

        #region HARMONY PATCHES

        /// <summary>
        /// Adds a check whether we have enough dev points for reform, when the user clicks Done in the reform dialog
        /// </summary>
        public static bool IdeoDevelopmentUtility_ConfirmChangesToIdeo(Ideo ideo, Ideo newIdeo)
        {
            points = IdeoUtility.GetPoints(ideo, newIdeo, out explanation);
            LogUtility.Log($"Available dev points: {IdeoUtility.PlayerIdeoDevelopment.points}.");
            if (IdeoUtility.PlayerIdeoDevelopment.points < points)
            {
                Messages.Message($"Can't reform ideoligion: {points} development points needed.", MessageTypeDefOf.RejectInput, false);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Replaces RimWorld.IdeoDevelopmentTracker.TryAddDevelopmentPoints so that dev points aren't capped by the amount needed for development
        /// </summary>
        public static bool IdeoDevelopmentTracker_TryAddDevelopmentPoints(IdeoDevelopmentTracker __instance, ref bool __result, int pointsToAdd)
        {
            LogUtility.Log($"IdeoDevelopmentTracker_TryAddDevelopmentPoints({__instance.ideo}, {pointsToAdd})");
            bool canReformNow = __instance.CanReformNow;
            __instance.points += pointsToAdd * Settings.DevPointsMultiplier;
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
            LogUtility.Log($"IdeoDevelopmentTracker_ResetDevelopmentPoints({__instance.ideo})");
            __instance.points -= points;
            points = 0;
            return false;
        }

        /// <summary>
        /// Replaces IdeoDevelopmentTracker.NextReformationDevelopmentPoints to change dev points requirements
        /// </summary>
        public static bool IdeoDevelopmentTracker_NextReformationDevelopmentPoints(IdeoDevelopmentTracker __instance, ref int __result)
        {
            __result = IdeoUtility.DevPointsReformCost;
            return false;
        }

        /// <summary>
        /// Displays current and needed dev points
        /// </summary>
        public static void Dialog_ReformIdeo_DoWindowContents(Dialog_ReformIdeo __instance, Rect inRect, Ideo ___ideo, Ideo ___newIdeo)
        {
            points = IdeoUtility.GetPoints(___ideo, ___newIdeo, out explanation, false);
            int availablePoints = IdeoUtility.PlayerIdeoDevelopment.Points;
            if (availablePoints < points)
                GUI.color = Color.red;
            else GUI.color = Color.white;
            float y = inRect.y;
            Widgets.Label(inRect.x + inRect.width - 100, ref y, 100, $"Points: {points} / {availablePoints}", explanation);
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
