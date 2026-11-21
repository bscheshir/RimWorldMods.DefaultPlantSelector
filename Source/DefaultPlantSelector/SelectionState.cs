using Verse;

namespace DefaultPlantSelector
{
    public static class SelectionState
    {
        public static bool IsSelectingDefault;
        public static ThingDef TargetBuildingDef;
        public static Window TrackedWindow;

        public static void Reset()
        {
            IsSelectingDefault = false;
            TargetBuildingDef = null;
            TrackedWindow = null;
        }
    }
}
