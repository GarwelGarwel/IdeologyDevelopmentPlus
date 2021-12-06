using Verse;

namespace IdeologyDevelopmentPlus
{
    public class Settings : ModSettings
    {
        public static int DevPointsMultiplier = DevPointsMultiplier_Default;
        public static int DevPointsReformCostBase = DevPointsReformCostBase_Default;
        public static int DevPointsReformCostPerReform = DevPointsReformCostPerReform_Default;
        public static int DevPointsReformCostMax = DevPointsReformCostMax_Default;
        public static int DevPointsPerImpact = DevPointsPerImpact_Default;
        public static int DevPointsPerIssue = DevPointsPerIssue_Default;
        public static int DevPointsPerPrecept = DevPointsPerPrecept_Default;

        internal const int DevPointsMultiplier_Default = 2;
        internal const int DevPointsReformCostBase_Default = 10;
        internal const int DevPointsReformCostPerReform_Default = 2;
        internal const int DevPointsReformCostMax_Default = 20;
        internal const int DevPointsPerImpact_Default = 2;
        internal const int DevPointsPerIssue_Default = 1;
        internal const int DevPointsPerPrecept_Default = 2;

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref DevPointsMultiplier, "DevPointsMultiplier", DevPointsMultiplier_Default);
            Scribe_Values.Look(ref DevPointsReformCostBase, "DevPointsReformCostBase", DevPointsReformCostBase_Default);
            Scribe_Values.Look(ref DevPointsReformCostPerReform, "DevPointsReformCostPerReform", DevPointsReformCostPerReform_Default);
            Scribe_Values.Look(ref DevPointsReformCostMax, "DevPointsReformCostMax", DevPointsReformCostMax_Default);
            Scribe_Values.Look(ref DevPointsPerImpact, "DevPointsPerImpact", DevPointsPerImpact_Default);
            Scribe_Values.Look(ref DevPointsPerIssue, "DevPointsPerIssue", DevPointsPerIssue_Default);
            Scribe_Values.Look(ref DevPointsPerPrecept, "DevPointsPerPrecept", DevPointsPerPrecept_Default);
        }
    }
}
