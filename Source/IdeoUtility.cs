using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace IdeologyDevelopmentPlus
{
    public static class IdeoUtility
    {
        public static Ideo PlayerIdeo => Faction.OfPlayer?.ideos?.PrimaryIdeo;

        public static IdeoDevelopmentTracker PlayerIdeoDevelopment => PlayerIdeo?.development;

        public static int DevPointsReformCost =>
            Math.Min(
                IdeologyDevelopmentPlus.DevPointsReformCostBase + PlayerIdeo.development.reformCount * IdeologyDevelopmentPlus.DevPointsReformCostPerReform,
                IdeologyDevelopmentPlus.DevPointsReformCostMax);

        public static int GetDevPointsCost(this Def def) => def.HasModExtension<DevelopmentCosts>() ? def.GetModExtension<DevelopmentCosts>().cost : 0;

        public static int GetPreceptOrder(this PreceptDef def) => def.HasModExtension<DevelopmentCosts>() ? def.GetModExtension<DevelopmentCosts>().order : 0;

        public static List<MemeDef> GetAddedMemes(Ideo ideo1, Ideo ideo2) => ideo2.memes.FindAll(meme => !ideo1.memes.Contains(meme));

        public static List<MemeDef> GetRemovedMemes(Ideo ideo1, Ideo ideo2) => GetAddedMemes(ideo2, ideo1);

        public static List<Precept> GetAddedPrecepts(Ideo ideo1, Ideo ideo2) => ideo2.PreceptsListForReading.FindAll(precept => !ideo1.HasPrecept(precept.def));

        public static List<Precept> GetRemovedPrecepts(Ideo ideo1, Ideo ideo2) => GetAddedPrecepts(ideo2, ideo1);

        public static List<IssueDef> GetChangedIssues(Ideo ideo1, Ideo ideo2) =>
            GetAddedPrecepts(ideo1, ideo2).Union(GetRemovedPrecepts(ideo1, ideo2)).Select(precept => precept.def.issue).Distinct().ToList();

        public static IEnumerable<PreceptDef> GetPreceptsForIssue(this Ideo ideo, IssueDef issue) =>
            ideo.PreceptsListForReading.Select(precept => precept.def).Where(def => def.issue == issue);

        static int GetPreceptOrderDifference(Ideo ideo1, Ideo ideo2, IssueDef issue)
        {
            PreceptDef precept1 = ideo1.GetPreceptsForIssue(issue).FirstOrDefault();
            PreceptDef precept2 = ideo2.GetPreceptsForIssue(issue).FirstOrDefault();
            if (precept1 == null && precept2 == null)
                return 0;
            if (precept1 == null)
                return precept2.GetPreceptOrder();
            if (precept2 == null)
                return precept1.GetPreceptOrder();
            return Math.Abs(precept1.GetPreceptOrder() - precept2.GetPreceptOrder());
        }

        public static int GetPoints(Ideo ideo, Ideo newIdeo, bool log = true)
        {
            int Sqr(int x) => x * x;

            log &= Prefs.DevMode;
            int points = DevPointsReformCost;
            IEnumerable<MemeDef> changedMemes = GetAddedMemes(ideo, newIdeo).Union(GetRemovedMemes(ideo, newIdeo));
            if (log)
            {
                LogUtility.Log($"Added memes: {GetAddedMemes(ideo, newIdeo).Select(meme => $"{meme} (impact {meme.impact})").ToCommaList()}");
                LogUtility.Log($"Removed memes: {GetRemovedMemes(ideo, newIdeo).Select(meme => $"{meme} (impact {meme.impact})").ToCommaList()}");
            }
            int points2 = changedMemes.Sum(meme => GetDevPointsCost(meme) * meme.impact) * IdeologyDevelopmentPlus.DevPointsPerImpact;
            points += points2;
            if (log)
                LogUtility.Log($"Dev points for memes: {points2}");
            points2 = GetAddedPrecepts(ideo, newIdeo).Sum(precept => GetDevPointsCost(precept.def)) * IdeologyDevelopmentPlus.DevPointsPerPrecept;
            points += points2;
            if (log)
            {
                LogUtility.Log($"Added precepts: {GetAddedPrecepts(ideo, newIdeo).Select(precept => precept.def.ToString()).ToCommaList()}");
                LogUtility.Log($"Dev points for adding precepts: {points2}");
            }
            points2 = -GetRemovedPrecepts(ideo, newIdeo).Sum(precept => GetDevPointsCost(precept.def)) * IdeologyDevelopmentPlus.DevPointsPerPrecept;
            points += points2;
            if (log)
            {
                LogUtility.Log($"Removed precepts: {GetRemovedPrecepts(ideo, newIdeo).Select(precept => precept.def.ToString()).ToCommaList()}");
                LogUtility.Log($"Dev points for removing precepts: {points2}");
            }
            IEnumerable<IssueDef> changedIssues = GetChangedIssues(ideo, newIdeo);
            points2 = changedIssues.Sum(issue => Math.Max(Sqr(GetPreceptOrderDifference(ideo, newIdeo, issue)), 1) * issue.GetDevPointsCost()) * IdeologyDevelopmentPlus.DevPointsPerIssue;
            points += points2;
            if (log)
            {
                LogUtility.Log($"Affected issues: {changedIssues.Select(issue => issue.defName).ToCommaList()}");
                LogUtility.Log($"Dev points for issues: {points2}");
                LogUtility.Log($"Total dev points required for reform: {points}");
            }
            return points;
        }
    }
}
