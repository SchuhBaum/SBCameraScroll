﻿using Expedition;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using RWCustom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

using static SBCameraScroll.AbstractRoomMod;
using static SBCameraScroll.MainMod;
using static SBCameraScroll.RainWorldMod;
using static SBCameraScroll.RoomMod;
using static SBCameraScroll.ShortcutHandlerMod;
using static SBCameraScroll.SplitScreenCoopMod;

namespace SBCameraScroll;

public static class RoomCameraMod {
    // Maybe I can use this for debugging purposes more often.
    // public static FSprite sprite_0 = new("Futile_White");
    // public static FSprite sprite_1 = new("Futile_White");
    // public static DebugSprite debug_sprite_0 = new(new(), sprite_0, null);
    // public static DebugSprite debug_sprite_1 = new(new(), sprite_1, null);

    //
    // parameters
    //

    public static CameraType camera_type = CameraType.Position;
    public static float smoothing_factor = 0.16f;

    // used in CoopTweaks; don't rename;
    public static float number_of_frames_per_shortcut_udpate = 3f;
    public static List<string> blacklisted_rooms = new() { "RM_AI", "GW_ARTYSCENES", "GW_ARTYNIGHTMARE", "SB_E05SAINT", "SL_AI" };

    // makes some shader glitch out more;
    // not recommended;
    public static float camera_zoom = 1f;
    public static float Half_Inverse_Camera_Zoom_XY => 0.5f * (1f / camera_zoom - 1f);
    public static bool Is_Camera_Zoom_Enabled => !is_split_screen_coop_enabled && camera_zoom != 1f;

    //
    // variables
    //

    internal static readonly Dictionary<RoomCamera, Attached_Fields> _all_attached_fields = new();
    public static Attached_Fields Get_Attached_Fields(this RoomCamera room_camera) => _all_attached_fields[room_camera];
    public static bool Is_Type_Camera_Not_Used(this RoomCamera room_camera) => room_camera.Get_Attached_Fields() is Attached_Fields attached_fields && (attached_fields.is_room_blacklisted || !attached_fields.is_camera_scroll_enabled && !attached_fields.is_camera_scroll_forced_by_split_screen) || room_camera.voidSeaMode;

    public static string? next_text_prompt_message = null;

    public static Hook? hook_RoomCamera_LevelTexture = null;

    // The variable new(10) does not allocate memory yet. As soon as the first
    // element is added it will reserve memory for the other 9 spots.
    public static List<Texture2D>[] level_texture_lists = {new(10), new(10), new(10), new(10)};
    public static List<string>[] level_texture_room_name_lists = {new(10), new(10), new(10), new(10)};
    public static Texture2D? Get_Level_Texture(int camera_number, int camera_index) {
        if (camera_number < 0 || camera_number > 3) return null;
        while (camera_index >= level_texture_lists[camera_number].Count) {
            Texture2D level_texture = new Texture2D(1400, 800, TextureFormat.ARGB32, mipChain: false);
            level_texture.anisoLevel = 0;
            level_texture.filterMode = FilterMode.Point;
            level_texture.wrapMode = TextureWrapMode.Clamp;
            level_texture_lists[camera_number].Add(level_texture);
        }
        return level_texture_lists[camera_number][camera_index];
    }
    public static string? Get_Level_Texture_Room_Name(int camera_number, int camera_index) {
        if (camera_number < 0 || camera_number > 3) return null;
        while (camera_index >= level_texture_room_name_lists[camera_number].Count) {
            level_texture_room_name_lists[camera_number].Add("");
        }
        return level_texture_room_name_lists[camera_number][camera_index];
    }
    public static void Set_Level_Texture_Room_Name(string room_name, int camera_number, int camera_index) {
        if (camera_number < 0 || camera_number > 3) return;
        while (camera_index >= level_texture_room_name_lists[camera_number].Count) {
            level_texture_room_name_lists[camera_number].Add("");
        }
        level_texture_room_name_lists[camera_number][camera_index] = room_name;
    }

    public static RenderTexture?[] render_texture_array = {null, null, null, null};
    public static RenderTexture? Render_Texture(this RoomCamera room_camera) {
        if (room_camera.cameraNumber < 0 || room_camera.cameraNumber > 3) return null;
        return render_texture_array[room_camera.cameraNumber];
    }

    //
    //
    //

    internal static void On_Config_Changed() {
        hook_RoomCamera_LevelTexture?.Dispose();
        hook_RoomCamera_LevelTexture = null;

        IL.RoomCamera.ApplyPositionChange -= IL_RoomCamera_ApplyPositionChange;
        IL.RoomCamera.Update -= IL_RoomCamera_Update;

        On.RoomCamera.ChangeCameraToPlayer -= RoomCamera_ChangeCameraToPlayer;
        On.RoomCamera.MoveCamera_Room_int -= RoomCamera_MoveCamera_Room;
        On.RoomCamera.UpdateSnowLight -= RoomCamera_UpdateSnowLight;

        // Contains an Option_JIT_Merging-specific part.
        IL.RoomCamera.Update += IL_RoomCamera_Update;

        if (Option_JIT_Merging) {
            // Trying to hook On.PersistentData.ctor does not work. The mod is
            // not loaded when that function is called.
            for (int camera_number = 0; camera_number < render_texture_array.Length; ++camera_number) {
                RenderTexture render_texture = new RenderTexture(1, 1, 0, RenderTextureFormat.ARGB32) {
                    anisoLevel = 0,
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                    useMipMap = false,
                };
                render_texture_array[camera_number] = render_texture;
                Replace_Or_Add_Atlas("LevelTexture" + ((camera_number == 0) ? "" : camera_number.ToString()), render_texture);
            }

            if (Type.GetType("RoomCamera, Assembly-CSharp") is Type RoomCamera) {
                try {
                    hook_RoomCamera_LevelTexture = new Hook(RoomCamera.GetMethod("get_levelTexture", BindingFlags.NonPublic | BindingFlags.Instance), typeof(RoomCameraMod).GetMethod("RoomCamera_LevelTexture"));
                } catch (Exception exception) {
                    Debug.Log(mod_id + ": " + exception);
                }
            }

            IL.RoomCamera.ApplyPositionChange += IL_RoomCamera_ApplyPositionChange;
            On.RoomCamera.ChangeCameraToPlayer += RoomCamera_ChangeCameraToPlayer;
            On.RoomCamera.MoveCamera_Room_int += RoomCamera_MoveCamera_Room;
            On.RoomCamera.UpdateSnowLight += RoomCamera_UpdateSnowLight;
        } else {
            Texture2D[,] camera_textures = RWCustom.Custom.rainWorld.persistentData.cameraTextures;
            for (int camera_number = 0; camera_number < camera_textures.GetLength(0); ++camera_number) {
                Replace_Or_Add_Atlas("LevelTexture" + ((camera_number == 0) ? "" : camera_number.ToString()), camera_textures[camera_number,0]);

                List<Texture2D> level_texture_list = level_texture_lists[camera_number];
                for (int camera_index = level_texture_list.Count-1; camera_index >= 0; --camera_index) {
                    UnityEngine.Object.Destroy(level_texture_list[camera_index]);
                }
                level_texture_lists[camera_number] = new(10);
                level_texture_room_name_lists[camera_number] = new(10);

                RenderTexture? render_texture = render_texture_array[camera_number];
                render_texture_array[camera_number] = null;
                if (render_texture != null) {
                    render_texture.Release();
                    UnityEngine.Object.Destroy(render_texture);
                }
            }
        }
    }

    internal static void OnEnable() {
        IL.RoomCamera.DrawUpdate += IL_RoomCamera_DrawUpdate;

        On.RoomCamera.ApplyDepth += RoomCamera_ApplyDepth;
        On.RoomCamera.ApplyPalette += RoomCamera_ApplyPalette;
        On.RoomCamera.ApplyPositionChange += RoomCamera_ApplyPositionChange;
        On.RoomCamera.ctor += RoomCamera_Ctor;

        On.RoomCamera.DepthAtCoordinate += RoomCamera_DepthAtCoordinate;
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

    public static void Apply_Camera_Zoom(RoomCamera room_camera) {
        if (!Is_Camera_Zoom_Enabled) return;

        // copied from SlugcatEyebrowRaise mod
        for (int sprite_layer_index = 0; sprite_layer_index < 11; ++sprite_layer_index) {
            FContainer sprite_layer = room_camera.SpriteLayers[sprite_layer_index];
            sprite_layer.scale = 1f;
            sprite_layer.SetPosition(Vector2.zero);

            // this makes it such that the graphics are centered;
            // 
            // still, there are scaling issues:
            // for example, the underwater glow is only aligned with
            // slugcat when in the center of the screen;
            // when zoomed out it will move faster away from the
            // center compared to slugcat which can only reach the
            // border of the level visuals; the glow seem to reach
            // the border of the screen instead;
            sprite_layer.ScaleAroundPointRelative(0.5f * room_camera.sSize, camera_zoom, camera_zoom);
        }
    }

    public static void AddFadeTransition(RoomCamera room_camera) {
        if (room_camera.room is not Room room) return;
        if (room.roomSettings.fadePalette == null) return;

        // the day-night fade effect does not update paletteBlend in all cases;
        // so this can otherwise reset it sometimes;
        // priotize day-night over this;
        if (room_camera.effect_dayNight > 0f && room.world.rainCycle.timer >= room.world.rainCycle.cycleLength) return;
        if (ModManager.Expedition && room.game.rainWorld.ExpeditionMode && ExpeditionGame.activeUnlocks.Contains("bur-blinded")) return;

        // the fade is automatically applied in RoomCamera.Update();
        room_camera.paletteBlend = Mathf.Lerp(room_camera.paletteBlend, room_camera.room.roomSettings.fadePalette.fades[room_camera.currentCameraPosition], 0.01f);
    }

    public static void CheckBorders(RoomCamera room_camera, ref Vector2 position) {
        if (room_camera.room == null) return;
        Vector2 screen_size = room_camera.sSize;
        Vector2 min_camera_position = room_camera.room.abstractRoom.Get_Attached_Fields().min_camera_position; // regionGate's min_camera_position might be unitialized => RegionGateMod

        // half of the camera screen is not visible; the other half is centered; let the
        // non-visible part move past room borders;
        Vector2 screen_offset = is_split_screen_coop_enabled ? Get_Screen_Offset(room_camera, screen_size) : new();

        // Half_Inverse_Camera_Zoom_XY:
        // in percent; how much screen space is added left and right, top and bottom;
        // example: camera_zoom = 0.8f increases the screen size in x and y by 25% each; Half_Inverse_Camera_Zoom_XY = 0.5 * 25%;
        Vector2 screen_size_increase = Is_Camera_Zoom_Enabled ? Half_Inverse_Camera_Zoom_XY * room_camera.sSize : Vector2.zero;
        float min_x = min_camera_position.x - screen_offset.x + screen_size_increase.x;
        float max_x = min_camera_position.x + screen_offset.x - screen_size_increase.x + room_camera.levelGraphic.width - screen_size.x;

        if (min_x < max_x) {
            // stop position at room texture borders;
            position.x = Mathf.Clamp(position.x, min_x, max_x);
        } else {
            // keep the position centered in case the camera is zoomed;
            position.x = 0.5f * (min_x + max_x);
        }

        // not sure why I have to decrease max_y by a constant;
        // I picked 18f bc room_camera.seekPos.y gets changed by 18f in Update();
        // seems to work, i.e. I don't see black bars;
        float min_y = min_camera_position.y - screen_offset.y + screen_size_increase.y;
        float max_y = min_camera_position.y + screen_offset.y - screen_size_increase.y + room_camera.levelGraphic.height - screen_size.y - 18f;

        if (min_y < max_y) {
            position.y = Mathf.Clamp(position.y, min_y, max_y);
        } else {
            position.y = 0.5f * (min_y + max_y);
        }
    }

    public static Vector2 GetCreaturePosition(Creature creature) {
        if (creature is Player player) {
            // reduce movement when "rolling" in place in ZeroG;
            if (player.room?.gravity == 0.0f || player.animation == Player.AnimationIndex.Roll) {
                return 0.5f * (player.bodyChunks[0].pos + player.bodyChunks[1].pos);
            }

            // use the center (of mass(?)) instead;
            // makes rolls more predictable;
            // use lower y such that crouching does not move camera;
            return new() {
                x = 0.5f * (player.bodyChunks[0].pos.x + player.bodyChunks[1].pos.x),
                y = Mathf.Min(player.bodyChunks[0].pos.y, player.bodyChunks[1].pos.y)
            };
        }
        // otherwise when the overseer jumps back and forth the camera would move as well;
        // I consider this a bug;
        // the overseer should not jump around when focusing on a shortcut;
        // because the audio stops playing as well;
        else if (creature.abstractCreature.abstractAI is OverseerAbstractAI abstract_ai && abstract_ai.safariOwner && abstract_ai.doorSelectionIndex != -1) {
            return abstract_ai.parent.Room.realizedRoom.MiddleOfTile(abstract_ai.parent.Room.realizedRoom.ShortcutLeadingToNode(abstract_ai.doorSelectionIndex).startCoord);
        }
        return creature.mainBodyChunk.pos;
    }

    public static void Load_Image(string room_name, int camera_number, int camera_index, byte[]? byte_array) {
        if (byte_array == null) return;
        if (byte_array.Length == 0) return;
        Get_Level_Texture(camera_number, camera_index)?.LoadImage(byte_array, markNonReadable: false);
        Set_Level_Texture_Room_Name(room_name, camera_number, camera_index);
    }

    public static void Merge_All_Camera_Textures_On_GPU(RoomCamera room_camera, RenderTexture render_texture) {
        int camera_number = room_camera.cameraNumber;
        if (room_camera.room is not Room room || room.cameraPositions.Length < 2 || room_camera.Is_Type_Camera_Not_Used()) {
            Graphics.Blit(Get_Level_Texture(camera_number, room_camera.currentCameraPosition), render_texture);
            return;
        }

        for (int camera_index = 0; camera_index < room.cameraPositions.Length; ++camera_index) {
            Vector2 texture_offset = room.cameraPositions[camera_index] - room.abstractRoom.Get_Attached_Fields().min_camera_position; // already contains the offsetModifier

            int x = (int)texture_offset.x;
            int y = (int)texture_offset.y;
            int cutoff_x = 0;
            int cutoff_y = 0;

            if (x < 0) cutoff_x = -x;
            if (y < 0) cutoff_y = -y;

            if (x < maximum_texture_width && y < maximum_texture_height) {
                int width = Math.Min(1400 - cutoff_x, maximum_texture_width - x);
                int height = Math.Min(800 - cutoff_y, maximum_texture_height - y);
                Graphics.CopyTexture(Get_Level_Texture(camera_number, camera_index), 0, 0, cutoff_x, cutoff_y, width, height, render_texture, 0, 0, Mathf.Max(x, 0), Mathf.Max(y, 0));
            }
        }
    }

    public static void ResetCameraPosition(RoomCamera room_camera) {
        // vanilla copy & paste stuff
        if (room_camera.Is_Type_Camera_Not_Used()) {
            room_camera.seekPos = room_camera.CamPos(room_camera.currentCameraPosition);
            room_camera.seekPos.x += room_camera.hDisplace + 8f;
            room_camera.seekPos.y += 18f;
            room_camera.leanPos *= 0.0f;

            room_camera.lastPos = room_camera.seekPos;
            room_camera.pos = room_camera.seekPos;
            Reset_Camera_Zoom(room_camera);
            return;
        }

        room_camera.Get_Attached_Fields().type_camera.Reset();
        Apply_Camera_Zoom(room_camera);
    }

    public static void Reset_Camera_Zoom(RoomCamera room_camera) {
        if (!Is_Camera_Zoom_Enabled) return;
        for (int sprite_layer_index = 0; sprite_layer_index < 11; ++sprite_layer_index) {
            FContainer sprite_layer = room_camera.SpriteLayers[sprite_layer_index];
            sprite_layer.scale = 1f;
            sprite_layer.SetPosition(Vector2.zero);
            sprite_layer.ScaleAroundPointRelative(Vector2.zero, 1f, 1f);
        }
    }

    // This functions needs to be public. Otherwise, the hook creation fails.
    public static Texture2D RoomCamera_LevelTexture(Func<RoomCamera,Texture2D> orig, RoomCamera room_camera) { // Option_JIT_Merging
        if (Get_Level_Texture(room_camera.cameraNumber, room_camera.currentCameraPosition) is not Texture2D texture) {
            return orig(room_camera);
        }
        return texture;
    }

    public static void Send_TextPrompt_Message(RoomCamera room_camera) {
        if (next_text_prompt_message == null) return;
        if (room_camera.hud is not HUD.HUD hud) return;
        if (room_camera.game is not RainWorldGame game) return;

        if (hud.textPrompt.currentlyShowing != HUD.TextPrompt.InfoID.Nothing) {
            next_text_prompt_message = null;
            return;
        }

        hud.textPrompt.AddMessage(game.rainWorld.inGameTranslator.Translate(next_text_prompt_message), wait: 0, time: 200, darken: false, hideHud: false);
        next_text_prompt_message = null;
    }

    // accounts for room boundaries and shortcuts
    public static void UpdateOnScreenPosition(RoomCamera room_camera) {
        if (room_camera.room == null) return;
        if (room_camera.followAbstractCreature == null) return;
        if (room_camera.followAbstractCreature.Room != room_camera.room.abstractRoom) return;
        if (room_camera.followAbstractCreature.realizedCreature is not Creature creature) return;

        Vector2 position = -0.5f * room_camera.sSize;
        if (creature.inShortcut && GetShortcutVessel(room_camera.game.shortcuts, room_camera.followAbstractCreature) is ShortcutHandler.ShortCutVessel shortcut_vessel) {
            Vector2 current_position = room_camera.room.MiddleOfTile(shortcut_vessel.pos);
            Vector2 next_in_shortcut_position = room_camera.room.MiddleOfTile(ShortcutHandler.NextShortcutPosition(shortcut_vessel.pos, shortcut_vessel.lastPos, room_camera.room));

            // shortcuts get only updated every 3 frames => calculate exact position here // in CoopTweaks it can also be 2 frames in order to remove slowdown, i.e. compensate for the mushroom effect
            position += Vector2.Lerp(current_position, next_in_shortcut_position, room_camera.game.updateShortCut / number_of_frames_per_shortcut_udpate);
        } else {
            // use the center (of mass(?)) instead // makes rolls more predictable // use lower y such that crouching does not move camera
            position += GetCreaturePosition(creature);
        }

        Attached_Fields attached_fields = room_camera.Get_Attached_Fields();
        attached_fields.last_on_screen_position = attached_fields.on_screen_position;
        attached_fields.on_screen_position = position;
    }

    //
    // private
    //

    private static void IL_RoomCamera_ApplyPositionChange(ILContext context) {
		// LogAllInstructions(context);

        // Remove the vanilla call to LoadImage(). This is done in before
        // calling orig() in ApplyPositionChange() when Option_JIT_Merging is
        // enabled.
		ILCursor cursor = new(context);
		cursor.RemoveRange(7);
		// LogAllInstructions(context);
	}

    private static void IL_RoomCamera_DrawUpdate(ILContext context) {
        // LogAllInstructions(context);
        ILCursor cursor = new(context);

        if (cursor.TryGotoNext(instruction => instruction.MatchCall<RoomCamera>("CamPos"))) {
            if (can_log_il_hooks) {
                Debug.Log("SBCameraScroll: IL_RoomCamera_DrawUpdate: Index " + cursor.Index); // 100 
            }

            cursor.Goto(cursor.Index - 2);
            cursor.RemoveRange(3); // remove CamPos(currentCameraPosition)

            cursor.Emit(OpCodes.Ldloc_1);
            cursor.EmitDelegate<Func<RoomCamera, Vector2, Vector2>>((room_camera, camera_position) => {
                if (room_camera.Is_Type_Camera_Not_Used()) {
                    // Mathf.Clamp(vector.x, CamPos(currentCameraPosition).x + hDisplace + 8f - 20f, CamPos(currentCameraPosition).x + hDisplace + 8f + 20f);
                    return room_camera.CamPos(room_camera.currentCameraPosition);
                }

                // hDisplace gives a straight offset when using non-default screen resolutions;
                // we don't want offsets; we just want to skip the clamping;
                camera_position.x -= room_camera.hDisplace;
                return camera_position;
            });
        } else {
            if (can_log_il_hooks) {
                Debug.Log("SBCameraScroll: IL_RoomCamera_DrawUpdate failed.");
            }
            return;
        }

        if (cursor.TryGotoNext(instruction => instruction.MatchCall<RoomCamera>("CamPos"))) {
            if (can_log_il_hooks) {
                Debug.Log("SBCameraScroll: IL_RoomCamera_DrawUpdate: Index " + cursor.Index); // 112 
            }

            cursor.Goto(cursor.Index - 2);
            cursor.RemoveRange(3);

            cursor.Emit(OpCodes.Ldloc_1);
            cursor.EmitDelegate<Func<RoomCamera, Vector2, Vector2>>((room_camera, camera_position) => {
                if (room_camera.Is_Type_Camera_Not_Used()) {
                    return room_camera.CamPos(room_camera.currentCameraPosition);
                }

                camera_position.x -= room_camera.hDisplace;
                return camera_position;
            });
        } else {
            if (can_log_il_hooks) {
                Debug.Log("SBCameraScroll: IL_RoomCamera_DrawUpdate failed.");
            }
            return;
        }

        if (cursor.TryGotoNext(instruction => instruction.MatchCall<RoomCamera>("CamPos"))) {
            if (can_log_il_hooks) {
                Debug.Log("SBCameraScroll: IL_RoomCamera_DrawUpdate: Index " + cursor.Index); // 129 
            }

            cursor.Goto(cursor.Index - 2);
            cursor.RemoveRange(3);

            cursor.Emit(OpCodes.Ldloc_1);
            cursor.EmitDelegate<Func<RoomCamera, Vector2, Vector2>>((room_camera, camera_position) => {
                if (room_camera.Is_Type_Camera_Not_Used()) {
                    return room_camera.CamPos(room_camera.currentCameraPosition);
                }
                return camera_position;
            });
        } else {
            if (can_log_il_hooks) {
                Debug.Log("SBCameraScroll: IL_RoomCamera_DrawUpdate failed.");
            }
            return;
        }

        if (cursor.TryGotoNext(instruction => instruction.MatchCall<RoomCamera>("CamPos"))) {
            if (can_log_il_hooks) {
                Debug.Log("SBCameraScroll: IL_RoomCamera_DrawUpdate: Index " + cursor.Index); // 145 
            }

            cursor.Goto(cursor.Index - 2);
            cursor.RemoveRange(3);

            cursor.Emit(OpCodes.Ldloc_1);
            cursor.EmitDelegate<Func<RoomCamera, Vector2, Vector2>>((room_camera, camera_position) => {
                if (room_camera.Is_Type_Camera_Not_Used()) {
                    return room_camera.CamPos(room_camera.currentCameraPosition);
                }
                return camera_position;
            });
        } else {
            if (can_log_il_hooks) {
                Debug.Log("SBCameraScroll: IL_RoomCamera_DrawUpdate failed.");
            }
            return;
        }

        //
        //
        //

        if (cursor.TryGotoNext(instruction => instruction.MatchCall<RoomCamera>("CamPos"))) {
            if (can_log_il_hooks) {
                Debug.Log("SBCameraScroll: IL_RoomCamera_DrawUpdate: Index " + cursor.Index); // 321
            }

            cursor.Goto(cursor.Index - 4);
            cursor.RemoveRange(43); // 317-359

            cursor.Emit(OpCodes.Ldloc_1);
            cursor.EmitDelegate<Action<RoomCamera, Vector2>>((room_camera, camera_position) => {
                if (room_camera.Is_Type_Camera_Not_Used()) {
                    room_camera.levelGraphic.x = room_camera.CamPos(room_camera.currentCameraPosition).x - camera_position.x;
                    room_camera.levelGraphic.y = room_camera.CamPos(room_camera.currentCameraPosition).y - camera_position.y;
                    room_camera.backgroundGraphic.x = room_camera.CamPos(room_camera.currentCameraPosition).x - camera_position.x;
                    room_camera.backgroundGraphic.y = room_camera.CamPos(room_camera.currentCameraPosition).y - camera_position.y;
                    return;
                }

                // not sure what this does // seems to visually darken stuff (apply shader or something) when offscreen
                // I think that textureOffset is only needed(?) for compatibility reasons with room.cameraPositions
                Vector2 min_camera_position = room_camera.room.abstractRoom.Get_Attached_Fields().min_camera_position;
                room_camera.levelGraphic.SetPosition(min_camera_position - camera_position);
                room_camera.backgroundGraphic.SetPosition(min_camera_position - camera_position);
            });
        } else {
            if (can_log_il_hooks) {
                Debug.Log("SBCameraScroll: IL_RoomCamera_DrawUpdate failed.");
            }
            return;
        }

        if (cursor.TryGotoNext(instruction => instruction.MatchCall<RoomCamera>("CamPos"))) {
            if (can_log_il_hooks) {
                Debug.Log("SBCameraScroll: IL_RoomCamera_DrawUpdate: Index " + cursor.Index); // 425
            }

            cursor.Goto(cursor.Index - 9);
            cursor.RemoveRange(71); // 416-486

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldloc_1);
            cursor.EmitDelegate<Action<RoomCamera, Vector2>>((room_camera, camera_position) => {
                if (room_camera.Is_Type_Camera_Not_Used()) {
                    Shader.SetGlobalVector(RainWorld.ShadPropSpriteRect, new Vector4((-camera_position.x - 0.5f + room_camera.CamPos(room_camera.currentCameraPosition).x) / room_camera.sSize.x, (-camera_position.y + 0.5f + room_camera.CamPos(room_camera.currentCameraPosition).y) / room_camera.sSize.y, (-camera_position.x - 0.5f + room_camera.levelGraphic.width + room_camera.CamPos(room_camera.currentCameraPosition).x) / room_camera.sSize.x, (-camera_position.y + 0.5f + room_camera.levelGraphic.height + room_camera.CamPos(room_camera.currentCameraPosition).y) / room_camera.sSize.y));
                    return;
                }

                if (!Is_Camera_Zoom_Enabled) {
                    Shader.SetGlobalVector(RainWorld.ShadPropSpriteRect, new Vector4((room_camera.levelGraphic.x - 0.5f) / room_camera.sSize.x, (room_camera.levelGraphic.y + 0.5f) / room_camera.sSize.y, (room_camera.levelGraphic.x + room_camera.levelGraphic.width - 0.5f) / room_camera.sSize.x, (room_camera.levelGraphic.y + room_camera.levelGraphic.height + 0.5f) / room_camera.sSize.y));
                    return;
                }

                // When zooming out the screen gets smaller. The offset is to
                // center the sprite rectangle. If your screen is 0.25 times as
                // big then you want to move 1.5 small screens towards bottom-left.
                // (There fit 4 small screens in total, so left margin is 1.5
                // small screens and right one as well.) If you plug that into
                // the Shader.SetGlobalVector() formula below and simplify then
                // you get the zoomed and scaled version:
                //
                // screen_offset
                // = camera_zoom * (Half_Inverse_Camera_Zoom_XY * sSize.x) / sSize.x
                // = camera_zoom * Half_Inverse_Camera_Zoom_XY
                // = 0.25f       * 1.5f // in the example
                float screen_offset = 0.5f * (1f - camera_zoom);

                // room_camera.levelGraphic.x = textureOffset.x - cameraPosition.x;
                // same for y;
                // 
                // there seem to be rounding errors when zooming;
                // in some instances you see a black outline;
                // but not in others; depends on the camera position;
                //
                // if the 0.5f is missing then you get black outlines;
                // even without zoom;
                Shader.SetGlobalVector(RainWorld.ShadPropSpriteRect, new Vector4(screen_offset + (camera_zoom * room_camera.levelGraphic.x - 0.5f) / room_camera.sSize.x, screen_offset + (camera_zoom * room_camera.levelGraphic.y + 0.5f) / room_camera.sSize.y, screen_offset + (camera_zoom * (room_camera.levelGraphic.x + room_camera.levelGraphic.width) - 0.5f) / room_camera.sSize.x, screen_offset + (camera_zoom * (room_camera.levelGraphic.y + room_camera.levelGraphic.height) + 0.5f) / room_camera.sSize.y));
            });
        } else {
            if (can_log_il_hooks) {
                Debug.Log("SBCameraScroll: IL_RoomCamera_DrawUpdate failed.");
            }
            return;
        }
        // LogAllInstructions(context);
    }

    private static void IL_RoomCamera_Update(ILContext context) {
        // LogAllInstructions(context);
        ILCursor cursor = new(context);

        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate<Action<RoomCamera>>(room_camera => {
            Send_TextPrompt_Message(room_camera);
        });

        // maybe it is just me or is stuff noticeably slower when using On-Hooks + GPU stuff?
        // IL_RoomCamera_DrawUpdate() seems to do a lot..
        // maybe it is better to do Update as an IL-Hook as well;

        if (cursor.TryGotoNext(instruction => instruction.MatchCall<RoomCamera>("UpdateDayNightPalette"))) {
            if (can_log_il_hooks) {
                Debug.Log("SBCameraScroll: IL_RoomCamera_Update: Index " + cursor.Index); // 400
            }

            // put before UpdateDayNightPalette()
            cursor.EmitDelegate<Action<RoomCamera>>(room_camera => {
                // in four player split screen you can zoom the camera out by double tapping the
                // map-button; to better transition when doing so I want the scroll to be enabled
                // in both cases => simply check Is_Split; otherwise it teleports to the target 
                // location immediately;
                room_camera.Get_Attached_Fields().is_camera_scroll_forced_by_split_screen = is_split_screen_coop_enabled && Is_Split;
                if (room_camera.Is_Type_Camera_Not_Used()) return;
                AddFadeTransition(room_camera);
            });
            cursor.Emit(OpCodes.Ldarg_0);
        } else {
            if (can_log_il_hooks) {
                Debug.Log("SBCameraScroll: IL_RoomCamera_Update failed.");
            }
            return;
        }

        if (cursor.TryGotoNext(instruction => instruction.MatchLdfld("HUD.HUD", "owner"))) {
            cursor.Goto(cursor.Index + 2);

            if (can_log_il_hooks) {
                Debug.Log(mod_id + ": IL_RoomCamera_Update: Index " + cursor.Index);
            }

            if (Option_JIT_Merging) {
                // The hud owner can be null when the room loads too slowly. Add
                // missing null check.
                cursor.Next.OpCode = OpCodes.Brtrue;
                cursor.EmitDelegate<Func<HUD.IOwnAHUD?, Player, bool>>((hud_owner, player) => {
                    return hud_owner == null || hud_owner == player;
                });
            }
        } else {
            if (can_log_il_hooks) {
                Debug.Log(mod_id + ": IL_RoomCamera_Update failed.");
            }
            return;
        }

        // putting it after normal pos updates but before the screen shake effect;
        // in the On-Hook it was after; so the screen shake did nothing;
        if (cursor.TryGotoNext(instruction => instruction.MatchCall<RoomCamera>("get_screenShake"))) {
            if (can_log_il_hooks) {
                Debug.Log("SBCameraScroll: IL_RoomCamera_Update: Index " + cursor.Index); // before: 916 // after: 920
            }

            cursor.EmitDelegate<Action<RoomCamera>>(room_camera => {
                if (room_camera.Is_Type_Camera_Not_Used()) return;
                room_camera.Get_Attached_Fields().type_camera.Update();
            });
            cursor.Emit(OpCodes.Ldarg_0);
        } else {
            if (can_log_il_hooks) {
                Debug.Log("SBCameraScroll: IL_RoomCamera_Update failed.");
            }
            return;
        }
        // LogAllInstructions(context);
    }

    //
    //
    //

    private static Vector2 RoomCamera_ApplyDepth(On.RoomCamera.orig_ApplyDepth orig, RoomCamera room_camera, Vector2 position, float depth) {
        if (room_camera.Is_Type_Camera_Not_Used()) return orig(room_camera, position, depth);
        if (room_camera.room is not Room room) return orig(room_camera, position, depth);
        int camera_index = CameraViewingPoint(room, position);
        if (camera_index == -1) return orig(room_camera, position, depth);

        // before this I would change the depth based on the camera position; but it is 
        // static and only needs to match the pre-rendered visuals of the room;
        return Custom.ApplyDepthOnVector(position, room_camera.CamPos(camera_index) + new Vector2(700f, 1600f / 3f), depth);
    }

    private static void RoomCamera_ApplyPalette(On.RoomCamera.orig_ApplyPalette orig, RoomCamera room_camera) {
        orig(room_camera);

        if (room_camera.fullScreenEffect == null) return;
        if (Option_FullScreenEffects) return;
        room_camera.fullScreenEffect.RemoveFromContainer();
        room_camera.fullScreenEffect = null;
    }

    private static void RoomCamera_ApplyPositionChange(On.RoomCamera.orig_ApplyPositionChange orig, RoomCamera room_camera) {
        RenderTexture? render_texture = room_camera.Render_Texture();
        Attached_Fields room_camera_fields = room_camera.Get_Attached_Fields();

        // Always load the room when Option_JIT_Merging is enabled -- even
        // one-screen and blacklisted rooms. Since even in that case, the vanilla
        // call to LoadImage() is removed.
        if (Option_JIT_Merging) {
            if (room_camera_fields.is_pre_loading_whole_room && room_camera.loadingRoom is Room loading_room) {
                // Case 1: The whole room gets pre-loaded.
                //
                //         The function MoveCamera2() uses pre_loaded_camera_index
                //         implicitly.
                //
                //         The variable pre_loaded_camera_index is similar to the
                //         variable loadingCameraPos. But it is only used to loop
                //         all screen that need to be merged. This way this is the
                //         only thing that changes. De-couples it from the other
                //         logic in the functions MoveCamera_Room() -> MoveCamera2().
                //
                //         MoveCamera2() only loads the texture. If it would only be
                //         that function then both could be the same.
                string loading_room_name = loading_room.abstractRoom.name;
                Load_Image(loading_room_name, room_camera.cameraNumber, room_camera_fields.pre_loaded_camera_index, room_camera.preLoadedTexture);

                for (int camera_index = room_camera_fields.pre_loaded_camera_index+1; camera_index < loading_room.cameraPositions.Length; ++camera_index) {
                    if (Get_Level_Texture_Room_Name(room_camera.cameraNumber, camera_index) == loading_room_name) continue;
                    room_camera_fields.pre_loaded_camera_index = camera_index;
                    room_camera.MoveCamera2(loading_room_name, room_camera.loadingCameraPos);
                    return;
                }
            } else {
                // Case 2: Room is blacklisted.
                //
                //         In this case, this function can be called when moving
                //         inside the same room as well (i.e., loadingRoom is null).
                string? current_room_name = null;
                if (room_camera.loadingRoom != null) {
                    current_room_name = room_camera.loadingRoom.abstractRoom.name;
                } else {
                    current_room_name = room_camera.room?.abstractRoom.name;
                }

                int camera_index = room_camera.loadingCameraPos;
                if (camera_index == -1) {
                    // Should never happen.
                    camera_index = room_camera.currentCameraPosition;
                }

                if (current_room_name != null) {
                    Load_Image(current_room_name, room_camera.cameraNumber, camera_index, room_camera.preLoadedTexture);
                }
            }
        }

        room_camera_fields.is_pre_loading_whole_room = false;
        room_camera_fields.pre_loaded_camera_index = 0;

        // INFO: updates currentCameraPosition;
        //       updates room_camera.room if needed;
        //       updates room_camera.loadingRoom;
        //
        // resizes the levelTexture automatically (and the corresponding atlas texture);
        // constantly resizing might be a problem (memory fragmentation?)
        // what is the purpose of an atlas?; collecting sprites?;
        bool is_changing_room = room_camera.loadingRoom != null;
        orig(room_camera);

        // The memory address changes in most cases. Iirc Windows caches them so
        // going back and forth might reuse them and not create a new byte array.
        // This block requires this function to me marked with unsafe and in
        // csproj AllowUnsafeBlocks must be set to true.
        // Still, this might create garbage, so I am unsure how much the just-in-
        // time merging can achieve.
        // IntPtr current_address;
        // if (pre_loaded_texture_lists[room_camera.cameraNumber].Count > 1) {
        //     // fixed (byte* ptr = room_camera.preLoadedTexture) {
        //     fixed (byte* ptr = pre_loaded_texture_lists[room_camera.cameraNumber][1]) {
        //         current_address = (IntPtr)ptr;
        //         Debug.Log(mod_id + ": memory address " + current_address);
        //     }
        // }

        if (room_camera.room is not Room room) {
            if (is_changing_room) {
                Debug.Log("SBCameraScroll: The current room is blacklisted.");
            }

            // this case should never happen since ApplyPositionChange() calls ChangeRoom() 
            // and should always update room_camera.room;
            // if it would happen then I am blind to how many cameraPositions this room has;
            // I would also not be able to check blacklisted_rooms;
            // blacklisting the room is just a guess at this point;

            room_camera_fields.is_room_blacklisted = true;
            room_camera_fields.is_camera_scroll_enabled = false;

            // uses currentCameraPosition and is_room_blacklisted;
            ResetCameraPosition(room_camera);
            return;
        }

        // If I blacklist too early then the camera might jump in the current
        // room. Do it after calling orig() / ChangeRoom().
        AbstractRoomMod.Attached_Fields abstract_room_fields = room.abstractRoom.Get_Attached_Fields();
        string room_name = room.abstractRoom.name;

        // CRS (Custom-Region-Support) can replace rooms now; I need to check this; 
        // otherwise I might blacklist the wrong room;
        if (abstract_room_fields.name_when_replaced_by_crs is string new_room_name) {
            room_name = new_room_name;
        }

        if (blacklisted_rooms.Contains(room_name) || !Option_JIT_Merging && !File.Exists(WorldLoader.FindRoomFile(room_name, false, "_0.png")) && room.cameraPositions.Length > 1) {
            if (is_changing_room) {
                Debug.Log("SBCameraScroll: The room " + room_name + " is blacklisted.");
            }

            room_camera_fields.is_room_blacklisted = true;
            room_camera_fields.is_camera_scroll_enabled = false;
        } else {
            room_camera_fields.is_room_blacklisted = false;
            room_camera_fields.is_camera_scroll_enabled = room.cameraPositions.Length > 1 || Option_ScrollOneScreenRooms;
        }

        // Do this even when the room is not changing. In that case, Graphics.Blit()
        // needs to be called.
        if (Option_JIT_Merging && render_texture != null) {
            // Uses Is_Type_Camera_Not_Used(). Needs to be placed after the
            // variables is_room_blacklisted etc. are updated.
            if (room_camera.Is_Type_Camera_Not_Used()) {
                if (render_texture.width != 1400 || render_texture.height != 800) {
                    render_texture.Release();
                    render_texture.width = 1400;
                    render_texture.height = 800;
                }
            } else {
                int total_width = abstract_room_fields.total_width;
                int total_height = abstract_room_fields.total_height;
                if (render_texture.width != total_width || render_texture.height != total_height) {
                    render_texture.Release();
                    render_texture.width = total_width;
                    render_texture.height = total_height;
                }
            }

            // SetGlobalTexture needs to happen here. Otherwise, this can mess up
            // shaders if it is set only before the resizing (and merging?) happened.
            Merge_All_Camera_Textures_On_GPU(room_camera, render_texture);
            Shader.SetGlobalTexture(RainWorld.ShadPropLevelTex, render_texture);
        }

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

        Texture level_texture = room_camera.levelGraphic._atlas.texture;
        room_camera.levelGraphic.width = level_texture.width;
        room_camera.levelGraphic.height = level_texture.height;
        room_camera.backgroundGraphic.width = room_camera.backgroundTexture.width;
        room_camera.backgroundGraphic.height = room_camera.backgroundTexture.height;

        if (is_changing_room) {
            // Graphics.Blit() creates this texture again; but only when UpdateSnowLight() is called;
            // this is forced for example when the room is changed in orig(); therefore, only do
            // this when this happens;

            RenderTexture snow_texture = room_camera.SnowTexture;
            snow_texture.Release();
            snow_texture.width = level_texture.width;
            snow_texture.height = level_texture.height;
        }

        // This is needed for some shader. The camera textures contain additional
        // color information (palette pixels). The offsets are used to find them 
        // again inside the shader.
        if (room.cameraPositions.Length > 30 || level_texture.width <= 1400 && level_texture.height <= 800) {
            Shader.SetGlobalInt(TextureOffsetArrayLength, 0);
            Shader.SetGlobalVectorArray(TextureOffsetArray, new Vector4[30]);
        } else {
            Vector2 min_camera_position = abstract_room_fields.min_camera_position;
            Vector4[] texture_offset_array = new Vector4[30];

            for (int camera_index = 0; camera_index < room.cameraPositions.Length; ++camera_index) {
                texture_offset_array[camera_index] = (Vector4)(room.cameraPositions[camera_index] - min_camera_position);
            }

            Shader.SetGlobalInt(TextureOffsetArrayLength, room.cameraPositions.Length);
            Shader.SetGlobalVectorArray(TextureOffsetArray, texture_offset_array);
        }

        // uses currentCameraPosition and is_room_blacklisted;
        ResetCameraPosition(room_camera);
    }

    private static void RoomCamera_ChangeCameraToPlayer(On.RoomCamera.orig_ChangeCameraToPlayer orig, RoomCamera room_camera, AbstractCreature camera_target) { // Option_JIT_Merging
        // The room can be null when it loads too slowly. Add missing null check.
        if (room_camera.room == null) return;
        orig(room_camera, camera_target);
    }

    private static void RoomCamera_Ctor(On.RoomCamera.orig_ctor orig, RoomCamera room_camera, RainWorldGame game, int camera_number) {
        orig(room_camera, game, camera_number);
        if (_all_attached_fields.ContainsKey(room_camera)) return;
        _all_attached_fields.Add(room_camera, new(room_camera));
    }

    private static float RoomCamera_DepthAtCoordinate(On.RoomCamera.orig_DepthAtCoordinate orig, RoomCamera room_camera, Vector2 position) {
        // similar to RoomCamera_PixelColorAtCoordinate();
        if (room_camera.Is_Type_Camera_Not_Used()) return orig(room_camera, position);
        if (room_camera.room is not Room room) return orig(room_camera, position);

        if (Option_JIT_Merging) {
            int camera_index = CameraViewingPoint(room, position);
            if (camera_index != -1) {
                int current_camera_index = room_camera.currentCameraPosition;
                room_camera.currentCameraPosition = camera_index;
                float result = orig(room_camera, position);
                room_camera.currentCameraPosition = current_camera_index;
                return result;
            }
            return orig(room_camera, position);
        }

        return orig(room_camera, position + room_camera.CamPos(room_camera.currentCameraPosition) - room.abstractRoom.Get_Attached_Fields().min_camera_position);
    }

    private static bool RoomCamera_IsViewedByCameraPosition(On.RoomCamera.orig_IsViewedByCameraPosition orig, RoomCamera room_camera, int camera_position_index, Vector2 test_position) {
        if (room_camera.Is_Type_Camera_Not_Used()) {
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
    private static bool RoomCamera_IsVisibleAtCameraPosition(On.RoomCamera.orig_IsVisibleAtCameraPosition orig, RoomCamera room_camera, int camera_position_index, Vector2 test_position) {
        if (room_camera.Is_Type_Camera_Not_Used()) {
            return orig(room_camera, camera_position_index, test_position);
        }

        Vector2 screen_size_increase = Is_Camera_Zoom_Enabled ? Half_Inverse_Camera_Zoom_XY * room_camera.sSize : Vector2.zero;
        return test_position.x > room_camera.pos.x - 188f - screen_size_increase.x && test_position.x < room_camera.pos.x + 188f + room_camera.game.rainWorld.options.ScreenSize.x + screen_size_increase.x && test_position.y > room_camera.pos.y - 18f - screen_size_increase.y && test_position.y < room_camera.pos.y + 18f + 768f + screen_size_increase.y;
        // return test_position.x > room_camera.pos.x - 200f - 188f && test_position.x < room_camera.pos.x + 200f + 188f + room_camera.game.rainWorld.options.ScreenSize.x && test_position.y > room_camera.pos.y - 200f - 18f && test_position.y < room_camera.pos.y + 200f + 18f + 768f;
        // return test_position.x > room_camera.pos.x - 380f && test_position.x < room_camera.pos.x + 380f + 1400f && test_position.y > room_camera.pos.y - 20f && test_position.y < room_camera.pos.y + 20f + 800f;
    }

    private static void RoomCamera_MoveCamera(On.RoomCamera.orig_MoveCamera_int orig, RoomCamera room_camera, int camera_position_index) {
        // only called when moving camera positions inside the same room 
        // if the ID changed then do a smooth transition instead 
        // the logic for that is done in UpdateCameraPosition()

        if (room_camera.Is_Type_Camera_Not_Used()) {
            orig(room_camera, camera_position_index);
            return;
        }

        room_camera.currentCameraPosition = camera_position_index;
        if (room_camera.followAbstractCreature != null && room_camera.Get_Attached_Fields().type_camera is VanillaTypeCamera vanilla_type_camera && vanilla_type_camera.are_vanilla_positions_used && vanilla_type_camera.follow_abstract_creature_id == room_camera.followAbstractCreature.ID) {
            // Otherwise, the camera moves after a vanilla transition. But
            // ignore is during a smooth transition, i.e. when follow_abstract_creature_id
            // is set to null (kinda ugly to not have a separate variable for that).
            ResetCameraPosition(room_camera);
        }
    }

    private static void RoomCamera_MoveCamera_Room(On.RoomCamera.orig_MoveCamera_Room_int orig, RoomCamera room_camera, Room loading_room, int loading_camera_index) {
        // Why does the orig() function change the shader and stuff. The room is not changed yet and
        // there is a bug where these things are loaded before the room actually switched.
        // Why is this not done in ChangeRoom() instead?
        if (room_camera.Get_Attached_Fields() is not Attached_Fields attached_fields) {
            orig(room_camera, loading_room, loading_camera_index);
            return;
        }

        // The ShortcutHandler might be too trigger-happy and call this again
        // while the room is pre-loading => return early.
        // The function RoomCamera.ChangeCameraToPlayer() throws a null ref
        // exception. Probably by the same cause.
        if (attached_fields.is_pre_loading_whole_room) return;
        attached_fields.is_pre_loading_whole_room = true;

        // At the very least the screen with camera index equal to zero needs to
        // be loaded again. When you back to larger rooms some of the screens with
        // larger camera index values might still be loaded / cached.
        attached_fields.pre_loaded_camera_index = 0;
        orig(room_camera, loading_room, loading_camera_index);
    }

    // preloads textures // RoomCamera.ApplyPositionChange() is called when they are ready
    private static void RoomCamera_MoveCamera2(On.RoomCamera.orig_MoveCamera2 orig, RoomCamera room_camera, string room_name, int loading_camera_index) {
        // // room is not updated yet;
        // // gets updated in ApplyPositionChange();
        // // although ChangeRoom() has a non-null check;
        // // it would still update the room;
        // // not sure what would happen if the room would be null;
        // // don't do this:
        // if (room_camera.room == null) {
        //     if (!blacklisted_rooms.Contains(room_name)) {
        //         blacklisted_rooms.Add(room_name);
        //     }
        //     orig(room_camera, room_name, camera_position_index);
        //     return;
        // }

        if (room_camera.Get_Attached_Fields() is not Attached_Fields attached_fields) {
            orig(room_camera, room_name, loading_camera_index);
            return;
        }

        // this is consistent with what CRS is doing in this function when it replaces a
        // room;
        if (room_camera.loadingRoom?.abstractRoom.Get_Attached_Fields().name_when_replaced_by_crs is string new_room_name) {
            room_name = new_room_name;
        }

        // The variable is_room_blacklisted is not updated yet. Since the loading
        // might not be completed instantly it needs to be updated in the function
        // ApplyPositionChange() instead. But in any case, I need to check for
        // blacklisted rooms here since for example "RM_AI" can be merged but is
        // incompatible.
        if (blacklisted_rooms.Contains(room_name)) {
            attached_fields.is_pre_loading_whole_room = false;
            orig(room_camera, room_name, loading_camera_index);
            return;
        }

        if (!Option_JIT_Merging && File.Exists(WorldLoader.FindRoomFile(room_name, false, "_0.png"))) {
            orig(room_camera, room_name, -1);
            return;
        }

        if (Option_JIT_Merging && attached_fields.is_pre_loading_whole_room) {
            orig(room_camera, room_name, attached_fields.pre_loaded_camera_index);
            return;
        }
        orig(room_camera, room_name, loading_camera_index);
    }

    private static Color RoomCamera_PixelColorAtCoordinate(On.RoomCamera.orig_PixelColorAtCoordinate orig, RoomCamera room_camera, Vector2 position) {
        if (room_camera.Is_Type_Camera_Not_Used()) return orig(room_camera, position);
        if (room_camera.room is not Room room) return orig(room_camera, position);

        if (Option_JIT_Merging) {
            int camera_index = CameraViewingPoint(room, position);
            if (camera_index != -1) {
                int current_camera_index = room_camera.currentCameraPosition;
                room_camera.currentCameraPosition = camera_index;
                Color result = orig(room_camera, position);
                room_camera.currentCameraPosition = current_camera_index;
                return result;
            }
            return orig(room_camera, position);
        }

        // cancel the effect of the function CamPos() inside the function orig(); otherwise,
        // the color of lights might "jump"; the texture_offset is used to translate room
        // coordinates to level_texture coordinates; these are needed since level_texture.
        // GetPixel() is called;
        return orig(room_camera, position + room_camera.CamPos(room_camera.currentCameraPosition) - room.abstractRoom.Get_Attached_Fields().min_camera_position);
    }

    // use room_camera.pos as reference instead of camPos(..) // seems to be important for unloading graphics and maybe other things
    private static bool RoomCamera_PositionCurrentlyVisible(On.RoomCamera.orig_PositionCurrentlyVisible orig, RoomCamera room_camera, Vector2 test_position, float margin, bool widescreen) {
        if (room_camera.Is_Type_Camera_Not_Used()) {
            return orig(room_camera, test_position, margin, widescreen);
        }

        Vector2 screen_size_increase = Is_Camera_Zoom_Enabled ? Half_Inverse_Camera_Zoom_XY * room_camera.sSize : Vector2.zero;
        return test_position.x > room_camera.pos.x - 188f - margin - (widescreen ? 190f : 0f) - screen_size_increase.x && test_position.x < room_camera.pos.x + 188f + (ModManager.MMF ? room_camera.game.rainWorld.options.ScreenSize.x : 1024f) + margin + (widescreen ? 190f : 0f) + screen_size_increase.x && test_position.y > room_camera.pos.y - 18f - margin - screen_size_increase.y && test_position.y < room_camera.pos.y + 18f + 768f + margin + screen_size_increase.y;
        // return test_position.x > room_camera.pos.x - 200f - 188f - margin - (widescreen ? 190f : 0f) && test_position.x < room_camera.pos.x + 200f + 188f + (ModManager.MMF ? room_camera.game.rainWorld.options.ScreenSize.x : 1024f) + margin + (widescreen ? 190f : 0f) && test_position.y > room_camera.pos.y - 200f - 18f - margin && test_position.y < room_camera.pos.y + 200f + 18f + 768f + margin;
        // return test_position.x > room_camera.pos.x - 380f - margin && test_position.x < room_camera.pos.x + 380f + 1400f + margin && test_position.y > room_camera.pos.y - 20f - margin && test_position.y < room_camera.pos.y + 20f + 800f + margin;
    }

    private static bool RoomCamera_PositionVisibleInNextScreen(On.RoomCamera.orig_PositionVisibleInNextScreen orig, RoomCamera room_camera, Vector2 test_position, float margin, bool widescreen) {
        if (room_camera.Is_Type_Camera_Not_Used()) {
            return orig(room_camera, test_position, margin, widescreen);
        }

        Vector2 screen_size_increase = Is_Camera_Zoom_Enabled ? Half_Inverse_Camera_Zoom_XY * room_camera.sSize : Vector2.zero;
        float screen_size_x = ModManager.MMF ? room_camera.game.rainWorld.options.ScreenSize.x : 1024f;
        return test_position.x > room_camera.pos.x - screen_size_x - 188f - margin - (widescreen ? 190f : 0f) - screen_size_increase.x && test_position.x < room_camera.pos.x + 2f * screen_size_x + 188f + margin + (widescreen ? 190f : 0f) + screen_size_increase.x && test_position.y > room_camera.pos.y - 768f - 18f - margin - screen_size_increase.y && test_position.y < room_camera.pos.y + 2f * 768f + 18f + margin + screen_size_increase.y;
        // return test_position.x > room_camera.pos.x - (ModManager.MMF ? room_camera.game.rainWorld.options.ScreenSize.x : 1024f) - 200f - 188f - margin - (widescreen ? 190f : 0f) && test_position.x < room_camera.pos.x + 2f * (ModManager.MMF ? room_camera.game.rainWorld.options.ScreenSize.x : 1024f) + 200f + 188f + margin + (widescreen ? 190f : 0f) && test_position.y > room_camera.pos.y - 768f - 200f - 18f - margin && test_position.y < room_camera.pos.y + 2f * 768f + 200f + 18f + margin;
        // return test_position.x > room_camera.pos.x - 380f - 1400f - margin && test_position.x < room_camera.pos.x + 380f + 2800f + margin && test_position.y > room_camera.pos.y - 20f - 800f - margin && test_position.y < room_camera.pos.y + 20f + 1600f + margin;
    }

    private static void RoomCamera_PreLoadTexture(On.RoomCamera.orig_PreLoadTexture orig, RoomCamera room_camera, Room room, int camera_position_index) {
        //this function is only called when moving inside rooms but not between them 
        if (!room_camera.Is_Type_Camera_Not_Used()) return;
        orig(room_camera, room, camera_position_index);
    }

    private static bool RoomCamera_RectCurrentlyVisible(On.RoomCamera.orig_RectCurrentlyVisible orig, RoomCamera room_camera, Rect test_rectangle, float margin, bool widescreen) {
        if (room_camera.Is_Type_Camera_Not_Used()) {
            return orig(room_camera, test_rectangle, margin, widescreen);
        }

        Vector2 screen_size_increase = Is_Camera_Zoom_Enabled ? Half_Inverse_Camera_Zoom_XY * room_camera.sSize : Vector2.zero;
        Rect other_rectangle = default;

        other_rectangle.xMin = room_camera.pos.x - 188f - margin - (widescreen ? 190f : 0f) - screen_size_increase.x;
        other_rectangle.xMax = room_camera.pos.x + 188f + (ModManager.MMF ? room_camera.game.rainWorld.options.ScreenSize.x : 1024f) + margin + (widescreen ? 190f : 0f) + screen_size_increase.x;
        other_rectangle.yMin = room_camera.pos.y - 18f - margin - screen_size_increase.y;
        other_rectangle.yMax = room_camera.pos.y + 18f + 768f + margin + screen_size_increase.y;

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

    private static void RoomCamera_ScreenMovement(On.RoomCamera.orig_ScreenMovement orig, RoomCamera room_camera, Vector2? source_position, Vector2 bump, float shake) {
        // should remove effects on camera like camera shakes caused by other creatures // feels weird otherwise
        if (!room_camera.Is_Type_Camera_Not_Used()) return;
        orig(room_camera, source_position, bump, shake);
    }

    private static void RoomCamera_UpdateSnowLight(On.RoomCamera.orig_UpdateSnowLight orig, RoomCamera room_camera) { // Option_JIT_Merging
        orig(room_camera);
        if (room_camera.Render_Texture() is not RenderTexture render_texture) return;
        Graphics.Blit(render_texture, room_camera.SnowTexture, new Material(room_camera.game.rainWorld.Shaders["LevelSnowShader"].shader));
    }

    //
    //
    //

    public sealed class Attached_Fields {
        public bool is_camera_scroll_enabled = true;
        public bool is_camera_scroll_forced_by_split_screen = false;
        public bool is_room_blacklisted = false;
        public bool is_pre_loading_whole_room = false;

        public int pre_loaded_camera_index = 0;

        public Vector2 last_on_screen_position = new();
        public Vector2 on_screen_position = new();

        public IAmATypeCamera type_camera;

        public Attached_Fields(RoomCamera room_camera) {
            if (camera_type == CameraType.Position) {
                type_camera = new PositionTypeCamera(room_camera, this);
                return;
            }

            if (camera_type == CameraType.Vanilla) {
                type_camera = new VanillaTypeCamera(room_camera, this);
                return;
            }
            type_camera = new SwitchTypeCamera(room_camera, this);
        }
    }

    public enum CameraType {
        Position,
        Vanilla,
        Switch
    }
}
