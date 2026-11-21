using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace DefaultPlantSelector
{
    [HarmonyPatch(typeof(Zone_Growing), nameof(Zone_Growing.GetGizmos))]
    public static class Patch_ZoneGrowing_GetGizmos
    {
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> gizmos, Zone_Growing __instance)
        {
            foreach (var g in gizmos)
                yield return g;

            var currentDefault = DefaultPlantMod.Settings.GetZonePlant();
            yield return GizmoHelper.CreateDefaultPlantCommand(__instance, currentDefault, buildingDef: null);
        }
    }

    [HarmonyPatch(typeof(Building_PlantGrower), nameof(Building_PlantGrower.GetGizmos))]
    public static class Patch_BuildingPlantGrower_GetGizmos
    {
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> gizmos, Building_PlantGrower __instance)
        {
            foreach (var g in gizmos)
                yield return g;

            if (__instance.def?.building?.sowTag == null)
                yield break;

            var currentDefault = DefaultPlantMod.Settings.GetBuildingPlant(__instance.def);
            yield return GizmoHelper.CreateDefaultPlantCommand(__instance, currentDefault, buildingDef: __instance.def);
        }
    }

    internal static class GizmoHelper
    {
        public static Command_Action CreateDefaultPlantCommand(IPlantToGrowSettable settable, ThingDef currentDefault, ThingDef buildingDef)
        {
            string desc = buildingDef != null
                ? "DPS_SetDefaultBuildingDesc".Translate(buildingDef.LabelCap)
                : "DPS_SetDefaultZoneDesc".Translate();

            return new Command_Action
            {
                defaultLabel = "DPS_SetDefaultLabel".Translate(currentDefault.LabelCap),
                defaultDesc = desc,
                icon = currentDefault.uiIcon,
                iconAngle = currentDefault.uiIconAngle,
                iconOffset = currentDefault.uiIconOffset,
                action = () =>
                {
                    SelectionState.IsSelectingDefault = true;
                    SelectionState.TargetBuildingDef = buildingDef;

                    var cmd = new Command_SetPlantToGrow
                    {
                        settable = settable
                    };

                    int windowsBefore = Find.WindowStack.Count;
                    cmd.ProcessInput(Event.current);

                    if (Find.WindowStack.Count > windowsBefore)
                    {
                        SelectionState.TrackedWindow = Find.WindowStack.Windows.LastOrDefault();
                    }
                }
            };
        }
    }
}
