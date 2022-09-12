using System;
using System.Collections.Generic;
using System.IO;
using RWCustom;
using UnityEngine;
using WeakTables;

namespace SBCameraScroll
{
    public static class AbstractRoomMod
    {
        //
        // variables / WeakTables
        //

        public sealed class AttachedFields
        {
            public bool isInitialized = false;
            public Vector2 textureOffset = new Vector2();

            public AttachedFields() { }
        }

        private static WeakTable<AbstractRoom, AttachedFields> attachedFields = new WeakTable<AbstractRoom, AttachedFields>(_ => new AttachedFields());
        public static AttachedFields GetAttachedFields(this AbstractRoom abstractRoom) => attachedFields[abstractRoom];

        public static Dictionary<AbstractRoom, WormGrass> abstractRoomsWithWormGrass = new Dictionary<AbstractRoom, WormGrass>();

        //
        //
        //

        public static readonly Dictionary<string, Vector2> textureOffsetModifier = new Dictionary<string, Vector2>()
        {
            ["SB_J03"] = new Vector2(300f, 0.0f)
        };

        public static readonly Texture2D mergedTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
        public static readonly Texture2D cameraTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false);

        //
        //
        //

        internal static void OnEnable()
        {
            On.AbstractRoom.Abstractize += AbstractRoom_Abstractize;
        }

        // ---------------- //
        // public functions //
        // ---------------- //

        public static void AddCameraTexture(int cameraIndex, string filePath, Vector2[] cameraPositions, Vector2 baseTextureOffset)
        {
            Vector2 _textureOffset = cameraPositions[cameraIndex] - baseTextureOffset; // already contains the offsetModifier
            cameraTexture.LoadImage(File.ReadAllBytes(filePath)); // resizes if needed

            int x = (int)_textureOffset.x;
            int y = (int)_textureOffset.y;
            int cutoffX = 0;
            int cutoffY = 0;

            if (x < 0)
            {
                cutoffX = -x;
            }

            if (y < 0)
            {
                cutoffY = -y;
            }

            if (x < 10000 && y < 10000)
            {
                int width = Math.Min(1400 - cutoffX, 10000 - x);
                int height = Math.Min(800 - cutoffY, 10000 - y);
                mergedTexture.SetPixels(Mathf.Max(x, 0), Mathf.Max(y, 0), width, height, cameraTexture.GetPixels(cutoffX, cutoffY, width, height));
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

                List<Vector2> cameraPositions_ = new List<Vector2>();
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

        public static void ClearWormGrassInAbstractRoom(AbstractRoom abstractRoom)
        {
            if (abstractRoomsWithWormGrass.ContainsKey(abstractRoom))
            {
                Debug.Log("SBCameraScroll: Remove worm grass from " + abstractRoom.name + ".");

                // I expect only one wormGrass per room
                // wormGrass can have multiple patches with multiple tiles each

                WormGrass wormGrass = abstractRoomsWithWormGrass[abstractRoom];
                wormGrass.Destroy(); // sets slatedForDeletetion
                abstractRoomsWithWormGrass.Remove(abstractRoom);

                foreach (WormGrass.WormGrassPatch wormGrassPatch in wormGrass.patches)
                {
                    WormGrassPatchMod.cosmeticWormsOnTiles.Remove(wormGrassPatch);
                }
            }
        }

        public static string? GetCustomRegionsRelativeRoomsPath(string? roomName)
        {
            if (roomName == null)
            {
                return null;
            }

            List<string> activeModdedRegions = CustomRegions.Mod.CustomWorldMod.activeModdedRegions;
            Dictionary<string, string>.ValueCollection folderNames = CustomRegions.Mod.CustomWorldMod.activatedPacks.Values;

            string regionNameFromRoomName = roomName.Split(new char[] { '_' })[0];
            foreach (string folderName in folderNames)
            {
                for (int regionIndex = 0; regionIndex < activeModdedRegions.Count; ++regionIndex)
                {
                    string relativeRegionsPath = "Mods" + Path.DirectorySeparatorChar + "CustomResources" + Path.DirectorySeparatorChar + folderName + Path.DirectorySeparatorChar + "World" + Path.DirectorySeparatorChar + "Regions" + Path.DirectorySeparatorChar;
                    string relativeRoomsPath = relativeRegionsPath + regionNameFromRoomName + Path.DirectorySeparatorChar + "Rooms" + Path.DirectorySeparatorChar;

                    if (File.Exists(Custom.RootFolderDirectory() + relativeRoomsPath + roomName + "_1.png"))
                    {
                        return relativeRoomsPath;
                    }

                    string regionName = activeModdedRegions[regionIndex];
                    relativeRoomsPath = relativeRegionsPath + regionName + Path.DirectorySeparatorChar + "Rooms" + Path.DirectorySeparatorChar;

                    if (File.Exists(Custom.RootFolderDirectory() + relativeRoomsPath + roomName + "_1.png"))
                    {
                        return relativeRoomsPath;
                    }
                }
            }
            return null;
        }

        // creates directories if they don't exist
        public static string GetRelativeRoomsPath(string? regionName)
        {
            if (regionName == null)
            {
                return "";
            }

            string relativeRegionPath = "World" + Path.DirectorySeparatorChar + "Regions" + Path.DirectorySeparatorChar + regionName;
            MainMod.CreateDirectory(MainMod.modDirectoryPath + relativeRegionPath);
            relativeRegionPath += Path.DirectorySeparatorChar + "Rooms";
            MainMod.CreateDirectory(MainMod.modDirectoryPath + relativeRegionPath);

            return relativeRegionPath + Path.DirectorySeparatorChar;
        }

        public static Vector2[]? LoadCameraPositions(string? roomName)
        {
            if (roomName == null)
            {
                return null;
            }

            string filePath = WorldLoader.FindRoomFileDirectory(roomName, false) + ".txt";
            if (!File.Exists(filePath))
            {
                return null;
            }

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
            if (abstractRoom == null || regionName == null || RoomCameraMod.blacklistedRooms.Contains(abstractRoom.name))
            {
                return;
            }

            string roomName = abstractRoom.name;
            string? customRegionsRelativeRoomsPath = null;
            string vanillaRelativeRoomsPath = GetRelativeRoomsPath(regionName);

            if (MainMod.isCustomRegionsModEnabled)
            {
                customRegionsRelativeRoomsPath = GetCustomRegionsRelativeRoomsPath(roomName);
            }

            // check if custom regions already contains the merged room texture // was this needed? // hm..., I think it was
            // ignore empty merged texture files that were created but not written to
            if (customRegionsRelativeRoomsPath != null && File.Exists(Custom.RootFolderDirectory() + customRegionsRelativeRoomsPath + roomName + ".png") || File.Exists(MainMod.modDirectoryPath + vanillaRelativeRoomsPath + roomName + ".png") && new FileInfo(MainMod.modDirectoryPath + vanillaRelativeRoomsPath + roomName + ".png").Length > 0)
            {
                return;
            }

            cameraPositions ??= LoadCameraPositions(roomName);
            if (cameraPositions == null || cameraPositions.Length <= 1) // skip one screen rooms
            {
                return;
            }

            CheckCameraPositions(ref cameraPositions);
            UpdateTextureOffset(abstractRoom, cameraPositions);

            Vector2 baseTextureOffset = abstractRoom.GetAttachedFields().textureOffset;
            int maxWidth = 0;
            int maxHeight = 0;

            Vector2 offsetModifier = new Vector2();
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
            if (maxWidth > 10000 || maxHeight > 10000)
            {
                Debug.Log("SBCameraScroll: Warning! Merged texture width or height is too large. Setting to 10000 and hoping for the best.");
                maxWidth = Mathf.Min(maxWidth, 10000); // 10000 seems to be the limit in Unity v4.
                maxHeight = Mathf.Min(maxHeight, 10000);
            }

            if (textureOffsetModifier.ContainsKey(roomName))
            {
                Debug.Log("SBCameraScroll: Cutting edges by modifying the texture offset.");
                Debug.Log("SBCameraScroll: offsetModifier " + offsetModifier);
            }
            mergedTexture.Resize(maxWidth, maxHeight, TextureFormat.ARGB32, false); // don't create new Texture2D objects => high memory usage

            if (mergedTexture.width != maxWidth || mergedTexture.height != maxHeight)
            {
                Debug.Log("SBCameraScroll: Resize failed. Blacklist room " + roomName + ".");
                if (!RoomCameraMod.blacklistedRooms.Contains(roomName))
                {
                    RoomCameraMod.blacklistedRooms.Add(roomName);
                }
                return;
            }

            Color32[] pixels = new Color32[maxWidth * maxHeight];
            for (int index = 0; index < pixels.Length; ++index)
            {
                pixels[index] = new Color(0.004f, 0.0f, 0.0f); // non-transparent black (dark grey)
            }
            mergedTexture.SetPixels32(pixels);
            //cameraTexture.Resize(1400, 800, TextureFormat.ARGB32, false); // just to be sure

            for (int cameraIndex = 0; cameraIndex < cameraPositions.Length; ++cameraIndex)
            {
                string filePath = Custom.RootFolderDirectory() + (customRegionsRelativeRoomsPath ?? vanillaRelativeRoomsPath) + roomName + "_" + (cameraIndex + 1) + ".png";
                if (File.Exists(filePath))
                {
                    AddCameraTexture(cameraIndex, filePath, cameraPositions, baseTextureOffset); // changes cameraTexture and mergedTexture
                }
                else
                {
                    filePath = Custom.RootFolderDirectory() + vanillaRelativeRoomsPath + roomName + "_" + (cameraIndex + 1) + ".png";
                    if (customRegionsRelativeRoomsPath != null && File.Exists(filePath))
                    {
                        AddCameraTexture(cameraIndex, filePath, cameraPositions, baseTextureOffset); // changes cameraTexture and mergedTexture
                    }
                    else
                    {
                        Debug.Log("SBCameraScroll: Could not find or load texture with path " + filePath + ". Blacklist " + roomName + ".");
                        if (!RoomCameraMod.blacklistedRooms.Contains(roomName))
                        {
                            RoomCameraMod.blacklistedRooms.Add(roomName);
                        }
                        return;
                    }
                }
            }

            try
            {
                File.WriteAllBytes(MainMod.modDirectoryPath + vanillaRelativeRoomsPath + roomName + ".png", mergedTexture.EncodeToPNG());
                if (RoomCameraMod.blacklistedRooms.Contains(roomName))
                {
                    RoomCameraMod.blacklistedRooms.Remove(roomName);
                }
            }
            catch (Exception exception)
            {
                Debug.Log("SBCameraScroll: Could not write merged texture to disk. Blacklist " + roomName + ".");
                Debug.Log("SBCameraScroll: " + exception);

                if (!RoomCameraMod.blacklistedRooms.Contains(roomName))
                {
                    RoomCameraMod.blacklistedRooms.Add(roomName);
                }
            }

            Debug.Log("SBCameraScroll: Merging complete.");
            mergedTexture.Resize(1, 1);
            cameraTexture.Resize(1, 1);
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

        private static void AbstractRoom_Abstractize(On.AbstractRoom.orig_Abstractize orig, AbstractRoom abstractRoom)
        {
            ClearWormGrassInAbstractRoom(abstractRoom);
            orig(abstractRoom);
        }
    }
}