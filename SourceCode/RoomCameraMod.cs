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
        // parameters
        public static CameraType cameraType = CameraType.Position;
        public static float innerCameraBoxX = 0.0f; // don't move camera when player is too close
        public static float innerCameraBoxY = 0.0f; // set default values in option menu
        public static float outerCameraBoxX = 0.0f; // the camera will always be at least this close
        public static float outerCameraBoxY = 0.0f;
        public static float smoothingFactorX = 0.0f; // set default values in option menu
        public static float smoothingFactorY = 0.0f;

        // parameters
        public static float maxUpdateShortcut = 3f;
        public static List<string> blacklistedRooms = new List<string>();

        // properties // vectors with size camera count
        public static AbstractCreature?[] followAbstractCreature = new AbstractCreature?[0];
        public static Vector2[] onScreenPosition = new Vector2[0];
        public static Vector2[] lastOnScreenPosition = new Vector2[0];
        public static Vector2[] vanillaTypePosition = new Vector2[0];

        // properties // vectors with size camera count
        public static bool[] isRoomBlacklisted = new bool[0];
        public static bool[] useVanillaPositions = new bool[0]; // for vanilla type camera
        public static bool[] isCentered = new bool[0];

        internal static void OnEnable()
        {
            On.RoomCamera.ApplyDepth += RoomCamera_ApplyDepth;
            On.RoomCamera.ApplyPalette += RoomCamera_ApplyPalette;
            On.RoomCamera.ApplyPositionChange += RoomCamera_ApplyPositionChange;
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

            Vector2 textureOffset = roomCamera.room.abstractRoom.GetAttachedFields().textureOffset;
            if (position.x >= roomCamera.levelGraphic.width - roomCamera.sSize.x + textureOffset.x) // stop position at room texture borders // probably works with room.PixelWidth - roomCamera.sSize.x / 2f instead as well
            {
                position.x = roomCamera.levelGraphic.width - roomCamera.sSize.x + textureOffset.x;
            }
            else if (position.x <= textureOffset.x)
            {
                position.x = textureOffset.x;
            }

            if (position.y >= roomCamera.levelGraphic.height - roomCamera.sSize.y + textureOffset.y - 18f) // not sure why I have to decrease positionY by a constant // I picked 18f bc roomCamera.seekPos.y gets changed by 18f in Update() // seems to work , i.e. I don't see black bars
            {
                position.y = roomCamera.levelGraphic.height - roomCamera.sSize.y + textureOffset.y - 18f;
            }
            else if (position.y <= textureOffset.y)
            {
                position.y = textureOffset.y;
            }
        }

        public static void ResetCameraPosition(RoomCamera roomCamera)
        {
            int cameraNumber = roomCamera.cameraNumber;
            followAbstractCreature[cameraNumber] = null; // do a smooth transition // this actually makes a difference for the vanilla type camera // otherwise the map input would immediately be processed

            if (isRoomBlacklisted[cameraNumber] || !RoomMod.CanScrollCamera(roomCamera.room) || roomCamera.voidSeaMode)
            {
                roomCamera.seekPos = roomCamera.CamPos(roomCamera.currentCameraPosition);
                roomCamera.seekPos.x += roomCamera.hDisplace + 8f;
                roomCamera.seekPos.y += 18f;
                roomCamera.leanPos *= 0.0f;

                roomCamera.lastPos = roomCamera.seekPos;
                roomCamera.pos = roomCamera.seekPos;
                return;
            }

            UpdateOnScreenPosition(roomCamera);
            CheckBorders(roomCamera, ref onScreenPosition[cameraNumber]); // do not move past room boundaries

            if (cameraType == CameraType.Vanilla)
            {
                roomCamera.seekPos = roomCamera.CamPos(roomCamera.currentCameraPosition);
                roomCamera.seekPos.x += roomCamera.hDisplace + 8f;
                roomCamera.seekPos.y += 18f;
                roomCamera.leanPos *= 0.0f;

                // center camera on vanilla position
                roomCamera.lastPos = roomCamera.seekPos;
                roomCamera.pos = roomCamera.seekPos;

                vanillaTypePosition[cameraNumber] = onScreenPosition[cameraNumber];
                useVanillaPositions[cameraNumber] = true;
                isCentered[cameraNumber] = false;
            }
            else
            {
                // center camera on player
                roomCamera.lastPos = onScreenPosition[cameraNumber];
                roomCamera.pos = onScreenPosition[cameraNumber];
            }
        }

        public static void SmoothCameraXY_Position(ref float cameraPosition, float lastCameraPosition, float onScreenPosition, float smoothingFactor, float innerCameraBox)
        {
            float distance = Mathf.Abs(onScreenPosition - lastCameraPosition);
            if (distance > innerCameraBox)
            {
                // the goal is to reach innerCameraBox-close to onScreenPosition
                // the result is the same as:
                // cameraPosition = Mathf.Lerp(lastCameraPosition, innerCameraBox-close to onScreenPosition, t = smoothingFactor);
                cameraPosition = Mathf.Lerp(lastCameraPosition, onScreenPosition, smoothingFactor * (distance - innerCameraBox) / distance);
            }
            else
            {
                cameraPosition = lastCameraPosition;
            }
        }

        public static void SmoothCameraXY_Vanilla(ref float cameraPosition, ref float lastCameraPosition, ref float vanillaTypePosition, float onScreenPosition, float outerCameraBox, float innerCameraBox)
        {
            float direction = Mathf.Sign(onScreenPosition - vanillaTypePosition);
            float distance = direction * (onScreenPosition - vanillaTypePosition);

            if (distance > outerCameraBox)
            {
                vanillaTypePosition += direction * (distance + innerCameraBox); // new distance is equal to innerCameraBox
                lastCameraPosition = vanillaTypePosition; // prevent transition with in-between frames
                cameraPosition = vanillaTypePosition;
            }
            else if (distance > innerCameraBox)
            {
                cameraPosition = Mathf.Lerp(vanillaTypePosition, vanillaTypePosition + direction * (outerCameraBox - innerCameraBox), (distance - innerCameraBox) / (outerCameraBox - innerCameraBox));
            }
            else
            {
                cameraPosition = vanillaTypePosition;
            }
        }

        public static void SmoothCameraXY_Velocity(ref float cameraPosition, float lastCameraPosition, float onScreenPosition, float lastOnScreenPosition, float outerCameraBox, float innerCameraBox)
        {
            float distance = Mathf.Abs(onScreenPosition - lastCameraPosition);
            if (distance > outerCameraBox)
            {
                // makes sure that the camera is exactly outerCameraBox-far behind targetPosition // some animation are a bit much speed in very few frames // this can feel jittering
                cameraPosition = Mathf.Lerp(lastCameraPosition, onScreenPosition, (distance - outerCameraBox) / distance);
            }
            else if (distance > innerCameraBox && (onScreenPosition == lastOnScreenPosition || onScreenPosition > lastOnScreenPosition == onScreenPosition > lastCameraPosition))
            {
                // t(distance = innerCameraBox) = 0 // don't move at all
                // t(distance = outerCameraBox) = 1 // move at the same speed as the player
                //float t = (distance - innerCameraBox) / (outerCameraBox - innerCameraBox);
                cameraPosition = Mathf.Lerp(lastCameraPosition, lastCameraPosition + onScreenPosition - lastOnScreenPosition, (distance - innerCameraBox) / (outerCameraBox - innerCameraBox));
            }
            else
            {
                cameraPosition = lastCameraPosition;
            }
        }

        // expanding camera logic from bee's CameraScroll mod
        public static void UpdateCameraPosition(RoomCamera roomCamera)
        {
            if (roomCamera.followAbstractCreature == null || roomCamera.room == null)
            {
                return;
            }

            UpdateOnScreenPosition(roomCamera);
            int cameraNumber = roomCamera.cameraNumber;

            if (followAbstractCreature[cameraNumber] != roomCamera.followAbstractCreature) // smooth transition when switching cameras in the same room
            {
                followAbstractCreature[cameraNumber] = null; // keep transition going even when switching back
                if (useVanillaPositions[cameraNumber])
                {
                    SmoothCameraXY_Position(ref roomCamera.pos.x, roomCamera.lastPos.x, roomCamera.seekPos.x, smoothingFactorX, 0.0f);
                    SmoothCameraXY_Position(ref roomCamera.pos.y, roomCamera.lastPos.y, roomCamera.seekPos.y, smoothingFactorY, 0.0f);
                }
                else
                {
                    Vector2 targetPosition = onScreenPosition[cameraNumber];
                    CheckBorders(roomCamera, ref targetPosition);
                    SmoothCameraXY_Position(ref roomCamera.pos.x, roomCamera.lastPos.x, targetPosition.x, smoothingFactorX, 0.0f);
                    SmoothCameraXY_Position(ref roomCamera.pos.y, roomCamera.lastPos.y, targetPosition.y, smoothingFactorY, 0.0f);
                }

                float minimumVelocityX = Mathf.Abs(onScreenPosition[cameraNumber].x - lastOnScreenPosition[cameraNumber].x) > 1f ? 10f : 1f;
                float minimumVelocityY = Mathf.Abs(onScreenPosition[cameraNumber].y - lastOnScreenPosition[cameraNumber].y) > 1f ? 10f : 1f;

                // stop transition earlier when player is moving
                if (Math.Abs(roomCamera.pos.x - roomCamera.lastPos.x) <= minimumVelocityX && Math.Abs(roomCamera.pos.y - roomCamera.lastPos.y) <= minimumVelocityY)
                {
                    followAbstractCreature[cameraNumber] = roomCamera.followAbstractCreature;
                    vanillaTypePosition[cameraNumber] = roomCamera.pos;
                    isCentered[cameraNumber] = true; // for vanilla type only
                }
            }
            else if (cameraType == 0) // position type
            {
                Vector2 borderPosition = onScreenPosition[cameraNumber];
                CheckBorders(roomCamera, ref borderPosition);

                if (borderPosition == onScreenPosition[cameraNumber])
                {
                    SmoothCameraXY_Position(ref roomCamera.pos.x, roomCamera.lastPos.x, onScreenPosition[cameraNumber].x, smoothingFactorX, innerCameraBoxX);
                    SmoothCameraXY_Position(ref roomCamera.pos.y, roomCamera.lastPos.y, onScreenPosition[cameraNumber].y, smoothingFactorY, innerCameraBoxY);
                }
                else // slow down at borders
                {
                    SmoothCameraXY_Position(ref roomCamera.pos.x, roomCamera.lastPos.x, borderPosition.x, smoothingFactorX, Math.Max(0, innerCameraBoxX - Math.Abs(borderPosition.x - onScreenPosition[cameraNumber].x)));
                    SmoothCameraXY_Position(ref roomCamera.pos.y, roomCamera.lastPos.y, borderPosition.y, smoothingFactorY, Math.Max(0, innerCameraBoxY - Math.Abs(borderPosition.y - onScreenPosition[cameraNumber].y)));
                }
                CheckBorders(roomCamera, ref roomCamera.pos);
            }
            else if (cameraType == CameraType.Velocity)
            {
                SmoothCameraXY_Velocity(ref roomCamera.pos.x, roomCamera.lastPos.x, onScreenPosition[cameraNumber].x, lastOnScreenPosition[cameraNumber].x, outerCameraBoxX, innerCameraBoxX);
                SmoothCameraXY_Velocity(ref roomCamera.pos.y, roomCamera.lastPos.y, onScreenPosition[cameraNumber].y, lastOnScreenPosition[cameraNumber].y, outerCameraBoxY, innerCameraBoxY);
                CheckBorders(roomCamera, ref roomCamera.pos);
            }
            else // vanilla type
            {
                if (isCentered[cameraNumber] && (Math.Abs(onScreenPosition[cameraNumber].x - lastOnScreenPosition[cameraNumber].x) > 1f || Math.Abs(onScreenPosition[cameraNumber].y - lastOnScreenPosition[cameraNumber].y) > 1f))
                {
                    isCentered[cameraNumber] = false;
                }

                if (!useVanillaPositions[cameraNumber])
                {
                    SmoothCameraXY_Vanilla(ref roomCamera.pos.x, ref roomCamera.lastPos.x, ref vanillaTypePosition[cameraNumber].x, onScreenPosition[cameraNumber].x, roomCamera.sSize.x / 2f - outerCameraBoxX, roomCamera.sSize.x / 2f - innerCameraBoxX);
                    SmoothCameraXY_Vanilla(ref roomCamera.pos.y, ref roomCamera.lastPos.y, ref vanillaTypePosition[cameraNumber].y, onScreenPosition[cameraNumber].y, roomCamera.sSize.y / 2f - outerCameraBoxY, roomCamera.sSize.y / 2f - innerCameraBoxY);

                    CheckBorders(roomCamera, ref vanillaTypePosition[cameraNumber]);
                    CheckBorders(roomCamera, ref roomCamera.lastPos);
                    CheckBorders(roomCamera, ref roomCamera.pos);
                }

                if (roomCamera.followAbstractCreature?.realizedCreature is Player player_ && player_.input[0].mp && !player_.input[1].mp)
                {
                    if (useVanillaPositions[cameraNumber] || isCentered[cameraNumber])
                    {
                        useVanillaPositions[cameraNumber] = !useVanillaPositions[cameraNumber];
                    }
                    followAbstractCreature[cameraNumber] = null; // start a smooth transition
                }
            }
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

                int cameraNumber = roomCamera.cameraNumber;
                lastOnScreenPosition[cameraNumber] = onScreenPosition[cameraNumber];
                onScreenPosition[cameraNumber] = position;
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
            Vector2 startPosition = new Vector2(roomCamera.levelGraphic.x, roomCamera.levelGraphic.y);
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
            if (isRoomBlacklisted[roomCamera.cameraNumber] || roomCamera.voidSeaMode)
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
            else if (roomCamera.fullScreenEffect.shader.name == "Fog" && !MainMod.isFogFullScreenEffectOptionEnabled || roomCamera.fullScreenEffect.shader.name != "Fog" && !MainMod.isOtherFullScreenEffectsOptionEnabled)
            {
                roomCamera.fullScreenEffect.RemoveFromContainer();
                roomCamera.fullScreenEffect = null;
            }
        }

        private static void RoomCamera_ApplyPositionChange(On.RoomCamera.orig_ApplyPositionChange orig, RoomCamera roomCamera)
        {
            orig(roomCamera); // updates currentCameraPosition // resizes the levelTexture automatically (and the corresponding atlas texture) // what is the purpose of an atlas?

            // resizes levelGraphic such that the levelTexture fits and is not squashed
            // holy moly don't use roomCamera.www.texture.width, etc. // "WWW.texture property allocates a new Texture2D every time"
            if (roomCamera.levelGraphic.width != roomCamera.levelTexture.width || roomCamera.levelGraphic.height != roomCamera.levelTexture.height)
            {
                roomCamera.levelGraphic.width = roomCamera.levelTexture.width;
                roomCamera.levelGraphic.height = roomCamera.levelTexture.height;
            }

            isRoomBlacklisted[roomCamera.cameraNumber] = roomCamera.room == null || blacklistedRooms.Contains(roomCamera.room.abstractRoom.name);
            ResetCameraPosition(roomCamera); // uses currentCameraPosition

            if (isRoomBlacklisted[roomCamera.cameraNumber])
            {
                Debug.Log("SBCameraScroll: The current room is blacklisted.");
            }
        }

        // updates all the visual stuff // calls UpdateScreen() // mainly adepts the camera texture to the current (smoothed) position
        private static void RoomCamera_DrawUpdate(On.RoomCamera.orig_DrawUpdate orig, RoomCamera roomCamera, float timeStacker, float timeSpeed)
        {
            if (isRoomBlacklisted[roomCamera.cameraNumber] || !RoomMod.CanScrollCamera(roomCamera.room) || roomCamera.voidSeaMode)
            {
                orig(roomCamera, timeStacker, timeSpeed);
                return;
            }

            if (roomCamera.hud != null)
            {
                roomCamera.hud.Draw(timeStacker);
            }

            if (roomCamera.room is Room room)
            {
                roomCamera.virtualMicrophone.DrawUpdate(timeStacker, timeSpeed);
                Vector2 cameraPosition = Vector2.Lerp(roomCamera.lastPos, roomCamera.pos, timeStacker); // makes movement look smoother // adds in-between frames

                if (roomCamera.microShake > 0.0)
                {
                    cameraPosition += Custom.RNV() * 8f * roomCamera.microShake * UnityEngine.Random.value;
                }

                cameraPosition.x = Mathf.Clamp(cameraPosition.x, cameraPosition.x + roomCamera.hDisplace - 12f, cameraPosition.x + roomCamera.hDisplace + 28f);
                cameraPosition.x = Mathf.Floor(cameraPosition.x) - 0.02f;

                cameraPosition.y = Mathf.Clamp(cameraPosition.y, cameraPosition.y + 1f - (!roomCamera.splitScreenMode ? 0.0f : 192f), cameraPosition.y + 33f + (!roomCamera.splitScreenMode ? 0.0f : 192f));
                cameraPosition.y = Mathf.Floor(cameraPosition.y) - 0.02f;

                roomCamera.levelGraphic.isVisible = true;
                //cameraPosition += roomCamera.offset; // offset might be needed for multiple cameras // not used by SplitScreenMod

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

                roomCamera.shortcutGraphics.Draw(0.0f, cameraPosition);
                UpdateScreen(roomCamera, cameraPosition); // update visible screen texture

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

                Shader.SetGlobalFloat("_cloudsSpeed", (1f + 3f * roomCamera.ghostMode));
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

                if (roomCamera.fullScreenEffect == null)
                {
                    return;
                }

                if (roomCamera.lightBloomAlphaEffect == RoomSettings.RoomEffect.Type.Lightning)
                {
                    roomCamera.fullScreenEffect.alpha = Mathf.InverseLerp(0.0f, 0.2f, roomCamera.lightBloomAlpha) * Mathf.InverseLerp(-0.7f, 0.0f, lightModifier);
                }
                else if (roomCamera.lightBloomAlpha > 0.0f && (roomCamera.lightBloomAlphaEffect == RoomSettings.RoomEffect.Type.Bloom || roomCamera.lightBloomAlphaEffect == RoomSettings.RoomEffect.Type.SkyBloom || (roomCamera.lightBloomAlphaEffect == RoomSettings.RoomEffect.Type.SkyAndLightBloom || roomCamera.lightBloomAlphaEffect == RoomSettings.RoomEffect.Type.LightBurn)))
                {
                    roomCamera.fullScreenEffect.alpha = roomCamera.lightBloomAlpha * Mathf.InverseLerp(-0.7f, 0.0f, lightModifier);
                }
                else
                {
                    roomCamera.fullScreenEffect.alpha = roomCamera.lightBloomAlpha;
                }
            }
        }

        // only called when moving camera positions inside the same room // if the ID changed then do a smooth transition instead // the logic for that is done in UpdateCameraPosition()
        private static void RoomCamera_MoveCamera(On.RoomCamera.orig_MoveCamera_int orig, RoomCamera roomCamera, int camPos)
        {
            int cameraNumber = roomCamera.cameraNumber;
            if (isRoomBlacklisted[cameraNumber] || roomCamera.voidSeaMode || roomCamera.followAbstractCreature == null)
            {
                orig(roomCamera, camPos);
                return;
            }

            roomCamera.currentCameraPosition = camPos;
            if (useVanillaPositions[cameraNumber] && followAbstractCreature[cameraNumber] == roomCamera.followAbstractCreature) // camera moves otherwise after vanilla transition since variables are not reset // ignore reset during a smooth transition
            {
                ResetCameraPosition(roomCamera);
            }
        }

        // preloads textures // RoomCamera.ApplyPositionChange() is called when they are ready
        private static void RoomCamera_MoveCamera2(On.RoomCamera.orig_MoveCamera2 orig, RoomCamera roomCamera, string requestedTexture)
        {
            if (roomCamera.game.IsArenaSession)
            {
                orig(roomCamera, requestedTexture);
                return;
            }

            string relativeFilePath = "World" + requestedTexture.Split(new string[] { Path.DirectorySeparatorChar + "World" }, StringSplitOptions.None)[1];
            string[] splittedFilePath = relativeFilePath.Split(new char[] { '_' });
            relativeFilePath = relativeFilePath.Split(new string[] { "_" + splittedFilePath[splittedFilePath.Length - 1] }, StringSplitOptions.None)[0]; // remove camera number at the end // example: LF_ABC_1 becomes LF_ABC

            // check if file without camera number exists // if not use vanilla
            string filePath = MainMod.modDirectoryPath + relativeFilePath + ".png";
            if (File.Exists(filePath))
            {
                requestedTexture = "file:///" + filePath;
            }

            // I forgot // why do I check for merged(?) textures in vanilla folders? // vanilla files always have the format LF_ABC_1(?) // maybe this helps when you directly merge custom regions with vanilla folder // I saw a custom room without a number at the end iirc
            filePath = Custom.RootFolderDirectory() + relativeFilePath + ".png"; // it does not hurt I guess
            if (File.Exists(filePath))
            {
                requestedTexture = "file:///" + filePath;
            }
            orig(roomCamera, requestedTexture);
        }

        private static Color RoomCamera_PixelColorAtCoordinate(On.RoomCamera.orig_PixelColorAtCoordinate orig, RoomCamera roomCamera, Vector2 coord)
        {
            if (isRoomBlacklisted[roomCamera.cameraNumber] || roomCamera.voidSeaMode)
            {
                return orig(roomCamera, coord);
            }

            // remove effect of roomCamera.CamPos(roomCamera.currentCameraPosition) // color of lights might otherwise "jump" in color
            return orig(roomCamera, coord + roomCamera.CamPos(roomCamera.currentCameraPosition));
        }

        // use roomCamera.pos as reference instead of camPos(..) // seems to be important for unloading graphics and maybe other things
        private static bool RoomCamera_PositionCurrentlyVisible(On.RoomCamera.orig_PositionCurrentlyVisible orig, RoomCamera roomCamera, Vector2 testPos, float margin, bool widescreen)
        {
            if (isRoomBlacklisted[roomCamera.cameraNumber] || roomCamera.voidSeaMode)
            {
                return orig(roomCamera, testPos, margin, widescreen);
            }
            return testPos.x > roomCamera.pos.x - margin && testPos.x < roomCamera.pos.x + 1400f + margin && testPos.y > roomCamera.pos.y - margin && testPos.y < roomCamera.pos.y + 800f + margin;
        }

        private static bool RoomCamera_PositionVisibleInNextScreen(On.RoomCamera.orig_PositionVisibleInNextScreen orig, RoomCamera roomCamera, Vector2 testPos, float margin, bool widescreen)
        {
            if (isRoomBlacklisted[roomCamera.cameraNumber] || roomCamera.voidSeaMode)
            {
                return orig(roomCamera, testPos, margin, widescreen);
            }
            return testPos.x > roomCamera.pos.x - 1400f - margin && testPos.x < roomCamera.pos.x + 2800f + margin && testPos.y > roomCamera.pos.y - 800f - margin && testPos.y < roomCamera.pos.y + 1600f + margin;
        }

        private static void RoomCamera_PreLoadTexture(On.RoomCamera.orig_PreLoadTexture orig, RoomCamera roomCamera, Room room, int camPos)
        {
            //this function is only called when moving inside rooms but not between them 
            if (isRoomBlacklisted[roomCamera.cameraNumber] || roomCamera.voidSeaMode)
            {
                orig(roomCamera, room, camPos);
            }
        }

        private static void RoomCamera_ScreenMovement(On.RoomCamera.orig_ScreenMovement orig, RoomCamera roomCamera, Vector2? sourcePos, Vector2 bump, float shake)
        {
            // should remove effects on camera like camera shakes caused by other creatures // feels weird otherwise
            if (isRoomBlacklisted[roomCamera.cameraNumber] || roomCamera.voidSeaMode)
            {
                orig(roomCamera, sourcePos, bump, shake);
            }
        }

        // updated physics related things like the camera position
        private static void RoomCamera_Update(On.RoomCamera.orig_Update orig, RoomCamera roomCamera)
        {
            orig(roomCamera); // updates isRoomBlacklisted
            if (!isRoomBlacklisted[roomCamera.cameraNumber] && RoomMod.CanScrollCamera(roomCamera.room) && !roomCamera.voidSeaMode) // don't smooth the camera position in the void sea // treat void sea as being blacklisted
            {
                UpdateCameraPosition(roomCamera);
            }
        }
    }
}