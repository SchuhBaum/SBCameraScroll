namespace SBCameraScroll;

internal static class GoldFlakesMod {
    // same idea as in AboveCloudsViewMod
    internal static void OnEnable() {
        On.GoldFlakes.GoldFlake.PlaceRandomlyInRoom += GoldFlake_PlaceRandomlyInRoom;
    }

    // ----------------- //
    // private functions //
    // ----------------- //

    private static void GoldFlake_PlaceRandomlyInRoom(On.GoldFlakes.GoldFlake.orig_PlaceRandomlyInRoom orig, GoldFlakes.GoldFlake gold_flake) {
        if (gold_flake.savedCamPos == -1) {
            orig(gold_flake);
        }
    }
}
