using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace IdeologyDevelopmentPlus
{
    public class IdeologyDevelopmentPlus : GameComponent
    {
        static Harmony harmony;

        static int points;
        static string explanation = "";

        public static int DevPointsMultiplier = 2;

        public static int DevPointsReformCostBase = 10;
        public static int DevPointsReformCostPerReform = 2;
        public static int DevPointsReformCostMax = 20;
        public static int DevPointsPerImpact = 2;
        public static int DevPointsPerPrecept = 2;
        public static int DevPointsPerIssue = 1;

        public IdeologyDevelopmentPlus(Game game)
        {
            Harmony.DEBUG = Prefs.DevMode;

            if (harmony != null)
                return;

            harmony = new Harmony("Garwel.IdeologyDevelopmentPlus");
            Type type = typeof(IdeologyDevelopmentPlus);

            void Patch(string methodToPatch, string prefix = null, string postfix = null)
            {
                MethodInfo patchInfo = null;
                try
                {
                    patchInfo = harmony.Patch(AccessTools.Method(methodToPatch),
                        prefix != null ? new HarmonyMethod(type.GetMethod(prefix)) : null,
                        postfix != null ? new HarmonyMethod(type.GetMethod(postfix)) : null);
                }
                catch (Exception ex)
                { LogUtility.Log($"Exception while patching {methodToPatch}: {ex}"); }
                if (patchInfo != null)
                    LogUtility.Log($"{methodToPatch} patched successfully.");
                else LogUtility.Log($"Error patching {methodToPatch}.", LogLevel.Error);
            }

            Patch("RimWorld.IdeoDevelopmentUtility:ConfirmChangesToIdeo", "IdeoDevelopmentUtility_ConfirmChangesToIdeo");
            Patch("RimWorld.IdeoDevelopmentTracker:TryAddDevelopmentPoints", "IdeoDevelopmentTracker_TryAddDevelopmentPoints");
            Patch("RimWorld.IdeoDevelopmentTracker:ResetDevelopmentPoints", "IdeoDevelopmentTracker_ResetDevelopmentPoints");
            Patch("RimWorld.Dialog_ReformIdeo:DoWindowContents", postfix: "Dialog_ReformIdeo_DoWindowContents");
            harmony.Patch(
                AccessTools.PropertyGetter(typeof(IdeoDevelopmentTracker), "NextReformationDevelopmentPoints"),
                new HarmonyMethod(type.GetMethod("IdeoDevelopmentTracker_NextReformationDevelopmentPoints")));
            LogUtility.Log($"Inititalization complete for {Assembly.GetExecutingAssembly()}.");
        }

        static void MakePlayerIdeoFluid()
        {
            Ideo ideo = IdeoUtility.PlayerIdeo;
            if (ideo != null && !ideo.Fluid)
            {
                LogUtility.Log($"Making player's ideo {ideo} fluid.");
                ideo.Fluid = true;
            }
        }

        #region HARMONY PATCHES

        /// <summary>
        /// Adds a check whether we have enough dev points for reform, when the user clicks Done in the reform dialog
        /// </summary>
        public static bool IdeoDevelopmentUtility_ConfirmChangesToIdeo(Ideo ideo, Ideo newIdeo)
        {
            points = IdeoUtility.GetPoints(ideo, newIdeo, out explanation);
            LogUtility.Log($"Available dev points: {IdeoUtility.PlayerIdeoDevelopment.points}.");
            if (points > IdeoUtility.PlayerIdeoDevelopment.points)
            {
                Messages.Message($"Can't reform ideoligion: {points} development points needed.", MessageTypeDefOf.RejectInput, false);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Replaces RimWorld.IdeoDevelopmentTracker.TryAddDevelopmentPoints so that dev points aren't capped by the amount needed for development
        /// </summary>
        public static bool IdeoDevelopmentTracker_TryAddDevelopmentPoints(IdeoDevelopmentTracker __instance, ref bool __result, int pointsToAdd)
        {
            LogUtility.Log($"IdeoDevelopmentTracker_TryAddDevelopmentPoints({__instance.ideo}, {pointsToAdd})");
            bool canReformNow = __instance.CanReformNow;
            __instance.points += pointsToAdd * DevPointsMultiplier;
            if (!canReformNow && __instance.CanReformNow)
                Find.LetterStack.ReceiveLetter("LetterLabelReformIdeo".Translate(), "LetterTextReformIdeo".Translate(__instance.ideo), LetterDefOf.PositiveEvent);
            __result = true;
            return false;
        }

        /// <summary>
        /// Replaces RimWorld.IdeoDevelopmentTracker.ResetDevelopmentPoints to only remove the spent number of dev points
        /// </summary>
        public static bool IdeoDevelopmentTracker_ResetDevelopmentPoints(IdeoDevelopmentTracker __instance)
        {
            LogUtility.Log($"IdeoDevelopmentTracker_ResetDevelopmentPoints({__instance.ideo})");
            __instance.points -= points;
            points = 0;
            return false;
        }

        /// <summary>
        /// Replaces IdeoDevelopmentTracker.NextReformationDevelopmentPoints to change dev points requirements
        /// </summary>
        public static bool IdeoDevelopmentTracker_NextReformationDevelopmentPoints(IdeoDevelopmentTracker __instance, ref int __result)
        {
            __result = IdeoUtility.DevPointsReformCost;
            return false;
        }

        /// <summary>
        /// Displays current and needed dev points
        /// </summary>
        public static void Dialog_ReformIdeo_DoWindowContents(Dialog_ReformIdeo __instance, Rect inRect, Ideo ___ideo, Ideo ___newIdeo)
        {
            points = IdeoUtility.GetPoints(___ideo, ___newIdeo, out explanation, false);
            int availablePoints = IdeoUtility.PlayerIdeoDevelopment.Points;
            if (availablePoints < points)
                GUI.color = Color.red;
            else GUI.color = Color.white;
            float y = inRect.y;
            Widgets.Label(inRect.x + inRect.width - 100, ref y, 100, $"Points: {points} / {availablePoints}", explanation);
            GUI.color = Color.white; 
            if (Widgets.ButtonText(new Rect(inRect.x + inRect.width - 100, y, 100, 40), "Reset"))
            {
                LogUtility.Log($"Resetting the ideo.");
                ___ideo.CopyTo(___newIdeo);
            }
        }

        #endregion HARMONY PATCHES

        #region DEVMODE

        public static void AddIdeoDevPoints(int n)
        {
            MakePlayerIdeoFluid();
            IdeoDevelopmentTracker dev = IdeoUtility.PlayerIdeoDevelopment;
            if (dev == null)
            {
                LogUtility.Log("Player faction's ideo dev tracker is null!", LogLevel.Error);
                return;
            }
            IdeoUtility.PlayerIdeoDevelopment.points += n;
            LogUtility.Log($"{dev.ideo} dev points: {dev.Points} / {dev.NextReformationDevelopmentPoints}");
        }

        [DebugAction(name = "Add 1 Ideo Dev Point", requiresIdeology = true)]
        public static void AddIdeoDevPoint() => AddIdeoDevPoints(1);

        [DebugAction(name = "Add 10 Ideo Dev Points", requiresIdeology = true)]
        public static void Add10IdeoDevPoints() => AddIdeoDevPoints(10);

        #endregion DEVMODE
    }
}
