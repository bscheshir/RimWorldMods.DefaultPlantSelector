using System.Collections.Generic;
using RimWorld;
using Verse;

namespace DefaultPlantSelector
{
    public class DefaultPlantSettings : ModSettings
    {
        public string defaultZonePlantDefName = "Plant_Rice";
        public Dictionary<string, string> buildingDefaultPlants = new Dictionary<string, string>();

        private List<string> _buildingKeysBuffer;
        private List<string> _plantValuesBuffer;

        public ThingDef GetZonePlant()
        {
            return DefDatabase<ThingDef>.GetNamedSilentFail(defaultZonePlantDefName)
                   ?? DefDatabase<ThingDef>.GetNamedSilentFail("Plant_Rice")
                   ?? ThingDefOf.Plant_Potato;
        }

        public ThingDef GetBuildingPlant(ThingDef buildingDef)
        {
            if (buildingDef != null && buildingDefaultPlants != null &&
                buildingDefaultPlants.TryGetValue(buildingDef.defName, out var plantDefName))
            {
                var savedPlant = DefDatabase<ThingDef>.GetNamedSilentFail(plantDefName);
                if (savedPlant != null)
                    return savedPlant;
            }

            // Если для постройки еще не выбрана культура — возвращаем её ванильное дефолтное растение (например, рис/картофель/розу)
            return buildingDef?.building?.defaultPlantToGrow
                   ?? DefDatabase<ThingDef>.GetNamedSilentFail("Plant_Rice")
                   ?? ThingDefOf.Plant_Potato;
        }

        public void SetBuildingPlant(string buildingDefName, string plantDefName)
        {
            if (string.IsNullOrEmpty(buildingDefName) || string.IsNullOrEmpty(plantDefName))
                return;

            buildingDefaultPlants ??= new Dictionary<string, string>();
            buildingDefaultPlants[buildingDefName] = plantDefName;
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref defaultZonePlantDefName, "defaultZonePlantDefName", "Plant_Rice");
            Scribe_Collections.Look(ref buildingDefaultPlants, "buildingDefaultPlants", LookMode.Value, LookMode.Value, ref _buildingKeysBuffer, ref _plantValuesBuffer);

            if (Scribe.mode == LoadSaveMode.PostLoadInit && buildingDefaultPlants == null)
            {
                buildingDefaultPlants = new Dictionary<string, string>();
            }

            base.ExposeData();
        }
    }
}
