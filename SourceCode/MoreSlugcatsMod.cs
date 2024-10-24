﻿using Mono.Cecil.Cil;
using MonoMod.Cil;
using RWCustom;
using System;
using UnityEngine;
using static SBCameraScroll.MainMod;

namespace SBCameraScroll;

internal static class MoreSlugcatsMod {
    internal static void OnEnable() {
        IL.MoreSlugcats.BlizzardGraphics.DrawSprites += IL_BlizzardGraphics_DrawSprites;
        On.MoreSlugcats.BlizzardGraphics.Update += BlizzardGraphics_Update;
        On.MoreSlugcats.SnowSource.PackSnowData += SnowSource_PackSnowData;
        On.MoreSlugcats.SnowSource.Update += SnowSource_Update;
    }

    //
    // private
    //

    private static void IL_BlizzardGraphics_DrawSprites(ILContext context) {
        // MainMod.LogAllInstructions(context);

        ILCursor cursor = new(context);
        if (cursor.TryGotoNext(instruction => instruction.MatchLdarg(2))) {
            if (can_log_il_hooks) {
                Debug.Log("SBCameraScroll: IL_BlizzardGraphics_DrawSprites: Index " + cursor.Index); // 16
            }

            cursor.Next.OpCode = OpCodes.Ldarg_0; // blizzardGraphics
            cursor.GotoNext();
            cursor.RemoveRange(9);
            cursor.Emit(OpCodes.Ldarg, 4); // cameraPosition

            cursor.EmitDelegate<Func<MoreSlugcats.BlizzardGraphics, Vector2, Vector2>>((blizzard_graphics, camera_position) => {
                RoomCamera room_camera = blizzard_graphics.rCam;
                if (room_camera.Is_Type_Camera_Not_Used()) {
                    return room_camera.pos - blizzard_graphics.room.cameraPositions[room_camera.currentCameraPosition];
                }

                // jumps but I can't cover the whole room otherwise..;
                // the size of blizzardGraphics is probably determined by the shader;
                // if you scale the sprites then the snow flakes are scaled instead;
                return camera_position - blizzard_graphics.room.cameraPositions[room_camera.currentCameraPosition];
            });
        } else {
            if (can_log_il_hooks) {
                Debug.Log("SBCameraScroll: IL_BlizzardGraphics_DrawSprites failed.");
            }
            return;
        }
        // MainMod.LogAllInstructions(context);
    }

    //
    //
    //

    private static void BlizzardGraphics_Update(On.MoreSlugcats.BlizzardGraphics.orig_Update orig, MoreSlugcats.BlizzardGraphics blizzard_graphics, bool eu) {
        RoomCamera room_camera = blizzard_graphics.rCam;
        if (room_camera.room == null) {
            orig(blizzard_graphics, eu);
            return;
        }

        if (room_camera.Is_Type_Camera_Not_Used()) {
            orig(blizzard_graphics, eu);
            return;
        }

        // prevent jumps when currentCameraPositionIndex changes
        // this does not seem to change the position;
        // the jumps in DrawSprites have a purpose;
        // because the whole thing seems to be fixed in size;
        // I can only move it around;
        //
        // these here seem to don't have that; they have see below;
        float room_camera_position_x = room_camera.room.cameraPositions[room_camera.currentCameraPosition].x;

        // this is a compromise (only change x);
        // this results in extra jumps for the snow;
        // but otherwise show might fall through the ceiling for some screens;

        room_camera.room.cameraPositions[room_camera.currentCameraPosition].x = room_camera.room.cameraPositions[0].x;
        orig(blizzard_graphics, eu);
        room_camera.room.cameraPositions[room_camera.currentCameraPosition].x = room_camera_position_x;
    }

    private static Vector4[] SnowSource_PackSnowData(On.MoreSlugcats.SnowSource.orig_PackSnowData orig, MoreSlugcats.SnowSource snow_source) {
        if (snow_source.room is not Room room) return orig(snow_source);
        RoomCamera room_camera = snow_source.room.game.cameras[0];
        if (room_camera.Is_Type_Camera_Not_Used()) return orig(snow_source);

        // this should be more consistent with vanilla; min_camera_position is in most cases
        // the camera position of the bottom left screen (unless the max texture size is reached);
        // level texture size would be (1400f, 800f) for one screen;
        Vector2 min_camera_position = room.abstractRoom.Get_Attached_Fields().min_camera_position;

        // saves an approximation of a float (in [0, 1)) (in steps of size 1f/255f) and the remainder (times 255f for some reason) in a Vector2;
        Vector2 approximated_position_x = Custom.EncodeFloatRG((snow_source.pos.x - min_camera_position.x) / room_camera.levelTexture.width * 0.3f + 0.3f);
        Vector2 approximated_position_y = Custom.EncodeFloatRG((snow_source.pos.y - min_camera_position.y) / room_camera.levelTexture.height * 0.3f + 0.3f);

        // all snow sources being visible (prevents pop ins); for some reason there
        // is too much snow; this is a workaround such that snow sources have less
        // impact => less snow;
        Vector2 approximated_radius = Custom.EncodeFloatRG(snow_source.rad / (1600f * Mathf.Max(room_camera.levelTexture.width / 1400f, room_camera.levelTexture.height / 800f)));

        Vector4[] array = new Vector4[3];
        array[0] = new Vector4(approximated_position_x.x, approximated_position_x.y, approximated_position_y.x, approximated_position_y.y);
        array[1] = new Vector4(approximated_radius.x, approximated_radius.y, snow_source.intensity, snow_source.noisiness);
        array[2] = new Vector4(0f, 0f, 0f, (float)snow_source.shape / 5f);
        return array;
    }

    private static void SnowSource_Update(On.MoreSlugcats.SnowSource.orig_Update orig, MoreSlugcats.SnowSource snow_source, bool eu) {
        // snowChange updates the overlay texture for fallen snow;
        if (snow_source.room?.game.cameras[0] is not RoomCamera room_camera) {
            orig(snow_source, eu);
            return;
        }

        if (room_camera.Is_Type_Camera_Not_Used()) {
            orig(snow_source, eu);
            return;
        }

        // when entering the room;
        if (room_camera.snowChange) {
            snow_source.visibility = 1;
            return;
        }

        orig(snow_source, eu);

        // visibility equal to one means that it is used when the snow light is updated in roomCamera;
        // there doesn't seem to be downside to always setting it to one;
        // snowSource.visibility = roomCamera.CheckVisibility(snowSource);
        // snowSource.visibility = roomCamera.PositionVisibleInNextScreen(snowSource.pos, 100f, true) ? 1 : 0;
        snow_source.visibility = 1;

        // this is too performance intensive;
        // changing visibility seems to be enough anyways;
        // roomCamera.snowChange = true;

        // don't update again after the room is loaded;
        room_camera.snowChange = false;
    }
}
