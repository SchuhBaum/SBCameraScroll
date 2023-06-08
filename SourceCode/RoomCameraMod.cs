using System;
using System.Collections.Generic;
using Expedition;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RWCustom;
using UnityEngine;

using static SBCameraScroll.MainMod;
using static SBCameraScroll.ShortcutHandlerMod;

namespace SBCameraScroll;

public static class RoomCameraMod
{
    //
    // parameters
    //

    public static CameraType camera_type = CameraType.Position;

    public static float smoothing_factor_x = 0.16f;
    public static float smoothing_factor_y = 0.16f;

    // used in CoopTweaks; don't rename;
    public static float number_of_frames_per_shortcut_udpate = 3f;
    public static List<string> blacklisted_rooms = new() { "RM_AI", "GW_ARTYSCENES", "GW_ARTYNIGHTMARE", "SB_E05SAINT", "SL_AI" };

    //
    // variables
    //

    internal static readonly Dictionary<RoomCamera, AttachedFields> all_attached_fields = new();
    public static AttachedFields GetAttachedFields(this RoomCamera room_camera) => all_attached_fields[room_camera];
    public static bool Is_Type_Camera_Not_Used(this RoomCamera room_camera) => room_camera.GetAttachedFields().is_room_blacklisted || room_camera.voidSeaMode;

    // call only if is_split_screen_coop_enabled is true;
    public static bool Is_Split => Is_Split_Horizontally || Is_Split_Vertically;
    public static bool Is_Split_Horizontally => SplitScreenCoop.SplitScreenCoop.CurrentSplitMode == SplitScreenCoop.SplitScreenCoop.SplitMode.SplitHorizontal;
    public static bool Is_Split_Vertically => SplitScreenCoop.SplitScreenCoop.CurrentSplitMode == SplitScreenCoop.SplitScreenCoop.SplitMode.SplitVertical;

    //
    //
    //

    internal static void OnEnable()
    {
        IL.RoomCamera.DrawUpdate += IL_RoomCamera_DrawUpdate;
        IL.RoomCamera.Update += IL_RoomCamera_Update;

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

    public static void AddFadeTransition(RoomCamera room_camera)
    {
        if (room_camera.room is not Room room) return;
        if (room.roomSettings.fadePalette == null) return;

        // the day-night fade effect does not update paletteBlend in all cases;
        // so this can otherwise reset it sometimes;
        // priotize day-night over this;
        if ((room_camera.effect_dayNight > 0f && room.world.rainCycle.timer >= room.world.rainCycle.cycleLength) || (ModManager.Expedition && room.game.rainWorld.ExpeditionMode && ExpeditionGame.activeUnlocks.Contains("bur-blinded"))) return;

        // the fade is automatically applied in RoomCamera.Update();
        room_camera.paletteBlend = Mathf.Lerp(room_camera.paletteBlend, room_camera.room.roomSettings.fadePalette.fades[room_camera.currentCameraPosition], 0.01f);
    }

    public static void CheckBorders(RoomCamera room_camera, ref Vector2 position)
    {
        if (room_camera.room == null) return;

        Vector2 screen_size = room_camera.sSize;
        Vector2 texture_offset = room_camera.room.abstractRoom.Get_Attached_Fields().texture_offset; // regionGate's texture offset might be unitialized => RegionGateMod

        if (is_split_screen_coop_enabled)
        {
            Vector2 screen_offset = SplitScreenMod_GetScreenOffset(screen_size); // half of the camera screen is not visible // the other half is centered // let the non-visible part move past room borders
            position.x = Mathf.Clamp(position.x, texture_offset.x - screen_offset.x, texture_offset.x + screen_offset.x + room_camera.levelGraphic.width - screen_size.x);
            position.y = Mathf.Clamp(position.y, texture_offset.y - screen_offset.y, texture_offset.y + screen_offset.y + room_camera.levelGraphic.height - screen_size.y - 18f);
            return;
        }

        position.x = Mathf.Clamp(position.x, texture_offset.x, room_camera.levelGraphic.width - screen_size.x + texture_offset.x); // stop position at room texture borders // probably works with room.PixelWidth - room_camera.sSize.x / 2f instead as well
        position.y = Mathf.Clamp(position.y, texture_offset.y, room_camera.levelGraphic.height - screen_size.y + texture_offset.y - 18f); // not sure why I have to decrease positionY by a constant // I picked 18f bc room_camera.seekPos.y gets changed by 18f in Update() // seems to work , i.e. I don't see black bars
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

            // use the center (of mass(?)) instead;
            // makes rolls more predictable;
            // use lower y such that crouching does not move camera;
            return new()
            {
                x = 0.5f * (player.bodyChunks[0].pos.x + player.bodyChunks[1].pos.x),
                y = Mathf.Min(player.bodyChunks[0].pos.y, player.bodyChunks[1].pos.y)
            };
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

    public static void ResetCameraPosition(RoomCamera room_camera)
    {
        // vanilla copy & paste stuff
        if (room_camera.Is_Type_Camera_Not_Used())
        {
            room_camera.seekPos = room_camera.CamPos(room_camera.currentCameraPosition);
            room_camera.seekPos.x += room_camera.hDisplace + 8f;
            room_camera.seekPos.y += 18f;
            room_camera.leanPos *= 0.0f;

            room_camera.lastPos = room_camera.seekPos;
            room_camera.pos = room_camera.seekPos;
            return;
        }
        room_camera.GetAttachedFields().type_camera.Reset();
    }

    public static Vector2 SplitScreenMod_GetScreenOffset(in Vector2 screen_size)
    {
        if (Is_Split_Horizontally) return new Vector2(0.0f, 0.25f * screen_size.y);
        if (Is_Split_Vertically) return new Vector2(0.25f * screen_size.x, 0.0f);
        return new Vector2();
    }

    // accounts for room boundaries and shortcuts
    public static void UpdateOnScreenPosition(RoomCamera room_camera)
    {
        if (room_camera.room == null) return;
        if (room_camera.followAbstractCreature == null) return;
        if (room_camera.followAbstractCreature.Room != room_camera.room.abstractRoom) return;
        if (room_camera.followAbstractCreature.realizedCreature is not Creature creature) return;

        Vector2 position = -0.5f * room_camera.sSize;
        if (creature.inShortcut && GetShortcutVessel(room_camera.game.shortcuts, room_camera.followAbstractCreature) is ShortcutHandler.ShortCutVessel shortcutVessel)
        {
            Vector2 current_position = room_camera.room.MiddleOfTile(shortcutVessel.pos);
            Vector2 next_in_shortcut_position = room_camera.room.MiddleOfTile(ShortcutHandler.NextShortcutPosition(shortcutVessel.pos, shortcutVessel.lastPos, room_camera.room));

            // shortcuts get only updated every 3 frames => calculate exact position here // in CoopTweaks it can also be 2 frames in order to remove slowdown, i.e. compensate for the mushroom effect
            position += Vector2.Lerp(current_position, next_in_shortcut_position, room_camera.game.updateShortCut / number_of_frames_per_shortcut_udpate);
        }
        else // use the center (of mass(?)) instead // makes rolls more predictable // use lower y such that crouching does not move camera
        {
            position += GetCreaturePosition(creature);
        }

        AttachedFields attachedFields = room_camera.GetAttachedFields();
        attachedFields.last_on_screen_position = attachedFields.on_screen_position;
        attachedFields.on_screen_position = position;
    }

    //
    // private
    //

    private static void IL_RoomCamera_DrawUpdate(ILContext context)
    {
        // LogAllInstructions(context);
        ILCursor cursor = new(context);

        if (cursor.TryGotoNext(instruction => instruction.MatchCall<RoomCamera>("CamPos")))
        {
            if (can_log_il_hooks)
            {
                Debug.Log("SBCameraScroll: IL_RoomCamera_DrawUpdate: Index " + cursor.Index); // 100 
            }

            cursor.Goto(cursor.Index - 2);
            cursor.RemoveRange(3); // remove CamPos(currentCameraPosition)

            cursor.Emit(OpCodes.Ldloc_1);
            cursor.EmitDelegate<Func<RoomCamera, Vector2, Vector2>>((room_camera, cameraPosition) =>
           {
               if (room_camera.Is_Type_Camera_Not_Used())
               {
                   // Mathf.Clamp(vector.x, CamPos(currentCameraPosition).x + hDisplace + 8f - 20f, CamPos(currentCameraPosition).x + hDisplace + 8f + 20f);
                   return room_camera.CamPos(room_camera.currentCameraPosition);
               }

               // hDisplace gives a straight offset when using non-default screen resolutions;
               // we don't want offsets; we just want to skip the clamping;
               cameraPosition.x -= room_camera.hDisplace;
               return cameraPosition;
           });
        }
        else
        {
            if (can_log_il_hooks)
            {
                Debug.Log("SBCameraScroll: IL_RoomCamera_DrawUpdate failed.");
            }
            return;
        }

        if (cursor.TryGotoNext(instruction => instruction.MatchCall<RoomCamera>("CamPos")))
        {
            if (can_log_il_hooks)
            {
                Debug.Log("SBCameraScroll: IL_RoomCamera_DrawUpdate: Index " + cursor.Index); // 112 
            }

            cursor.Goto(cursor.Index - 2);
            cursor.RemoveRange(3);

            cursor.Emit(OpCodes.Ldloc_1);
            cursor.EmitDelegate<Func<RoomCamera, Vector2, Vector2>>((room_camera, cameraPosition) =>
           {
               if (room_camera.Is_Type_Camera_Not_Used())
               {
                   return room_camera.CamPos(room_camera.currentCameraPosition);
               }

               cameraPosition.x -= room_camera.hDisplace;
               return cameraPosition;
           });
        }
        else
        {
            if (can_log_il_hooks)
            {
                Debug.Log("SBCameraScroll: IL_RoomCamera_DrawUpdate failed.");
            }
            return;
        }

        if (cursor.TryGotoNext(instruction => instruction.MatchCall<RoomCamera>("CamPos")))
        {
            if (can_log_il_hooks)
            {
                Debug.Log("SBCameraScroll: IL_RoomCamera_DrawUpdate: Index " + cursor.Index); // 129 
            }

            cursor.Goto(cursor.Index - 2);
            cursor.RemoveRange(3);

            cursor.Emit(OpCodes.Ldloc_1);
            cursor.EmitDelegate<Func<RoomCamera, Vector2, Vector2>>((room_camera, cameraPosition) =>
           {
               if (room_camera.Is_Type_Camera_Not_Used())
               {
                   return room_camera.CamPos(room_camera.currentCameraPosition);
               }
               return cameraPosition;
           });
        }
        else
        {
            if (can_log_il_hooks)
            {
                Debug.Log("SBCameraScroll: IL_RoomCamera_DrawUpdate failed.");
            }
            return;
        }

        if (cursor.TryGotoNext(instruction => instruction.MatchCall<RoomCamera>("CamPos")))
        {
            if (can_log_il_hooks)
            {
                Debug.Log("SBCameraScroll: IL_RoomCamera_DrawUpdate: Index " + cursor.Index); // 145 
            }

            cursor.Goto(cursor.Index - 2);
            cursor.RemoveRange(3);

            cursor.Emit(OpCodes.Ldloc_1);
            cursor.EmitDelegate<Func<RoomCamera, Vector2, Vector2>>((room_camera, cameraPosition) =>
           {
               if (room_camera.Is_Type_Camera_Not_Used())
               {
                   return room_camera.CamPos(room_camera.currentCameraPosition);
               }
               return cameraPosition;
           });
        }
        else
        {
            if (can_log_il_hooks)
            {
                Debug.Log("SBCameraScroll: IL_RoomCamera_DrawUpdate failed.");
            }
            return;
        }

        //
        //
        //

        if (cursor.TryGotoNext(instruction => instruction.MatchCall<RoomCamera>("CamPos")))
        {
            if (can_log_il_hooks)
            {
                Debug.Log("SBCameraScroll: IL_RoomCamera_DrawUpdate: Index " + cursor.Index); // 321
            }

            cursor.Goto(cursor.Index - 4);
            cursor.RemoveRange(43); // 317-359

            cursor.Emit(OpCodes.Ldloc_1);
            cursor.EmitDelegate<Action<RoomCamera, Vector2>>((room_camera, camera_position) =>
            {
                if (room_camera.Is_Type_Camera_Not_Used())
                {
                    room_camera.levelGraphic.x = room_camera.CamPos(room_camera.currentCameraPosition).x - camera_position.x;
                    room_camera.levelGraphic.y = room_camera.CamPos(room_camera.currentCameraPosition).y - camera_position.y;
                    room_camera.backgroundGraphic.x = room_camera.CamPos(room_camera.currentCameraPosition).x - camera_position.x;
                    room_camera.backgroundGraphic.y = room_camera.CamPos(room_camera.currentCameraPosition).y - camera_position.y;
                    return;
                }

                // not sure what this does // seems to visually darken stuff (apply shader or something) when offscreen
                // I think that textureOffset is only needed(?) for compatibility reasons with room.cameraPositions
                Vector2 texture_offset = room_camera.room.abstractRoom.Get_Attached_Fields().texture_offset;
                room_camera.levelGraphic.SetPosition(texture_offset - camera_position);
                room_camera.backgroundGraphic.SetPosition(texture_offset - camera_position);
            });
        }
        else
        {
            if (can_log_il_hooks)
            {
                Debug.Log("SBCameraScroll: IL_RoomCamera_DrawUpdate failed.");
            }
            return;
        }

        if (cursor.TryGotoNext(instruction => instruction.MatchCall<RoomCamera>("CamPos")))
        {
            if (can_log_il_hooks)
            {
                Debug.Log("SBCameraScroll: IL_RoomCamera_DrawUpdate: Index " + cursor.Index); // 425
            }

            cursor.Goto(cursor.Index - 9);
            cursor.RemoveRange(71); // 416-486

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldloc_1);
            cursor.EmitDelegate<Action<RoomCamera, Vector2>>((room_camera, camera_position) =>
            {
                if (room_camera.Is_Type_Camera_Not_Used())
                {
                    Shader.SetGlobalVector("_spriteRect", new Vector4((-camera_position.x - 0.5f + room_camera.CamPos(room_camera.currentCameraPosition).x) / room_camera.sSize.x, (-camera_position.y + 0.5f + room_camera.CamPos(room_camera.currentCameraPosition).y) / room_camera.sSize.y, (-camera_position.x - 0.5f + room_camera.levelGraphic.width + room_camera.CamPos(room_camera.currentCameraPosition).x) / room_camera.sSize.x, (-camera_position.y + 0.5f + room_camera.levelGraphic.height + room_camera.CamPos(room_camera.currentCameraPosition).y) / room_camera.sSize.y));
                    return;
                }

                // room_camera.levelGraphic.x = textureOffset.x - cameraPosition.x
                // same for y
                Shader.SetGlobalVector("_spriteRect", new Vector4((room_camera.levelGraphic.x - 0.5f) / room_camera.sSize.x, (room_camera.levelGraphic.y + 0.5f) / room_camera.sSize.y, (room_camera.levelGraphic.x + room_camera.levelGraphic.width - 0.5f) / room_camera.sSize.x, (room_camera.levelGraphic.y + room_camera.levelGraphic.height + 0.5f) / room_camera.sSize.y)); // if the 0.5f is missing then you get black outlines
            });
        }
        else
        {
            if (can_log_il_hooks)
            {
                Debug.Log("SBCameraScroll: IL_RoomCamera_DrawUpdate failed.");
            }
            return;
        }
        // LogAllInstructions(context);
    }

    private static void IL_RoomCamera_Update(ILContext context)
    {
        ILCursor cursor = new(context);
        // LogAllInstructions(context);

        // maybe it is just me or is stuff noticeably slower when using On-Hooks + GPU stuff?
        // IL_RoomCamera_DrawUpdate() seems to do a lot..
        // maybe it is better to do Update as an IL-Hook as well;

        if (cursor.TryGotoNext(instruction => instruction.MatchCall<RoomCamera>("UpdateDayNightPalette")))
        {
            if (can_log_il_hooks)
            {
                Debug.Log("SBCameraScroll: IL_RoomCamera_Update: Index " + cursor.Index); // 400
            }

            cursor.EmitDelegate<Action<RoomCamera>>(room_camera => // put before UpdateDayNightPalette()
            {
                if (room_camera.Is_Type_Camera_Not_Used()) return;
                AddFadeTransition(room_camera);
            });
            cursor.Emit(OpCodes.Ldarg_0);
        }
        else
        {
            if (can_log_il_hooks)
            {
                Debug.Log("SBCameraScroll: IL_RoomCamera_Update failed.");
            }
            return;
        }

        // putting it after normal pos updates but before the screen shake effect;
        // in the On-Hook it was after; so the screen shake did nothing;
        if (cursor.TryGotoNext(instruction => instruction.MatchCall<RoomCamera>("get_screenShake")))
        {
            if (can_log_il_hooks)
            {
                Debug.Log("SBCameraScroll: IL_RoomCamera_Update: Index " + cursor.Index); // before: 916 // after: 920
            }

            cursor.EmitDelegate<Action<RoomCamera>>(room_camera =>
            {
                if (room_camera.Is_Type_Camera_Not_Used()) return;
                room_camera.GetAttachedFields().type_camera.Update();
            });
            cursor.Emit(OpCodes.Ldarg_0);
        }
        else
        {
            if (can_log_il_hooks)
            {
                Debug.Log("SBCameraScroll: IL_RoomCamera_Update failed.");
            }
            return;
        }
        // LogAllInstructions(context);
    }

    //
    //
    //

    private static Vector2 RoomCamera_ApplyDepth(On.RoomCamera.orig_ApplyDepth orig, RoomCamera room_camera, Vector2 ps, float depth)
    {
        if (room_camera.Is_Type_Camera_Not_Used())
        {
            return orig(room_camera, ps, depth);
        }
        return Custom.ApplyDepthOnVector(ps, room_camera.pos + new Vector2(700f, 1600f / 3f), depth);
    }

    private static void RoomCamera_ApplyPalette(On.RoomCamera.orig_ApplyPalette orig, RoomCamera room_camera)
    {
        orig(room_camera);

        if (room_camera.fullScreenEffect == null) return;
        if (room_camera.fullScreenEffect.shader.name == "Fog" && !Option_FogFullScreenEffect || room_camera.fullScreenEffect.shader.name != "Fog" && !Option_OtherFullScreenEffects)
        {
            room_camera.fullScreenEffect.RemoveFromContainer();
            room_camera.fullScreenEffect = null;
        }
    }

    private static void RoomCamera_ApplyPositionChange(On.RoomCamera.orig_ApplyPositionChange orig, RoomCamera room_camera)
    {
        // don't log on every screen change;
        // only log when the room changes;
        bool isLoggingEnabled = room_camera.loadingRoom != null;

        // updates currentCameraPosition;
        // updates room_camera.room if needed;
        // updates room_camera.loadingRoom;
        //
        // resizes the levelTexture automatically (and the corresponding atlas texture);
        // constantly resizing might be a problem (memory fragmentation?)
        // what is the purpose of an atlas?; collecting sprites?;
        orig(room_camera);

        // www has a texture too;
        // not sure what exactly happens when www.LoadImageIntoTexture(room_camera.levelTexture) is called in orig();
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
        // holy moly don't use room_camera.www.texture.width, etc. // "WWW.texture property allocates a new Texture2D every time"
        room_camera.levelGraphic.width = room_camera.levelTexture.width;
        room_camera.levelGraphic.height = room_camera.levelTexture.height;
        room_camera.backgroundGraphic.width = room_camera.backgroundTexture.width;
        room_camera.backgroundGraphic.height = room_camera.backgroundTexture.height;

        if (room_camera.room == null)
        {
            if (isLoggingEnabled)
            {
                Debug.Log("SBCameraScroll: The current room is blacklisted.");
            }

            // this case should never happen since ApplyPositionChange() calls ChangeRoom() 
            // and should always update room_camera.room;
            // if it would happen then I am blind to how many cameraPositions this room has;
            // I would also not be able to check blacklisted_rooms;
            // blacklisting the room is just a guess at this point;

            room_camera.GetAttachedFields().is_room_blacklisted = true;
            ResetCameraPosition(room_camera); // uses currentCameraPosition and isRoomBlacklisted
            return;
        }

        // if I blacklist too early then the camera might jump in the current room
        string roomName = room_camera.room.abstractRoom.name;
        if (blacklisted_rooms.Contains(roomName) || WorldLoader.FindRoomFile(roomName, false, "_0.png") == null && room_camera.room.cameraPositions.Length > 1)
        {
            if (isLoggingEnabled)
            {
                Debug.Log("SBCameraScroll: The room " + roomName + " is blacklisted.");
            }

            room_camera.GetAttachedFields().is_room_blacklisted = true;
            ResetCameraPosition(room_camera);
            return;
        }

        // blacklist instead of checking if you can scroll;
        // they have the same purpose anyways;
        room_camera.GetAttachedFields().is_room_blacklisted = !room_camera.room.CanScrollCamera();
        ResetCameraPosition(room_camera);
    }

    private static void RoomCamera_ctor(On.RoomCamera.orig_ctor orig, RoomCamera room_camera, RainWorldGame game, int camera_number)
    {
        orig(room_camera, game, camera_number);

        if (all_attached_fields.ContainsKey(room_camera)) return;
        all_attached_fields.Add(room_camera, new(room_camera));
    }

    private static bool RoomCamera_IsViewedByCameraPosition(On.RoomCamera.orig_IsViewedByCameraPosition orig, RoomCamera room_camera, int camera_position_index, Vector2 test_position)
    {
        if (room_camera.Is_Type_Camera_Not_Used())
        {
            return orig(room_camera, camera_position_index, test_position);
        }

        // snow can be fall down into screens that should be outside of the visibility range;
        // changing this back to vanilla + room_camera.pos didn't help with the snow;
        // but for consistency it might be better to leave it as is;
        return test_position.x > room_camera.pos.x - 188f && test_position.x < room_camera.pos.x + 188f + 1024f && test_position.y > room_camera.pos.y - 18f && test_position.y < room_camera.pos.y + 18f + 768f;
        // buffer: 200f
        // return test_position.x > room_camera.pos.x - 200f - 188f && test_position.x < room_camera.pos.x + 200f + 188f + 1024f && test_position.y > room_camera.pos.y - 200f - 18f && test_position.y < room_camera.pos.y + 200f + 18f + 768f;
        // return test_position.x > room_camera.pos.x - 380f && test_position.x < room_camera.pos.x + 380f + 1400f && test_position.y > room_camera.pos.y - 20f && test_position.y < room_camera.pos.y + 20f + 800f;
    }

    // looking at the source code this seems to be only used with currentCameraPosition at this point;
    // => treat is like RoomCamera_PositionCurrentlyVisible();
    private static bool RoomCamera_IsVisibleAtCameraPosition(On.RoomCamera.orig_IsVisibleAtCameraPosition orig, RoomCamera room_camera, int camera_position_index, Vector2 test_position)
    {
        if (room_camera.Is_Type_Camera_Not_Used())
        {
            return orig(room_camera, camera_position_index, test_position);
        }
        return test_position.x > room_camera.pos.x - 188f && test_position.x < room_camera.pos.x + 188f + room_camera.game.rainWorld.options.ScreenSize.x && test_position.y > room_camera.pos.y - 18f && test_position.y < room_camera.pos.y + 18f + 768f;
        // return test_position.x > room_camera.pos.x - 200f - 188f && test_position.x < room_camera.pos.x + 200f + 188f + room_camera.game.rainWorld.options.ScreenSize.x && test_position.y > room_camera.pos.y - 200f - 18f && test_position.y < room_camera.pos.y + 200f + 18f + 768f;
        // return test_position.x > room_camera.pos.x - 380f && test_position.x < room_camera.pos.x + 380f + 1400f && test_position.y > room_camera.pos.y - 20f && test_position.y < room_camera.pos.y + 20f + 800f;
    }

    private static void RoomCamera_MoveCamera(On.RoomCamera.orig_MoveCamera_int orig, RoomCamera room_camera, int camera_position_index)
    {
        // only called when moving camera positions inside the same room 
        // if the ID changed then do a smooth transition instead 
        // the logic for that is done in UpdateCameraPosition()

        if (room_camera.Is_Type_Camera_Not_Used() || room_camera.followAbstractCreature == null)
        {
            orig(room_camera, camera_position_index);
            return;
        }

        room_camera.currentCameraPosition = camera_position_index;
        if (room_camera.GetAttachedFields().type_camera is VanillaTypeCamera vanillaTypeCamera && vanillaTypeCamera.use_vanilla_positions && vanillaTypeCamera.follow_abstract_creature_id == room_camera.followAbstractCreature.ID) // camera moves otherwise after vanilla transition since variables are not reset // ignore reset during a smooth transition
        {
            ResetCameraPosition(room_camera);
        }
    }

    // preloads textures // RoomCamera.ApplyPositionChange() is called when they are ready
    private static void RoomCamera_MoveCamera2(On.RoomCamera.orig_MoveCamera2 orig, RoomCamera room_camera, string room_name, int camera_position_index)
    {
        // // room is not updated yet;
        // // gets updated in ApplyPositionChange();
        // // although ChangeRoom() has a non-null check;
        // // it would still update the room;
        // // not sure what would happen if the room would be null;
        // // don't do this:
        // if (room_camera.room == null)
        // {
        //     if (!blacklisted_rooms.Contains(room_name))
        //     {
        //         blacklisted_rooms.Add(room_name);
        //     }

        //     orig(room_camera, room_name, camera_position_index);
        //     return;
        // }

        // is_room_blacklisted is not updated yet;
        // needs to be updated in ApplyPositionChange();
        // I need to check for blacklisted room anyway 
        // since for example "RM_AI" can be merged but is incompatible;
        if (blacklisted_rooms.Contains(room_name) || WorldLoader.FindRoomFile(room_name, false, "_0.png") == null)
        {
            orig(room_camera, room_name, camera_position_index);
            return;
        }
        orig(room_camera, room_name, -1);
    }

    private static Color RoomCamera_PixelColorAtCoordinate(On.RoomCamera.orig_PixelColorAtCoordinate orig, RoomCamera room_camera, Vector2 coord)
    {
        if (room_camera.Is_Type_Camera_Not_Used())
        {
            return orig(room_camera, coord);
        }

        // remove effect of room_camera.CamPos(room_camera.currentCameraPosition) // color of lights might otherwise "jump" in color
        return orig(room_camera, coord + room_camera.CamPos(room_camera.currentCameraPosition));
    }

    // use room_camera.pos as reference instead of camPos(..) // seems to be important for unloading graphics and maybe other things
    private static bool RoomCamera_PositionCurrentlyVisible(On.RoomCamera.orig_PositionCurrentlyVisible orig, RoomCamera room_camera, Vector2 test_position, float margin, bool widescreen)
    {
        if (room_camera.Is_Type_Camera_Not_Used())
        {
            return orig(room_camera, test_position, margin, widescreen);
        }
        return test_position.x > room_camera.pos.x - 188f - margin - (widescreen ? 190f : 0f) && test_position.x < room_camera.pos.x + 188f + (ModManager.MMF ? room_camera.game.rainWorld.options.ScreenSize.x : 1024f) + margin + (widescreen ? 190f : 0f) && test_position.y > room_camera.pos.y - 18f - margin && test_position.y < room_camera.pos.y + 18f + 768f + margin;
        // return test_position.x > room_camera.pos.x - 200f - 188f - margin - (widescreen ? 190f : 0f) && test_position.x < room_camera.pos.x + 200f + 188f + (ModManager.MMF ? room_camera.game.rainWorld.options.ScreenSize.x : 1024f) + margin + (widescreen ? 190f : 0f) && test_position.y > room_camera.pos.y - 200f - 18f - margin && test_position.y < room_camera.pos.y + 200f + 18f + 768f + margin;
        // return test_position.x > room_camera.pos.x - 380f - margin && test_position.x < room_camera.pos.x + 380f + 1400f + margin && test_position.y > room_camera.pos.y - 20f - margin && test_position.y < room_camera.pos.y + 20f + 800f + margin;
    }

    private static bool RoomCamera_PositionVisibleInNextScreen(On.RoomCamera.orig_PositionVisibleInNextScreen orig, RoomCamera room_camera, Vector2 test_position, float margin, bool widescreen)
    {
        if (room_camera.Is_Type_Camera_Not_Used())
        {
            return orig(room_camera, test_position, margin, widescreen);
        }

        float screenSizeX = ModManager.MMF ? room_camera.game.rainWorld.options.ScreenSize.x : 1024f;
        return test_position.x > room_camera.pos.x - screenSizeX - 188f - margin - (widescreen ? 190f : 0f) && test_position.x < room_camera.pos.x + 2f * screenSizeX + 188f + margin + (widescreen ? 190f : 0f) && test_position.y > room_camera.pos.y - 768f - 18f - margin && test_position.y < room_camera.pos.y + 2f * 768f + 18f + margin;
        // return test_position.x > room_camera.pos.x - (ModManager.MMF ? room_camera.game.rainWorld.options.ScreenSize.x : 1024f) - 200f - 188f - margin - (widescreen ? 190f : 0f) && test_position.x < room_camera.pos.x + 2f * (ModManager.MMF ? room_camera.game.rainWorld.options.ScreenSize.x : 1024f) + 200f + 188f + margin + (widescreen ? 190f : 0f) && test_position.y > room_camera.pos.y - 768f - 200f - 18f - margin && test_position.y < room_camera.pos.y + 2f * 768f + 200f + 18f + margin;
        // return test_position.x > room_camera.pos.x - 380f - 1400f - margin && test_position.x < room_camera.pos.x + 380f + 2800f + margin && test_position.y > room_camera.pos.y - 20f - 800f - margin && test_position.y < room_camera.pos.y + 20f + 1600f + margin;
    }

    private static void RoomCamera_PreLoadTexture(On.RoomCamera.orig_PreLoadTexture orig, RoomCamera room_camera, Room room, int camera_position_index)
    {
        //this function is only called when moving inside rooms but not between them 
        if (!room_camera.Is_Type_Camera_Not_Used()) return;
        orig(room_camera, room, camera_position_index);
    }

    private static bool RoomCamera_RectCurrentlyVisible(On.RoomCamera.orig_RectCurrentlyVisible orig, RoomCamera room_camera, Rect test_rectangle, float margin, bool widescreen)
    {
        if (room_camera.Is_Type_Camera_Not_Used())
        {
            return orig(room_camera, test_rectangle, margin, widescreen);
        }

        Rect other_rectangle = default;

        other_rectangle.xMin = room_camera.pos.x - 188f - margin - (widescreen ? 190f : 0f);
        other_rectangle.xMax = room_camera.pos.x + 188f + (ModManager.MMF ? room_camera.game.rainWorld.options.ScreenSize.x : 1024f) + margin + (widescreen ? 190f : 0f);
        other_rectangle.yMin = room_camera.pos.y - 18f - margin;
        other_rectangle.yMax = room_camera.pos.y + 18f + 768f + margin;

        // other_rectangle.xMin = room_camera.pos.x - 200f - 188f - margin - (widescreen ? 190f : 0f);
        // other_rectangle.xMax = room_camera.pos.x + 200f + 188f + (ModManager.MMF ? room_camera.game.rainWorld.options.ScreenSize.x : 1024f) + margin + (widescreen ? 190f : 0f);
        // other_rectangle.yMin = room_camera.pos.y - 200f - 18f - margin;
        // other_rectangle.yMax = room_camera.pos.y + 200f + 18f + 768f + margin;

        // other_rectangle.xMin = room_camera.pos.x - 380f - margin;
        // other_rectangle.xMax = room_camera.pos.x + 380f + 1400f + margin;
        // other_rectangle.yMin = room_camera.pos.y - 20f - margin;
        // other_rectangle.yMax = room_camera.pos.y + 20f + 800f + margin;

        return test_rectangle.CheckIntersect(other_rectangle);
    }

    private static void RoomCamera_ScreenMovement(On.RoomCamera.orig_ScreenMovement orig, RoomCamera room_camera, Vector2? source_position, Vector2 bump, float shake)
    {
        // should remove effects on camera like camera shakes caused by other creatures // feels weird otherwise
        if (!room_camera.Is_Type_Camera_Not_Used()) return;
        orig(room_camera, source_position, bump, shake);
    }

    //
    //
    //

    public sealed class AttachedFields
    {
        public bool is_room_blacklisted = false;

        public Vector2 last_on_screen_position = new();
        public Vector2 on_screen_position = new();

        public IAmATypeCamera type_camera;

        public AttachedFields(RoomCamera room_camera)
        {
            if (camera_type == CameraType.Position)
            {
                type_camera = new PositionTypeCamera(room_camera, this);
                return;
            }

            if (camera_type == CameraType.Vanilla)
            {
                type_camera = new VanillaTypeCamera(room_camera, this);
                return;
            }
            type_camera = new SwitchTypeCamera(room_camera, this);
        }
    }

    public enum CameraType
    {
        Position,
        Vanilla,
        Switch
    }
}