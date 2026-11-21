using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace DefaultPlantSelector
{
    public class DefaultPlantMod : Mod
    {
        public static DefaultPlantSettings Settings;

        private Vector2 _scrollPosition = Vector2.zero;
        private string _searchFilter = string.Empty;
        private List<ThingDef> _cachedGrowerBuildings;

        public DefaultPlantMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<DefaultPlantSettings>();
            var harmony = new Harmony("BSCheshir.DefaultPlantSelector");
            harmony.PatchAll();
        }

        public override string SettingsCategory() => "DPS_SettingsCategory".Translate();

        public override void WriteSettings()
        {
            base.WriteSettings();
            SelectionState.Reset();
        }

        private List<ThingDef> GetAllGrowerBuildings()
        {
            if (_cachedGrowerBuildings == null)
            {
                _cachedGrowerBuildings = DefDatabase<ThingDef>.AllDefsListForReading
                    .Where(d => d.building != null
                                && !string.IsNullOrEmpty(d.building.sowTag)
                                && typeof(IPlantToGrowSettable).IsAssignableFrom(d.thingClass))
                    .OrderBy(d => d.label ?? d.defName)
                    .ToList();
            }
            return _cachedGrowerBuildings;
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var list = new Listing_Standard();
            list.Begin(inRect);

            var zonePlant = Settings.GetZonePlant();
            Rect zoneRow = list.GetRect(30f);
            DrawPlantSelectionRow(
                zoneRow,
                buildingDef: null,
                title: "DPS_SettingsZonePlant".Translate(),
                currentPlant: zonePlant,
                isCustom: Settings.defaultZonePlantDefName != "Plant_Rice",
                onReset: () =>
                {
                    Settings.defaultZonePlantDefName = "Plant_Rice";
                    Settings.Write();
                });

            list.GapLine();

            var allBuildings = GetAllGrowerBuildings();
            Rect headerRect = list.GetRect(26f);
            Widgets.Label(headerRect.LeftPart(0.55f), "DPS_SettingsBuildingsHeader".Translate(allBuildings.Count));
            _searchFilter = Widgets.TextField(headerRect.RightPart(0.42f), _searchFilter);

            list.Gap(6f);

            var filteredBuildings = string.IsNullOrWhiteSpace(_searchFilter)
                ? allBuildings
                : allBuildings.Where(b =>
                    (b.label != null && b.label.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    b.defName.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

            float scrollHeight = inRect.height - list.CurHeight - 42f;
            Rect outRect = list.GetRect(scrollHeight);
            Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, filteredBuildings.Count * 32f);

            Widgets.BeginScrollView(outRect, ref _scrollPosition, viewRect);
            float curY = 0f;

            foreach (var bDef in filteredBuildings)
            {
                var capturedDef = bDef;
                bool hasCustom = Settings.buildingDefaultPlants != null &&
                                 Settings.buildingDefaultPlants.ContainsKey(capturedDef.defName);
                ThingDef currentPlant = Settings.GetBuildingPlant(capturedDef);

                Rect rowRect = new Rect(0f, curY, viewRect.width, 28f);
                if (Mouse.IsOver(rowRect))
                {
                    Widgets.DrawHighlight(rowRect);
                }

                DrawPlantSelectionRow(
                    rowRect,
                    buildingDef: capturedDef,
                    title: capturedDef.LabelCap,
                    currentPlant: currentPlant,
                    isCustom: hasCustom,
                    onReset: () =>
                    {
                        Settings.buildingDefaultPlants?.Remove(capturedDef.defName);
                        Settings.Write();
                    });

                curY += 32f;
            }

            Widgets.EndScrollView();

            list.Gap(6f);
            if (list.ButtonText("DPS_ResetAllButton".Translate()))
            {
                Settings.defaultZonePlantDefName = "Plant_Rice";
                Settings.buildingDefaultPlants?.Clear();
                Settings.Write();
            }

            list.End();
        }

        private void DrawPlantSelectionRow(
            Rect rect,
            ThingDef buildingDef,
            string title,
            ThingDef currentPlant,
            bool isCustom,
            Action onReset)
        {
            float curX = rect.x;

            if (buildingDef != null)
            {
                Widgets.ThingIcon(new Rect(curX, rect.y + 2f, 24f, 24f), buildingDef);
                if (Mouse.IsOver(new Rect(curX, rect.y, 26f, rect.height)))
                {
                    TooltipHandler.TipRegion(
                        new Rect(curX, rect.y, 26f, rect.height),
                        $"{buildingDef.LabelCap}\n({buildingDef.defName})\n{buildingDef.modContentPack?.Name}");
                }
                curX += 30f;
            }

            float titleWidth = rect.width * 0.40f - (buildingDef != null ? 30f : 0f);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(curX, rect.y, titleWidth, rect.height), title);
            curX += titleWidth + 6f;

            if (currentPlant != null)
            {
                Widgets.ThingIcon(new Rect(curX, rect.y + 2f, 24f, 24f), currentPlant);
                curX += 28f;

                string plantText = isCustom
                    ? currentPlant.LabelCap.ToString()
                    : $"{currentPlant.LabelCap} {"DPS_VanillaDefaultTag".Translate()}";

                if (!isCustom) GUI.color = Color.gray;
                float plantLabelWidth = rect.width * 0.28f;
                Widgets.Label(new Rect(curX, rect.y, plantLabelWidth, rect.height), plantText);
                GUI.color = Color.white;
                curX += plantLabelWidth + 6f;
            }

            Text.Anchor = TextAnchor.UpperLeft;

            float resetWidth = 75f;
            float selectWidth = rect.xMax - curX - resetWidth - 6f;
            if (Widgets.ButtonText(new Rect(curX, rect.y + 1f, selectWidth, rect.height - 2f), "DPS_SelectButton".Translate()))
            {
                OpenPlantFloatMenu(buildingDef);
            }

            Rect resetRect = new Rect(rect.xMax - resetWidth, rect.y + 1f, resetWidth, rect.height - 2f);
            if (isCustom)
            {
                if (Widgets.ButtonText(resetRect, "DPS_ResetButton".Translate()))
                {
                    onReset();
                }
            }
            else
            {
                GUI.color = new Color(1f, 1f, 1f, 0.35f);
                Widgets.ButtonText(resetRect, "DPS_ResetButton".Translate(), active: false);
                GUI.color = Color.white;
            }
        }

        private void OpenPlantFloatMenu(ThingDef buildingDef)
        {
            string requiredTag = buildingDef != null ? buildingDef.building.sowTag : "Ground";

            var validPlants = DefDatabase<ThingDef>.AllDefsListForReading
                .Where(p => p.plant != null
                            && p.plant.Sowable
                            && p.plant.sowTags != null
                            && p.plant.sowTags.Contains(requiredTag))
                .OrderBy(p => p.label ?? p.defName)
                .ToList();

            var options = new List<FloatMenuOption>();
            foreach (var plant in validPlants)
            {
                var capturedPlant = plant;
                options.Add(new FloatMenuOption(
                    capturedPlant.LabelCap,
                    () =>
                    {
                        if (buildingDef != null)
                            Settings.SetBuildingPlant(buildingDef.defName, capturedPlant.defName);
                        else
                            Settings.defaultZonePlantDefName = capturedPlant.defName;
                        Settings.Write();
                    },
                    capturedPlant.uiIcon,
                    Color.white));
            }

            if (options.Count == 0)
            {
                options.Add(new FloatMenuOption("NoneBrackets".Translate(), null));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }
    }
}
