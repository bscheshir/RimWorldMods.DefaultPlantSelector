using HarmonyLib;
using RimWorld;
using Verse;

namespace DefaultPlantSelector
{
    // Подстановка культуры при создании новой зоны посадок
    [HarmonyPatch(typeof(Zone_Growing), MethodType.Constructor, new[] { typeof(ZoneManager) })]
    public static class Patch_ZoneGrowing_Ctor
    {
        public static void Postfix(Zone_Growing __instance)
        {
            var plant = DefaultPlantMod.Settings?.GetZonePlant();
            if (plant?.plant != null)
            {
                __instance.SetPlantDefToGrow(plant);
            }
        }
    }

    // Подстановка культуры при постройке конкретной грядки / гидропоники
    [HarmonyPatch(typeof(Building_PlantGrower), nameof(Building_PlantGrower.PostMake))]
    public static class Patch_BuildingPlantGrower_PostMake
    {
        public static void Postfix(Building_PlantGrower __instance)
        {
            if (__instance.def?.building?.sowTag == null)
                return;

            var plant = DefaultPlantMod.Settings?.GetBuildingPlant(__instance.def);
            if (plant?.plant?.sowTags != null && plant.plant.sowTags.Contains(__instance.def.building.sowTag))
            {
                __instance.SetPlantDefToGrow(plant);
            }
        }
    }

    // Перехват выбора растения в меню
    [HarmonyPatch(typeof(Command_SetPlantToGrow), "SetPlant")]
    public static class Patch_CommandSetPlantToGrow_SetPlant
    {
        public static bool Prefix(ThingDef plantDef)
        {
            if (!SelectionState.IsSelectingDefault)
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

            SelectionState.Reset();
            return false;
        }
    }

    // Сброс флага при закрытии меню без выбора
    [HarmonyPatch(typeof(WindowStack), nameof(WindowStack.TryRemove), new[] { typeof(Window), typeof(bool) })]
    public static class Patch_WindowStack_TryRemove
    {
        public static void Postfix(Window window)
        {
            if (SelectionState.IsSelectingDefault && window != null && window == SelectionState.TrackedWindow)
            {
                SelectionState.Reset();
            }
        }
    }
}
