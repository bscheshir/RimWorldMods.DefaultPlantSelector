using HarmonyLib;
using RimWorld;
using Verse;

namespace DefaultPlantSelector
{
    // 1. Подстановка культуры при создании новой зоны посадок
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

    // 2. Подстановка культуры при постройке конкретной грядки / гидропоники
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

    // 3. Ловля любого открываемого окна сразу после нажатия нашей кнопки
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

    // 4. Сброс состояния строго при закрытии пойманного окна
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

    // 5. Перехват выбора растения для зоны посадок (пока открыто пойманное окно)
    [HarmonyPatch(typeof(Zone_Growing), nameof(Zone_Growing.SetPlantDefToGrow))]
    public static class Patch_ZoneGrowing_SetPlantDefToGrow
    {
        public static bool Prefix(ThingDef plantDef)
        {
            return PlantSelectionHandler.HandleSetPlant(plantDef);
        }
    }

    // 6. Перехват выбора растения для грядки / гидропоники (пока открыто пойманное окно)
    [HarmonyPatch(typeof(Building_PlantGrower), nameof(Building_PlantGrower.SetPlantDefToGrow))]
    public static class Patch_BuildingPlantGrower_SetPlantDefToGrow
    {
        public static bool Prefix(ThingDef plantDef)
        {
            return PlantSelectionHandler.HandleSetPlant(plantDef);
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

            // Если окно так и не открылось (другой вариант выбора используется), сбрасываем после выбора
            if (SelectionState.IsArmed && SelectionState.TrackedWindow == null)
            {
                SelectionState.Reset();
            }

            return false;
        }
    }
}
