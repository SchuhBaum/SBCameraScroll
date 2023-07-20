namespace SBCameraScroll;

internal static class GhostWorldPresenceMod {
    // same as in AboveCloudsViewMod
    internal static void OnEnable() {
        On.GhostWorldPresence.GhostMode_Room_int += GhostWorldPresence_GhostMode;
    }

    // ----------------- //
    // private functions //
    // ----------------- //

    private static float GhostWorldPresence_GhostMode(On.GhostWorldPresence.orig_GhostMode_Room_int orig, GhostWorldPresence ghost_world_presence, Room room, int cam_pos) {
        return orig(ghost_world_presence, room, 0);
    }
}
