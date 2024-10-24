﻿using RWCustom;
using System;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using UnityEngine;
using static SBCameraScroll.MainMod;
using static SBCameraScroll.RoomCameraMod;

namespace SBCameraScroll;

public static class AbstractRoomMod {
    //
    // parameters
    //

    public static readonly int maximum_texture_width = 16384;
    public static readonly int maximum_texture_height = 16384;
    public static bool HasCopyTextureSupport => SystemInfo.copyTextureSupport >= UnityEngine.Rendering.CopyTextureSupport.TextureToRT;

    public static ComputeShader? fill_empty_spaces_compute_shader = null;

    //
    // variables
    //

    internal static readonly Dictionary<AbstractRoom, Attached_Fields> _all_attached_fields = new();
    public static Attached_Fields Get_Attached_Fields(this AbstractRoom abstract_room) => _all_attached_fields[abstract_room];

    public static readonly Dictionary<string, Vector2> min_camera_position_modifier = new();

    public static RenderTexture? compute_shader_texture = null;
    public static RenderTexture? merged_render_texture = null;
    public static readonly Texture2D merged_texture = new(1, 1, TextureFormat.RGB24, false);
    public static readonly Texture2D camera_texture = new(1, 1, TextureFormat.RGB24, false);

    //
    //
    //

    internal static void OnEnable() {
        On.AbstractRoom.ctor += AbstractRoom_Ctor;
        On.AbstractRoom.Abstractize += AbstractRoom_Abstractize;
    }

    // ---------------- //
    // public functions //
    // ---------------- //

    public static void AddCameraTexture(int camera_index, string room_file_path, in Vector2[] camera_positions, in Vector2 min_camera_position) {
        Vector2 texture_offset = camera_positions[camera_index] - min_camera_position; // already contains the offsetModifier
        camera_texture.LoadImage(File.ReadAllBytes(room_file_path)); // resizes if needed // calls Apply() as well

        int x = (int)texture_offset.x;
        int y = (int)texture_offset.y;
        int cutoff_x = 0;
        int cutoff_y = 0;

        if (x < 0) cutoff_x = -x;
        if (y < 0) cutoff_y = -y;

        if (x < maximum_texture_width && y < maximum_texture_height) {
            int width = Math.Min(1400 - cutoff_x, maximum_texture_width - x);
            int height = Math.Min(800 - cutoff_y, maximum_texture_height - y);

            // I would need to de-compress the source first;
            // how do I even do that?;
            // Buffer.BlockCopy(bytes, 3 * ((cutoffX + 1) * (cutoffY + 1) - 1), mergedTexture.GetRawTextureData(), 3 * ((Mathf.Max(x, 0) + 1) * (Mathf.Max(y, 0) + 1) - 1), 3 * ((width - cutoffX) * (height - cutoffY) - 1));

            if (HasCopyTextureSupport && merged_render_texture != null) {
                Graphics.CopyTexture(camera_texture, 0, 0, cutoff_x, cutoff_y, width, height, merged_render_texture, 0, 0, Mathf.Max(x, 0), Mathf.Max(y, 0));
            } else {
                merged_texture.SetPixels(Mathf.Max(x, 0), Mathf.Max(y, 0), width, height, camera_texture.GetPixels(cutoff_x, cutoff_y, width, height));
            }
        }
    }

    public static void CheckCameraPositions(ref Vector2[] camera_positions) {
        bool is_faulty_camera_found = false;
        foreach (Vector2 camera_position in camera_positions) {
            if (Mathf.Abs(camera_position.x) > 20000f || Mathf.Abs(camera_position.y) > 20000f) {
                is_faulty_camera_found = true;
            }
        }

        if (is_faulty_camera_found) {
            // SL_C01 has two cameras which are in outer space or something => needed too much memory
            Debug.Log("SBCameraScroll: One or more camera screen positions are out of bounds. Remove them from cameraPositions.");

            List<Vector2> camera_positions_ = new();
            foreach (Vector2 camera_position in camera_positions) {
                if (Mathf.Abs(camera_position.x) <= 20000f && Mathf.Abs(camera_position.y) <= 20000f) {
                    camera_positions_.Add(camera_position);
                }
            }
            camera_positions = camera_positions_.ToArray();
        }
    }

    public static void Clean_Up() {
        merged_texture.Resize(1, 1);
        camera_texture.Resize(1, 1);

        if (merged_render_texture != null) {
            merged_render_texture.Release();
            merged_render_texture = null;
        }

        Resources.UnloadUnusedAssets();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    public static void DestroyWormGrassInAbstractRoom(AbstractRoom abstract_room) {
        Attached_Fields attached_fields = abstract_room.Get_Attached_Fields();
        if (attached_fields.worm_grass is WormGrass worm_grass) {
            Debug.Log("SBCameraScroll: Remove worm grass from " + abstract_room.name + ".");

            // I expect only one wormGrass per room
            // wormGrass can have multiple patches with multiple tiles each

            worm_grass.Destroy();
            WormGrassMod._all_attached_fields.Remove(worm_grass);
        }
        attached_fields.worm_grass = null;
    }

    // creates directories if they don't exist
    public static string GetRelativeRoomsPath(string? region_name) {
        if (region_name == null) return "";

        string relative_region_path = "world" + Path.DirectorySeparatorChar + region_name.ToLower() + "-rooms";
        CreateDirectory(mod_directory_path + relative_region_path);
        return relative_region_path + Path.DirectorySeparatorChar;
    }

    public static string GetRelativeRoomsPath_Arena() {
        string relative_region_path = "levels";
        CreateDirectory(mod_directory_path + relative_region_path);
        return relative_region_path + Path.DirectorySeparatorChar;
    }

    // copied from: https://stackoverflow.com/questions/60857830/finding-png-image-width-height-via-file-metadata-net-core-3-1-c-sharp
    public static IntVector2 GetImageSize_PNG(string file_path) {
        // using disposes IDisposable after it leaves scope
        using FileStream file_stream = File.OpenRead(file_path);
        using BinaryReader binary_reader = new(file_stream);
        binary_reader.BaseStream.Position = 16;

        byte[] widthbytes = new byte[sizeof(int)];
        for (int i = 0; i < sizeof(int); i++) {
            widthbytes[sizeof(int) - 1 - i] = binary_reader.ReadByte();
        }
        int width = BitConverter.ToInt32(widthbytes, 0);

        byte[] heightbytes = new byte[sizeof(int)];
        for (int i = 0; i < sizeof(int); i++) {
            heightbytes[sizeof(int) - 1 - i] = binary_reader.ReadByte();
        }
        int height = BitConverter.ToInt32(heightbytes, 0);

        return new IntVector2(width, height);
    }

    public static Vector2[]? LoadCameraPositions(string? room_name) {
        if (room_name == null) return null;

        string file_path = WorldLoader.FindRoomFile(room_name, false, ".txt");
        if (!File.Exists(file_path)) return null;

        // copy and paste from vanilla code
        string[] lines = File.ReadAllLines(file_path);
        int height = Convert.ToInt32(lines[1].Split('|')[0].Split('*')[1]);
        string[] line3 = lines[3].Split('|');
        Vector2[] camera_positions = new Vector2[line3.Length];

        for (int index = 0; index < line3.Length; ++index) {
            camera_positions[index] = new Vector2(Convert.ToSingle(line3[index].Split(',')[0]), height * 20f - 800f - Convert.ToSingle(line3[index].Split(',')[1]));
        }
        return camera_positions;
    }

    public static void MergeCameraTextures(AbstractRoom? abstract_room, string? region_name, Vector2[]? camera_positions = null) {
        if (abstract_room == null) return;
        if (abstract_room.offScreenDen) return;

        string room_name = abstract_room.name;
        if (abstract_room.Get_Attached_Fields().name_when_replaced_by_crs is string new_room_name) {
            room_name = new_room_name;
        }

        if (blacklisted_rooms.Contains(room_name)) return;

        string relative_rooms_path;
        if (region_name == null) {
            if (abstract_room.world == null || !abstract_room.world.game.IsArenaSession) {
                Debug.Log("SBCameraScroll: Region is null. Blacklist room " + room_name + ".");
                blacklisted_rooms.Add(room_name);
                return;
            }
            relative_rooms_path = GetRelativeRoomsPath_Arena();
        } else {
            relative_rooms_path = GetRelativeRoomsPath(region_name);
        }

        string merged_room_file_path = mod_directory_path + relative_rooms_path + room_name.ToLower() + "_0.png";

        camera_positions ??= LoadCameraPositions(room_name);
        if (camera_positions == null) {
            Debug.Log("SBCameraScroll: Camera positions could not be loaded. Blacklist room " + room_name + ".");
            blacklisted_rooms.Add(room_name);
            return;
        }
        if (camera_positions.Length <= 1) return; // skip one screen rooms

        Attached_Fields attached_fields = abstract_room.Get_Attached_Fields();
        Vector2 min_camera_position = abstract_room.Get_Attached_Fields().min_camera_position;
        int max_width = attached_fields.total_width;
        int max_height = attached_fields.total_height;

        Vector2 offset_modifier = new();
        if (min_camera_position_modifier.ContainsKey(room_name)) {
            offset_modifier = min_camera_position_modifier[room_name];
        }

        if (max_width > SystemInfo.maxTextureSize || max_height > SystemInfo.maxTextureSize) {
            Debug.Log("SBCameraScroll: This graphics card does not support large textures. Blacklist room " + room_name + ".");
            blacklisted_rooms.Add(room_name);
            return;
        }

        // ignore empty merged texture files that were created but not written to
        if (File.Exists(merged_room_file_path) && new FileInfo(merged_room_file_path).Length > 0) {
            try {
                IntVector2 image_size = GetImageSize_PNG(merged_room_file_path);

                // this needs to be first since it blacklists room that were merged but are too large;
                if (image_size.x > SystemInfo.maxTextureSize || image_size.y > SystemInfo.maxTextureSize) {
                    Debug.Log("SBCameraScroll: This graphics card does not support large textures. Blacklist room " + room_name + ".");
                    blacklisted_rooms.Add(room_name);
                    return;
                }

                // does not need to get updated;
                // using region mods maxWidth and maxHeight might change;
                if (image_size.x == max_width && image_size.y == max_height) return;
                if (!Option_RegionMods) return;

                // the load order matters here;
                // ONH (SB_F03 with 8 cameras) needs to be higher than MSC (SB_F03 with 6 cameras) for example;
                Debug.Log("SBCameraScroll: The dimensions for the merged texture have changed. Merge again");
            } catch {
                // the chance that they have the same dimensions is high;
                // don't update when the size could not be determined;
                return;
            }
        }

        if (min_camera_position_modifier.ContainsKey(room_name)) {
            Debug.Log("SBCameraScroll: Cutting edges by modifying the texture offset.");
            Debug.Log("SBCameraScroll: offsetModifier " + offset_modifier);
        }

        Debug.Log("SBCameraScroll: Merge camera textures for room " + room_name + " with " + camera_positions.Length + " cameras.");

        // not sure if this helps;
        // someone ran into an out of memory exception for this function;
        try {
            merged_texture.Resize(max_width, max_height); // don't create new Texture2D objects => high memory usage
            if (merged_texture.width != max_width || merged_texture.height != max_height) {
                Debug.Log("SBCameraScroll: Resize failed. Blacklist room " + room_name + ".");
                blacklisted_rooms.Add(room_name);
                Clean_Up();
                return;
            }

            if (merged_render_texture != null) {
                merged_render_texture.Release();
                merged_render_texture = null;
            }

            if (HasCopyTextureSupport) {
                // uses GPU instead of CPU;
                // I can't really tell the difference in speed;
                // but using this makes memory consumption during merging basically zero;
                merged_render_texture = new(max_width, max_height, 24, RenderTextureFormat.ARGB32) {
                    enableRandomWrite = true
                };
                Graphics.SetRenderTarget(merged_render_texture); // sets RenderTexture.active

                if (merged_render_texture.width != max_width || merged_render_texture.height != max_height) {
                    Debug.Log("SBCameraScroll: Resize failed. Blacklist room " + room_name + ".");
                    blacklisted_rooms.Add(room_name);
                    Clean_Up();
                    return;
                }

                // clears the active;
                // and fills it with non-transparent black (dark grey);
                Color default_color = new Color(1f / 255f, 0.0f, 0.0f, 1f);
                if (Option_FillEmptySpaces && fill_empty_spaces_compute_shader != null) {
                    // Magenta is probably fine as well. The textures are mostly
                    // black, red and shades of blue. When it is purple it is
                    // probably not fully opague. I have not seen green so far
                    // so use that instead.
                    default_color = Color.green;
                }
                GL.Clear(true, true, default_color);
            } else {
                NativeArray<byte> colors = merged_texture.GetRawTextureData<byte>();
                for (int color_index = 0; color_index < colors.Length; ++color_index) {
                    colors[color_index] = color_index % 3 == 0 ? (byte)1 : (byte)0; // non-transparent black (dark grey)
                }
            }

            for (int camera_index = 0; camera_index < camera_positions.Length; ++camera_index) {
                string room_file_path = WorldLoader.FindRoomFile(room_name, false, "_" + (camera_index + 1) + ".png");
                if (File.Exists(room_file_path)) {
                    AddCameraTexture(camera_index, room_file_path, camera_positions, min_camera_position); // changes cameraTexture and mergedTexture
                } else {
                    Debug.Log("SBCameraScroll: Could not find or load texture with path " + room_file_path + ". Blacklist " + room_name + ".");
                    blacklisted_rooms.Add(room_name);
                    Clean_Up();
                    return;
                }
            }

            if (HasCopyTextureSupport && merged_render_texture != null) {
                if (Option_FillEmptySpaces && fill_empty_spaces_compute_shader != null) {
                    Debug.Log(mod_id + ": Fill empty spaces with pre-rendered content.");
                    Color default_color = Color.green;

                    // Shader colors are RBGA format only.
                    fill_empty_spaces_compute_shader.SetVector("defaultColor", new Vector4(default_color.r, default_color.g, default_color.b, default_color.a));
                    fill_empty_spaces_compute_shader.SetInt("width", merged_render_texture.width);
                    fill_empty_spaces_compute_shader.SetInt("height", merged_render_texture.height);

                    // Using the merged_render_texture as ResultTexture directly
                    // does not work for some reason. Maybe only using one texture
                    // with read-write access would work. But then the order of
                    // execution would matter, i.e. which pixels are set first.
                    // Leave it as is.
                    fill_empty_spaces_compute_shader.SetTexture(0, "SourceTexture", merged_render_texture);
                    // fill_empty_spaces.SetTexture(0, "ResultTexture", merged_render_texture);

                    compute_shader_texture = new(merged_render_texture.width, merged_render_texture.height, 24, RenderTextureFormat.ARGB32) {
                        enableRandomWrite = true
                    };
                    compute_shader_texture.Create();
                    fill_empty_spaces_compute_shader.SetTexture(0, "ResultTexture", compute_shader_texture);

                    int threadGroupsX = Mathf.CeilToInt((float)merged_render_texture.width / 8.0f);
                    int threadGroupsY = Mathf.CeilToInt((float)merged_render_texture.height / 8.0f);
                    fill_empty_spaces_compute_shader.Dispatch(0, threadGroupsX, threadGroupsY, 1);

                    // This would not be required if I could use merged_render_texture
                    // as ResultTexture.
                    Graphics.Blit(compute_shader_texture, merged_render_texture);
                    compute_shader_texture.Release();
                }
                merged_texture.ReadPixels(new Rect(0.0f, 0.0f, merged_render_texture.width, merged_render_texture.height), 0, 0);
            }

            File.WriteAllBytes(merged_room_file_path, merged_texture.EncodeToPNG());
            Debug.Log("SBCameraScroll: Merging completed.");
            next_text_prompt_message = mod_id + ": Merging camera textures completed.";
        } catch (Exception exception) {
            Debug.Log("SBCameraScroll: " + exception);
            Debug.Log("SBCameraScroll: Encountered an exception. Blacklist " + room_name + ".");
            blacklisted_rooms.Add(room_name);
        }
        Clean_Up();

        // mergedTexture.Resize(1, 1);
        // cameraTexture.Resize(1, 1);

        // // cameraTexture uses LoadImage() which calls Apply() to upload to the GPU;
        // // maybe this is better;
        // // doesn't seem to do much..; it's also the gpu not cpu;
        // // cameraTexture.Apply();

        // // this seems to help sometimes; do the loaded images stay in memory otherwise?;
        // // there is still some sort of memory fragmentation(?) going on; maybe bc of the resizing;
        // // or maybe stuff just gets accumulated and you get an inital boost by releasing it;
        // Resources.UnloadUnusedAssets();

        // // this seems to help more; this makes sense since you generate
        // // a lot of garbage;
        // // merging DW after starting rain world with GC cuts down memory by 500MB
        // // with GC: 1.5GB; without: ~2GB;
        // // 500MB are used by rain world when starting;
        // // (not counting stuff that get allocated when loading into the game);
        // // leaving <1.0GB or <1.5GB from merging 138 rooms, respectively;
        // GC.Collect();
        // GC.WaitForPendingFinalizers();
        // GC.Collect();
    }

    [Obsolete("Use InitializeAttachedFields(...) instead.")]
    public static void UpdateTextureOffset(AbstractRoom abstract_room, in Vector2[]? camera_positions) {
        UpdateAttachedFields(abstract_room);
    }

    // I need to initialize the fields again if CRS changes the room name.
    // Otherwise, the camera textures are misaligned or not merged.
    public static void UpdateAttachedFields(AbstractRoom abstract_room) {
        Attached_Fields attached_fields = abstract_room.Get_Attached_Fields();

        string room_name = abstract_room.name;
        if (abstract_room.Get_Attached_Fields().name_when_replaced_by_crs is string new_room_name) {
            room_name = new_room_name;
        }

        Vector2[]? camera_positions = LoadCameraPositions(room_name);
        if (camera_positions == null) return;
        CheckCameraPositions(ref camera_positions);

        if (camera_positions == null || camera_positions.Length == 0) {
            Debug.Log(mod_id + ": Failed to initialize attached_fields for room " + room_name + ".");
            return;
        }

        int total_width = 0;
        int total_height = 0;
        attached_fields.min_camera_position = camera_positions[0];

        foreach (Vector2 camera_position in camera_positions) {
            attached_fields.min_camera_position.x = Mathf.Min(attached_fields.min_camera_position.x, camera_position.x);
            attached_fields.min_camera_position.y = Mathf.Min(attached_fields.min_camera_position.y, camera_position.y);
            total_width = Mathf.Max(total_width, (int)camera_position.x + 1400);
            total_height = Mathf.Max(total_height, (int)camera_position.y + 800);
        }

        // Ignore the effect of any position modifiers here.
        total_width -= (int)attached_fields.min_camera_position.x;
        total_height -= (int)attached_fields.min_camera_position.y;

        if (total_width > maximum_texture_width || total_height > maximum_texture_height) {
            Debug.Log("SBCameraScroll: Warning! Merged texture width or height is too large. Setting to the maximum and hoping for the best.");
            total_width = Mathf.Min(total_width, maximum_texture_width);
            total_height = Mathf.Min(total_height, maximum_texture_height);
        }

        attached_fields.total_width = total_width;
        attached_fields.total_height = total_height;

        if (min_camera_position_modifier.ContainsKey(room_name)) {
            // In the current version this is not used. The old Unity had a limit
            // for the texture size of ~8kx8k. In that case it was necessary to cut
            // specific rooms. The texture offset was modified to center the image.
            attached_fields.min_camera_position += min_camera_position_modifier[room_name];
        }
        // Debug.Log(mod_id + ": Initialized attached_fields for room " + room_name + ".");
    }

    //
    // private
    //

    private static void AbstractRoom_Ctor(On.AbstractRoom.orig_ctor orig, AbstractRoom abstract_room, string room_name, int[] connections, int index, int swarm_room_index, int shelter_index, int gate_index) {
        orig(abstract_room, room_name, connections, index, swarm_room_index, shelter_index, gate_index);
        if (_all_attached_fields.ContainsKey(abstract_room)) return;
        _all_attached_fields.Add(abstract_room, new Attached_Fields());
        UpdateAttachedFields(abstract_room);
    }

    private static void AbstractRoom_Abstractize(On.AbstractRoom.orig_Abstractize orig, AbstractRoom abstract_room) {
        DestroyWormGrassInAbstractRoom(abstract_room);
        orig(abstract_room);
    }

    //
    //
    //

    public sealed class Attached_Fields {
        public int total_width = 1400;
        public int total_height = 800;
        public string? name_when_replaced_by_crs = null;

        [Obsolete("Use min_camera_position instead.")]
        public Vector2 texture_offset => min_camera_position;

        public Vector2 min_camera_position = new();
        public WormGrass? worm_grass = null;
    }
}
