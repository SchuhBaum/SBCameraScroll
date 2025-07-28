
namespace SBCameraScroll;

public static class RippleCameraDataMod {
    internal static void OnEnable() {
        IL.Watcher.RippleCameraData.AddCommandBuffer += IL_RippleCameraData_AddCommandBuffer;
        IL.Watcher.RippleCameraData.SetGlobals       += IL_RippleCameraData_SetGlobals;
        IL.Watcher.RippleCameraData.SetTarget        += IL_RippleCameraData_SetTarget;
    }

    //
    // public
    //

    public static UnityEngine.Texture RippleCameraDataMod_GetRippleTargetScreen(Watcher.RippleCameraData ripple_data) {
        if (Custom.rainWorld?.processManager?.currentMainLoop is not RainWorldGame game) {
            Debug.Log("SBCameraScroll.RippleCameraDataMod_GetRippleTargetScreen: [WARNING] Expected to be in-game. But I did not find the instance for RainWorldGame. I assume now that this is the camera for player 1 and hope for the best.");
            return ripple_data.rippleTargetScreen;
        }

        RoomCamera? room_camera = null;
        foreach (RoomCamera rc in game.cameras) {
            if (ripple_data == rc.rippleData) {
                room_camera = rc;
                break;
            }
        }

        if (room_camera == null) {
            Debug.Log($"SBCameraScroll.RippleCameraDataMod_GetRippleTargetScreen: [WARNING] Expected to be in-game. But I did not find the room camera for the rippleData {ripple_data}. I assume now this is the camera for player 1 and hope for the best.");
            return ripple_data.rippleTargetScreen;
        }

        if (RippleCameraDataMod_IsRippleRoomBlacklisted(room_camera)) {
            return ripple_data.rippleTargetScreen;
        }
        return room_camera.Render_Texture();
    }

    public static bool RippleCameraDataMod_IsRippleRoomBlacklisted(RoomCamera room_camera) {
        string? room_name = room_camera.RippleSettings?.destRoom;
        if (room_name == null) {
            room_name = room_camera.loadingRoom?.abstractRoom.FileName;
        }
        if (room_name == null) {
            room_name = room_camera.room?.abstractRoom.FileName;
        }
        if (room_name == null) {
            Debug.Log("SBCameraScroll.RippleCameraDataMod_IsRippleRoomBlacklisted: [WARNING] Did not find any room name. Assuming the room is blacklisted.");
            return true;
        }
        return room_camera.IsRoomBlacklisted(room_name);
    }

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

        if (RippleCameraDataMod_IsRippleRoomBlacklisted(room_camera)) {
            // vanilla case
            ripple_data.rippleTargetScreen.LoadImage(ripple_data.preLoadTexture, markNonReadable: false);
            return;
        }

        // Not needed anymore, we noew use the render texture / modded level
        // texture from the room camera instead.
        // Util_ClearTexture(ripple_data.rippleTargetScreen, new Color(1f/255f, 0f, 0f));
    }

    //
    // private
    //

    private static void IL_RippleCameraData_AddCommandBuffer(ILContext context) {
        // LogAllInstructions(context);
        ILCursor cursor = new ILCursor(context);
        if (!IL_RippleCameraDataMod_PatchRippleTargetScreen(cursor, function_name: "AddCommandBuffer")) {
            return;
        }
        // LogAllInstructions(context);
    }

    private static void IL_RippleCameraData_SetGlobals(ILContext context) {
        // LogAllInstructions(context);
        ILCursor cursor = new ILCursor(context);
        if (!IL_RippleCameraDataMod_PatchRippleTargetScreen(cursor, function_name: "SetGlobals")) {
            return;
        }
        // LogAllInstructions(context);
    }

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

    private static bool IL_RippleCameraDataMod_PatchRippleTargetScreen(ILCursor cursor, string function_name) {
        if (cursor.TryGotoNext(instruction => instruction.MatchLdfld("Watcher.RippleCameraData", "rippleTargetScreen"))) {
            if (can_log_il_hooks) {
                Debug.Log($"{mod_id}: IL_RippleCameraData_{function_name}: Index {cursor.Index}");
            }

            cursor.RemoveRange(1);
            cursor.EmitDelegate<Func<Watcher.RippleCameraData, UnityEngine.Texture>>(RippleCameraDataMod_GetRippleTargetScreen);

        } else {
            if (can_log_il_hooks) {
                Debug.Log($"{mod_id}: IL_RippleCameraData_{function_name} failed.");
            }
            return false;
        }

        return true;
    }
}
