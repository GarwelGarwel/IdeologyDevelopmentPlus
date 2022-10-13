using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace IdeologyDevelopmentPlus
{
    public static class IdeoUtility
    {
        public static Ideo PlayerIdeo => Find.FactionManager?.OfPlayer?.ideos?.PrimaryIdeo;

        public static int PlayerIdeoPoints => PlayerIdeo.development.Points;

        public static void MakeIdeoFluid() => PlayerIdeo.Fluid = true;

        public static int BaseReformCost =>
            Math.Min(Settings.ReformCostStart + PlayerIdeo.development.reformCount * Settings.ReformCostIncrement, Settings.ReformCostMax);

        public static int GetDevPointsCost(this Def def) => def.HasModExtension<DevelopmentCosts>() ? def.GetModExtension<DevelopmentCosts>().cost : 0;

        public static int GetPreceptOrder(this PreceptDef def) => def.HasModExtension<DevelopmentCosts>() ? def.GetModExtension<DevelopmentCosts>().order : 0;

        public static IEnumerable<MemeDef> GetAddedMemes(Ideo ideo1, Ideo ideo2) => ideo2.memes.Where(meme => !ideo1.memes.Contains(meme));

        public static IEnumerable<MemeDef> GetRemovedMemes(Ideo ideo1, Ideo ideo2) => GetAddedMemes(ideo2, ideo1);

        public static IEnumerable<Precept> GetAddedPrecepts(Ideo ideo1, Ideo ideo2) => ideo2.PreceptsListForReading.Where(precept => !ideo1.HasPrecept(precept.def));

        public static IEnumerable<Precept> GetRemovedPrecepts(Ideo ideo1, Ideo ideo2) => GetAddedPrecepts(ideo2, ideo1);

        public static List<IssueDef> GetChangedIssues(Ideo ideo1, Ideo ideo2) =>
            GetAddedPrecepts(ideo1, ideo2).Union(GetRemovedPrecepts(ideo1, ideo2)).Select(precept => precept.def.issue).Distinct().ToList();

        public static PreceptDef GetPreceptForIssue(this Ideo ideo, IssueDef issue) =>
            ideo.PreceptsListForReading.Select(precept => precept.def).FirstOrDefault(def => def.issue == issue);

        public static int GetPreceptOrderDifference(Ideo ideo1, Ideo ideo2, IssueDef issue)
        {
            PreceptDef precept1 = ideo1.GetPreceptForIssue(issue);
            PreceptDef precept2 = ideo2.GetPreceptForIssue(issue);
            if (precept1 == null && precept2 == null)
                return 0;
            if (precept1 == null)
                return Math.Abs(precept2.GetPreceptOrder());
            if (precept2 == null)
                return Math.Abs(precept1.GetPreceptOrder());
            return Math.Abs(precept1.GetPreceptOrder() - precept2.GetPreceptOrder());
        }

        public static string GetFullName(this Precept precept) => $"{precept.LabelCap}: {precept.def.LabelCap}";

        public static int GetPoints(Ideo ideo, Ideo newIdeo, out string explanation, bool log = true)
        {
            log &= Prefs.DevMode;
            int points = BaseReformCost;
            int points2;
            explanation = $"Base: {points.ToStringCached()}";

            IEnumerable<MemeDef> changedMemes = GetAddedMemes(ideo, newIdeo).Union(GetRemovedMemes(ideo, newIdeo));
            if (log)
            {
                LogUtility.Log($"Added memes: {GetAddedMemes(ideo, newIdeo).Select(meme => $"{meme} (impact {meme.impact.ToStringCached()})").ToCommaList()}");
                LogUtility.Log($"Removed memes: {GetRemovedMemes(ideo, newIdeo).Select(meme => $"{meme} (impact {meme.impact.ToStringCached()})").ToCommaList()}");
            }
            foreach (MemeDef meme in changedMemes)
            {
                points2 = meme.GetDevPointsCost() * meme.impact * Settings.MemeCostPerImpact;
                if (points2 != 0)
                {
                    if (log)
                        LogUtility.Log($"Meme {meme} (impact {meme.impact.ToStringCached()}): {points2}");
                    points += points2;
                    explanation += $"\n{meme.LabelCap}: {points2.ToStringCached()}";
                }
            }

            if (Settings.RandomizePrecepts)
                return points;

            IEnumerable<IssueDef> changedIssues = GetChangedIssues(ideo, newIdeo);
            foreach (IssueDef issue in changedIssues)
            {
                points2 = Math.Max(GetPreceptOrderDifference(ideo, newIdeo, issue), 1) * issue.GetDevPointsCost() * Settings.IssueCost;
                if (points2 != 0)
                {
                    if (log)
                        LogUtility.Log($"Issue {issue}: {points2.ToStringCached()}");
                    points += points2;
                    explanation += $"\n{issue.LabelCap} changed: {points2.ToStringCached()}";
                }
            }

            foreach (Precept precept in GetAddedPrecepts(ideo, newIdeo))
            {
                points2 = precept.def.GetDevPointsCost() * Settings.PreceptCost;
                if (points2 != 0)
                {
                    if (log)
                        LogUtility.Log($"Precept {precept.def} added: {points2.ToStringCached()}");
                    points += points2;
                    explanation += $"\n{precept.GetFullName()} added: {points2.ToStringCached()}";
                }
            }

            foreach (Precept precept in GetRemovedPrecepts(ideo, newIdeo))
            {
                points2 = -precept.def.GetDevPointsCost() * Settings.PreceptCost;
                if (points2 != 0)
                {
                    if (log)
                        LogUtility.Log($"Precept {precept.def} removed: {points2.ToStringCached()}");
                    points += points2;
                    explanation += $"\n{precept.GetFullName()} removed: {points2.ToStringCached()}";
                }
            }

            if (log)
                LogUtility.Log($"Total dev points required for reform: {points.ToStringCached()}");
            return points;
        }
    }
}
