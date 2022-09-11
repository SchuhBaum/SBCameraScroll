namespace SBCameraScroll
{
    internal static class GhostWorldPresenceMod
    {
        // same as in AboveCloudsViewMod
        internal static void OnEnable()
        {
            On.GhostWorldPresence.GhostMode_Room_int += GhostWorldPresence_GhostMode;
        }

        // ----------------- //
        // private functions //
        // ----------------- //

        private static float GhostWorldPresence_GhostMode(On.GhostWorldPresence.orig_GhostMode_Room_int orig, GhostWorldPresence ghostWorldPresence, Room room, int camPos)
        {
            return orig(ghostWorldPresence, room, 0);
        }
    }
}