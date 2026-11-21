using System.Linq;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace DefaultPlantSelector
{
    public class DefaultPlantMod : Mod
    {
        public static DefaultPlantSettings Settings;
        private Vector2 _scrollPosition = Vector2.zero;

        public DefaultPlantMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<DefaultPlantSettings>();
            var harmony = new Harmony("BSCheshir.DefaultPlantSelector");
            harmony.PatchAll();
        }

        public override string SettingsCategory() => "DPS_SettingsCategory".Translate();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var list = new Listing_Standard();
            list.Begin(inRect);

            // Строка настроек для обычных зон посадок
            var zonePlant = Settings.GetZonePlant();
            Rect zoneRow = list.GetRect(28f);
            Widgets.Label(zoneRow.LeftPart(0.75f), "DPS_SettingsZonePlant".Translate(zonePlant.LabelCap));
            if (Widgets.ButtonText(zoneRow.RightPart(0.22f), "DPS_ResetButton".Translate()))
            {
                Settings.defaultZonePlantDefName = "Plant_Rice";
                Settings.Write();
            }

            list.GapLine();
            list.Label("DPS_SettingsBuildingsHeader".Translate());
            list.Gap(6f);

            var dict = Settings.buildingDefaultPlants;
            if (dict == null || dict.Count == 0)
            {
                GUI.color = Color.gray;
                list.Label("DPS_SettingsNoBuildings".Translate());
                GUI.color = Color.white;
            }
            else
            {
                // Прокручиваемый список настроенных построек
                float scrollHeight = inRect.height - list.CurHeight - 45f;
                Rect outRect = list.GetRect(scrollHeight);
                Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, dict.Count * 30f);

                Widgets.BeginScrollView(outRect, ref _scrollPosition, viewRect);
                float curY = 0f;

                // Копируем ключи в список, чтобы безопасно удалять элементы при нажатии "Сброс"
                foreach (var buildingDefName in dict.Keys.ToList())
                {
                    string plantDefName = dict[buildingDefName];
                    var buildingDef = DefDatabase<ThingDef>.GetNamedSilentFail(buildingDefName);
                    var plantDef = DefDatabase<ThingDef>.GetNamedSilentFail(plantDefName);

                    string buildingLabel = buildingDef != null ? buildingDef.LabelCap.ToString() : buildingDefName;
                    string plantLabel = plantDef != null ? plantDef.LabelCap.ToString() : plantDefName;

                    Rect rowRect = new Rect(0f, curY, viewRect.width, 26f);
                    Widgets.Label(rowRect.LeftPart(0.75f), $"{buildingLabel}: {plantLabel}");

                    if (Widgets.ButtonText(rowRect.RightPart(0.22f), "DPS_ResetButton".Translate()))
                    {
                        dict.Remove(buildingDefName);
                        Settings.Write();
                    }

                    curY += 30f;
                }

                Widgets.EndScrollView();
            }

            list.Gap(8f);
            if (list.ButtonText("DPS_ResetAllButton".Translate()))
            {
                Settings.defaultZonePlantDefName = "Plant_Rice";
                Settings.buildingDefaultPlants?.Clear();
                Settings.Write();
            }

            list.End();
        }
    }
}
