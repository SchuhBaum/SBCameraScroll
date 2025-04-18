using RWCustom;
using System;
using System.IO;
using UnityEngine;

using static SBCameraScroll.AbstractRoomMod;
using static SBCameraScroll.MainMod;

namespace SBCameraScroll;

public static class Util {
    public static readonly Texture2D camera_texture = new Texture2D(1400, 800, TextureFormat.ARGB32, mipChain: false) {
        anisoLevel = 0,
        filterMode = FilterMode.Point,
        wrapMode = TextureWrapMode.Clamp
    };

    //
    //

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
            room_name = $"gate_{splitted_room_name[1]}_{splitted_room_name[2]}";
        } else {
            room_name = $"{splitted_room_name[0]}_{splitted_room_name[1]}";
        }

        return room_name.ToLower();
    }

    public static void Util_LoadRoomTextureIntoRenderTexture(string room_name, RenderTexture render_texture, int? use_cache_camera_number = null) {
        if (Custom.rainWorld?.processManager?.currentMainLoop is not RainWorldGame game) {
            Debug.Log($"{mod_id}.Util_LoadRoomTextureIntoRenderTexture: [WARNING] Expected to be in-game. But I did not find the instance for RainWorldGame. Aborting.");
            return;
        }

        if (CalculateLevelTextureRectangle(room_name) is not RectInt rect) {
            Debug.Log($"{mod_id}.Util_LoadRoomTextureIntoRenderTexture: [WARNING] Could not calculate the level texture rectangle. Aborting.");
            return;
        }

        Vector2[]? camera_positions = LoadCameraPositions(room_name);
        if (camera_positions == null) {
            Debug.Log($"{mod_id}.Util_LoadRoomTextureIntoRenderTexture: [WARNING] Could not load camera positions. Aborting.");
            return;
        }

        CheckCameraPositions(ref camera_positions);
        if (camera_positions == null || camera_positions.Length == 0) {
            Debug.Log($"{mod_id}.Util_LoadRoomTextureIntoRenderTexture: [WARNING] Could not load camera positions. Aborting.");
            return;
        }

        int total_width  = rect.width;
        int total_height = rect.height;
        if (total_width > maximum_texture_width || total_height > maximum_texture_height) {
            Debug.Log($"{mod_id}.Util_LoadRoomTextureIntoRenderTexture: Warning! Merged texture width or height is too large. Setting to the maximum and hoping for the best.");
            total_width  = Mathf.Min(total_width, maximum_texture_width);
            total_height = Mathf.Min(total_height, maximum_texture_height);
        }

        if (render_texture.width != total_width || render_texture.height != total_height) {
            render_texture.Release();
            render_texture.width  = total_width;
            render_texture.height = total_height;
        }

        int camera_number = -1;
        if (use_cache_camera_number is int new_camera_number) {
            if (new_camera_number < 0 || new_camera_number > 3) {
                Debug.Log($"{mod_id}.Util_LoadRoomTextureIntoRenderTexture: [WARNING] I got the invalid camera number {new_camera_number}. I will use 0 instead.");
                camera_number = 0;
            } else {
                camera_number = new_camera_number;
            }
        }

        Vector2 min_camera_position = new Vector2(rect.x, rect.y);
        for (int cam_pos_index = 0; cam_pos_index < camera_positions.Length; ++cam_pos_index) {
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

                if (camera_number == -1) {
                    string camera_texture_path = WorldLoader.FindRoomFile(room_name, includeRootDirectory: true, $"_{cam_pos_index+1}.png");
                    byte[] bytes = AssetManager.PreLoadTexture(camera_texture_path);

                    camera_texture.LoadImage(bytes);
                    Graphics.CopyTexture(camera_texture, 0, 0, cutoff_x, cutoff_y, width, height, render_texture, 0, 0, Mathf.Max(x, 0), Mathf.Max(y, 0));

                } else {
                    if (room_name != RoomCameraMod.Get_Level_Texture_Room_Name(camera_number, cam_pos_index)) {
                        // Load and cache.
                        string camera_texture_path = WorldLoader.FindRoomFile(room_name, includeRootDirectory: true, $"_{cam_pos_index+1}.png");
                        byte[] bytes = AssetManager.PreLoadTexture(camera_texture_path);
                        RoomCameraMod.Load_Image(room_name, camera_number, cam_pos_index, bytes);
                    }
                    Graphics.CopyTexture(RoomCameraMod.Get_Level_Texture(camera_number, cam_pos_index), 0, 0, cutoff_x, cutoff_y, width, height, render_texture, 0, 0, Mathf.Max(x, 0), Mathf.Max(y, 0));
                }
            }
        }
    }
}
