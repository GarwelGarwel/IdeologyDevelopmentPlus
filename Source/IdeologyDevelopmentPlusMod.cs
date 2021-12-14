using UnityEngine;
using Verse;

namespace IdeologyDevelopmentPlus
{
    public class IdeologyDevelopmentPlusMod : Mod
    {
        Vector2 scrollPosition = new Vector2();
        Rect viewRect = new Rect();

        public IdeologyDevelopmentPlusMod(ModContentPack content)
            : base(content) =>
            GetSettings<Settings>();

        public override void DoSettingsWindowContents(Rect rect)
        {
            if (viewRect.height <= 0)
            {
                viewRect.width = rect.width - 16;
                viewRect.height = 1000;
            }
            Widgets.BeginScrollView(rect, ref scrollPosition, viewRect);
            Listing_Standard content = new Listing_Standard();
            content.ColumnWidth = rect.width - 20;
            content.Begin(viewRect);

            content.Label(
                $"Dev points multiplier: {Settings.DevPointsMultiplier.ToStringCached()}",
                tooltip: $"Acquired development points are multiplied by this value (default: {Settings.DevPointsMultiplier_Default.ToStringCached()})");
            Settings.DevPointsMultiplier = (int)content.Slider(Settings.DevPointsMultiplier, 1, 10);

            content.Label(
                $"First reform base cost: {Settings.ReformCostStart.ToStringCached()}",
                tooltip: $"How many dev points needed for the first reform (default: {Settings.ReformCostStart_Default.ToStringCached()})");
            Settings.ReformCostStart = (int)content.Slider(Settings.ReformCostStart, 0, 20);

            content.Label(
                $"Base cost increase per reform: {Settings.ReformCostIncrement.ToStringCached()}",
                tooltip: $"Cost of reform is increased by this amount after every reform (default: {Settings.ReformCostIncrement_Default.ToStringCached()})");
            Settings.ReformCostIncrement = (int)content.Slider(Settings.ReformCostIncrement, 0, 10);

            content.Label(
                $"Max reform base cost: {Settings.ReformCostMax.ToStringCached()}",
                tooltip: $"Base cost of a reform stops increasing after reaching this value (default: {Settings.ReformCostMax_Default.ToStringCached()})");
            Settings.ReformCostMax = (int)content.Slider(Settings.ReformCostMax, Settings.ReformCostStart, Settings.ReformCostStart + 10 * Settings.ReformCostIncrement);

            content.Label(
                $"Meme cost per impact: {Settings.MemeCostPerImpact.ToStringCached()}",
                tooltip: $"How many dev points adding or removing a meme costs per impact level (default: {Settings.MemeCostPerImpact_Default.ToStringCached()})");
            Settings.MemeCostPerImpact = (int)content.Slider(Settings.MemeCostPerImpact, 0, 10);

            content.Label(
              $"Precept change cost: {Settings.IssueCost.ToStringCached()}",
              tooltip: $"How many dev points shifting a precept by one step costs (default: {Settings.IssueCost_Default.ToStringCached()})");
            Settings.IssueCost = (int)content.Slider(Settings.IssueCost, 0, 10);

            content.Label(
                $"Precept cost per impact: {Settings.PreceptCost.ToStringCached()}",
                tooltip: $"How many dev points adding or removing a precept costs, scaled with precept's gameplay impact (default: {Settings.PreceptCost_Default.ToStringCached()})");
            Settings.PreceptCost = (int)content.Slider(Settings.PreceptCost, 0, 10);

            content.CheckboxLabeled(
                "Surprise Precepts Mode",
                ref Settings.RandomizePrecepts,
                "In this mode, you only choose memes when reforming ideoligion, and the precepts are randomly generated and can't be directly changed (but they cost no dev points)");

            if (Current.ProgramState == ProgramState.Playing && IdeoUtility.PlayerIdeo != null && !IdeoUtility.PlayerIdeo.Fluid && content.ButtonText("Make ideo fluid"))
                IdeoUtility.MakeIdeoFluid();

            if (content.ButtonText("Reset"))
                Settings.Reset();

            viewRect.height = content.CurHeight;
            content.End();
            Widgets.EndScrollView();
        }

        public override string SettingsCategory() => IdeologyDevelopmentPlus.Name;
    }
}
