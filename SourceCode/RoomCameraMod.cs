using System;
using System.Collections.Generic;
using Expedition;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RWCustom;
using UnityEngine;

namespace SBCameraScroll
{
    public enum CameraType
    {
        Position,
        Vanilla
    }

    public static class RoomCameraMod
    {
        //
        // parameters
        //

        public static CameraType camera_type = CameraType.Position;

        public static float smoothing_factor_x = 0.16f;
        public static float smoothing_factor_y = 0.16f;

        // used in CoopTweaks; don't rename;
        public static float maxUpdateShortcut = 3f;
        public static List<string> blacklisted_rooms = new() { "RM_AI", "GW_ARTYSCENES", "GW_ARTYNIGHTMARE", "SB_E05SAINT" };

        //
        // variables
        //

        internal static readonly Dictionary<RoomCamera, AttachedFields> all_attached_fields = new();
        public static AttachedFields GetAttachedFields(this RoomCamera roomCamera) => all_attached_fields[roomCamera];

        //
        //
        //

        internal static void OnEnable()
        {
            IL.RoomCamera.DrawUpdate += IL_RoomCamera_DrawUpdate;
            IL.RoomCamera.Update += IL_RoomCamera_Update;
            // On.RoomCamera.Update += RoomCamera_Update;

            On.RoomCamera.ApplyDepth += RoomCamera_ApplyDepth;
            On.RoomCamera.ApplyPalette += RoomCamera_ApplyPalette;
            On.RoomCamera.ApplyPositionChange += RoomCamera_ApplyPositionChange;
            On.RoomCamera.ctor += RoomCamera_ctor;

            On.RoomCamera.IsViewedByCameraPosition += RoomCamera_IsViewedByCameraPosition;
            On.RoomCamera.IsVisibleAtCameraPosition += RoomCamera_IsVisibleAtCameraPosition;
            On.RoomCamera.MoveCamera_int += RoomCamera_MoveCamera;
            On.RoomCamera.MoveCamera2 += RoomCamera_MoveCamera2;

            On.RoomCamera.PixelColorAtCoordinate += RoomCamera_PixelColorAtCoordinate;
            On.RoomCamera.PositionCurrentlyVisible += RoomCamera_PositionCurrentlyVisible;
            On.RoomCamera.PositionVisibleInNextScreen += RoomCamera_PositionVisibleInNextScreen;

            On.RoomCamera.PreLoadTexture += RoomCamera_PreLoadTexture;
            On.RoomCamera.RectCurrentlyVisible += RoomCamera_RectCurrentlyVisible;
            On.RoomCamera.ScreenMovement += RoomCamera_ScreenMovement;
        }

        //
        // public
        //

        public static void AddFadeTransition(RoomCamera roomCamera)
        {
            if (roomCamera.GetAttachedFields().isRoomBlacklisted || roomCamera.voidSeaMode) return;

            if (roomCamera.room is not Room room) return;
            if (room.roomSettings.fadePalette == null) return;
            // if (!MainMod.Option_PaletteFade) return;

            // the day-night fade effect does not update paletteBlend in all cases;
            // so this can otherwise reset it sometimes;
            // priotize day-night over this;
            if ((roomCamera.effect_dayNight > 0f && room.world.rainCycle.timer >= room.world.rainCycle.cycleLength) || (ModManager.Expedition && room.game.rainWorld.ExpeditionMode && ExpeditionGame.activeUnlocks.Contains("bur-blinded"))) return;

            // the fade is automatically applied in RoomCamera.Update();
            roomCamera.paletteBlend = Mathf.Lerp(roomCamera.paletteBlend, roomCamera.room.roomSettings.fadePalette.fades[roomCamera.currentCameraPosition], 0.01f);
        }

        public static void CheckBorders(RoomCamera roomCamera, ref Vector2 position)
        {
            if (roomCamera.room == null) return;

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

        public static Vector2 GetCreaturePosition(Creature creature)
        {
            if (creature is Player player)
            {
                // reduce movement when "rolling" in place in ZeroG;
                if (player.room?.gravity == 0.0f || player.animation == Player.AnimationIndex.Roll)
                {
                    return 0.5f * (player.bodyChunks[0].pos + player.bodyChunks[1].pos);
                }
                else
                {
                    // use the center (of mass(?)) instead 
                    // makes rolls more predictable 
                    // use lower y such that crouching does not move camera
                    return new()
                    {
                        x = 0.5f * (player.bodyChunks[0].pos.x + player.bodyChunks[1].pos.x),
                        y = Mathf.Min(player.bodyChunks[0].pos.y, player.bodyChunks[1].pos.y)
                    };
                }
            }
            // otherwise when the overseer jumps back and forth the camera would move as well;
            // I consider this a bug;
            // the overseer should not jump around when focusing on a shortcut;
            // because the audio stops playing as well;
            else if (creature.abstractCreature.abstractAI is OverseerAbstractAI abstractAI && abstractAI.safariOwner && abstractAI.doorSelectionIndex != -1)
            {
                return abstractAI.parent.Room.realizedRoom.MiddleOfTile(abstractAI.parent.Room.realizedRoom.ShortcutLeadingToNode(abstractAI.doorSelectionIndex).startCoord);
            }
            return creature.mainBodyChunk.pos;
        }

        public static void ResetCameraPosition(RoomCamera roomCamera)
        {
            AttachedFields attachedFields = roomCamera.GetAttachedFields();

            // vanilla copy & paste stuff
            if (attachedFields.isRoomBlacklisted || roomCamera.voidSeaMode)
            {
                roomCamera.seekPos = roomCamera.CamPos(roomCamera.currentCameraPosition);
                roomCamera.seekPos.x += roomCamera.hDisplace + 8f;
                roomCamera.seekPos.y += 18f;
                roomCamera.leanPos *= 0.0f;

                roomCamera.lastPos = roomCamera.seekPos;
                roomCamera.pos = roomCamera.seekPos;
                return;
            }
            attachedFields.typeCamera.Reset();
        }

        public static Vector2 SplitScreenMod_GetScreenOffset(in Vector2 screenSize) => SplitScreenCoop.SplitScreenCoop.CurrentSplitMode switch
        {
            SplitScreenCoop.SplitScreenCoop.SplitMode.SplitVertical => new Vector2(0.25f * screenSize.x, 0.0f),
            SplitScreenCoop.SplitScreenCoop.SplitMode.SplitHorizontal => new Vector2(0.0f, 0.25f * screenSize.y),
            _ => new Vector2(),
        };

        // accounts for room boundaries and shortcuts
        public static void UpdateOnScreenPosition(RoomCamera roomCamera)
        {
            if (roomCamera.room == null) return;
            if (roomCamera.followAbstractCreature == null) return;
            if (roomCamera.followAbstractCreature.Room != roomCamera.room.abstractRoom) return;
            if (roomCamera.followAbstractCreature.realizedCreature is not Creature creature) return;

            AttachedFields attachedFields = roomCamera.GetAttachedFields();
            Vector2 position = -roomCamera.sSize / 2f;

            if (creature.inShortcut && ShortcutHandlerMod.GetShortcutVessel(roomCamera.game.shortcuts, roomCamera.followAbstractCreature) is ShortcutHandler.ShortCutVessel shortcutVessel)
            {
                Vector2 currentPosition = roomCamera.room.MiddleOfTile(shortcutVessel.pos);
                Vector2 nextInShortcutPosition = roomCamera.room.MiddleOfTile(ShortcutHandler.NextShortcutPosition(shortcutVessel.pos, shortcutVessel.lastPos, roomCamera.room));

                // shortcuts get only updated every 3 frames => calculate exact position here // in JollyCoopFixesAndStuff it can also be 2 frames in order to remove slowdown, i.e. compensate for the mushroom effect
                position += Vector2.Lerp(currentPosition, nextInShortcutPosition, roomCamera.game.updateShortCut / maxUpdateShortcut);
            }
            else // use the center (of mass(?)) instead // makes rolls more predictable // use lower y such that crouching does not move camera
            {
                position += GetCreaturePosition(creature);
            }

            attachedFields.lastOnScreenPosition = attachedFields.onScreenPosition;
            attachedFields.onScreenPosition = position;
        }

        //
        // private
        //

        private static void IL_RoomCamera_DrawUpdate(ILContext context)
        {
            // MainMod.LogAllInstructions(context);

            //
            // the first four instance remove the clamping;
            // otherwise you can't scroll past the current screen;
            //

            ILCursor cursor = new(context);
            if (cursor.TryGotoNext(instruction => instruction.MatchCall<RoomCamera>("CamPos")))
            {
                Debug.Log("SBCameraScroll: IL_RoomCamera_DrawUpdate_1: Index " + cursor.Index); // before: 100 // after: 100
                cursor.Goto(cursor.Index - 2);
                cursor.RemoveRange(3); // remove CamPos(currentCameraPosition)

                cursor.Emit(OpCodes.Ldloc_1);
                cursor.EmitDelegate<Func<RoomCamera, Vector2, Vector2>>((roomCamera, cameraPosition) =>
               {
                   if (roomCamera.GetAttachedFields().isRoomBlacklisted || roomCamera.voidSeaMode)
                   {
                       return roomCamera.CamPos(roomCamera.currentCameraPosition);
                   }
                   return cameraPosition;
               });
            }
            else
            {
                Debug.LogException(new Exception("SBCameraScroll: IL_RoomCamera_DrawUpdate_1 failed."));
            }

            if (cursor.TryGotoNext(instruction => instruction.MatchCall<RoomCamera>("CamPos")))
            {
                Debug.Log("SBCameraScroll: IL_RoomCamera_DrawUpdate_2: Index " + cursor.Index); // before: 112 // after: 109
                cursor.Goto(cursor.Index - 2);
                cursor.RemoveRange(3);

                cursor.Emit(OpCodes.Ldloc_1);
                cursor.EmitDelegate<Func<RoomCamera, Vector2, Vector2>>((roomCamera, cameraPosition) =>
               {
                   if (roomCamera.GetAttachedFields().isRoomBlacklisted || roomCamera.voidSeaMode)
                   {
                       return roomCamera.CamPos(roomCamera.currentCameraPosition);
                   }
                   return cameraPosition;
               });
            }
            else
            {
                Debug.LogException(new Exception("SBCameraScroll: IL_RoomCamera_DrawUpdate_2 failed."));
            }

            if (cursor.TryGotoNext(instruction => instruction.MatchCall<RoomCamera>("CamPos")))
            {
                Debug.Log("SBCameraScroll: IL_RoomCamera_DrawUpdate_3: Index " + cursor.Index); // before: 129 // after: 123
                cursor.Goto(cursor.Index - 2);
                cursor.RemoveRange(3);

                cursor.Emit(OpCodes.Ldloc_1);
                cursor.EmitDelegate<Func<RoomCamera, Vector2, Vector2>>((roomCamera, cameraPosition) =>
               {
                   if (roomCamera.GetAttachedFields().isRoomBlacklisted || roomCamera.voidSeaMode)
                   {
                       return roomCamera.CamPos(roomCamera.currentCameraPosition);
                   }
                   return cameraPosition;
               });
            }
            else
            {
                Debug.LogException(new Exception("SBCameraScroll: IL_RoomCamera_DrawUpdate_3 failed."));
            }

            if (cursor.TryGotoNext(instruction => instruction.MatchCall<RoomCamera>("CamPos")))
            {
                Debug.Log("SBCameraScroll: IL_RoomCamera_DrawUpdate_4: Index " + cursor.Index); // before: 145 // after: 136
                cursor.Goto(cursor.Index - 2);
                cursor.RemoveRange(3);

                cursor.Emit(OpCodes.Ldloc_1);
                cursor.EmitDelegate<Func<RoomCamera, Vector2, Vector2>>((roomCamera, cameraPosition) =>
               {
                   if (roomCamera.GetAttachedFields().isRoomBlacklisted || roomCamera.voidSeaMode)
                   {
                       return roomCamera.CamPos(roomCamera.currentCameraPosition);
                   }
                   return cameraPosition;
               });
            }
            else
            {
                Debug.LogException(new Exception("SBCameraScroll: IL_RoomCamera_DrawUpdate_4 failed."));
            }

            //
            //
            //

            if (cursor.TryGotoNext(instruction => instruction.MatchCall<RoomCamera>("CamPos")))
            {
                Debug.Log("SBCameraScroll: IL_RoomCamera_DrawUpdate_5: Index " + cursor.Index); // before: 321 // after: 309
                cursor.Goto(cursor.Index - 4);
                cursor.RemoveRange(43); // 317-359

                cursor.Emit(OpCodes.Ldloc_1);
                cursor.EmitDelegate<Action<RoomCamera, Vector2>>((roomCamera, cameraPosition) =>
                {
                    if (roomCamera.GetAttachedFields().isRoomBlacklisted || roomCamera.voidSeaMode)
                    {
                        roomCamera.levelGraphic.x = roomCamera.CamPos(roomCamera.currentCameraPosition).x - cameraPosition.x;
                        roomCamera.levelGraphic.y = roomCamera.CamPos(roomCamera.currentCameraPosition).y - cameraPosition.y;
                        roomCamera.backgroundGraphic.x = roomCamera.CamPos(roomCamera.currentCameraPosition).x - cameraPosition.x;
                        roomCamera.backgroundGraphic.y = roomCamera.CamPos(roomCamera.currentCameraPosition).y - cameraPosition.y;
                        return;
                    }

                    // not sure what this does // seems to visually darken stuff (apply shader or something) when offscreen
                    // I think that textureOffset is only needed(?) for compatibility reasons with room.cameraPositions
                    Vector2 textureOffset = roomCamera.room.abstractRoom.GetAttachedFields().textureOffset;
                    roomCamera.levelGraphic.SetPosition(textureOffset - cameraPosition);
                    roomCamera.backgroundGraphic.SetPosition(textureOffset - cameraPosition);
                });
            }
            else
            {
                Debug.LogException(new Exception("SBCameraScroll: IL_RoomCamera_DrawUpdate_5 failed."));
            }

            if (cursor.TryGotoNext(instruction => instruction.MatchCall<RoomCamera>("CamPos")))
            {
                Debug.Log("SBCameraScroll: IL_RoomCamera_DrawUpdate_6: Index " + cursor.Index); // before: 425 // after: 374
                cursor.Goto(cursor.Index - 9);
                cursor.RemoveRange(71); // 416-486

                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldloc_1);
                cursor.EmitDelegate<Action<RoomCamera, Vector2>>((roomCamera, cameraPosition) =>
                {
                    if (roomCamera.GetAttachedFields().isRoomBlacklisted || roomCamera.voidSeaMode)
                    {
                        Shader.SetGlobalVector("_spriteRect", new Vector4((-cameraPosition.x - 0.5f + roomCamera.CamPos(roomCamera.currentCameraPosition).x) / roomCamera.sSize.x, (-cameraPosition.y + 0.5f + roomCamera.CamPos(roomCamera.currentCameraPosition).y) / roomCamera.sSize.y, (-cameraPosition.x - 0.5f + roomCamera.levelGraphic.width + roomCamera.CamPos(roomCamera.currentCameraPosition).x) / roomCamera.sSize.x, (-cameraPosition.y + 0.5f + roomCamera.levelGraphic.height + roomCamera.CamPos(roomCamera.currentCameraPosition).y) / roomCamera.sSize.y));
                        return;
                    }

                    // roomCamera.levelGraphic.x = textureOffset.x - cameraPosition.x
                    // same for y
                    Shader.SetGlobalVector("_spriteRect", new Vector4((roomCamera.levelGraphic.x - 0.5f) / roomCamera.sSize.x, (roomCamera.levelGraphic.y + 0.5f) / roomCamera.sSize.y, (roomCamera.levelGraphic.x + roomCamera.levelGraphic.width - 0.5f) / roomCamera.sSize.x, (roomCamera.levelGraphic.y + roomCamera.levelGraphic.height + 0.5f) / roomCamera.sSize.y)); // if the 0.5f is missing then you get black outlines
                });
            }
            else
            {
                Debug.LogException(new Exception("SBCameraScroll: IL_RoomCamera_DrawUpdate_6 failed."));
            }
            // MainMod.LogAllInstructions(context);
        }

        private static void IL_RoomCamera_Update(ILContext context)
        {
            ILCursor cursor = new(context);
            // MainMod.LogAllInstructions(context);

            // maybe it is just me or is stuff noticeably slower when using On-Hooks + GPU stuff?
            // IL_RoomCamera_DrawUpdate() seems to do a lot..
            // maybe it is better to do Update as an IL-Hook as well;

            if (cursor.TryGotoNext(instruction => instruction.MatchCall<RoomCamera>("UpdateDayNightPalette")))
            {
                Debug.Log("SBCameraScroll: IL_RoomCamera_Update_1: Index " + cursor.Index); // 400
                cursor.EmitDelegate<Action<RoomCamera>>(roomCamera => // put before UpdateDayNightPalette()
                {
                    AddFadeTransition(roomCamera);
                });
                cursor.Emit(OpCodes.Ldarg_0);
            }
            else
            {
                Debug.LogException(new Exception("SBCameraScroll: IL_RoomCamera_Update_1 failed."));
            }

            // putting it after normal pos updates but before the screen shake effect;
            // in the On-Hook it was after; so the screen shake did nothing;
            if (cursor.TryGotoNext(instruction => instruction.MatchCall<RoomCamera>("get_screenShake")))
            {
                Debug.Log("SBCameraScroll: IL_RoomCamera_Update_2: Index " + cursor.Index); // before: 916 // after: 920
                cursor.EmitDelegate<Action<RoomCamera>>(roomCamera =>
                {
                    AttachedFields attachedFields = roomCamera.GetAttachedFields();
                    if (attachedFields.isRoomBlacklisted || roomCamera.voidSeaMode) return;
                    attachedFields.typeCamera.Update();
                });
                cursor.Emit(OpCodes.Ldarg_0);
            }
            else
            {
                Debug.LogException(new Exception("SBCameraScroll: IL_RoomCamera_Update_2 failed."));
            }
            // MainMod.LogAllInstructions(context);
        }

        //
        //
        //

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

            if (roomCamera.fullScreenEffect == null) return;
            if (roomCamera.fullScreenEffect.shader.name == "Fog" && !MainMod.Option_FogFullScreenEffect || roomCamera.fullScreenEffect.shader.name != "Fog" && !MainMod.Option_OtherFullScreenEffects)
            {
                roomCamera.fullScreenEffect.RemoveFromContainer();
                roomCamera.fullScreenEffect = null;
            }
        }

        private static void RoomCamera_ApplyPositionChange(On.RoomCamera.orig_ApplyPositionChange orig, RoomCamera roomCamera)
        {
            // don't log on every screen change;
            // only log when the room changes;
            bool isLoggingEnabled = roomCamera.loadingRoom != null;

            // updates currentCameraPosition;
            // updates roomCamera.room if needed;
            // updates roomCamera.loadingRoom;
            //
            // resizes the levelTexture automatically (and the corresponding atlas texture);
            // constantly resizing might be a problem (memory fragmentation?)
            // what is the purpose of an atlas?; collecting sprites?;
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
            roomCamera.levelGraphic.width = roomCamera.levelTexture.width;
            roomCamera.levelGraphic.height = roomCamera.levelTexture.height;
            roomCamera.backgroundGraphic.width = roomCamera.backgroundTexture.width;
            roomCamera.backgroundGraphic.height = roomCamera.backgroundTexture.height;

            if (roomCamera.room == null)
            {
                if (isLoggingEnabled)
                {
                    Debug.Log("SBCameraScroll: The current room is blacklisted.");
                }

                roomCamera.GetAttachedFields().isRoomBlacklisted = true;
                ResetCameraPosition(roomCamera); // uses currentCameraPosition and isRoomBlacklisted
                return;
            }

            // if I blacklist too early then the camera might jump in the current room
            string roomName = roomCamera.room.abstractRoom.name;
            if (blacklisted_rooms.Contains(roomName) || WorldLoader.FindRoomFile(roomName, false, "_0.png") == null && roomCamera.room.cameraPositions.Length > 1)
            {
                if (isLoggingEnabled)
                {
                    Debug.Log("SBCameraScroll: The room " + roomName + " is blacklisted.");
                }

                roomCamera.GetAttachedFields().isRoomBlacklisted = true;
                ResetCameraPosition(roomCamera);
                return;
            }

            // blacklist instead of checking if you can scroll;
            // they have the same purpose anyways;
            roomCamera.GetAttachedFields().isRoomBlacklisted = !RoomMod.CanScrollCamera(roomCamera.room);
            ResetCameraPosition(roomCamera);
        }

        private static void RoomCamera_ctor(On.RoomCamera.orig_ctor orig, RoomCamera roomCamera, RainWorldGame game, int cameraNumber)
        {
            orig(roomCamera, game, cameraNumber);

            if (all_attached_fields.ContainsKey(roomCamera)) return;
            all_attached_fields.Add(roomCamera, new(roomCamera));
        }

        private static bool RoomCamera_IsViewedByCameraPosition(On.RoomCamera.orig_IsViewedByCameraPosition orig, RoomCamera roomCamera, int camPos, Vector2 testPos)
        {
            if (roomCamera.GetAttachedFields().isRoomBlacklisted || roomCamera.voidSeaMode)
            {
                return orig(roomCamera, camPos, testPos);
            }

            // snow can be fall down into screens that should be outside of the visibility range;
            // changing this back to vanilla + roomCamera.pos didn't help with the snow;
            // but for consistency it might be better to leave it as is;
            return testPos.x > roomCamera.pos.x - 188f && testPos.x < roomCamera.pos.x + 188f + 1024f && testPos.y > roomCamera.pos.y - 18f && testPos.y < roomCamera.pos.y + 18f + 768f;
            // buffer: 200f
            // return testPos.x > roomCamera.pos.x - 200f - 188f && testPos.x < roomCamera.pos.x + 200f + 188f + 1024f && testPos.y > roomCamera.pos.y - 200f - 18f && testPos.y < roomCamera.pos.y + 200f + 18f + 768f;
            // return testPos.x > roomCamera.pos.x - 380f && testPos.x < roomCamera.pos.x + 380f + 1400f && testPos.y > roomCamera.pos.y - 20f && testPos.y < roomCamera.pos.y + 20f + 800f;
        }

        // looking at the source code this seems to be only used with currentCameraPosition at this point;
        // => treat is like RoomCamera_PositionCurrentlyVisible();
        private static bool RoomCamera_IsVisibleAtCameraPosition(On.RoomCamera.orig_IsVisibleAtCameraPosition orig, RoomCamera roomCamera, int camPos, Vector2 testPos)
        {
            if (roomCamera.GetAttachedFields().isRoomBlacklisted || roomCamera.voidSeaMode)
            {
                return orig(roomCamera, camPos, testPos);
            }
            return testPos.x > roomCamera.pos.x - 188f && testPos.x < roomCamera.pos.x + 188f + roomCamera.game.rainWorld.options.ScreenSize.x && testPos.y > roomCamera.pos.y - 18f && testPos.y < roomCamera.pos.y + 18f + 768f;
            // return testPos.x > roomCamera.pos.x - 200f - 188f && testPos.x < roomCamera.pos.x + 200f + 188f + roomCamera.game.rainWorld.options.ScreenSize.x && testPos.y > roomCamera.pos.y - 200f - 18f && testPos.y < roomCamera.pos.y + 200f + 18f + 768f;
            // return testPos.x > roomCamera.pos.x - 380f && testPos.x < roomCamera.pos.x + 380f + 1400f && testPos.y > roomCamera.pos.y - 20f && testPos.y < roomCamera.pos.y + 20f + 800f;
        }

        private static void RoomCamera_MoveCamera(On.RoomCamera.orig_MoveCamera_int orig, RoomCamera roomCamera, int camPos)
        {
            // only called when moving camera positions inside the same room 
            // if the ID changed then do a smooth transition instead 
            // the logic for that is done in UpdateCameraPosition()

            AttachedFields attachedFields = roomCamera.GetAttachedFields();
            if (attachedFields.isRoomBlacklisted || roomCamera.voidSeaMode || roomCamera.followAbstractCreature == null)
            {
                orig(roomCamera, camPos);
                return;
            }

            roomCamera.currentCameraPosition = camPos;
            if (attachedFields.typeCamera is VanillaTypeCamera vanillaTypeCamera && vanillaTypeCamera.use_vanilla_positions && vanillaTypeCamera.follow_abstract_creature_id == roomCamera.followAbstractCreature.ID) // camera moves otherwise after vanilla transition since variables are not reset // ignore reset during a smooth transition
            {
                ResetCameraPosition(roomCamera);
            }
        }

        // preloads textures // RoomCamera.ApplyPositionChange() is called when they are ready
        private static void RoomCamera_MoveCamera2(On.RoomCamera.orig_MoveCamera2 orig, RoomCamera roomCamera, string roomName, int camPos)
        {
            // isRoomBlacklisted is not updated yet;
            // needs to be updated in ApplyPositionChange();
            // I need to check for blacklisted room anyway 
            // since for example "RM_AI" can be merged but is incompatible;
            if (blacklisted_rooms.Contains(roomName) || WorldLoader.FindRoomFile(roomName, false, "_0.png") == null) // roomCamera.game.IsArenaSession ||
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
            return testPos.x > roomCamera.pos.x - 188f - margin - (widescreen ? 190f : 0f) && testPos.x < roomCamera.pos.x + 188f + (ModManager.MMF ? roomCamera.game.rainWorld.options.ScreenSize.x : 1024f) + margin + (widescreen ? 190f : 0f) && testPos.y > roomCamera.pos.y - 18f - margin && testPos.y < roomCamera.pos.y + 18f + 768f + margin;
            // return testPos.x > roomCamera.pos.x - 200f - 188f - margin - (widescreen ? 190f : 0f) && testPos.x < roomCamera.pos.x + 200f + 188f + (ModManager.MMF ? roomCamera.game.rainWorld.options.ScreenSize.x : 1024f) + margin + (widescreen ? 190f : 0f) && testPos.y > roomCamera.pos.y - 200f - 18f - margin && testPos.y < roomCamera.pos.y + 200f + 18f + 768f + margin;
            // return testPos.x > roomCamera.pos.x - 380f - margin && testPos.x < roomCamera.pos.x + 380f + 1400f + margin && testPos.y > roomCamera.pos.y - 20f - margin && testPos.y < roomCamera.pos.y + 20f + 800f + margin;
        }

        private static bool RoomCamera_PositionVisibleInNextScreen(On.RoomCamera.orig_PositionVisibleInNextScreen orig, RoomCamera roomCamera, Vector2 testPos, float margin, bool widescreen)
        {
            if (roomCamera.GetAttachedFields().isRoomBlacklisted || roomCamera.voidSeaMode)
            {
                return orig(roomCamera, testPos, margin, widescreen);
            }

            float screenSizeX = ModManager.MMF ? roomCamera.game.rainWorld.options.ScreenSize.x : 1024f;
            return testPos.x > roomCamera.pos.x - screenSizeX - 188f - margin - (widescreen ? 190f : 0f) && testPos.x < roomCamera.pos.x + 2f * screenSizeX + 188f + margin + (widescreen ? 190f : 0f) && testPos.y > roomCamera.pos.y - 768f - 18f - margin && testPos.y < roomCamera.pos.y + 2f * 768f + 18f + margin;
            // return testPos.x > roomCamera.pos.x - (ModManager.MMF ? roomCamera.game.rainWorld.options.ScreenSize.x : 1024f) - 200f - 188f - margin - (widescreen ? 190f : 0f) && testPos.x < roomCamera.pos.x + 2f * (ModManager.MMF ? roomCamera.game.rainWorld.options.ScreenSize.x : 1024f) + 200f + 188f + margin + (widescreen ? 190f : 0f) && testPos.y > roomCamera.pos.y - 768f - 200f - 18f - margin && testPos.y < roomCamera.pos.y + 2f * 768f + 200f + 18f + margin;
            // return testPos.x > roomCamera.pos.x - 380f - 1400f - margin && testPos.x < roomCamera.pos.x + 380f + 2800f + margin && testPos.y > roomCamera.pos.y - 20f - 800f - margin && testPos.y < roomCamera.pos.y + 20f + 1600f + margin;
        }

        private static void RoomCamera_PreLoadTexture(On.RoomCamera.orig_PreLoadTexture orig, RoomCamera roomCamera, Room room, int camPos)
        {
            //this function is only called when moving inside rooms but not between them 
            if (roomCamera.GetAttachedFields().isRoomBlacklisted || roomCamera.voidSeaMode)
            {
                orig(roomCamera, room, camPos);
            }
        }

        private static bool RoomCamera_RectCurrentlyVisible(On.RoomCamera.orig_RectCurrentlyVisible orig, RoomCamera roomCamera, Rect testRect, float margin, bool widescreen)
        {
            if (roomCamera.GetAttachedFields().isRoomBlacklisted || roomCamera.voidSeaMode)
            {
                return orig(roomCamera, testRect, margin, widescreen);
            }

            Rect otherRect = default;

            otherRect.xMin = roomCamera.pos.x - 188f - margin - (widescreen ? 190f : 0f);
            otherRect.xMax = roomCamera.pos.x + 188f + (ModManager.MMF ? roomCamera.game.rainWorld.options.ScreenSize.x : 1024f) + margin + (widescreen ? 190f : 0f);
            otherRect.yMin = roomCamera.pos.y - 18f - margin;
            otherRect.yMax = roomCamera.pos.y + 18f + 768f + margin;

            // otherRect.xMin = roomCamera.pos.x - 200f - 188f - margin - (widescreen ? 190f : 0f);
            // otherRect.xMax = roomCamera.pos.x + 200f + 188f + (ModManager.MMF ? roomCamera.game.rainWorld.options.ScreenSize.x : 1024f) + margin + (widescreen ? 190f : 0f);
            // otherRect.yMin = roomCamera.pos.y - 200f - 18f - margin;
            // otherRect.yMax = roomCamera.pos.y + 200f + 18f + 768f + margin;

            // otherRect.xMin = roomCamera.pos.x - 380f - margin;
            // otherRect.xMax = roomCamera.pos.x + 380f + 1400f + margin;
            // otherRect.yMin = roomCamera.pos.y - 20f - margin;
            // otherRect.yMax = roomCamera.pos.y + 20f + 800f + margin;

            return testRect.CheckIntersect(otherRect);
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
        // private static void RoomCamera_Update(On.RoomCamera.orig_Update orig, RoomCamera roomCamera)
        // {
        //     orig(roomCamera); // updates isRoomBlacklisted

        //     if (roomCamera.GetAttachedFields().isRoomBlacklisted || roomCamera.voidSeaMode) return; // don't smooth the camera position in the void sea // treat void sea as being blacklisted 
        //     UpdateCameraPosition(roomCamera);
        //     AddFadeTransition(roomCamera);
        // }

        //
        //
        //

        public sealed class AttachedFields
        {
            public bool isRoomBlacklisted = false;

            public Vector2 lastOnScreenPosition = new();
            public Vector2 onScreenPosition = new();

            public IAmATypeCamera typeCamera;

            public AttachedFields(RoomCamera roomCamera)
            {
                if (camera_type == CameraType.Position)
                {
                    typeCamera = new PositionTypeCamera(roomCamera, this);
                    return;
                }
                typeCamera = new VanillaTypeCamera(roomCamera, this);
            }
        }
    }
}