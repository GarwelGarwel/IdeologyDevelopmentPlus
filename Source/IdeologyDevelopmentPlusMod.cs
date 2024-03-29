﻿using UnityEngine;
using Verse;

using static IdeologyDevelopmentPlus.Settings;

namespace IdeologyDevelopmentPlus
{
    public class IdeologyDevelopmentPlusMod : Mod
    {
        Vector2 scrollPosition = new Vector2();
        Rect viewRect = new Rect();

        public IdeologyDevelopmentPlusMod(ModContentPack content)
            : base(content) =>
            GetSettings<Settings>();

        public override string SettingsCategory() => IdeologyDevelopmentPlus.Name;

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
                $"Dev points multiplier: {DevPointsMultiplier.ToStringPercent()}",
                tooltip: $"Acquired development points are multiplied by this value (default: {DevPointsMultiplier_Default.ToStringPercent()}).");
            DevPointsMultiplier = GenMath.RoundTo(content.Slider(DevPointsMultiplier, 1, 10), 0.5f);

            content.Label(
                $"First reform base cost: {ReformCostStart.ToStringCached()}",
                tooltip: $"How many dev points needed for the first reform (default: {ReformCostStart_Default.ToStringCached()}).");
            ReformCostStart = (int)content.Slider(ReformCostStart, 0, 20);

            content.Label(
                $"Base cost increase per reform: {ReformCostIncrement.ToStringCached()}",
                tooltip: $"Cost of a reform is increased by this amount after every reform (default: {ReformCostIncrement_Default.ToStringCached()}).");
            ReformCostIncrement = (int)content.Slider(ReformCostIncrement, 0, 10);

            content.Label(
                $"Base cost increase per colonist believer: {ReformCostPerBeliever:F1}",
                tooltip: $"Cost of a reform is increased with the number of its current believers in the colony (default: {ReformCostPerBeliever_Default:F1}).");
            ReformCostPerBeliever = Mathf.Round(content.Slider(ReformCostPerBeliever, 0, 5) * 2) / 2;

            content.Label(
                $"Base cost increase per likeminded faction: {ReformCostPerFaction.ToStringCached()}",
                tooltip: $"Cost of a reform is increased with the number of foreign factions that follow this ideoligion (default: {ReformCostPerFaction_Default.ToStringCached()}).");
            ReformCostPerFaction = (int)content.Slider(ReformCostPerFaction, 0, 20);

            content.Label(
                $"Max reform base cost: {ReformCostMax.ToStringCached()}",
                tooltip: $"Base cost of a reform stops increasing after reaching this value (default: {ReformCostMax_Default.ToStringCached()}).");
            int reformCostTheoreticalMax = ReformCostStart + 10 * ReformCostIncrement + (int)(20 * ReformCostPerBeliever) + 5 * ReformCostPerFaction;
            ReformCostMax = (int)content.Slider(Mathf.Clamp(ReformCostMax, ReformCostStart, reformCostTheoreticalMax), ReformCostStart, reformCostTheoreticalMax);

            content.Label(
                $"Meme cost per impact: {MemeCostPerImpact.ToStringCached()}",
                tooltip: $"How many dev points adding or removing a meme costs per impact level (default: {MemeCostPerImpact_Default.ToStringCached()}).");
            MemeCostPerImpact = (int)content.Slider(MemeCostPerImpact, 0, 10);

            content.Label(
                $"Precept change cost: {IssueCost.ToStringCached()}",
                tooltip: $"How many dev points shifting a precept by one step costs (default: {IssueCost_Default.ToStringCached()}).");
            IssueCost = (int)content.Slider(IssueCost, 0, 10);

            content.Label(
                $"Precept cost per impact: {PreceptCost.ToStringCached()}",
                tooltip: $"How many dev points adding or removing a precept costs, scaled with precept's gameplay impact (default: {PreceptCost_Default.ToStringCached()}).");
            PreceptCost = (int)content.Slider(PreceptCost, 0, 10);

            content.CheckboxLabeled(
                "Surprise precepts mode",
                ref RandomizePrecepts,
                "In this mode, you only choose memes when reforming ideoligion. Precepts are randomly generated and can't be directly changed, but they cost no dev points.");

            content.CheckboxLabeled(
                "Offer to make ideoligion fluid",
                ref OfferToMakeIdeoFluid,
                "If the current player's ideoligion is not fluid, show a window offering to make it such. ID+ has no effect on non-fluid ideoligions.");

            content.CheckboxLabeled(
                "Debug mode",
                ref DebugMode,
                "Detailed logging, necessary for reporting issues.");

            if (Current.ProgramState == ProgramState.Playing && IdeoUtility.PlayerIdeo != null && !IdeoUtility.PlayerIdeo.Fluid && content.ButtonText("Make ideo fluid"))
                IdeoUtility.MakeIdeoFluid();

            if (content.ButtonText("Reset"))
                Reset();

            viewRect.height = content.CurHeight;
            content.End();
            Widgets.EndScrollView();
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
            LogUtility.Log($"Settings saved.");
            Print();
        }
    }
}
