using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace IdeologyDevelopmentPlus
{
    public class IdeologyDevelopmentPlus : GameComponent
    {
        static Harmony harmony;

        static int DevPointsReformCost = 5;
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

            void Patch(string methodToPatch, string prefixName) => harmony.Patch(AccessTools.Method(methodToPatch), new HarmonyMethod(type.GetMethod(prefixName)));

            Patch("RimWorld.IdeoDevelopmentUtility:ConfirmChangesToIdeo", "IdeoDevelopmentUtility_ConfirmChangesToIdeo_Prefix");
            Patch("RimWorld.IdeoDevelopmentTracker:TryAddDevelopmentPoints", "IdeoDevelopmentTracker_TryAddDevelopmentPoints_Prefix");
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

        #region HARMONY PATCHES

        public static bool IdeoDevelopmentUtility_ConfirmChangesToIdeo_Prefix(Ideo ideo, Ideo newIdeo)
        {
            int points = DevPointsReformCost;
            IEnumerable<MemeDef> changedMemes = GetAddedMemes(ideo, newIdeo).Union(GetRemovedMemes(ideo, newIdeo));
            LogUtility.Log($"Added memes: {GetAddedMemes(ideo, newIdeo).Select(meme => $"{meme} (impact {meme.impact})").ToCommaList()}");
            LogUtility.Log($"Removed memes: {GetRemovedMemes(ideo, newIdeo).Select(meme => $"{meme} (impact {meme.impact})").ToCommaList()}");
            int points2 = changedMemes.Count() * DevPointsPerMeme;
            LogUtility.Log($"Dev points for memes (basic): {points}");
            points += points2;
            points2 = changedMemes.Sum(meme => meme.impact * DevPointsPerImpact);
            LogUtility.Log($"Dev points for memes' impact: {points2}");
            points += points2;
            IEnumerable<Precept> changedPrecepts = GetAddedPrecepts(ideo, newIdeo).Union(GetRemovedPrecepts(ideo, newIdeo));
            LogUtility.Log($"Added precepts: {GetAddedPrecepts(ideo, newIdeo).Select(precept => precept.def.ToString()).ToCommaList()}");
            LogUtility.Log($"Removed precepts: {GetRemovedPrecepts(ideo, newIdeo).Select(precept => precept.def.ToString()).ToCommaList()}");
            points2 = changedPrecepts.Count() * DevPointsPerPrecept;
            LogUtility.Log($"Dev points for precepts: {points2}");
            points += points2;
            IEnumerable<IssueDef> changedIssues = GetChangedIssues(ideo, newIdeo);
            LogUtility.Log($"Affected issues: {changedIssues.Select(issue => issue.defName).ToCommaList()}");
            points2 = changedIssues.Count() * DevPointsPerIssue;
            LogUtility.Log($"Dev points for issues: {points2}");
            points += points2;
            LogUtility.Log($"Total dev points required for reform: {points}.");
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
            //dev.points += n;
            PlayerIdeo.development.TryAddDevelopmentPoints(n);
            LogUtility.Log($"{dev.ideo} dev points: {dev.Points} / {dev.NextReformationDevelopmentPoints}");
        }

        [DebugAction(name = "Add 1 Ideo Dev Point", requiresIdeology = true)]
        public static void AddIdeoDevPoint() => AddIdeoDevPoints(1);

        [DebugAction(name = "Add 10 Ideo Dev Points", requiresIdeology = true)]
        public static void Add10IdeoDevPoints() => AddIdeoDevPoints(10);

        #endregion DEVMODE
    }
}
