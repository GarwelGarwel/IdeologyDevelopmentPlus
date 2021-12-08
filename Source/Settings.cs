using Verse;

namespace IdeologyDevelopmentPlus
{
    public class Settings : ModSettings
    {
        public static int DevPointsMultiplier = DevPointsMultiplier_Default;
        public static int ReformCostStart = ReformCostStart_Default;
        public static int ReformCostIncrement = ReformCostIncrement_Default;
        public static int ReformCostMax = ReformCostMax_Default;
        public static int MemeCostPerImpact = MemeCostPerImpact_Default;
        public static int IssueCost = IssueCost_Default;
        public static int PreceptCost = PreceptCost_Default;
        public static bool RandomizePrecepts;

        internal const int DevPointsMultiplier_Default = 2;
        internal const int ReformCostStart_Default = 10;
        internal const int ReformCostIncrement_Default = 2;
        internal const int ReformCostMax_Default = 20;
        internal const int MemeCostPerImpact_Default = 2;
        internal const int IssueCost_Default = 1;
        internal const int PreceptCost_Default = 2;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref DevPointsMultiplier, "DevPointsMultiplier", DevPointsMultiplier_Default);
            Scribe_Values.Look(ref ReformCostStart, "ReformCostStart", ReformCostStart_Default);
            Scribe_Values.Look(ref ReformCostIncrement, "ReformCostIncrement", ReformCostIncrement_Default);
            Scribe_Values.Look(ref ReformCostMax, "ReformCostMax", ReformCostMax_Default);
            Scribe_Values.Look(ref MemeCostPerImpact, "MemeCostPerImpact", MemeCostPerImpact_Default);
            Scribe_Values.Look(ref IssueCost, "IssueCost", IssueCost_Default);
            Scribe_Values.Look(ref PreceptCost, "PreceptCost", PreceptCost_Default);
            Scribe_Values.Look(ref RandomizePrecepts, "RandomizePrecepts");
        }

        public static void Reset()
        {
            DevPointsMultiplier = DevPointsMultiplier_Default;
            ReformCostStart = ReformCostStart_Default;
            ReformCostIncrement = ReformCostIncrement_Default;
            ReformCostMax = ReformCostMax_Default;
            MemeCostPerImpact = MemeCostPerImpact_Default;
            IssueCost = IssueCost_Default;
            PreceptCost = PreceptCost_Default;
        }
    }
}
