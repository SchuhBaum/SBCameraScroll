using System;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using UnityEngine;

namespace SBCameraScroll
{
    public static class AbstractRoomMod
    {
        //
        // parameters
        //

        public static readonly int maximumTextureWidth = 16384;
        public static readonly int maximumTextureHeight = 16384;
        public static bool CopyTextureSupport => SystemInfo.copyTextureSupport >= UnityEngine.Rendering.CopyTextureSupport.TextureToRT;

        //
        // variables
        //

        internal static readonly Dictionary<AbstractRoom, AttachedFields> allAttachedFields = new();
        public static AttachedFields GetAttachedFields(this AbstractRoom abstractRoom) => allAttachedFields[abstractRoom];

        public static readonly Dictionary<string, Vector2> textureOffsetModifier = new();

        public static RenderTexture mergedRenderTexture = new(1, 1, 24, RenderTextureFormat.ARGB32);
        public static readonly Texture2D mergedTexture = new(1, 1, TextureFormat.RGB24, false);
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

                if (CopyTextureSupport)
                {
                    Graphics.CopyTexture(cameraTexture, 0, 0, cutoffX, cutoffY, width, height, mergedRenderTexture, 0, 0, Mathf.Max(x, 0), Mathf.Max(y, 0));
                }
                else
                {
                    mergedTexture.SetPixels(Mathf.Max(x, 0), Mathf.Max(y, 0), width, height, cameraTexture.GetPixels(cutoffX, cutoffY, width, height));
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

        public static void CleanUp()
        {
            mergedTexture.Resize(1, 1);
            cameraTexture.Resize(1, 1);

            if (CopyTextureSupport)
            {
                mergedRenderTexture.Release();
                mergedRenderTexture = new(1, 1, 24, RenderTextureFormat.ARGB32);
            }

            Resources.UnloadUnusedAssets();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        public static void DestroyWormGrassInAbstractRoom(AbstractRoom abstractRoom)
        {
            AttachedFields attachedFields = abstractRoom.GetAttachedFields();
            if (attachedFields.wormGrass is WormGrass wormGrass)
            {
                Debug.Log("SBCameraScroll: Remove worm grass from " + abstractRoom.name + ".");

                // I expect only one wormGrass per room
                // wormGrass can have multiple patches with multiple tiles each

                wormGrass.Destroy();
                WormGrassMod.allAttachedFields.Remove(wormGrass);
            }
            attachedFields.wormGrass = null;
        }

        // creates directories if they don't exist
        public static string GetRelativeRoomsPath(string? regionName)
        {
            if (regionName == null) return "";

            string relativeRegionPath = "world" + Path.DirectorySeparatorChar + regionName.ToLower() + "-rooms";
            MainMod.CreateDirectory(MainMod.modDirectoryPath + relativeRegionPath);
            return relativeRegionPath + Path.DirectorySeparatorChar;
        }

        public static string GetRelativeRoomsPath_Arena()
        {
            string relativeRegionPath = "levels";
            MainMod.CreateDirectory(MainMod.modDirectoryPath + relativeRegionPath);
            return relativeRegionPath + Path.DirectorySeparatorChar;
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

        public static void MergeCameraTextures(AbstractRoom? abstractRoom, string? regionName, Vector2[]? cameraPositions = null)
        {
            if (abstractRoom == null) return;
            if (abstractRoom.offScreenDen) return;

            string roomName = abstractRoom.name;
            if (RoomCameraMod.blacklistedRooms.Contains(roomName)) return;

            string relativeRoomsPath;
            if (regionName == null)
            {
                if (abstractRoom.world == null || !abstractRoom.world.game.IsArenaSession)
                {
                    Debug.Log("SBCameraScroll: Region is null. Blacklist room " + roomName + ".");
                    RoomCameraMod.blacklistedRooms.Add(roomName);
                    return;
                }
                relativeRoomsPath = GetRelativeRoomsPath_Arena();
            }
            else
            {
                relativeRoomsPath = GetRelativeRoomsPath(regionName);
            }

            string mergedRoomFilePath = MainMod.modDirectoryPath + relativeRoomsPath + roomName.ToLower() + "_0.png";

            // check if custom regions already contains the merged room texture // was this needed? // hm..., I think it was
            // ignore empty merged texture files that were created but not written to
            if (File.Exists(mergedRoomFilePath) && new FileInfo(mergedRoomFilePath).Length > 0) return;

            cameraPositions ??= LoadCameraPositions(roomName);
            if (cameraPositions == null)
            {
                Debug.Log("SBCameraScroll: Camera positions could not be loaded. Blacklist room " + roomName + ".");
                RoomCameraMod.blacklistedRooms.Add(roomName);
                return;
            }
            if (cameraPositions.Length <= 1) return; // skip one screen rooms

            CheckCameraPositions(ref cameraPositions);
            UpdateTextureOffset(abstractRoom, cameraPositions);

            Vector2 baseTextureOffset = abstractRoom.GetAttachedFields().textureOffset;
            int maxWidth = 0;
            int maxHeight = 0;

            Vector2 offsetModifier = new();
            if (textureOffsetModifier.ContainsKey(roomName))
            {
                offsetModifier = textureOffsetModifier[roomName];
            }

            foreach (Vector2 cameraPosition in cameraPositions)
            {
                Vector2 _textureOffset = offsetModifier + cameraPosition - baseTextureOffset; // remove the effect of offsetModifier // baseTextureOffset already contains the offsetModifier
                maxWidth = Mathf.Max(maxWidth, (int)_textureOffset.x + 1400);
                maxHeight = Mathf.Max(maxHeight, (int)_textureOffset.y + 800);
            }

            Debug.Log("SBCameraScroll: Merge camera textures for room " + roomName + " with " + cameraPositions.Length + " cameras.");
            if (maxWidth > maximumTextureWidth || maxHeight > maximumTextureHeight)
            {
                Debug.Log("SBCameraScroll: Warning! Merged texture width or height is too large. Setting to 10000 and hoping for the best.");
                maxWidth = Mathf.Min(maxWidth, maximumTextureWidth); // 10000 seems to be the limit in Unity v4.
                maxHeight = Mathf.Min(maxHeight, maximumTextureHeight);
            }

            if (textureOffsetModifier.ContainsKey(roomName))
            {
                Debug.Log("SBCameraScroll: Cutting edges by modifying the texture offset.");
                Debug.Log("SBCameraScroll: offsetModifier " + offsetModifier);
            }

            // not sure if this helps;
            // someone ran into an out of memory exception for this function;
            try
            {
                mergedTexture.Resize(maxWidth, maxHeight); // don't create new Texture2D objects => high memory usage
                if (mergedTexture.width != maxWidth || mergedTexture.height != maxHeight)
                {
                    Debug.Log("SBCameraScroll: Resize failed. Blacklist room " + roomName + ".");
                    RoomCameraMod.blacklistedRooms.Add(roomName);
                    CleanUp();
                    return;
                }

                if (CopyTextureSupport)
                {
                    // uses GPU instead of CPU;
                    // I can't really tell the difference in speed;
                    // but using this makes memory consumption during merging basically zero;
                    mergedRenderTexture = new(maxWidth, maxHeight, 24, RenderTextureFormat.ARGB32);
                    Graphics.SetRenderTarget(mergedRenderTexture); // sets RenderTexture.active

                    if (mergedRenderTexture.width != maxWidth || mergedRenderTexture.height != maxHeight)
                    {
                        Debug.Log("SBCameraScroll: Resize failed. Blacklist room " + roomName + ".");
                        RoomCameraMod.blacklistedRooms.Add(roomName);
                        CleanUp();
                        return;
                    }

                    // clears the active;
                    // and fills it with non-transparent black (dark grey);
                    GL.Clear(true, true, new Color(1f / 255f, 0.0f, 0.0f, 1f));
                }
                else
                {
                    NativeArray<byte> colors = mergedTexture.GetRawTextureData<byte>();
                    for (int colorIndex = 0; colorIndex < colors.Length; ++colorIndex)
                    {
                        colors[colorIndex] = colorIndex % 3 == 0 ? (byte)1 : (byte)0; // non-transparent black (dark grey)
                    }
                }

                for (int cameraIndex = 0; cameraIndex < cameraPositions.Length; ++cameraIndex)
                {
                    string roomFilePath = WorldLoader.FindRoomFile(roomName, false, "_" + (cameraIndex + 1) + ".png");
                    if (File.Exists(roomFilePath))
                    {
                        AddCameraTexture(cameraIndex, roomFilePath, cameraPositions, baseTextureOffset); // changes cameraTexture and mergedTexture
                    }
                    else
                    {
                        Debug.Log("SBCameraScroll: Could not find or load texture with path " + roomFilePath + ". Blacklist " + roomName + ".");
                        RoomCameraMod.blacklistedRooms.Add(roomName);
                        CleanUp();
                        return;
                    }
                }

                if (CopyTextureSupport)
                {
                    mergedTexture.ReadPixels(new Rect(0.0f, 0.0f, mergedRenderTexture.width, mergedRenderTexture.height), 0, 0);
                }
                File.WriteAllBytes(mergedRoomFilePath, mergedTexture.EncodeToPNG());
                Debug.Log("SBCameraScroll: Merging complete.");
            }
            catch (Exception exception)
            {
                Debug.Log("SBCameraScroll: " + exception);
                Debug.Log("SBCameraScroll: Encountered an exception. Blacklist " + roomName + ".");
                RoomCameraMod.blacklistedRooms.Add(roomName);
            }
            CleanUp();

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

        public static void UpdateTextureOffset(AbstractRoom abstractRoom, in Vector2[]? cameraPositions)
        {
            AttachedFields attachedFields = abstractRoom.GetAttachedFields();
            if (attachedFields.isInitialized)
            {
                return;
            }

            if (cameraPositions == null || cameraPositions.Length == 0)
            {
                Debug.Log("SBCameraScroll: Failed to initiate textureOffset properly. Setting as new Vector2()."); // automatically set
                attachedFields.isInitialized = true;
                return;
            }

            attachedFields.textureOffset = cameraPositions[0];
            foreach (Vector2 cameraPosition in cameraPositions)
            {
                attachedFields.textureOffset.x = Mathf.Min(attachedFields.textureOffset.x, cameraPosition.x);
                attachedFields.textureOffset.y = Mathf.Min(attachedFields.textureOffset.y, cameraPosition.y);
            }

            string roomName = abstractRoom.name;
            if (textureOffsetModifier.ContainsKey(roomName))
            {
                attachedFields.textureOffset += textureOffsetModifier[roomName];
            }
            attachedFields.isInitialized = true;
        }

        //
        // private
        //

        private static void AbstractRoom_ctor(On.AbstractRoom.orig_ctor orig, AbstractRoom abstractRoom, string name, int[] connections, int index, int swarmRoomIndex, int shelterIndex, int gateIndex)
        {
            orig(abstractRoom, name, connections, index, swarmRoomIndex, shelterIndex, gateIndex);
            allAttachedFields.Add(abstractRoom, new AttachedFields());
        }

        private static void AbstractRoom_Abstractize(On.AbstractRoom.orig_Abstractize orig, AbstractRoom abstractRoom)
        {
            DestroyWormGrassInAbstractRoom(abstractRoom);
            orig(abstractRoom);
        }

        //
        //
        //

        public sealed class AttachedFields
        {
            public bool isInitialized = false;
            public Vector2 textureOffset = new();
            public WormGrass? wormGrass = null;
        }
    }
}