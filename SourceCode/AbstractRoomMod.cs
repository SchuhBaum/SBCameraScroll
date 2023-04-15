using System;
using System.Collections.Generic;
using System.IO;
using RWCustom;
using Unity.Collections;
using UnityEngine;

using static SBCameraScroll.MainMod;
using static SBCameraScroll.RoomCameraMod;

namespace SBCameraScroll;

public static class AbstractRoomMod
{
    //
    // parameters
    //

    public static readonly int maximumTextureWidth = 16384;
    public static readonly int maximumTextureHeight = 16384;
    public static bool HasCopyTextureSupport => SystemInfo.copyTextureSupport >= UnityEngine.Rendering.CopyTextureSupport.TextureToRT;

    //
    // variables
    //

    internal static readonly Dictionary<AbstractRoom, Attached_Fields> all_attached_fields = new();
    public static Attached_Fields Get_Attached_Fields(this AbstractRoom abstractRoom) => all_attached_fields[abstractRoom];

    public static readonly Dictionary<string, Vector2> textureOffsetModifier = new();

    public static RenderTexture? merged_render_texture = null;
    public static readonly Texture2D merged_texture = new(1, 1, TextureFormat.RGB24, false);
    public static readonly Texture2D cameraTexture = new(1, 1, TextureFormat.RGB24, false);

    //
    //
    //

    internal static void OnEnable()
    {
        On.AbstractRoom.ctor += AbstractRoom_ctor;
        On.AbstractRoom.Abstractize += AbstractRoom_Abstractize;
    }

    // ---------------- //
    // public functions //
    // ---------------- //

    public static void AddCameraTexture(int cameraIndex, string roomFilePath, in Vector2[] cameraPositions, in Vector2 baseTextureOffset)
    {
        Vector2 _textureOffset = cameraPositions[cameraIndex] - baseTextureOffset; // already contains the offsetModifier
        cameraTexture.LoadImage(File.ReadAllBytes(roomFilePath)); // resizes if needed // calls Apply() as well

        int x = (int)_textureOffset.x;
        int y = (int)_textureOffset.y;
        int cutoffX = 0;
        int cutoffY = 0;

        if (x < 0) cutoffX = -x;
        if (y < 0) cutoffY = -y;

        if (x < maximumTextureWidth && y < maximumTextureHeight)
        {
            int width = Math.Min(1400 - cutoffX, maximumTextureWidth - x);
            int height = Math.Min(800 - cutoffY, maximumTextureHeight - y);

            // I would need to de-compress the source first;
            // how do I even do that?;
            // Buffer.BlockCopy(bytes, 3 * ((cutoffX + 1) * (cutoffY + 1) - 1), mergedTexture.GetRawTextureData(), 3 * ((Mathf.Max(x, 0) + 1) * (Mathf.Max(y, 0) + 1) - 1), 3 * ((width - cutoffX) * (height - cutoffY) - 1));

            if (HasCopyTextureSupport && merged_render_texture != null)
            {
                Graphics.CopyTexture(cameraTexture, 0, 0, cutoffX, cutoffY, width, height, merged_render_texture, 0, 0, Mathf.Max(x, 0), Mathf.Max(y, 0));
            }
            else
            {
                merged_texture.SetPixels(Mathf.Max(x, 0), Mathf.Max(y, 0), width, height, cameraTexture.GetPixels(cutoffX, cutoffY, width, height));
            }
        }
    }

    public static void CheckCameraPositions(ref Vector2[] cameraPositions)
    {
        bool isFaultyCameraFound = false;
        foreach (Vector2 cameraPosition in cameraPositions)
        {
            if (Mathf.Abs(cameraPosition.x) > 20000f || Mathf.Abs(cameraPosition.y) > 20000f)
            {
                isFaultyCameraFound = true;
            }
        }

        if (isFaultyCameraFound)
        {
            // SL_C01 has two cameras which are in outer space or something => needed too much memory
            Debug.Log("SBCameraScroll: One or more camera screen positions are out of bounds. Remove them from cameraPositions.");

            List<Vector2> cameraPositions_ = new();
            foreach (Vector2 cameraPosition in cameraPositions)
            {
                if (Mathf.Abs(cameraPosition.x) <= 20000f && Mathf.Abs(cameraPosition.y) <= 20000f)
                {
                    cameraPositions_.Add(cameraPosition);
                }
            }
            cameraPositions = cameraPositions_.ToArray();
        }
    }

    public static void Clean_Up()
    {
        merged_texture.Resize(1, 1);
        cameraTexture.Resize(1, 1);

        if (merged_render_texture != null)
        {
            merged_render_texture.Release();
            merged_render_texture = null;
        }

        Resources.UnloadUnusedAssets();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    public static void DestroyWormGrassInAbstractRoom(AbstractRoom abstract_room)
    {
        Attached_Fields attached_fields = abstract_room.Get_Attached_Fields();
        if (attached_fields.worm_grass is WormGrass wormGrass)
        {
            Debug.Log("SBCameraScroll: Remove worm grass from " + abstract_room.name + ".");

            // I expect only one wormGrass per room
            // wormGrass can have multiple patches with multiple tiles each

            wormGrass.Destroy();
            WormGrassMod.all_attached_fields.Remove(wormGrass);
        }
        attached_fields.worm_grass = null;
    }

    // creates directories if they don't exist
    public static string GetRelativeRoomsPath(string? regionName)
    {
        if (regionName == null) return "";

        string relativeRegionPath = "world" + Path.DirectorySeparatorChar + regionName.ToLower() + "-rooms";
        CreateDirectory(mod_directory_path + relativeRegionPath);
        return relativeRegionPath + Path.DirectorySeparatorChar;
    }

    public static string GetRelativeRoomsPath_Arena()
    {
        string relativeRegionPath = "levels";
        CreateDirectory(mod_directory_path + relativeRegionPath);
        return relativeRegionPath + Path.DirectorySeparatorChar;
    }

    // copied from: https://stackoverflow.com/questions/60857830/finding-png-image-width-height-via-file-metadata-net-core-3-1-c-sharp
    public static IntVector2 GetImageSize_PNG(string filePath)
    {
        // using disposes IDisposable after it leaves scope
        using FileStream fileStream = File.OpenRead(filePath);
        using BinaryReader binaryReader = new(fileStream);
        binaryReader.BaseStream.Position = 16;

        byte[] widthbytes = new byte[sizeof(int)];
        for (int i = 0; i < sizeof(int); i++)
        {
            widthbytes[sizeof(int) - 1 - i] = binaryReader.ReadByte();
        }
        int width = BitConverter.ToInt32(widthbytes, 0);

        byte[] heightbytes = new byte[sizeof(int)];
        for (int i = 0; i < sizeof(int); i++)
        {
            heightbytes[sizeof(int) - 1 - i] = binaryReader.ReadByte();
        }
        int height = BitConverter.ToInt32(heightbytes, 0);

        return new IntVector2(width, height);
    }

    public static Vector2[]? LoadCameraPositions(string? roomName)
    {
        if (roomName == null) return null;

        string filePath = WorldLoader.FindRoomFile(roomName, false, ".txt");
        if (!File.Exists(filePath)) return null;

        // copy and paste from vanilla code
        string[] lines = File.ReadAllLines(filePath);
        int height = Convert.ToInt32(lines[1].Split('|')[0].Split('*')[1]);
        string[] line3 = lines[3].Split('|');
        Vector2[] cameraPositions = new Vector2[line3.Length];

        for (int index = 0; index < line3.Length; ++index)
        {
            cameraPositions[index] = new Vector2(Convert.ToSingle(line3[index].Split(',')[0]), height * 20f - 800f - Convert.ToSingle(line3[index].Split(',')[1]));
        }
        return cameraPositions;
    }

    public static void MergeCameraTextures(AbstractRoom? abstract_room, string? region_name, Vector2[]? camera_positions = null)
    {
        if (abstract_room == null) return;
        if (abstract_room.offScreenDen) return;

        string room_name = abstract_room.name;
        if (blacklisted_rooms.Contains(room_name)) return;

        string relative_rooms_path;
        if (region_name == null)
        {
            if (abstract_room.world == null || !abstract_room.world.game.IsArenaSession)
            {
                Debug.Log("SBCameraScroll: Region is null. Blacklist room " + room_name + ".");
                blacklisted_rooms.Add(room_name);
                return;
            }
            relative_rooms_path = GetRelativeRoomsPath_Arena();
        }
        else
        {
            relative_rooms_path = GetRelativeRoomsPath(region_name);
        }

        string merged_room_file_path = mod_directory_path + relative_rooms_path + room_name.ToLower() + "_0.png";

        camera_positions ??= LoadCameraPositions(room_name);
        if (camera_positions == null)
        {
            Debug.Log("SBCameraScroll: Camera positions could not be loaded. Blacklist room " + room_name + ".");
            blacklisted_rooms.Add(room_name);
            return;
        }
        if (camera_positions.Length <= 1) return; // skip one screen rooms

        CheckCameraPositions(ref camera_positions);
        UpdateTextureOffset(abstract_room, camera_positions);

        Vector2 base_texture_offset = abstract_room.Get_Attached_Fields().texture_offset;
        int max_width = 0;
        int max_height = 0;

        Vector2 offset_modifier = new();
        if (textureOffsetModifier.ContainsKey(room_name))
        {
            offset_modifier = textureOffsetModifier[room_name];
        }

        foreach (Vector2 camera_position in camera_positions)
        {
            Vector2 texture_offset = offset_modifier + camera_position - base_texture_offset; // remove the effect of offsetModifier // baseTextureOffset already contains the offsetModifier
            max_width = Mathf.Max(max_width, (int)texture_offset.x + 1400);
            max_height = Mathf.Max(max_height, (int)texture_offset.y + 800);
        }

        if (max_width > maximumTextureWidth || max_height > maximumTextureHeight)
        {
            Debug.Log("SBCameraScroll: Warning! Merged texture width or height is too large. Setting to the maximum and hoping for the best.");
            max_width = Mathf.Min(max_width, maximumTextureWidth);
            max_height = Mathf.Min(max_height, maximumTextureHeight);
        }

        if (max_width > SystemInfo.maxTextureSize || max_height > SystemInfo.maxTextureSize)
        {
            Debug.Log("SBCameraScroll: This graphics card does not support large textures. Blacklist room " + room_name + ".");
            blacklisted_rooms.Add(room_name);
            return;
        }

        // ignore empty merged texture files that were created but not written to
        if (File.Exists(merged_room_file_path) && new FileInfo(merged_room_file_path).Length > 0)
        {
            try
            {
                IntVector2 image_size = GetImageSize_PNG(merged_room_file_path);

                // this needs to be first since it blacklists room that were merged but are too large;
                if (image_size.x > SystemInfo.maxTextureSize || image_size.y > SystemInfo.maxTextureSize)
                {
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
            }
            catch
            {
                // the chance that they have the same dimensions is high;
                // don't update when the size could not be determined;
                return;
            }
        }

        if (textureOffsetModifier.ContainsKey(room_name))
        {
            Debug.Log("SBCameraScroll: Cutting edges by modifying the texture offset.");
            Debug.Log("SBCameraScroll: offsetModifier " + offset_modifier);
        }

        Debug.Log("SBCameraScroll: Merge camera textures for room " + room_name + " with " + camera_positions.Length + " cameras.");

        // not sure if this helps;
        // someone ran into an out of memory exception for this function;
        try
        {
            merged_texture.Resize(max_width, max_height); // don't create new Texture2D objects => high memory usage
            if (merged_texture.width != max_width || merged_texture.height != max_height)
            {
                Debug.Log("SBCameraScroll: Resize failed. Blacklist room " + room_name + ".");
                blacklisted_rooms.Add(room_name);
                Clean_Up();
                return;
            }

            if (merged_render_texture != null)
            {
                merged_render_texture.Release();
                merged_render_texture = null;
            }

            if (HasCopyTextureSupport)
            {
                // uses GPU instead of CPU;
                // I can't really tell the difference in speed;
                // but using this makes memory consumption during merging basically zero;
                merged_render_texture = new(max_width, max_height, 24, RenderTextureFormat.ARGB32);
                Graphics.SetRenderTarget(merged_render_texture); // sets RenderTexture.active

                if (merged_render_texture.width != max_width || merged_render_texture.height != max_height)
                {
                    Debug.Log("SBCameraScroll: Resize failed. Blacklist room " + room_name + ".");
                    blacklisted_rooms.Add(room_name);
                    Clean_Up();
                    return;
                }

                // clears the active;
                // and fills it with non-transparent black (dark grey);
                GL.Clear(true, true, new Color(1f / 255f, 0.0f, 0.0f, 1f));
            }
            else
            {
                NativeArray<byte> colors = merged_texture.GetRawTextureData<byte>();
                for (int colorIndex = 0; colorIndex < colors.Length; ++colorIndex)
                {
                    colors[colorIndex] = colorIndex % 3 == 0 ? (byte)1 : (byte)0; // non-transparent black (dark grey)
                }
            }

            for (int cameraIndex = 0; cameraIndex < camera_positions.Length; ++cameraIndex)
            {
                string roomFilePath = WorldLoader.FindRoomFile(room_name, false, "_" + (cameraIndex + 1) + ".png");
                if (File.Exists(roomFilePath))
                {
                    AddCameraTexture(cameraIndex, roomFilePath, camera_positions, base_texture_offset); // changes cameraTexture and mergedTexture
                }
                else
                {
                    Debug.Log("SBCameraScroll: Could not find or load texture with path " + roomFilePath + ". Blacklist " + room_name + ".");
                    blacklisted_rooms.Add(room_name);
                    Clean_Up();
                    return;
                }
            }

            if (HasCopyTextureSupport && merged_render_texture != null)
            {
                merged_texture.ReadPixels(new Rect(0.0f, 0.0f, merged_render_texture.width, merged_render_texture.height), 0, 0);
            }
            File.WriteAllBytes(merged_room_file_path, merged_texture.EncodeToPNG());
            Debug.Log("SBCameraScroll: Merging complete.");
        }
        catch (Exception exception)
        {
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

    public static void UpdateTextureOffset(AbstractRoom abstract_room, in Vector2[]? cameraPositions)
    {
        Attached_Fields attached_fields = abstract_room.Get_Attached_Fields();
        if (attached_fields.is_initialized) return;

        if (cameraPositions == null || cameraPositions.Length == 0)
        {
            Debug.Log("SBCameraScroll: Failed to initiate textureOffset properly. Setting as new Vector2()."); // automatically set
            attached_fields.is_initialized = true;
            return;
        }

        attached_fields.texture_offset = cameraPositions[0];
        foreach (Vector2 cameraPosition in cameraPositions)
        {
            attached_fields.texture_offset.x = Mathf.Min(attached_fields.texture_offset.x, cameraPosition.x);
            attached_fields.texture_offset.y = Mathf.Min(attached_fields.texture_offset.y, cameraPosition.y);
        }

        string roomName = abstract_room.name;
        if (textureOffsetModifier.ContainsKey(roomName))
        {
            attached_fields.texture_offset += textureOffsetModifier[roomName];
        }
        attached_fields.is_initialized = true;
    }

    //
    // private
    //

    private static void AbstractRoom_ctor(On.AbstractRoom.orig_ctor orig, AbstractRoom abstract_room, string name, int[] connections, int index, int swarm_room_index, int shelter_index, int gate_index)
    {
        orig(abstract_room, name, connections, index, swarm_room_index, shelter_index, gate_index);
        all_attached_fields.Add(abstract_room, new Attached_Fields());
    }

    private static void AbstractRoom_Abstractize(On.AbstractRoom.orig_Abstractize orig, AbstractRoom abstract_room)
    {
        DestroyWormGrassInAbstractRoom(abstract_room);
        orig(abstract_room);
    }

    //
    //
    //

    public sealed class Attached_Fields
    {
        public bool is_initialized = false;
        public Vector2 texture_offset = new();
        public WormGrass? worm_grass = null;
    }
}