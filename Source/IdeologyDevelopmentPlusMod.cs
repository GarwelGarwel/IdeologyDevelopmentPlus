using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace IdeologyDevelopmentPlus
{
    public class IdeologyDevelopmentPlusMod : Mod
    {
        Vector2 scrollPosition = new Vector2();
        Rect viewRect = new Rect();

        public IdeologyDevelopmentPlusMod(ModContentPack content)
            : base(content)
        {
            GetSettings<Settings>();
        }

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
                $"Reform cost base: {Settings.DevPointsReformCostBase.ToStringCached()}",
                tooltip: $"How many dev points needed for the first reform (default: {Settings.DevPointsReformCostBase_Default.ToStringCached()})");
            Settings.DevPointsReformCostBase = (int)content.Slider(Settings.DevPointsReformCostBase, 0, 20);

            content.Label(
                $"Cost increase per reform: {Settings.DevPointsReformCostPerReform.ToStringCached()}",
                tooltip: $"Cost of reform is increased by this amount after every reform (default: {Settings.DevPointsReformCostPerReform_Default.ToStringCached()})");
            Settings.DevPointsReformCostPerReform = (int)content.Slider(Settings.DevPointsReformCostPerReform, 0, 10);

            content.Label(
                $"Max reform cost: {Settings.DevPointsReformCostMax.ToStringCached()}",
                tooltip: $"Maximum cost of a reform (default: {Settings.DevPointsReformCostMax_Default.ToStringCached()})");
            Settings.DevPointsReformCostMax = (int)content.Slider(Settings.DevPointsReformCostMax, Settings.DevPointsReformCostBase, Settings.DevPointsReformCostBase + 10 * Settings.DevPointsReformCostPerReform);

            content.Label(
                $"Meme cost per impact: {Settings.DevPointsPerImpact.ToStringCached()}",
                tooltip: $"How many dev points adding or removing a meme costs per impact level (default: {Settings.DevPointsPerImpact_Default.ToStringCached()})");
            Settings.DevPointsPerImpact = (int)content.Slider(Settings.DevPointsPerImpact, 0, 10);

            content.Label(
              $"Precept change cost: {Settings.DevPointsPerIssue.ToStringCached()}",
              tooltip: $"How many dev points shifting a precept by one step costs (default: {Settings.DevPointsPerIssue_Default.ToStringCached()})");
            Settings.DevPointsPerIssue = (int)content.Slider(Settings.DevPointsPerIssue, 0, 10);

            content.Label(
                $"Precept cost per impact: {Settings.DevPointsPerPrecept.ToStringCached()}",
                tooltip: $"How many dev points adding or removing a precept costs, scaled with precept's gameplay impact (default: {Settings.DevPointsPerPrecept_Default.ToStringCached()})");
            Settings.DevPointsPerPrecept = (int)content.Slider(Settings.DevPointsPerPrecept, 0, 10);

            if (!IdeoUtility.PlayerIdeo.Fluid && content.ButtonText("Make ideo fluid"))
                IdeologyDevelopmentPlus.MakeIdeoFluid();

            viewRect.height = content.CurHeight;
            content.End();
            Widgets.EndScrollView();
        }

        public override string SettingsCategory() => "Ideology Development+";
    }
}
