using System;
using System.Collections.Generic;
using System.Linq;
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

        static int DevPointsReformCostBase = 5;
        static int DevPointsReformCostPerReform = 2;
        static int DevPointsReformCostMax = 15;
        static int DevPointsPerMeme = 1;
        static int DevPointsPerImpact = 1;
        static int DevPointsPerPrecept = 0;
        static int DevPointsPerIssue = 1;

        public static Ideo PlayerIdeo => Faction.OfPlayer?.ideos?.PrimaryIdeo;

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
                if (patchInfo != null)
                    LogUtility.Log($"{methodToPatch} patched successfully.");
                else LogUtility.Log($"Error patching {methodToPatch}.", LogLevel.Error);
            }

            Patch("RimWorld.IdeoDevelopmentUtility:ConfirmChangesToIdeo", "IdeoDevelopmentUtility_ConfirmChangesToIdeo_Prefix");
            Patch("RimWorld.IdeoDevelopmentTracker:TryAddDevelopmentPoints", "IdeoDevelopmentTracker_TryAddDevelopmentPoints_Prefix");
            Patch("RimWorld.IdeoDevelopmentTracker:ResetDevelopmentPoints", "IdeoDevelopmentTracker_ResetDevelopmentPoints");
            Patch("RimWorld.Dialog_ReformIdeo:DoWindowContents", postfix: "Dialog_ReformIdeo_DoWindowContents_Postfix");
            harmony.Patch(
                AccessTools.PropertyGetter(typeof(IdeoDevelopmentTracker), "NextReformationDevelopmentPoints"),
                new HarmonyMethod(type.GetMethod("IdeoDevelopmentTracker_NextReformationDevelopmentPoints")));
            LogUtility.Log($"Inititalization complete for {Assembly.GetExecutingAssembly()}.");
        }

        static void MakePlayerIdeoFluid()
        {
            Ideo ideo = PlayerIdeo;
            if (ideo != null && !ideo.Fluid)
            {
                LogUtility.Log($"Making player's ideo {ideo} fluid.");
                ideo.Fluid = true;
            }
        }

        public static List<MemeDef> GetAddedMemes(Ideo ideo1, Ideo ideo2) => ideo2.memes.FindAll(meme => !ideo1.memes.Contains(meme));

        public static List<MemeDef> GetRemovedMemes(Ideo ideo1, Ideo ideo2) => GetAddedMemes(ideo2, ideo1);

        public static List<Precept> GetAddedPrecepts(Ideo ideo1, Ideo ideo2) => ideo2.PreceptsListForReading.FindAll(precept => !ideo1.HasPrecept(precept.def));

        public static List<Precept> GetRemovedPrecepts(Ideo ideo1, Ideo ideo2) => GetAddedPrecepts(ideo2, ideo1);

        public static List<IssueDef> GetChangedIssues(Ideo ideo1, Ideo ideo2) =>
            GetAddedPrecepts(ideo1, ideo2).Union(GetRemovedPrecepts(ideo1, ideo2)).Select(precept => precept.def.issue).Distinct().ToList();

        static int DevPointsReformCost => Math.Min(DevPointsReformCostBase + PlayerIdeo.development.reformCount * DevPointsReformCostPerReform, DevPointsReformCostMax);

        public static void RecalculatePoints(Ideo ideo, Ideo newIdeo, bool log = true)
        {
            points = DevPointsReformCost;
            log &= Prefs.DevMode;
            IEnumerable<MemeDef> changedMemes = GetAddedMemes(ideo, newIdeo).Union(GetRemovedMemes(ideo, newIdeo));
            if (log)
            {
                LogUtility.Log($"Added memes: {GetAddedMemes(ideo, newIdeo).Select(meme => $"{meme} (impact {meme.impact})").ToCommaList()}");
                LogUtility.Log($"Removed memes: {GetRemovedMemes(ideo, newIdeo).Select(meme => $"{meme} (impact {meme.impact})").ToCommaList()}");
            }
            int points2 = changedMemes.Count() * DevPointsPerMeme;
            if (log)
                LogUtility.Log($"Dev points for memes (basic): {points2}");
            points += points2;
            points2 = changedMemes.Sum(meme => meme.impact * DevPointsPerImpact);
            if (log)
                LogUtility.Log($"Dev points for memes' impact: {points2}");
            points += points2;
            IEnumerable<Precept> changedPrecepts = GetAddedPrecepts(ideo, newIdeo).Union(GetRemovedPrecepts(ideo, newIdeo));
            points2 = changedPrecepts.Count() * DevPointsPerPrecept;
            points += points2;
            if (log)
            {
                LogUtility.Log($"Added precepts: {GetAddedPrecepts(ideo, newIdeo).Select(precept => precept.def.ToString()).ToCommaList()}");
                LogUtility.Log($"Removed precepts: {GetRemovedPrecepts(ideo, newIdeo).Select(precept => precept.def.ToString()).ToCommaList()}");
                LogUtility.Log($"Dev points for precepts: {points2}");
            }
            IEnumerable<IssueDef> changedIssues = GetChangedIssues(ideo, newIdeo);
            points2 = changedIssues.Count() * DevPointsPerIssue;
            points += points2;
            if (log)
            {
                LogUtility.Log($"Affected issues: {changedIssues.Select(issue => issue.defName).ToCommaList()}");
                LogUtility.Log($"Dev points for issues: {points2}");
                LogUtility.Log($"Total dev points required for reform: {points}");
            }
        }

        #region HARMONY PATCHES

        /// <summary>
        /// Adds a check whether we have enough dev points for reform, when the user clicks Done in the reform dialog
        /// </summary>
        public static bool IdeoDevelopmentUtility_ConfirmChangesToIdeo_Prefix(Ideo ideo, Ideo newIdeo)
        {
            RecalculatePoints(ideo, newIdeo);
            LogUtility.Log($"Available dev points: {PlayerIdeo.development.points}.");
            if (points > PlayerIdeo.development.points)
            {
                Messages.Message($"Can't reform ideoligion: {points} development points needed.", MessageTypeDefOf.RejectInput, false);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Replaces RimWorld.IdeoDevelopmentTracker.TryAddDevelopmentPoints so that dev points aren't capped by the amount needed for development
        /// </summary>
        public static bool IdeoDevelopmentTracker_TryAddDevelopmentPoints_Prefix(IdeoDevelopmentTracker __instance, ref bool __result, int pointsToAdd)
        {
            LogUtility.Log($"IdeoDevelopmentTracker_TryAddDevelopmentPoints_Prefix({__instance.ideo}, {pointsToAdd})");
            bool canReformNow = __instance.CanReformNow;
            __instance.points += pointsToAdd;
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

        /// <summary>
        /// Replaces IdeoDevelopmentTracker.NextReformationDevelopmentPoints to change dev points requirements
        /// </summary>
        public static bool IdeoDevelopmentTracker_NextReformationDevelopmentPoints(IdeoDevelopmentTracker __instance, ref int __result)
        {
            __result = DevPointsReformCost;
            return false;
        }

        /// <summary>
        /// Displays current and needed dev points
        /// </summary>
        public static void Dialog_ReformIdeo_DoWindowContents_Postfix(Dialog_ReformIdeo __instance, Rect inRect, Ideo ___ideo, Ideo ___newIdeo)
        {
            RecalculatePoints(___ideo, ___newIdeo, false);
            int availablePoints = PlayerIdeo.development.Points;
            if (availablePoints < points)
                GUI.color = Color.red;
            else GUI.color = Color.white;
            Widgets.Label(new Rect(inRect.x + inRect.width - 100, inRect.y, 100, 40), $"Points: {points} / {availablePoints}");
        }

        #endregion HARMONY PATCHES

        #region DEVMODE

        public static void AddIdeoDevPoints(int n)
        {
            MakePlayerIdeoFluid();
            IdeoDevelopmentTracker dev = PlayerIdeo?.development;
            if (dev == null)
            {
                LogUtility.Log("Player faction's ideo dev tracker is null!", LogLevel.Error);
                return;
            }
            PlayerIdeo.development.points += n;
            LogUtility.Log($"{dev.ideo} dev points: {dev.Points} / {dev.NextReformationDevelopmentPoints}");
        }

        [DebugAction(name = "Add 1 Ideo Dev Point", requiresIdeology = true)]
        public static void AddIdeoDevPoint() => AddIdeoDevPoints(1);

        [DebugAction(name = "Add 10 Ideo Dev Points", requiresIdeology = true)]
        public static void Add10IdeoDevPoints() => AddIdeoDevPoints(10);

        #endregion DEVMODE
    }
}
