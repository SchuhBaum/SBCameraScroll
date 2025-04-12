using RWCustom;
using System;
using System.IO;
using UnityEngine;

using static SBCameraScroll.AbstractRoomMod;

namespace SBCameraScroll;

public static class Util {
    public static string? Util_ExtractRoomNameFromPath(string room_path) {
        string[] split = room_path.Split(Path.DirectorySeparatorChar);
        if (split.Length == 0) {
            return null;
        }

        string[] splitted_room_name = split[split.Length-1].Split('_');
        if (splitted_room_name.Length == 0) {
            return null;
        }

        string room_name;
        if (splitted_room_name[0].ToLower() == "gate") {
            if (splitted_room_name.Length <= 2) return null;
            room_name = "gate_" + splitted_room_name[1] + "_" + splitted_room_name[2];
        } else {
            room_name = splitted_room_name[0] + "_" + splitted_room_name[1];
        }

        return room_name.ToLower();
    }

    // TODO: this should check for changed names by crs;
    // TODO: this should be the default meaning that it should use cached room textures for merging when provided and used in all cases where you load a room texture into a render texture; currently this is only used for ripple effect destination rooms;
    public static void Util_LoadRoomTextureIntoRenderTexture(string room_name, RenderTexture render_texture) {
        if (Custom.rainWorld?.processManager?.currentMainLoop is not RainWorldGame game) {
            Debug.Log("SBCameraScroll: [WARNING] Expected to be in-game. But I did not find the instance for RainWorldGame. Aborting the function RippleCameraDataMod_GetRippleTargetScreen().");
            return;
        }

        if (CalculateLevelTextureRectangle(room_name) is not RectInt rect) {
            Debug.Log("SBCameraScroll: [WARNING] Could not calculate the level texture rectangle. Aborting the function Util_LoadRoomTextureIntoRenderTexture().");
            return;
        }

        Vector2[]? camera_positions = LoadCameraPositions(room_name);
        if (camera_positions == null) {
            Debug.Log("SBCameraScroll: [WARNING] Could not load camera positions. Aborting the function RippleCameraDataMod_GetRippleTargetScreen().");
            return;
        }

        CheckCameraPositions(ref camera_positions);
        if (camera_positions == null || camera_positions.Length == 0) {
            Debug.Log("SBCameraScroll: [WARNING] Could not load camera positions. Aborting the function RippleCameraDataMod_GetRippleTargetScreen().");
            return;
        }

        int total_width  = rect.width;
        int total_height = rect.height;
        if (total_width > maximum_texture_width || total_height > maximum_texture_height) {
            Debug.Log("SBCameraScroll: Warning! Merged texture width or height is too large. Setting to the maximum and hoping for the best.");
            total_width  = Mathf.Min(total_width, maximum_texture_width);
            total_height = Mathf.Min(total_height, maximum_texture_height);
        }

        if (render_texture.width != total_width || render_texture.height != total_height) {
            render_texture.Release();
            render_texture.width  = total_width;
            render_texture.height = total_height;
        }

        Vector2 min_camera_position = new Vector2(rect.x, rect.y);
        for (int cam_pos_index = 0; cam_pos_index < camera_positions.Length; ++cam_pos_index) {
            // already contains the offsetModifier;
            Vector2 texture_offset = camera_positions[cam_pos_index] - min_camera_position;

            int x = (int)texture_offset.x;
            int y = (int)texture_offset.y;
            int cutoff_x = 0;
            int cutoff_y = 0;

            if (x < 0) cutoff_x = -x;
            if (y < 0) cutoff_y = -y;

            if (x < maximum_texture_width && y < maximum_texture_height) {
                int width = Math.Min(1400 - cutoff_x, maximum_texture_width - x);
                int height = Math.Min(800 - cutoff_y, maximum_texture_height - y);

                Texture2D camera_texture = AbstractRoomMod.camera_texture;
                string camera_texture_path = WorldLoader.FindRoomFile(room_name, includeRootDirectory: true, "_" + (cam_pos_index+1) + ".png");
                byte[] bytes = AssetManager.PreLoadTexture(camera_texture_path);
                camera_texture.LoadImage(bytes);

                Graphics.CopyTexture(camera_texture, 0, 0, cutoff_x, cutoff_y, width, height, render_texture, 0, 0, Mathf.Max(x, 0), Mathf.Max(y, 0));
            }
        }
    }
}
