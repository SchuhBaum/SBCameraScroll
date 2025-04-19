using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using RWCustom;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static SBCameraScroll.AbstractRoomMod;
using static SBCameraScroll.MainMod;
using static SBCameraScroll.Util;

namespace SBCameraScroll;

internal static class RippleCameraDataMod {
    public static RenderTexture[] ripple_target_screens = new RenderTexture[4] {
        new RenderTexture(1, 1, 0, RenderTextureFormat.ARGB32) {
            anisoLevel = 0,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            useMipMap = false,
        },
        new RenderTexture(1, 1, 0, RenderTextureFormat.ARGB32) {
            anisoLevel = 0,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            useMipMap = false,
        },
        new RenderTexture(1, 1, 0, RenderTextureFormat.ARGB32) {
            anisoLevel = 0,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            useMipMap = false,
        },
        new RenderTexture(1, 1, 0, RenderTextureFormat.ARGB32) {
            anisoLevel = 0,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            useMipMap = false,
        }
    };

    //
    //

    internal static void OnEnable() {
        IL.Watcher.RippleCameraData.AddCommandBuffer += IL_RippleCameraData_AddCommandBuffer;
        IL.Watcher.RippleCameraData.SetGlobals       += IL_RippleCameraData_SetGlobals;
        IL.Watcher.RippleCameraData.SetTarget        += IL_RippleCameraData_SetTarget;
    }

    //
    // public
    //

    public static bool IL_RippleCameraDataMod_PatchRippleTargetScreen(ILCursor cursor, string function_name) {
        if (cursor.TryGotoNext(instruction => instruction.MatchLdfld("Watcher.RippleCameraData", "rippleTargetScreen"))) {
            if (can_log_il_hooks) {
                Debug.Log($"{mod_id}: IL_RippleCameraData_{function_name}: Index {cursor.Index}");
            }

            cursor.RemoveRange(1);
            cursor.EmitDelegate<Func<Watcher.RippleCameraData, UnityEngine.RenderTexture>>(RippleCameraDataMod_GetRippleTargetScreen);

        } else {
            if (can_log_il_hooks) {
                Debug.Log($"{mod_id}: IL_RippleCameraData_{function_name} failed.");
            }
            return false;
        }

        return true;
    }

    public static UnityEngine.RenderTexture RippleCameraDataMod_GetRippleTargetScreen(Watcher.RippleCameraData ripple_data) {
        if (Custom.rainWorld?.processManager?.currentMainLoop is not RainWorldGame game) {
            Debug.Log("SBCameraScroll.RippleCameraDataMod_GetRippleTargetScreen: [WARNING] Expected to be in-game. But I did not find the instance for RainWorldGame. I assume now that this is the camera for player 1 and hope for the best.");
            return ripple_target_screens[0];
        }

        foreach (RoomCamera room_camera in game.cameras) {
            if (ripple_data == room_camera.rippleData) {
                int camera_number = room_camera.cameraNumber;
                if (camera_number < 0 || camera_number > 3) {
                    Debug.Log($"{mod_id}.RippleCameraDataMod_GetRippleTargetScreen: [WARNING] I got the invalid camera number {camera_number}. I will use 0 instead.");
                    camera_number = 0;
                }
                return ripple_target_screens[room_camera.cameraNumber];
            }
        }

        Debug.Log($"SBCameraScroll.RippleCameraDataMod_GetRippleTargetScreen: [WARNING] Expected to be in-game. But I did not find the room camera for the rippleData {ripple_data}. I assume now this is the camera for player 1 and hope for the best.");
        return ripple_target_screens[0];
    }

    public static void RippleCameraDataMod_LoadRippleTargetScreen(Watcher.RippleCameraData ripple_data) {
        // Shouldn't get triggered since vanilla checks this too.
        if (!ripple_data.hasScreen) {
            return;
        }

        if (Custom.rainWorld?.processManager?.currentMainLoop is not RainWorldGame game) {
            Debug.Log("SBCameraScroll.RippleCameraDataMod_LoadRippleTargetScreen: [WARNING] Did not find the game process. Aborting the function RippleCameraDataMod_LoadRippleTargetScreen().");
            return;
        }

        RoomCamera? room_camera = null;
        foreach (RoomCamera rt in game.cameras) {
            if (ripple_data == rt.rippleData) {
                room_camera = rt;
                break;
            }
        }

        if (room_camera == null) {
            Debug.Log("SBCameraScroll.RippleCameraDataMod_LoadRippleTargetScreen: [WARNING] Did not find the room camera. Aborting the function RippleCameraDataMod_LoadRippleTargetScreen().");
            return;
        }

        string? room_name = room_camera.RippleSettings?.destRoom;
        if (room_name == null) {
            room_name = room_camera.loadingRoom?.abstractRoom.name;
        }
        if (room_name == null) {
            room_name = room_camera.room?.abstractRoom.name;
        }
        if (room_name == null) {
            Debug.Log("SBCameraScroll.RippleCameraDataMod_LoadRippleTargetScreen: [WARNING] Did not find any room name. Aborting the function RippleCameraDataMod_LoadRippleTargetScreen().");
            return;
        }

        if (room_name_to_crs_room_name.TryGetValue(room_name, out string new_room_name)) {
            room_name = new_room_name;
        }

        int camera_number = room_camera.cameraNumber;
        if (camera_number < 0 || camera_number > 3) {
            Debug.Log($"{mod_id}.RippleCameraDataMod_LoadRippleTargetScreen: [WARNING] I got the invalid camera number {camera_number}. I will use 0 instead.");
            camera_number = 0;
        }


        RenderTexture render_texture = ripple_target_screens[camera_number];
        if (room_camera.IsRoomBlacklisted(room_name)) {
            // vanilla case
            ripple_data.rippleTargetScreen.LoadImage(ripple_data.preLoadTexture, markNonReadable: false);

            if (render_texture.width != 1400 || render_texture.height != 800) {
                render_texture.Release();
                render_texture.width = 1400;
                render_texture.height = 800;
            }
            Graphics.CopyTexture(ripple_data.rippleTargetScreen, render_texture);
            return;
        }

        Util_LoadRoomTextureIntoRenderTexture(room_name, render_texture);
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
            cursor.EmitDelegate<Action<Watcher.RippleCameraData>>(RippleCameraDataMod_LoadRippleTargetScreen);

        } else {
            if (can_log_il_hooks) {
                Debug.Log($"{mod_id}: IL_RippleCameraData_SetTarget failed.");
            }
            return;
        }

        // LogAllInstructions(context);
    }
}
