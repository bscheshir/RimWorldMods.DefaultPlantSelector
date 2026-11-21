using System.Collections.Generic;
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
            yield return new Command_SetDefaultPlant(__instance, currentDefault, buildingDef: null);
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
            yield return new Command_SetDefaultPlant(__instance, currentDefault, buildingDef: __instance.def);
        }
    }

    internal class Command_SetDefaultPlant : Command_Action
    {
        private readonly IPlantToGrowSettable _settable;
        private readonly ThingDef _buildingDef;

        public Command_SetDefaultPlant(IPlantToGrowSettable settable, ThingDef currentDefault, ThingDef buildingDef)
        {
            _settable = settable;
            _buildingDef = buildingDef;

            defaultLabel = "DPS_SetDefaultLabel".Translate(currentDefault.LabelCap);
            defaultDesc = (buildingDef != null
                ? "DPS_SetDefaultBuildingDesc".Translate(buildingDef.LabelCap)
                : "DPS_SetDefaultZoneDesc".Translate());
            icon = currentDefault.uiIcon;
            iconAngle = currentDefault.uiIconAngle;
            iconOffset = currentDefault.uiIconOffset;
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            // Перехватываем ПКМ по нашей кнопке до того, как ваниль его проигнорирует
            Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
            if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && Mouse.IsOver(rect))
            {
                Event.current.Use();
                OpenPlantMenu(Event.current);
                return new GizmoResult(GizmoState.Interacted, Event.current);
            }

            return base.GizmoOnGUI(topLeft, maxWidth, parms);
        }

        public override void ProcessInput(Event ev)
        {
            // Обычный ЛКМ
            OpenPlantMenu(ev);
        }

        private void OpenPlantMenu(Event ev)
        {
            // Взводим ожидание окна выбора
            SelectionState.Arm(_buildingDef);

            // Создаем чистую ванильную команду — все моды (Mint Menus и др.) сами подхватят её вызов
            var cmd = new Command_SetPlantToGrow
            {
                settable = _settable
            };
            cmd.ProcessInput(ev);

            // Если ни одно окно не открылось (например, режим колеса без Window или пустой список),
            // сбрасываем ожидание в конце текущего кадра, чтобы флаг не висел вечно
            if (SelectionState.IsArmed)
            {
                LongEventHandler.ExecuteWhenFinished(() =>
                {
                    if (SelectionState.IsArmed)
                        SelectionState.Reset();
                });
            }
        }
    }
}
