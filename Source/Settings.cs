using Verse;

using static IdeologyDevelopmentPlus.LogUtility;

namespace IdeologyDevelopmentPlus
{
    public class Settings : ModSettings
    {
        public static float DevPointsMultiplier = DevPointsMultiplier_Default;
        public static int ReformCostStart = ReformCostStart_Default;
        public static int ReformCostIncrement = ReformCostIncrement_Default;
        public static int ReformCostMax = ReformCostMax_Default;
        public static int MemeCostPerImpact = MemeCostPerImpact_Default;
        public static int IssueCost = IssueCost_Default;
        public static int PreceptCost = PreceptCost_Default;
        public static bool RandomizePrecepts;
        public static bool DebugMode = Prefs.LogVerbose;

        internal const float DevPointsMultiplier_Default = 2;
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
            Scribe_Values.Look(ref DebugMode, "DebugMode");
            if (Scribe.mode == LoadSaveMode.LoadingVars)
                Print();
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
            RandomizePrecepts = false;
            Print();
        }

        public static void Print()
        {
            if (!DebugMode)
                return;
            Log($"DevPointsMultiplier: {DevPointsMultiplier}");
            Log($"ReformCostStart: {ReformCostStart}");
            Log($"ReformCostIncrement: {ReformCostIncrement}");
            Log($"ReformCostMax: {ReformCostMax}");
            Log($"MemeCostPerImpact: {MemeCostPerImpact}");
            Log($"IssueCost: {IssueCost}");
            Log($"PreceptCost: {PreceptCost}");
            Log($"RandomizePrecepts: {RandomizePrecepts}");
        }
    }
}
