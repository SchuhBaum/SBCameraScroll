using System;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RWCustom;
using UnityEngine;

namespace SBCameraScroll
{
    internal static class MoreSlugcatsMod
    {
        internal static void OnEnable()
        {
            IL.MoreSlugcats.BlizzardGraphics.DrawSprites += IL_BlizzardGraphics_DrawSprites;
            On.MoreSlugcats.BlizzardGraphics.Update += BlizzardGraphics_Update;

            On.MoreSlugcats.SnowSource.PackSnowData += SnowSource_PackSnowData;
            On.MoreSlugcats.SnowSource.Update += SnowSource_Update;
        }

        //
        // private
        //

        private static void IL_BlizzardGraphics_DrawSprites(ILContext context)
        {
            // MainMod.LogAllInstructions(context);

            ILCursor cursor = new(context);
            if (cursor.TryGotoNext(instruction => instruction.MatchLdarg(2)))
            {
                Debug.Log("SBCameraScroll: IL_BlizzardGraphics_DrawSprites_1: Index " + cursor.Index); // 16

                cursor.Next.OpCode = OpCodes.Ldarg_0; // blizzardGraphics
                cursor.GotoNext();
                cursor.RemoveRange(9);
                cursor.Emit(OpCodes.Ldarg, 4); // cameraPosition

                cursor.EmitDelegate<Func<MoreSlugcats.BlizzardGraphics, Vector2, Vector2>>((blizzardGraphics, cameraPosition) =>
                {
                    RoomCamera roomCamera = blizzardGraphics.rCam;
                    if (roomCamera.Is_Type_Camera_Not_Used())
                    {
                        return roomCamera.pos - blizzardGraphics.room.cameraPositions[roomCamera.currentCameraPosition];
                    }

                    // jumps but I can't cover the whole room otherwise..;
                    // the size of blizzardGraphics is probably determined by the shader;
                    // if you scale the sprites then the snow flakes are scaled instead;
                    return cameraPosition - blizzardGraphics.room.cameraPositions[roomCamera.currentCameraPosition];
                });
            }
            else
            {
                Debug.LogException(new Exception("SBCameraScroll: IL_BlizzardGraphics_DrawSprites_1 failed."));
            }
            // MainMod.LogAllInstructions(context);
        }

        //
        //
        //

        private static void BlizzardGraphics_Update(On.MoreSlugcats.BlizzardGraphics.orig_Update orig, MoreSlugcats.BlizzardGraphics blizzardGraphics, bool eu)
        {
            RoomCamera roomCamera = blizzardGraphics.rCam;
            if (roomCamera.room == null)
            {
                orig(blizzardGraphics, eu);
                return;
            }

            if (roomCamera.Is_Type_Camera_Not_Used())
            {
                orig(blizzardGraphics, eu);
                return;
            }

            // prevent jumps when currentCameraPositionIndex changes
            // this does not seem to change the position;
            // the jumps in DrawSprites have a purpose;
            // because the whole thing seems to be fixed in size;
            // I can only move it around;
            //
            // these here seem to don't have that; they have see below;
            float roomCameraPositionX = roomCamera.room.cameraPositions[roomCamera.currentCameraPosition].x;

            // this is a compromise (only change x);
            // this results in extra jumps for the snow;
            // but otherwise show might fall through the ceiling for some screens;

            roomCamera.room.cameraPositions[roomCamera.currentCameraPosition].x = roomCamera.room.cameraPositions[0].x;
            orig(blizzardGraphics, eu);
            roomCamera.room.cameraPositions[roomCamera.currentCameraPosition].x = roomCameraPositionX;
        }

        private static Vector4[] SnowSource_PackSnowData(On.MoreSlugcats.SnowSource.orig_PackSnowData orig, MoreSlugcats.SnowSource snowSource)
        {
            if (snowSource.room == null) return orig(snowSource);

            RoomCamera roomCamera = snowSource.room.game.cameras[0];
            if (roomCamera.Is_Type_Camera_Not_Used()) return orig(snowSource);

            // what does this exactly do?
            // generating red green values out of one float?
            // simply a better storage thing?

            // this generates too much snow as well as
            // snow in areas where there shouldn't be;

            // roomCamera.pos.x without scroll is in most cases just the texture offset for the current camera (bottom left position; not anchored;);
            // for on-screen values this would lead to [0, 1] * 0.3f + 0.3f;

            // Vector2 xRedGreen = Custom.EncodeFloatRG((snowSource.pos.x - roomCamera.pos.x) / 1400f * 0.3f + 0.3f);
            // Vector2 yRedGreen = Custom.EncodeFloatRG((snowSource.pos.y - roomCamera.pos.y) / 800f * 0.3f + 0.3f);
            // Vector2 rRedGreen = Custom.EncodeFloatRG(snowSource.rad / 1600f);

            // Vector2 xRedGreen = Custom.EncodeFloatRG((snowSource.pos.x - roomCamera.pos.x) / roomCamera.levelTexture.width * 0.3f + 0.3f);
            // Vector2 yRedGreen = Custom.EncodeFloatRG((snowSource.pos.y - roomCamera.pos.y) / roomCamera.levelTexture.height * 0.3f + 0.3f);
            // Vector2 rRedGreen = Custom.EncodeFloatRG(snowSource.rad / (2f * roomCamera.levelTexture.height));

            // Vector2 xRedGreen = Custom.EncodeFloatRG((snowSource.pos.x - roomCamera.CamPos(roomCamera.currentCameraPosition).x) / 1400f * 0.3f + 0.3f);
            // Vector2 yRedGreen = Custom.EncodeFloatRG((snowSource.pos.y - roomCamera.CamPos(roomCamera.currentCameraPosition).y) / 800f * 0.3f + 0.3f);

            // prevent the texture scrolling with the camera;
            // instead have fixed positions;
            // how does rad need to change?;
            // leaving it at snowSource.rad/1600f creates too much snow;
            // Vector2 xRedGreen = Custom.EncodeFloatRG(snowSource.pos.x / roomCamera.levelTexture.width * 0.3f + 0.3f);
            // Vector2 yRedGreen = Custom.EncodeFloatRG(snowSource.pos.y / roomCamera.levelTexture.height * 0.3f + 0.3f);
            // Vector2 rRedGreen = Custom.EncodeFloatRG(snowSource.rad / (1600f * 0.5f * (roomCamera.levelTexture.width / 1400f + roomCamera.levelTexture.height / 800f)));

            Vector2 xRedGreen = Custom.EncodeFloatRG(snowSource.pos.x / snowSource.room.PixelWidth * 0.3f + 0.3f);
            Vector2 yRedGreen = Custom.EncodeFloatRG(snowSource.pos.y / snowSource.room.PixelHeight * 0.3f + 0.3f);
            Vector2 rRedGreen = Custom.EncodeFloatRG(snowSource.rad / (1600f * Mathf.Max(roomCamera.levelTexture.width / 1400f, roomCamera.levelTexture.height / 800f)));

            // Vector2 rRedGreen = Custom.EncodeFloatRG(snowSource.rad / 1600f);
            // Vector2 rRedGreen = Custom.EncodeFloatRG(snowSource.rad / (2f * roomCamera.levelTexture.height));
            // Vector2 rRedGreen = Custom.EncodeFloatRG(snowSource.rad / (roomCamera.levelTexture.width + 200f));
            // Vector2 rRedGreen = Custom.EncodeFloatRG(snowSource.rad / (1400f + 200f * 0.5f * (roomCamera.levelTexture.width / 1400f + roomCamera.levelTexture.height / 800f)));
            // Vector2 rRedGreen = Custom.EncodeFloatRG(snowSource.rad / (1600f * snowSource.room.cameraPositions.Length));
            // Vector2 xRedGreen = Custom.EncodeFloatRG(snowSource.pos.x / 1400f * 0.3f + 0.3f);
            // Vector2 yRedGreen = Custom.EncodeFloatRG(snowSource.pos.y / 800f * 0.3f + 0.3f);
            // Vector2 rRedGreen = Custom.EncodeFloatRG(snowSource.rad / 1600f);

            Vector4[] array = new Vector4[3];
            array[0] = new Vector4(xRedGreen.x, xRedGreen.y, yRedGreen.x, yRedGreen.y);
            array[1] = new Vector4(rRedGreen.x, rRedGreen.y, snowSource.intensity, snowSource.noisiness);
            array[2] = new Vector4(0f, 0f, 0f, (float)snowSource.shape / 5f);
            return array;
        }

        private static void SnowSource_Update(On.MoreSlugcats.SnowSource.orig_Update orig, MoreSlugcats.SnowSource snowSource, bool eu)
        {
            // snowChange updates the overlay texture for fallen snow;
            if (snowSource.room?.game.cameras[0] is not RoomCamera roomCamera)
            {
                orig(snowSource, eu);
                return;
            }

            if (roomCamera.Is_Type_Camera_Not_Used())
            {
                orig(snowSource, eu);
                return;
            }

            // when entering the room;
            if (roomCamera.snowChange)
            {
                snowSource.visibility = 1;
                return;
            }

            orig(snowSource, eu);

            // visibility equal to one means that it is used when the snow light is updated in roomCamera;
            // there doesn't seem to be downside to always setting it to one;
            // snowSource.visibility = roomCamera.CheckVisibility(snowSource);
            // snowSource.visibility = roomCamera.PositionVisibleInNextScreen(snowSource.pos, 100f, true) ? 1 : 0;
            snowSource.visibility = 1;

            // this is too performance intensive;
            // changing visibility seems to be enough anyways;
            // roomCamera.snowChange = true;

            // don't update again after the room is loaded;
            roomCamera.snowChange = false;
        }
    }
}
