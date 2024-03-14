using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace IdeologyDevelopmentPlus
{
    public static class IdeoUtility
    {
        public static Ideo PlayerIdeo => Find.FactionManager?.OfPlayer?.ideos?.PrimaryIdeo;

        public static int PlayerIdeoPoints => PlayerIdeo.development.Points;

        public static void MakeIdeoFluid() => PlayerIdeo.Fluid = true;

        public static int LikemindedFactionCount => Find.FactionManager.AllFactionsVisible.Count(faction => !faction.IsPlayer && faction.ideos.PrimaryIdeo == PlayerIdeo);

        public static int ReformCountCost => PlayerIdeo.development.reformCount * Settings.ReformCostIncrement;

        public static int BelieverCountCost => (int)(Settings.ReformCostPerBeliever * PlayerIdeo.ColonistBelieverCountCached);

        public static int LikemindedFactionsCost => Settings.ReformCostPerFaction * LikemindedFactionCount;

        public static int BaseReformCost =>
            Math.Min(Settings.ReformCostStart + ReformCountCost + BelieverCountCost + LikemindedFactionsCost, Settings.ReformCostMax);

        public static int GetDevPointsCost(this Def def) => def.HasModExtension<DevelopmentCosts>() ? def.GetModExtension<DevelopmentCosts>().cost : 0;

        public static int GetPreceptOrder(this PreceptDef def) => def.HasModExtension<DevelopmentCosts>() ? def.GetModExtension<DevelopmentCosts>().order : 0;

        public static bool SametAs(this Precept precept1, Precept precept2) =>
            precept1.Id == precept2.Id || (!precept1.def.issue.allowMultiplePrecepts && precept1.def == precept2.def);

        public static IEnumerable<MemeDef> GetAddedMemes(Ideo ideo1, Ideo ideo2) => ideo2.memes.Where(meme => !ideo1.memes.Contains(meme));

        public static IEnumerable<MemeDef> GetRemovedMemes(Ideo ideo1, Ideo ideo2) => GetAddedMemes(ideo2, ideo1);

        public static IEnumerable<Precept> GetAddedPrecepts(Ideo ideo1, Ideo ideo2) =>
            ideo2.PreceptsListForReading.Where(precept => ideo1.PreceptsListForReading.All(precept2 => !precept.SametAs(precept2)));

        public static IEnumerable<Precept> GetRemovedPrecepts(Ideo ideo1, Ideo ideo2) => GetAddedPrecepts(ideo2, ideo1);

        public static List<IssueDef> GetChangedIssues(Ideo ideo1, Ideo ideo2) =>
            GetAddedPrecepts(ideo1, ideo2).Union(GetRemovedPrecepts(ideo1, ideo2)).Select(precept => precept.def.issue).Distinct().ToList();

        public static PreceptDef GetPreceptForIssue(this Ideo ideo, IssueDef issue) =>
            ideo.PreceptsListForReading.Select(precept => precept.def).FirstOrDefault(def => def.issue == issue);

        public static int GetPreceptOrderDifference(Ideo ideo1, Ideo ideo2, IssueDef issue)
        {
            if (issue.allowMultiplePrecepts)
                return 0;
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

        static void LogPrecepts(Ideo ideo, Ideo newIdeo)
        {
            StringBuilder str = new StringBuilder("Old ideo's precepts:\n");
            int totalAdded = 0, totalRemoved = 0;
            foreach (Precept p in ideo.PreceptsListForReading)
            {
                bool changed = newIdeo.PreceptsListForReading.All(p2 => !p.SametAs(p2));
                str.AppendLine($"- {p.TipLabel} ({p.def.defName}, id {p.Id}){(changed ? " *removed*" : "")}");
                if (changed)
                    totalRemoved++;
            }
            str.AppendLine("New precepts:");
            foreach (Precept p in newIdeo.PreceptsListForReading)
            {
                bool changed = ideo.PreceptsListForReading.All(p2 => !p.SametAs(p2));
                str.AppendLine($"- {p.TipLabel} ({p.def.defName}, id {p.Id}){(changed ? " *new*" : "")}");
                if (changed)
                    totalAdded++;
            }
            str.Append($"{totalAdded} precepts added, {totalRemoved} removed.");
            LogUtility.Log(str.ToString());
        }

        public static int GetPoints(Ideo ideo, Ideo newIdeo, out string explanation, bool log = true)
        {
            log &= Prefs.DevMode;
            int points = BaseReformCost;
            int points2;
            StringBuilder exp = new StringBuilder();
            if (Settings.ReformCostStart > 0 && points > Settings.ReformCostStart)
                exp.AppendLine($"Base cost start: {Settings.ReformCostStart.ToStringCached()}");
            if ((points2 = ReformCountCost) > 0)
                exp.AppendLine($"Reform count cost: {points2.ToStringCached()} ({PlayerIdeo.development.reformCount.ToStringCached()} previous reforms)");
            if ((points2 = BelieverCountCost) > 0)
                exp.AppendLine($"Believer count cost: {points2.ToStringCached()} ({PlayerIdeo.ColonistBelieverCountCached.ToStringCached()} believers)");
            if ((points2 = LikemindedFactionsCost) > 0)
                exp.AppendLine($"Likeminded factions cost: {points2.ToStringCached()} ({LikemindedFactionCount.ToStringCached()} factions)");
            if (points == Settings.ReformCostMax && points > Settings.ReformCostStart)
                exp.AppendLine($"Base cost capped at {Settings.ReformCostMax.ToStringCached()}");
            exp.Append($"Base cost: {points.ToStringCached()}");
            if (points > Settings.ReformCostStart)
                exp.AppendLine();

            IEnumerable<MemeDef> changedMemes = GetAddedMemes(ideo, newIdeo).Union(GetRemovedMemes(ideo, newIdeo));
            if (log && changedMemes.Any())
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
                    exp.AppendInNewLine($"{meme.LabelCap}: {points2.ToStringCached()}");
                }
            }

            if (!Settings.RandomizePrecepts)
            {
                if (log)
                    LogPrecepts(ideo, newIdeo);

                IEnumerable<IssueDef> changedIssues = GetChangedIssues(ideo, newIdeo);
                foreach (IssueDef issue in changedIssues)
                {
                    points2 = Math.Max(GetPreceptOrderDifference(ideo, newIdeo, issue), 1) * issue.GetDevPointsCost() * Settings.IssueCost;
                    if (points2 != 0)
                    {
                        if (log)
                            LogUtility.Log($"Issue {issue}: {points2.ToStringCached()}");
                        points += points2;
                        exp.AppendInNewLine($"{issue.LabelCap} changed: {points2.ToStringCached()}");
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
                        exp.AppendInNewLine($"{precept.GetFullName()} added: {points2.ToStringCached()}");
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
                        exp.AppendInNewLine($"{precept.GetFullName()} removed: {points2.ToStringCached()}");
                    }
                }
            }

            if (log)
                LogUtility.Log($"Total dev points required for reform: {points.ToStringCached()}");
            explanation = exp.ToString();
            return points;
        }
    }
}
