
namespace SBCameraScroll;

internal static class RainWorldGameMod {
    internal static void OnEnable() {
        On.RainWorldGame.ctor += RainWorldGame_Ctor;
        On.RainWorldGame.ShutDownProcess += RainWorldGame_ShutDownProcess;
    }

    //
    // private
    //

    private static void RainWorldGame_Ctor(On.RainWorldGame.orig_ctor orig, RainWorldGame game, ProcessManager manager) {
        AbstractRoomMod._all_abstract_room_fields.Clear();
        RoomCameraMod._all_room_camera_fields.Clear();
        WormGrassMod._all_worm_grass_fields.Clear();

        Debug.Log($"{mod_id}: Initialize variables.");
        orig(game, manager);
    }

    private static void RainWorldGame_ShutDownProcess(On.RainWorldGame.orig_ShutDownProcess orig, RainWorldGame game) {
        Debug.Log($"{mod_id}: Cleanup.");
        orig(game);

        // Profiler.Api.WriteLine = Debug.Log;
        // Profiler.Api.PrintAndReset();

        AbstractRoomMod._all_abstract_room_fields.Clear();
        RoomCameraMod._all_room_camera_fields.Clear();
        WormGrassMod._all_worm_grass_fields.Clear();
    }
}
