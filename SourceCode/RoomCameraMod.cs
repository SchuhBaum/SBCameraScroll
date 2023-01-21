using System;
using System.Collections.Generic;
using System.IO;
using RWCustom;
using UnityEngine;

namespace SBCameraScroll
{
    public enum CameraType
    {
        Position,
        Vanilla,
        Velocity
    }

    public static class RoomCameraMod
    {
        //
        // variables
        //

        internal static readonly Dictionary<RoomCamera, AttachedFields> allAttachedFields = new();
        public static AttachedFields GetAttachedFields(this RoomCamera roomCamera) => allAttachedFields[roomCamera];

        //
        // parameters
        //

        public static CameraType cameraType = CameraType.Position;

        public static float innerCameraBoxX = 0.0f; // don't move camera when player is too close
        public static float innerCameraBoxY = 0.0f; // set default values in option menu
        public static float outerCameraBoxX = 0.0f; // the camera will always be at least this close
        public static float outerCameraBoxY = 0.0f;
        public static float smoothingFactorX = 0.0f; // set default values in option menu
        public static float smoothingFactorY = 0.0f;

        public static float maxUpdateShortcut = 3f;
        public static List<string> blacklistedRooms = new();

        //
        //
        //

        internal static void OnEnable()
        {
            On.RoomCamera.ApplyDepth += RoomCamera_ApplyDepth;
            On.RoomCamera.ApplyPalette += RoomCamera_ApplyPalette;
            On.RoomCamera.ApplyPositionChange += RoomCamera_ApplyPositionChange;
            On.RoomCamera.ctor += RoomCamera_ctor;

            On.RoomCamera.DrawUpdate += RoomCamera_DrawUpdate;
            On.RoomCamera.MoveCamera_int += RoomCamera_MoveCamera;
            On.RoomCamera.MoveCamera2 += RoomCamera_MoveCamera2;
            On.RoomCamera.PixelColorAtCoordinate += RoomCamera_PixelColorAtCoordinate;

            On.RoomCamera.PositionCurrentlyVisible += RoomCamera_PositionCurrentlyVisible;
            On.RoomCamera.PositionVisibleInNextScreen += RoomCamera_PositionVisibleInNextScreen;
            On.RoomCamera.PreLoadTexture += RoomCamera_PreLoadTexture;

            On.RoomCamera.ScreenMovement += RoomCamera_ScreenMovement;
            On.RoomCamera.Update += RoomCamera_Update;
        }

        // ---------------- //
        // public functions //
        // ---------------- //

        public static void CheckBorders(RoomCamera roomCamera, ref Vector2 position)
        {
            if (roomCamera.room == null)
            {
                return;
            }

            Vector2 screenSize = roomCamera.sSize;
            Vector2 textureOffset = roomCamera.room.abstractRoom.GetAttachedFields().textureOffset; // regionGate's texture offset might be unitialized => RegionGateMod

            if (MainMod.isSplitScreenModEnabled)
            {
                Vector2 screenOffset = SplitScreenMod_GetScreenOffset(screenSize); // half of the camera screen is not visible // the other half is centered // let the non-visible part move past room borders
                position.x = Mathf.Clamp(position.x, textureOffset.x - screenOffset.x, textureOffset.x + screenOffset.x + roomCamera.levelGraphic.width - screenSize.x);
                position.y = Mathf.Clamp(position.y, textureOffset.y - screenOffset.y, textureOffset.y + screenOffset.y + roomCamera.levelGraphic.height - screenSize.y - 18f);
            }
            else
            {
                position.x = Mathf.Clamp(position.x, textureOffset.x, roomCamera.levelGraphic.width - screenSize.x + textureOffset.x); // stop position at room texture borders // probably works with room.PixelWidth - roomCamera.sSize.x / 2f instead as well
                position.y = Mathf.Clamp(position.y, textureOffset.y, roomCamera.levelGraphic.height - screenSize.y + textureOffset.y - 18f); // not sure why I have to decrease positionY by a constant // I picked 18f bc roomCamera.seekPos.y gets changed by 18f in Update() // seems to work , i.e. I don't see black bars
            }
        }

        public static void ResetCameraPosition(RoomCamera roomCamera)
        {
            AttachedFields attachedFields = roomCamera.GetAttachedFields();

            // vanilla copy & paste stuff
            if (attachedFields.isRoomBlacklisted || roomCamera.voidSeaMode) //TODO
            {
                roomCamera.seekPos = roomCamera.CamPos(roomCamera.currentCameraPosition);
                roomCamera.seekPos.x += roomCamera.hDisplace + 8f;
                roomCamera.seekPos.y += 18f;
                roomCamera.leanPos *= 0.0f;

                roomCamera.lastPos = roomCamera.seekPos;
                roomCamera.pos = roomCamera.seekPos;
                return;
            }

            attachedFields.followAbstractCreatureID = null; // do a smooth transition // this actually makes a difference for the vanilla type camera // otherwise the map input would immediately be processed
            UpdateOnScreenPosition(roomCamera);
            CheckBorders(roomCamera, ref attachedFields.onScreenPosition); // do not move past room boundaries

            if (cameraType == CameraType.Vanilla)
            {
                roomCamera.seekPos = roomCamera.CamPos(roomCamera.currentCameraPosition);
                roomCamera.seekPos.x += roomCamera.hDisplace + 8f;
                roomCamera.seekPos.y += 18f;
                roomCamera.leanPos *= 0.0f;

                // center camera on vanilla position
                roomCamera.lastPos = roomCamera.seekPos;
                roomCamera.pos = roomCamera.seekPos;

                attachedFields.seekPosition *= 0.0f;
                attachedFields.vanillaTypePosition = attachedFields.onScreenPosition;
                attachedFields.useVanillaPositions = true;
                attachedFields.isCentered = false;
            }
            else
            {
                // center camera on player
                roomCamera.lastPos = attachedFields.onScreenPosition;
                roomCamera.pos = attachedFields.onScreenPosition;
            }
        }

        public static Vector2 SplitScreenMod_GetScreenOffset(in Vector2 screenSize) => SplitScreenMod.SplitScreenMod.CurrentSplitMode switch
        {
            SplitScreenMod.SplitScreenMod.SplitMode.SplitVertical => new Vector2(0.25f * screenSize.x, 0.0f),
            SplitScreenMod.SplitScreenMod.SplitMode.SplitHorizontal => new Vector2(0.0f, 0.25f * screenSize.y),
            _ => new Vector2(),
        };

        // expanding camera logic from bee's CameraScroll mod
        public static void UpdateCameraPosition(RoomCamera roomCamera)
        {
            if (roomCamera.followAbstractCreature == null || roomCamera.room == null)
            {
                return;
            }

            UpdateOnScreenPosition(roomCamera);
            AttachedFields attachedFields = roomCamera.GetAttachedFields();

            if (attachedFields.followAbstractCreatureID != roomCamera.followAbstractCreature.ID && roomCamera.followAbstractCreature.realizedCreature is Player player) // followAbstractCreature != null // smooth transition when switching cameras in the same room
            {
                attachedFields.followAbstractCreatureID = null; // keep transition going even when switching back
                UpdateCamera_PositionType(roomCamera, attachedFields); // needs followAbstractCreatureID = null

                if (attachedFields.transitionTrackingID != roomCamera.followAbstractCreature.ID)
                {
                    // another player can take the camera during the transition // keep track to prevent instant map press recognition
                    attachedFields.transitionTrackingID = roomCamera.followAbstractCreature.ID;
                }
                else if (cameraType == CameraType.Vanilla && player.input[0].mp && !player.input[1].mp)
                {
                    attachedFields.useVanillaPositions = !attachedFields.useVanillaPositions;
                }

                // stop transition earlier when player is moving && vanilla positions are not used
                if (roomCamera.pos == roomCamera.lastPos || !attachedFields.useVanillaPositions &&
                    (Mathf.Abs(player.mainBodyChunk.vel.x) <= 1f && roomCamera.pos.x == roomCamera.lastPos.x || Mathf.Abs(player.mainBodyChunk.vel.x) > 1f && Math.Abs(roomCamera.pos.x - roomCamera.lastPos.x) <= 10f) &&
                    (Mathf.Abs(player.mainBodyChunk.vel.y) <= 1f && roomCamera.pos.y == roomCamera.lastPos.y || Mathf.Abs(player.mainBodyChunk.vel.y) > 1f && Math.Abs(roomCamera.pos.y - roomCamera.lastPos.y) <= 10f))
                {
                    attachedFields.followAbstractCreatureID = roomCamera.followAbstractCreature.ID;
                    attachedFields.transitionTrackingID = null;
                    attachedFields.vanillaTypePosition = roomCamera.pos;
                    attachedFields.isCentered = true; // for vanilla type only
                }
            }
            else
            {
                switch (cameraType)
                {
                    case CameraType.Position:
                        UpdateCamera_PositionType(roomCamera, attachedFields); // don't skip smooth transition if part or set followAbstractCreatureID here
                        break;
                    case CameraType.Vanilla: // put after transition or map input gets registered immediately
                        if (attachedFields.isCentered && (Mathf.Abs(attachedFields.onScreenPosition.x - attachedFields.lastOnScreenPosition.x) > 1f || Mathf.Abs(attachedFields.onScreenPosition.y - attachedFields.lastOnScreenPosition.y) > 1f))
                        {
                            attachedFields.isCentered = false;
                        }

                        if (!attachedFields.useVanillaPositions)
                        {
                            UpdateCamera_VanillaType(roomCamera, attachedFields);
                        }

                        if (roomCamera.followAbstractCreature?.realizedCreature is Player player_ && player_.input[0].mp && !player_.input[1].mp)
                        {
                            if (attachedFields.useVanillaPositions || attachedFields.isCentered)
                            {
                                attachedFields.useVanillaPositions = !attachedFields.useVanillaPositions;
                            }
                            attachedFields.followAbstractCreatureID = null; // start a smooth transition
                        }
                        break;
                    case CameraType.Velocity:
                        UpdateCamera_VelocityType(roomCamera, attachedFields);
                        break;
                }
            }
        }

        public static void UpdateCamera_PositionType(RoomCamera roomCamera, in AttachedFields attachedFields)
        {
            //
            // setting up by using attachedFields
            //

            Vector2 targetPosition = attachedFields.onScreenPosition;
            CheckBorders(roomCamera, ref targetPosition); // stop at borders

            float innerCameraBoxX_ = 0.0f; // reach exact position during smooth transitions
            float innerCameraBoxY_ = 0.0f;

            if (attachedFields.followAbstractCreatureID != null) // not in a smooth transition
            {
                // slow down at borders
                innerCameraBoxX_ = targetPosition.x != attachedFields.onScreenPosition.x ? Math.Max(0.0f, innerCameraBoxX - Mathf.Abs(targetPosition.x - attachedFields.onScreenPosition.x)) : innerCameraBoxX;
                innerCameraBoxY_ = targetPosition.y != attachedFields.onScreenPosition.y ? Math.Max(0.0f, innerCameraBoxY - Mathf.Abs(targetPosition.y - attachedFields.onScreenPosition.y)) : innerCameraBoxY;
            }
            else if (cameraType == CameraType.Vanilla && attachedFields.useVanillaPositions)
            {
                // only case where the player is not the target
                // don't check borders for this one
                // seekPos can change during a transition // this extends the transition until the player stops changing screens

                targetPosition = roomCamera.seekPos;
            }

            //
            // clear cut // don't use attachedFields anymore // execute camera logic
            //

            float distanceX = Mathf.Abs(targetPosition.x - roomCamera.lastPos.x);
            if (distanceX > innerCameraBoxX_)
            {
                // the goal is to reach innerCameraBoxX_-close to targetPosition.x
                // the result is the same as:
                // roomCamera.pos.x = Mathf.Lerp(roomCamera.lastPos.x, innerCameraBoxX_-close to targetPosition.x, t = smoothingFactorX);
                roomCamera.pos.x = Mathf.Lerp(roomCamera.lastPos.x, targetPosition.x, smoothingFactorX * (distanceX - innerCameraBoxX_) / distanceX);

                if (Mathf.Abs(roomCamera.pos.x - roomCamera.lastPos.x) < 1f) // move at least one pixel
                {
                    roomCamera.pos.x = Mathf.Lerp(roomCamera.lastPos.x, targetPosition.x, 1f / distanceX);
                }
            }
            else
            {
                roomCamera.pos.x = roomCamera.lastPos.x;
            }

            float distanceY = Mathf.Abs(targetPosition.y - roomCamera.lastPos.y);
            if (distanceY > innerCameraBoxY_)
            {
                roomCamera.pos.y = Mathf.Lerp(Mathf.Lerp(roomCamera.lastPos.y, targetPosition.y, 1f / distanceY), targetPosition.y, smoothingFactorY * (distanceY - innerCameraBoxY_) / distanceY);
                if (Mathf.Abs(roomCamera.pos.y - roomCamera.lastPos.y) < 1f)
                {
                    roomCamera.pos.y = Mathf.Lerp(roomCamera.lastPos.y, targetPosition.y, 1f / distanceY);
                }
            }
            else
            {
                roomCamera.pos.y = roomCamera.lastPos.y;
            }
        }

        public static void UpdateCamera_VanillaType(RoomCamera roomCamera, in AttachedFields attachedFields)
        {
            Vector2 sSize_2 = roomCamera.sSize / 2f;

            float directionX = Mathf.Sign(attachedFields.onScreenPosition.x - attachedFields.vanillaTypePosition.x);
            float distanceX = directionX * (attachedFields.onScreenPosition.x - attachedFields.vanillaTypePosition.x);
            float leanStartDistanceX = 2f * Mathf.Abs(roomCamera.followCreatureInputForward.x);

            if (distanceX > sSize_2.x - outerCameraBoxX)
            {
                // I cannot use ResetCameraPosition() because it sets vanillaTypePosition to onScreenPosition and useVanillaPositions is set to true
                attachedFields.seekPosition.x = 0.0f;
                attachedFields.vanillaTypePosition.x += directionX * (distanceX + sSize_2.x - outerCameraBoxX - 50f); // new distance to the center of the screen: outerCameraBoxX + 50f // leanStartDistanceX can be up to 40f
                roomCamera.lastPos.x = attachedFields.vanillaTypePosition.x; // prevent transition with in-between frames
                roomCamera.pos.x = attachedFields.vanillaTypePosition.x;
            }
            else
            {
                if (distanceX > sSize_2.x - outerCameraBoxX - leanStartDistanceX) // lean effect // 20f is a simplification // vanilla uses 
                {
                    attachedFields.seekPosition.x = directionX * 8f;
                }
                else
                {
                    attachedFields.seekPosition.x *= 0.9f;
                }
                roomCamera.pos.x = Mathf.Lerp(roomCamera.lastPos.x, attachedFields.vanillaTypePosition.x + attachedFields.seekPosition.x, 0.1f); // mimic what vanilla is doing with roomCamera.leanPos in Update()
            }

            float directionY = Mathf.Sign(attachedFields.onScreenPosition.y - attachedFields.vanillaTypePosition.y);
            float distanceY = directionY * (attachedFields.onScreenPosition.y - attachedFields.vanillaTypePosition.y);
            float leanStartDistanceY = 2f * Mathf.Abs(roomCamera.followCreatureInputForward.y);

            if (distanceY > sSize_2.y - outerCameraBoxY)
            {
                attachedFields.seekPosition.y = 0.0f;
                attachedFields.vanillaTypePosition.y += directionY * (distanceY + sSize_2.y - outerCameraBoxY - 50f);
                roomCamera.lastPos.y = attachedFields.vanillaTypePosition.y;
                roomCamera.pos.y = attachedFields.vanillaTypePosition.y;
            }
            else
            {
                if (distanceY > sSize_2.y - outerCameraBoxY - leanStartDistanceY && attachedFields.seekPosition.x < 8f) // vanilla does not do the lean effect for both
                {
                    attachedFields.seekPosition.y = directionY * 8f;
                }
                else
                {
                    attachedFields.seekPosition.y *= 0.9f;
                }
                roomCamera.pos.y = Mathf.Lerp(roomCamera.lastPos.y, attachedFields.vanillaTypePosition.y + attachedFields.seekPosition.y, 0.1f);
            }

            CheckBorders(roomCamera, ref attachedFields.vanillaTypePosition);
            CheckBorders(roomCamera, ref roomCamera.lastPos);
            CheckBorders(roomCamera, ref roomCamera.pos);
        }

        public static void UpdateCamera_VelocityType(RoomCamera roomCamera, in AttachedFields attachedFields)
        {
            float distanceX = Mathf.Abs(attachedFields.onScreenPosition.x - roomCamera.lastPos.x);
            if (distanceX > outerCameraBoxX)
            {
                // makes sure that the camera is exactly outerCameraBoxX-far behind targetPosition // some animation are a bit much speed in very few frames // this can feel jittering
                roomCamera.pos.x = Mathf.Lerp(roomCamera.lastPos.x, attachedFields.onScreenPosition.x, (distanceX - outerCameraBoxX) / distanceX);
            }
            else if (distanceX > innerCameraBoxX && (attachedFields.onScreenPosition.x == attachedFields.lastOnScreenPosition.x || attachedFields.onScreenPosition.x > attachedFields.lastOnScreenPosition.x == attachedFields.onScreenPosition.x > roomCamera.lastPos.x))
            {
                // t(distanceX = innerCameraBoxX) = 0 // don't move at all
                // t(distanceX = outerCameraBoxX) = 1 // move at the same speed as the player
                //float t = (distanceX - innerCameraBoxX) / (outerCameraBoxX - innerCameraBoxX);
                roomCamera.pos.x = Mathf.Lerp(roomCamera.lastPos.x, roomCamera.lastPos.x + attachedFields.onScreenPosition.x - attachedFields.lastOnScreenPosition.x, (distanceX - innerCameraBoxX) / (outerCameraBoxX - innerCameraBoxX));
            }
            else
            {
                roomCamera.pos.x = roomCamera.lastPos.x;
            }

            float distanceY = Mathf.Abs(attachedFields.onScreenPosition.y - roomCamera.lastPos.y);
            if (distanceY > outerCameraBoxY)
            {
                // makes sure that the camera is exactly outerCameraBoxY-far behind targetPosition // some animation are a bit much speed in very few frames // this can feel jittering
                roomCamera.pos.y = Mathf.Lerp(roomCamera.lastPos.y, attachedFields.onScreenPosition.y, (distanceY - outerCameraBoxY) / distanceY);
            }
            else if (distanceY > innerCameraBoxY && (attachedFields.onScreenPosition.y == attachedFields.lastOnScreenPosition.y || attachedFields.onScreenPosition.y > attachedFields.lastOnScreenPosition.y == attachedFields.onScreenPosition.y > roomCamera.lastPos.y))
            {
                // t(distanceY = innerCameraBoxY) = 0 // don't move at all
                // t(distanceY = outerCameraBoxY) = 1 // move at the same speed as the player
                //float t = (distanceY - innerCameraBoxY) / (outerCameraBoxY - innerCameraBoxY);
                roomCamera.pos.y = Mathf.Lerp(roomCamera.lastPos.y, roomCamera.lastPos.y + attachedFields.onScreenPosition.y - attachedFields.lastOnScreenPosition.y, (distanceY - innerCameraBoxY) / (outerCameraBoxY - innerCameraBoxY));
            }
            else
            {
                roomCamera.pos.y = roomCamera.lastPos.y;
            }

            CheckBorders(roomCamera, ref roomCamera.pos);
        }

        // accounts for room boundaries and shortcuts
        public static void UpdateOnScreenPosition(RoomCamera roomCamera)
        {
            if (roomCamera.room == null || roomCamera.followAbstractCreature?.Room != roomCamera.room.abstractRoom)
            {
                return;
            }

            Vector2 position = -roomCamera.sSize / 2f;
            if (roomCamera.followAbstractCreature?.realizedCreature is Player player)
            {
                if (player.inShortcut && ShortcutHandlerMod.GetShortcutVessel(roomCamera.game.shortcuts, roomCamera.followAbstractCreature) is ShortcutHandler.ShortCutVessel shortcutVessel)
                {
                    Vector2 currentPosition = roomCamera.room.MiddleOfTile(shortcutVessel.pos);
                    Vector2 nextInShortcutPosition = roomCamera.room.MiddleOfTile(ShortcutHandler.NextShortcutPosition(shortcutVessel.pos, shortcutVessel.lastPos, roomCamera.room));

                    // shortcuts get only updated every 3 frames => calculate exact position here // in JollyCoopFixesAndStuff it can also be 2 frames in order to remove slowdown, i.e. compensate for the mushroom effect
                    position += Vector2.Lerp(currentPosition, nextInShortcutPosition, roomCamera.game.updateShortCut / maxUpdateShortcut);
                }
                else // use the center (of mass(?)) instead // makes rolls more predictable // use lower y such that crouching does not move camera
                {
                    position.x += 0.5f * (player.bodyChunks[0].pos.x + player.bodyChunks[1].pos.x);
                    position.y += Mathf.Min(player.bodyChunks[0].pos.y, player.bodyChunks[1].pos.y);
                }

                AttachedFields attachedFields = roomCamera.GetAttachedFields();
                attachedFields.lastOnScreenPosition = attachedFields.onScreenPosition;
                attachedFields.onScreenPosition = position;
            }
        }

        public static void UpdateScreen(RoomCamera roomCamera, Vector2 cameraPosition)
        {
            if (roomCamera.room == null)
            {
                return;
            }

            // this is what you see from levelGraphic / levelTexture on screen
            // scroll texture left when moving right and vice versa
            Vector2 startPosition = new(roomCamera.levelGraphic.x, roomCamera.levelGraphic.y);
            Vector2 endPosition = startPosition + new Vector2(roomCamera.levelGraphic.width, roomCamera.levelGraphic.height);

            Shader.SetGlobalVector("_spriteRect", new Vector4((startPosition.x - 0.5f) / roomCamera.sSize.x, (startPosition.y + 0.5f) / roomCamera.sSize.y, (endPosition.x - 0.5f) / roomCamera.sSize.x, (endPosition.y + 0.5f) / roomCamera.sSize.y)); // if the 0.5f is missing then you get black outlines
            Shader.SetGlobalVector("_camInRoomRect", new Vector4(cameraPosition.x / roomCamera.room.PixelWidth, cameraPosition.y / roomCamera.room.PixelHeight, roomCamera.sSize.x / roomCamera.room.PixelWidth, roomCamera.sSize.y / roomCamera.room.PixelHeight));
            Shader.SetGlobalVector("_screenSize", roomCamera.sSize);
        }

        // ---------------- //
        // private function //
        // ---------------- //

        private static Vector2 RoomCamera_ApplyDepth(On.RoomCamera.orig_ApplyDepth orig, RoomCamera roomCamera, Vector2 ps, float depth)
        {
            if (roomCamera.GetAttachedFields().isRoomBlacklisted || roomCamera.voidSeaMode)
            {
                return orig(roomCamera, ps, depth);
            }
            return Custom.ApplyDepthOnVector(ps, roomCamera.pos + new Vector2(700f, 1600f / 3f), depth);
        }

        private static void RoomCamera_ApplyPalette(On.RoomCamera.orig_ApplyPalette orig, RoomCamera roomCamera)
        {
            orig(roomCamera);
            if (roomCamera.fullScreenEffect == null)
            {
                return;
            }
            else if (roomCamera.fullScreenEffect.shader.name == "Fog" && !MainModOptions.isFogFullScreenEffectEnabled.Value || roomCamera.fullScreenEffect.shader.name != "Fog" && !MainModOptions.isOtherFullScreenEffectsEnabled.Value)
            {
                roomCamera.fullScreenEffect.RemoveFromContainer();
                roomCamera.fullScreenEffect = null;
            }
        }

        private static void RoomCamera_ApplyPositionChange(On.RoomCamera.orig_ApplyPositionChange orig, RoomCamera roomCamera)
        {
            // updates currentCameraPosition;
            // resizes the levelTexture automatically (and the corresponding atlas texture);
            // constantly resizing might be a problem (memory fragmentation?)
            // what is the purpose of an atlas?;
            orig(roomCamera);

            // www has a texture too;
            // not sure what exactly happens when www.LoadImageIntoTexture(roomCamera.levelTexture) is called in orig();
            // it probably just removes the reference to www.texture (or rather the old room texture) when it is not needed anymore
            // and waits for the garbage collector to kick in and clean up;
            // unloading it here might slow down memory fragmentation(?);
            //
            // this does increase load time;
            // the glow effect of slugcats takes longer to show;
            // this is slightly annoying;
            //
            // when quickly loading rooms by teleporting this doesn't seem to do much..;
            // given that this has a positive effect when merging; longer play sessions
            // with more garbage generated might benefit from it;
            // Resources.UnloadUnusedAssets();
            // GC.Collect();
            // GC.WaitForPendingFinalizers();
            // GC.Collect();

            // resizes levelGraphic such that the levelTexture fits and is not squashed
            // holy moly don't use roomCamera.www.texture.width, etc. // "WWW.texture property allocates a new Texture2D every time"
            if (roomCamera.levelGraphic.width != roomCamera.levelTexture.width || roomCamera.levelGraphic.height != roomCamera.levelTexture.height)
            {
                roomCamera.levelGraphic.width = roomCamera.levelTexture.width;
                roomCamera.levelGraphic.height = roomCamera.levelTexture.height;
            }

            // if I blacklist too early then the camera might jump in the current room
            if (roomCamera.room == null || blacklistedRooms.Contains(roomCamera.room.abstractRoom.name))
            {
                Debug.Log("SBCameraScroll: The current room is blacklisted.");
                roomCamera.GetAttachedFields().isRoomBlacklisted = true;
            }
            // blacklist instead of checking if you can scroll // they do the same thing anyways
            else if (roomCamera.game.IsArenaSession || roomCamera.game.rainWorld.safariMode || !RoomMod.CanScrollCamera(roomCamera.room))
            {
                roomCamera.GetAttachedFields().isRoomBlacklisted = true;
            }
            else
            {
                roomCamera.GetAttachedFields().isRoomBlacklisted = false;
            }
            ResetCameraPosition(roomCamera); // uses currentCameraPosition and isRoomBlacklisted
        }

        private static void RoomCamera_ctor(On.RoomCamera.orig_ctor orig, RoomCamera roomCamera, RainWorldGame game, int cameraNumber)
        {
            orig(roomCamera, game, cameraNumber);
            allAttachedFields.Add(roomCamera, new AttachedFields());
        }

        // updates all the visual stuff // calls UpdateScreen() // mainly adepts the camera texture to the current (smoothed) position
        private static void RoomCamera_DrawUpdate(On.RoomCamera.orig_DrawUpdate orig, RoomCamera roomCamera, float timeStacker, float timeSpeed)
        {
            // I could make an IL-Hook but I assume then I could not
            // easily turn it off and on again;
            // TODO: try it out
            if (roomCamera.GetAttachedFields().isRoomBlacklisted || roomCamera.voidSeaMode) // || !RoomMod.CanScrollCamera(roomCamera.room) 
            {
                orig(roomCamera, timeStacker, timeSpeed);
                return;
            }

            if (roomCamera.hud != null)
            {
                roomCamera.hud.Draw(timeStacker);
            }

            if (roomCamera.room is not Room room) return;

            if (roomCamera.blizzardGraphics != null && room.blizzardGraphics == null)
            {
                roomCamera.blizzardGraphics.lerpBypass = true;
                room.AddObject(roomCamera.blizzardGraphics);
                room.blizzardGraphics = roomCamera.blizzardGraphics;
                room.blizzard = true;
            }

            if (roomCamera.snowChange || roomCamera.fullscreenSync != Screen.fullScreen)
            {
                if (room.snow)
                {
                    roomCamera.UpdateSnowLight();
                }
                if (roomCamera.blizzardGraphics != null)
                {
                    roomCamera.blizzardGraphics.TileTexUpdate();
                }
            }

            roomCamera.fullscreenSync = Screen.fullScreen;
            roomCamera.virtualMicrophone.DrawUpdate(timeStacker, timeSpeed);
            Vector2 cameraPosition = Vector2.Lerp(roomCamera.lastPos, roomCamera.pos, timeStacker); // makes movement look smoother // adds in-between frames

            if (roomCamera.microShake > 0.0)
            {
                cameraPosition += Custom.RNV() * 8f * roomCamera.microShake * UnityEngine.Random.value;
            }

            // might clamp something when camera shakes
            cameraPosition.x = Mathf.Clamp(cameraPosition.x, cameraPosition.x + roomCamera.hDisplace - 12f, cameraPosition.x + roomCamera.hDisplace + 28f);
            cameraPosition.x = Mathf.Floor(cameraPosition.x) - 0.02f;

            cameraPosition.y = Mathf.Clamp(cameraPosition.y, cameraPosition.y + 1f - (!roomCamera.splitScreenMode ? 0.0f : 192f), cameraPosition.y + 33f + (!roomCamera.splitScreenMode ? 0.0f : 192f));
            cameraPosition.y = Mathf.Floor(cameraPosition.y) - 0.02f;

            cameraPosition += roomCamera.offset;
            cameraPosition += roomCamera.hardLevelGfxOffset;

            roomCamera.levelGraphic.isVisible = true;
            if (roomCamera.backgroundGraphic.isVisible)
            {
                roomCamera.backgroundGraphic.color = Color.Lerp(roomCamera.currentPalette.blackColor, roomCamera.currentPalette.fogColor, roomCamera.currentPalette.fogAmount);
            }

            if (roomCamera.waterLight is WaterLight waterLight)
            {
                if (room.gameOverRoom)
                {
                    waterLight.CleanOut();
                }
                else
                {
                    waterLight.DrawUpdate(cameraPosition);
                }
            }

            for (int spriteLeaserIndex = roomCamera.spriteLeasers.Count - 1; spriteLeaserIndex >= 0; spriteLeaserIndex--)
            {
                roomCamera.spriteLeasers[spriteLeaserIndex].Update(timeStacker, roomCamera, cameraPosition);
                if (roomCamera.spriteLeasers[spriteLeaserIndex].deleteMeNextFrame)
                {
                    roomCamera.spriteLeasers.RemoveAt(spriteLeaserIndex);
                }
            }

            foreach (ISingleCameraDrawable singleCameraDrawable in roomCamera.singleCameraDrawables)
            {
                singleCameraDrawable.Draw(roomCamera, timeStacker, cameraPosition);
            }

            if (room.game.DEBUGMODE)
            {
                roomCamera.levelGraphic.x = 5000f;
            }
            else
            {
                // not sure what this does // seems to visually darken stuff (apply shader or something) when offscreen
                // I think that textureOffset is only needed(?) for compatibility reasons with room.cameraPositions
                Vector2 textureOffset = room.abstractRoom.GetAttachedFields().textureOffset;
                roomCamera.levelGraphic.SetPosition(textureOffset - cameraPosition);
                roomCamera.backgroundGraphic.SetPosition(textureOffset - cameraPosition);
            }

            if (Futile.subjectToAspectRatioIrregularity)
            {
                int num2 = (int)(room.roomSettings.GetEffectAmount(RoomSettings.RoomEffect.Type.PixelShift) * 8f);
                roomCamera.levelGraphic.x -= num2 % 3;
                roomCamera.backgroundGraphic.x -= num2 % 3;
                roomCamera.levelGraphic.y -= num2 / 3;
                roomCamera.backgroundGraphic.y -= num2 / 3;
            }

            roomCamera.shortcutGraphics.Draw(0.0f, cameraPosition);
            UpdateScreen(roomCamera, cameraPosition); // screen texture // update variables for GPU

            // mostly vanilla code
            if (!room.abstractRoom.gate && !room.abstractRoom.shelter)
            {
                float waterLevel = 0.0f;
                if (room.waterObject != null)
                {
                    waterLevel = room.waterObject.fWaterLevel + 100f;
                }
                else if (room.deathFallGraphic != null)
                {
                    waterLevel = room.deathFallGraphic.height + 180f;
                }
                Shader.SetGlobalFloat("_waterLevel", Mathf.InverseLerp(roomCamera.sSize.y, 0.0f, waterLevel - cameraPosition.y));
            }
            else
            {
                Shader.SetGlobalFloat("_waterLevel", 0.0f);
            }

            float lightModifier = 1f;
            if (room.roomSettings.DangerType != RoomRain.DangerType.None)
            {
                lightModifier = room.world.rainCycle.ShaderLight;
            }

            if (room.lightning is Lightning lightning)
            {
                if (!lightning.bkgOnly)
                {
                    lightModifier = lightning.CurrentLightLevel(timeStacker);
                }

                roomCamera.paletteTexture.SetPixel(0, 7, lightning.CurrentBackgroundColor(timeStacker, roomCamera.currentPalette));
                roomCamera.paletteTexture.SetPixel(1, 7, lightning.CurrentFogColor(timeStacker, roomCamera.currentPalette));
                roomCamera.paletteTexture.Apply();
            }

            if (room.roomSettings.Clouds == 0.0f)
            {
                Shader.SetGlobalFloat("_light", 1f);
            }
            else
            {
                Shader.SetGlobalFloat("_light", Mathf.Lerp(Mathf.Lerp(lightModifier, -1f, room.roomSettings.Clouds), -0.4f, roomCamera.ghostMode));
            }

            Shader.SetGlobalFloat("_darkness", 1f - roomCamera.effect_darkness);
            Shader.SetGlobalFloat("_brightness", roomCamera.effect_brightness);
            Shader.SetGlobalFloat("_contrast", 1f + roomCamera.effect_contrast * 2f);

            Shader.SetGlobalFloat("_saturation", 1f - roomCamera.effect_desaturation);
            Shader.SetGlobalFloat("_hue", 360f * roomCamera.effect_hue);
            Shader.SetGlobalFloat("_cloudsSpeed", 1f + 3f * roomCamera.ghostMode);

            if (roomCamera.lightBloomAlphaEffect != RoomSettings.RoomEffect.Type.None)
            {
                roomCamera.lightBloomAlpha = room.roomSettings.GetEffectAmount(roomCamera.lightBloomAlphaEffect);
            }

            if (roomCamera.lightBloomAlphaEffect == RoomSettings.RoomEffect.Type.VoidMelt && roomCamera.fullScreenEffect != null)
            {
                if (room.roomSettings.GetEffectAmount(RoomSettings.RoomEffect.Type.VoidSea) > 0.0f)
                {
                    roomCamera.lightBloomAlpha *= roomCamera.voidSeaGoldFilter;
                    roomCamera.fullScreenEffect.color = new Color(Mathf.InverseLerp(-1200f, -6000f, cameraPosition.y) * Mathf.InverseLerp(0.9f, 0.0f, roomCamera.screenShake), 0.0f, 0.0f);
                    roomCamera.fullScreenEffect.isVisible = roomCamera.lightBloomAlpha > 0.0f;
                }
                else
                {
                    roomCamera.fullScreenEffect.color = new Color(0.0f, 0.0f, 0.0f);
                }
            }

            if (roomCamera.fullScreenEffect != null)
            {
                if (roomCamera.lightBloomAlphaEffect == RoomSettings.RoomEffect.Type.Lightning)
                {
                    roomCamera.fullScreenEffect.alpha = Mathf.InverseLerp(0.0f, 0.2f, roomCamera.lightBloomAlpha) * Mathf.InverseLerp(-0.7f, 0.0f, lightModifier);
                }
                else if (roomCamera.lightBloomAlpha > 0.0f && (roomCamera.lightBloomAlphaEffect == RoomSettings.RoomEffect.Type.Bloom || roomCamera.lightBloomAlphaEffect == RoomSettings.RoomEffect.Type.SkyBloom || roomCamera.lightBloomAlphaEffect == RoomSettings.RoomEffect.Type.SkyAndLightBloom || roomCamera.lightBloomAlphaEffect == RoomSettings.RoomEffect.Type.LightBurn))
                {
                    roomCamera.fullScreenEffect.alpha = roomCamera.lightBloomAlpha * Mathf.InverseLerp(-0.7f, 0.0f, lightModifier);
                }
                else
                {
                    roomCamera.fullScreenEffect.alpha = roomCamera.lightBloomAlpha;
                }
            }

            if (roomCamera.sofBlackFade > 0f)
            {
                Shader.SetGlobalFloat("_darkness", 1f - roomCamera.sofBlackFade);
            }
        }

        // only called when moving camera positions inside the same room // if the ID changed then do a smooth transition instead // the logic for that is done in UpdateCameraPosition()
        private static void RoomCamera_MoveCamera(On.RoomCamera.orig_MoveCamera_int orig, RoomCamera roomCamera, int camPos)
        {
            AttachedFields attachedFields = roomCamera.GetAttachedFields();
            if (attachedFields.isRoomBlacklisted || roomCamera.voidSeaMode || roomCamera.followAbstractCreature == null)
            {
                orig(roomCamera, camPos);
                return;
            }

            roomCamera.currentCameraPosition = camPos;
            if (cameraType == CameraType.Vanilla && attachedFields.useVanillaPositions && attachedFields.followAbstractCreatureID == roomCamera.followAbstractCreature.ID) // camera moves otherwise after vanilla transition since variables are not reset // ignore reset during a smooth transition
            {
                ResetCameraPosition(roomCamera);
            }
        }

        // preloads textures // RoomCamera.ApplyPositionChange() is called when they are ready
        private static void RoomCamera_MoveCamera2(On.RoomCamera.orig_MoveCamera2 orig, RoomCamera roomCamera, string roomName, int camPos)
        {
            // isRoomBlacklisted is not updated yet // needs to be updated in ApplyPositionChange()
            if (roomCamera.game.IsArenaSession || roomCamera.game.rainWorld.safariMode || WorldLoader.FindRoomFile(roomName, false, "_0.png") == null)
            {
                orig(roomCamera, roomName, camPos);
                return;
            }
            orig(roomCamera, roomName, -1);
        }

        private static Color RoomCamera_PixelColorAtCoordinate(On.RoomCamera.orig_PixelColorAtCoordinate orig, RoomCamera roomCamera, Vector2 coord)
        {
            if (roomCamera.GetAttachedFields().isRoomBlacklisted || roomCamera.voidSeaMode)
            {
                return orig(roomCamera, coord);
            }

            // remove effect of roomCamera.CamPos(roomCamera.currentCameraPosition) // color of lights might otherwise "jump" in color
            return orig(roomCamera, coord + roomCamera.CamPos(roomCamera.currentCameraPosition));
        }

        // use roomCamera.pos as reference instead of camPos(..) // seems to be important for unloading graphics and maybe other things
        private static bool RoomCamera_PositionCurrentlyVisible(On.RoomCamera.orig_PositionCurrentlyVisible orig, RoomCamera roomCamera, Vector2 testPos, float margin, bool widescreen)
        {
            if (roomCamera.GetAttachedFields().isRoomBlacklisted || roomCamera.voidSeaMode)
            {
                return orig(roomCamera, testPos, margin, widescreen);
            }
            return testPos.x > roomCamera.pos.x - margin && testPos.x < roomCamera.pos.x + 1400f + margin && testPos.y > roomCamera.pos.y - margin && testPos.y < roomCamera.pos.y + 800f + margin;
        }

        private static bool RoomCamera_PositionVisibleInNextScreen(On.RoomCamera.orig_PositionVisibleInNextScreen orig, RoomCamera roomCamera, Vector2 testPos, float margin, bool widescreen)
        {
            if (roomCamera.GetAttachedFields().isRoomBlacklisted || roomCamera.voidSeaMode)
            {
                return orig(roomCamera, testPos, margin, widescreen);
            }
            return testPos.x > roomCamera.pos.x - 1400f - margin && testPos.x < roomCamera.pos.x + 2800f + margin && testPos.y > roomCamera.pos.y - 800f - margin && testPos.y < roomCamera.pos.y + 1600f + margin;
        }

        private static void RoomCamera_PreLoadTexture(On.RoomCamera.orig_PreLoadTexture orig, RoomCamera roomCamera, Room room, int camPos)
        {
            //this function is only called when moving inside rooms but not between them 
            if (roomCamera.GetAttachedFields().isRoomBlacklisted || roomCamera.voidSeaMode)
            {
                orig(roomCamera, room, camPos);
            }
        }

        private static void RoomCamera_ScreenMovement(On.RoomCamera.orig_ScreenMovement orig, RoomCamera roomCamera, Vector2? sourcePos, Vector2 bump, float shake)
        {
            // should remove effects on camera like camera shakes caused by other creatures // feels weird otherwise
            if (roomCamera.GetAttachedFields().isRoomBlacklisted || roomCamera.voidSeaMode)
            {
                orig(roomCamera, sourcePos, bump, shake);
            }
        }

        // updated physics related things like the camera position
        private static void RoomCamera_Update(On.RoomCamera.orig_Update orig, RoomCamera roomCamera)
        {
            orig(roomCamera); // updates isRoomBlacklisted
            if (roomCamera.GetAttachedFields().isRoomBlacklisted || roomCamera.voidSeaMode) return; // don't smooth the camera position in the void sea // treat void sea as being blacklisted // && RoomMod.CanScrollCamera(roomCamera.room) TODO
            UpdateCameraPosition(roomCamera);
        }

        //
        //
        //

        public sealed class AttachedFields
        {
            public bool isCentered = false;
            public bool isRoomBlacklisted = false;
            public bool useVanillaPositions = false; // for vanilla type camera

            public EntityID? followAbstractCreatureID = null;
            public EntityID? transitionTrackingID = null;

            public Vector2 lastOnScreenPosition = new();
            public Vector2 onScreenPosition = new();
            public Vector2 seekPosition = new();
            public Vector2 vanillaTypePosition = new();
        }
    }
}