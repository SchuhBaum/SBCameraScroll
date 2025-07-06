
namespace SBCameraScroll;

internal static class RippleCameraDataMod {
    internal static void OnEnable() {
        IL.Watcher.RippleCameraData.SetTarget += IL_RippleCameraData_SetTarget;
    }

    //
    // public
    //

    public static void RippleCameraDataMod_LoadRippleTargetScreen(Watcher.RippleCameraData ripple_data) {
        // Shouldn't get triggered since vanilla checks this too.
        if (!ripple_data.hasScreen) {
            return;
        }

        if (Custom.rainWorld?.processManager?.currentMainLoop is not RainWorldGame game) {
            Debug.Log("SBCameraScroll.RippleCameraDataMod_LoadRippleTargetScreen: [WARNING] Did not find the game process. Aborting the function RippleCameraDataMod_LoadRippleTargetScreen().");
            ripple_data.rippleTargetScreen.LoadImage(ripple_data.preLoadTexture, markNonReadable: false);
            return;
        }

        RoomCamera? room_camera = null;
        foreach (RoomCamera rc in game.cameras) {
            if (ripple_data == rc.rippleData) {
                room_camera = rc;
                break;
            }
        }

        if (room_camera == null) {
            Debug.Log("SBCameraScroll.RippleCameraDataMod_LoadRippleTargetScreen: [WARNING] Did not find the room camera. Aborting the function RippleCameraDataMod_LoadRippleTargetScreen().");
            ripple_data.rippleTargetScreen.LoadImage(ripple_data.preLoadTexture, markNonReadable: false);
            return;
        }

        string? room_name = room_camera.RippleSettings?.destRoom;
        if (room_name == null) {
            room_name = room_camera.loadingRoom?.abstractRoom.FileName;
        }
        if (room_name == null) {
            room_name = room_camera.room?.abstractRoom.FileName;
        }
        if (room_name == null) {
            Debug.Log("SBCameraScroll.RippleCameraDataMod_LoadRippleTargetScreen: [WARNING] Did not find any room name. Aborting the function RippleCameraDataMod_LoadRippleTargetScreen().");
            ripple_data.rippleTargetScreen.LoadImage(ripple_data.preLoadTexture, markNonReadable: false);
            return;
        }

        if (room_camera.IsRoomBlacklisted(room_name)) {
            // vanilla case
            ripple_data.rippleTargetScreen.LoadImage(ripple_data.preLoadTexture, markNonReadable: false);
            return;
        }

        Util_ClearTexture(ripple_data.rippleTargetScreen, new Color(1f/255f, 0f, 0f));
    }

    //
    // private
    //

    private static void IL_RippleCameraData_SetTarget(ILContext context) {
        // LogAllInstructions(context);
        ILCursor cursor = new ILCursor(context);

        if (cursor.TryGotoNext(instruction => instruction.MatchLdfld("Watcher.RippleCameraData", "rippleTargetScreen"))) {
            if (can_log_il_hooks) {
                Debug.Log($"{mod_id}: IL_RippleCameraData_SetTarget: Index {cursor.Index}");
            }

            cursor.RemoveRange(6);
            cursor.EmitDelegate(RippleCameraDataMod_LoadRippleTargetScreen);

        } else {
            if (can_log_il_hooks) {
                Debug.Log($"{mod_id}: IL_RippleCameraData_SetTarget failed.");
            }
            return;
        }

        // LogAllInstructions(context);
    }
}
