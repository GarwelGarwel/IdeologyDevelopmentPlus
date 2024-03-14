using Verse;

using static IdeologyDevelopmentPlus.LogUtility;

namespace IdeologyDevelopmentPlus
{
    public class Settings : ModSettings
    {
        public static float DevPointsMultiplier;
        public static int ReformCostStart;
        public static int ReformCostIncrement;
        public static float ReformCostPerBeliever;
        public static int ReformCostPerFaction;
        public static int ReformCostMax;
        public static int MemeCostPerImpact;
        public static int IssueCost;
        public static int PreceptCost;
        public static bool RandomizePrecepts;
        public static bool OfferToMakeIdeoFluid;
        public static bool DebugMode = Prefs.LogVerbose;

        internal const float DevPointsMultiplier_Default = 2;
        internal const int ReformCostStart_Default = 10;
        internal const int ReformCostIncrement_Default = 2;
        internal const float ReformCostPerBeliever_Default = 0;
        internal const int ReformCostPerFaction_Default = 0;
        internal const int ReformCostMax_Default = 20;
        internal const int MemeCostPerImpact_Default = 2;
        internal const int IssueCost_Default = 1;
        internal const int PreceptCost_Default = 2;

        public Settings() => Reset();

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref DevPointsMultiplier, "DevPointsMultiplier", DevPointsMultiplier_Default);
            Scribe_Values.Look(ref ReformCostStart, "ReformCostStart", ReformCostStart_Default);
            Scribe_Values.Look(ref ReformCostIncrement, "ReformCostIncrement", ReformCostIncrement_Default);
            Scribe_Values.Look(ref ReformCostPerBeliever, "ReformCostPerBeliever", ReformCostPerBeliever_Default);
            Scribe_Values.Look(ref ReformCostPerBeliever, "ReformCostPerFaction", ReformCostPerFaction_Default);
            Scribe_Values.Look(ref ReformCostMax, "ReformCostMax", ReformCostMax_Default);
            Scribe_Values.Look(ref MemeCostPerImpact, "MemeCostPerImpact", MemeCostPerImpact_Default);
            Scribe_Values.Look(ref IssueCost, "IssueCost", IssueCost_Default);
            Scribe_Values.Look(ref PreceptCost, "PreceptCost", PreceptCost_Default);
            Scribe_Values.Look(ref RandomizePrecepts, "RandomizePrecepts");
            Scribe_Values.Look(ref OfferToMakeIdeoFluid, "OfferToMakeIdeoFluid", true);
            Scribe_Values.Look(ref DebugMode, "DebugMode");
            if (Scribe.mode == LoadSaveMode.LoadingVars)
                Print();
        }

        public static void Reset()
        {
            DevPointsMultiplier = DevPointsMultiplier_Default;
            ReformCostStart = ReformCostStart_Default;
            ReformCostIncrement = ReformCostIncrement_Default;
            ReformCostPerBeliever = ReformCostPerBeliever_Default;
            ReformCostPerFaction = ReformCostPerFaction_Default;
            ReformCostMax = ReformCostMax_Default;
            MemeCostPerImpact = MemeCostPerImpact_Default;
            IssueCost = IssueCost_Default;
            PreceptCost = PreceptCost_Default;
            RandomizePrecepts = false;
            OfferToMakeIdeoFluid = true;
            Log($"Settings reset.");
            Print();
        }

        public static void Print()
        {
            if (!DebugMode)
                return;
            Log($"DevPointsMultiplier: {DevPointsMultiplier.ToStringPercent()}");
            Log($"ReformCostStart: {ReformCostStart.ToStringCached()}");
            Log($"ReformCostIncrement: {ReformCostIncrement.ToStringCached()}");
            Log($"ReformCostPerBeliever: {ReformCostPerBeliever:F1}");
            Log($"ReformCostPerFaction: {ReformCostPerFaction.ToStringCached()}");
            Log($"ReformCostMax: {ReformCostMax.ToStringCached()}");
            Log($"MemeCostPerImpact: {MemeCostPerImpact.ToStringCached()}");
            Log($"IssueCost: {IssueCost.ToStringCached()}");
            Log($"PreceptCost: {PreceptCost.ToStringCached()}");
            Log($"RandomizePrecepts: {RandomizePrecepts}");
            Log($"OfferToMakeIdeoFluid: {OfferToMakeIdeoFluid}");
        }
    }
}
