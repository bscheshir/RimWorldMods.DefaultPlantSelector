using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace DefaultPlantSelector
{
    [HarmonyPatch(typeof(Zone_Growing), MethodType.Constructor, new[] { typeof(ZoneManager) })]
    public static class Patch_ZoneGrowing_Ctor
    {
        public static void Postfix(Zone_Growing __instance, ZoneManager zoneManager)
        {
            var plant = DefaultPlantMod.Settings?.GetZonePlant();
            if (plant?.plant == null)
                return;

            Map map = zoneManager?.map ?? Find.CurrentMap;
            if (PlantAvailabilityHelper.IsPlantAvailable(plant, map))
            {
                __instance.SetPlantDefToGrow(plant);
            }
            else
            {
                var fallback = DefDatabase<ThingDef>.GetNamedSilentFail("Plant_Rice");
                if (fallback != null && PlantAvailabilityHelper.IsPlantAvailable(fallback, map))
                {
                    __instance.SetPlantDefToGrow(fallback);
                }
            }
        }
    }

    [HarmonyPatch(typeof(Building_PlantGrower), nameof(Building_PlantGrower.PostMake))]
    public static class Patch_BuildingPlantGrower_PostMake
    {
        public static void Postfix(Building_PlantGrower __instance)
        {
            if (__instance.def?.building?.sowTag == null)
                return;

            var plant = DefaultPlantMod.Settings?.GetBuildingPlant(__instance.def);
            if (plant?.plant?.sowTags == null || !plant.plant.sowTags.Contains(__instance.def.building.sowTag))
                return;

            if (PlantAvailabilityHelper.IsPlantAvailable(plant, Find.CurrentMap))
            {
                __instance.SetPlantDefToGrow(plant);
            }
        }
    }

    [HarmonyPatch(typeof(WindowStack), nameof(WindowStack.Add))]
    public static class Patch_WindowStack_Add
    {
        public static void Postfix(Window window)
        {
            if (SelectionState.IsArmed)
            {
                SelectionState.BindWindow(window);
            }
        }
    }

    [HarmonyPatch(typeof(WindowStack), nameof(WindowStack.TryRemove), new[] { typeof(Window), typeof(bool) })]
    public static class Patch_WindowStack_TryRemove
    {
        public static void Postfix(Window window)
        {
            if (window != null && window == SelectionState.TrackedWindow)
            {
                SelectionState.Reset();
            }
        }
    }

    [HarmonyPatch(typeof(Zone_Growing), nameof(Zone_Growing.SetPlantDefToGrow))]
    public static class Patch_ZoneGrowing_SetPlantDefToGrow
    {
        public static bool Prefix(ThingDef plantDef)
        {
            return PlantSelectionHandler.HandleSetPlant(plantDef);
        }
    }

    [HarmonyPatch(typeof(Building_PlantGrower), nameof(Building_PlantGrower.SetPlantDefToGrow))]
    public static class Patch_BuildingPlantGrower_SetPlantDefToGrow
    {
        public static bool Prefix(ThingDef plantDef)
        {
            return PlantSelectionHandler.HandleSetPlant(plantDef);
        }
    }

    internal static class PlantAvailabilityHelper
    {
        private static readonly Func<ThingDef, Map, bool> IsPlantAvailableDelegate =
            AccessTools.MethodDelegate<Func<ThingDef, Map, bool>>(
                AccessTools.Method(typeof(Command_SetPlantToGrow), "IsPlantAvailable", new[] { typeof(ThingDef), typeof(Map) }));

        public static bool IsPlantAvailable(ThingDef plantDef, Map map)
        {
            if (plantDef?.plant == null || !plantDef.plant.Sowable || Current.Game == null)
                return false;

            if (IsPlantAvailableDelegate != null)
                return IsPlantAvailableDelegate(plantDef, map);

            if (plantDef.plant.sowResearchPrerequisites != null)
            {
                foreach (var research in plantDef.plant.sowResearchPrerequisites)
                {
                    if (!research.IsFinished)
                        return false;
                }
            }

            return true;
        }
    }

    internal static class PlantSelectionHandler
    {
        public static bool HandleSetPlant(ThingDef plantDef)
        {
            if (!SelectionState.IsActive)
                return true;

            if (plantDef != null)
            {
                if (SelectionState.TargetBuildingDef != null)
                {
                    DefaultPlantMod.Settings.SetBuildingPlant(SelectionState.TargetBuildingDef.defName, plantDef.defName);
                    Messages.Message(
                        "DPS_DefaultBuildingPlantChanged".Translate(SelectionState.TargetBuildingDef.LabelCap, plantDef.LabelCap),
                        MessageTypeDefOf.SilentInput,
                        false);
                }
                else
                {
                    DefaultPlantMod.Settings.defaultZonePlantDefName = plantDef.defName;
                    Messages.Message(
                        "DPS_DefaultPlantChanged".Translate(plantDef.LabelCap),
                        MessageTypeDefOf.SilentInput,
                        false);
                }

                DefaultPlantMod.Settings.Write();
            }

            if (SelectionState.IsArmed && SelectionState.TrackedWindow == null)
            {
                SelectionState.Reset();
            }

            return false;
        }
    }
}