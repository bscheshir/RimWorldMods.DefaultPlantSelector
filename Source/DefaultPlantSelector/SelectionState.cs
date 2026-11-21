using Verse;

namespace DefaultPlantSelector
{
    public static class SelectionState
    {
        // Режим 1: кнопка нажата, ждем открытия любого окна выбора
        public static bool IsArmed;
        // Режим 2: окно поймано и сейчас открыто
        public static Window TrackedWindow;
        public static ThingDef TargetBuildingDef;

        public static bool IsActive => IsArmed || (TrackedWindow != null && Find.WindowStack.IsOpen(TrackedWindow));

        public static void Arm(ThingDef buildingDef)
        {
            Reset();
            IsArmed = true;
            TargetBuildingDef = buildingDef;
        }

        public static void BindWindow(Window window)
        {
            if (window == null)
            {
                Reset();
                return;
            }

            IsArmed = false;
            TrackedWindow = window;
        }

        public static void Reset()
        {
            IsArmed = false;
            TrackedWindow = null;
            TargetBuildingDef = null;
        }
    }
}
