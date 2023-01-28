using System;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using UnityEngine;

namespace SBCameraScroll
{
    internal static class MoreSlugcatsMod
    {
        internal static void OnEnable()
        {
            // On.MoreSlugcats.BlizzardGraphics.DrawSprites += BlizzardGraphics_DrawSprites;
            IL.MoreSlugcats.BlizzardGraphics.DrawSprites += IL_BlizzardGraphics_DrawSprites;
            On.MoreSlugcats.BlizzardGraphics.Update += BlizzardGraphics_Update;

            // On.MoreSlugcats.SnowSource.CheckVisibility += SnowSource_CheckVisibility;
            // On.MoreSlugcats.SnowSource.PackSnowData += SnowSource_PackSnowData;
            On.MoreSlugcats.SnowSource.Update += SnowSource_Update;

            // On.MoreSlugcats.Snow.DrawSprites += Snow_DrawSprites;
        }

        // private static void Snow_DrawSprites(On.MoreSlugcats.Snow.orig_DrawSprites orig, MoreSlugcats.Snow snow, RoomCamera.SpriteLeaser spriteLeaser, RoomCamera roomCamera, float timeStacker, Vector2 cameraPosition)
        // {
        //     orig(snow, spriteLeaser, roomCamera, timeStacker, cameraPosition);
        //     if (snow.visibleSnow > 0)
        //     {
        //         spriteLeaser.sprites[0].x = Mathf.Lerp(snow., snow.pos.x, timeStacker) - cameraPosition.x;
        //         spriteLeaser.sprites[0].y = Mathf.Lerp(snow.lastPos.y, snow.pos.y, timeStacker) - cameraPosition.y;
        //     }
        // }

        //
        // private
        //

        private static void IL_BlizzardGraphics_DrawSprites(ILContext context)
        {
            ILCursor cursor = new(context);
            // MainMod.LogAllInstructions(context);

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
                   if (roomCamera.GetAttachedFields().isRoomBlacklisted || roomCamera.voidSeaMode)
                   {
                       return roomCamera.pos - blizzardGraphics.room.cameraPositions[roomCamera.currentCameraPosition];
                   }
                   //    return cameraPosition - blizzardGraphics.room.cameraPositions[0];
                   //    return cameraPosition - blizzardGraphics.room.abstractRoom.GetAttachedFields().textureOffset;

                   // jumps but I can't cover the whole room otherwise..
                   return cameraPosition - blizzardGraphics.room.cameraPositions[roomCamera.currentCameraPosition];
               });
            }
            else
            {
                Debug.LogException(new Exception("SBCameraScroll: IL_BlizzardGraphics_DrawSprites_1 failed."));
            }

            // if (cursor.TryGotoNext(instruction => instruction.MatchLdfld<RoomCamera>("currentCameraPosition")))
            // {
            //     Debug.Log("SBCameraScroll: IL_BlizzardGraphics_DrawSprites_1: Index " + cursor.Index); // 23
            //                                                                                            // cursor.Remove(); // remove currentCameraPosition //TODO
            //     cursor.Goto(cursor.Index - 4);
            //     cursor.RemoveRange(6); // remove room.cameraPositions[this.rCam.currentCameraPosition]

            //     // cursor.Emit<MoreSlugcats.BlizzardGraphics>(OpCodes.Ldfld, "rCam");
            //     cursor.EmitDelegate<Func<MoreSlugcats.BlizzardGraphics, Vector2>>(blizzardGraphics =>
            //    {
            //        RoomCamera roomCamera = blizzardGraphics.rCam;
            //        if (roomCamera.GetAttachedFields().isRoomBlacklisted || roomCamera.voidSeaMode)
            //        {
            //            return blizzardGraphics.room.cameraPositions[roomCamera.currentCameraPosition];
            //        }
            //        return blizzardGraphics.room.abstractRoom.GetAttachedFields().textureOffset;
            //    });
            // }
            // else
            // {
            //     Debug.LogException(new Exception("SBCameraScroll: IL_BlizzardGraphics_DrawSprites_1 failed."));
            // }

            // if (cursor.TryGotoNext(instruction => instruction.MatchCallvirt<Options>("get_ScreenSize")))
            // {
            //     Debug.Log("SBCameraScroll: IL_BlizzardGraphics_DrawSprites_2: Index " + cursor.Index); // 32 // 30
            //     cursor.Goto(cursor.Index - 4);
            //     cursor.RemoveRange(5); // remove game.RainWorld.Options.sSize

            //     cursor.Emit<MoreSlugcats.BlizzardGraphics>(OpCodes.Ldfld, "rCam");
            //     cursor.EmitDelegate<Func<RoomCamera, Vector2>>(roomCamera =>
            //    {
            //        if (roomCamera.room == null || roomCamera.GetAttachedFields().isRoomBlacklisted || roomCamera.voidSeaMode)
            //        {
            //            return roomCamera.sSize; // vanilla case again that was being removed before
            //        }

            //        // camera scroll case 
            //        // extend blizzard to the whole room
            //        //    return new Vector2(roomCamera.room.PixelWidth, roomCamera.room.PixelHeight); //TODO
            //        return new Vector2(roomCamera.levelTexture.width, roomCamera.levelTexture.height);
            //    });
            // }
            // else
            // {
            //     Debug.LogException(new Exception("SBCameraScroll: IL_BlizzardGraphics_DrawSprites_2 failed."));
            // }
            // MainMod.LogAllInstructions(context);
        }

        //
        //
        //

        // private static void BlizzardGraphics_DrawSprites(On.MoreSlugcats.BlizzardGraphics.orig_DrawSprites orig, MoreSlugcats.BlizzardGraphics blizzardGraphics, RoomCamera.SpriteLeaser spriteLeaser, RoomCamera roomCamera, float timeStacker, Vector2 cameraPosition)
        // {

        //     if (blizzardGraphics.slatedForDeletetion)
        //     {
        //         spriteLeaser.sprites[0].isVisible = false;
        //         spriteLeaser.sprites[1].isVisible = false;
        //         return;
        //     }

        //     // can I change the size of the whole thing?
        //     // or do I need to place multiple ones?

        //     // Vector2 vector = blizzardGraphics.rCam.pos - blizzardGraphics.room.cameraPositions[blizzardGraphics.rCam.currentCameraPosition];
        //     // Vector2 vector = cameraPosition - blizzardGraphics.room.cameraPositions[0]; // better
        //     Vector2 vector = cameraPosition - blizzardGraphics.room.abstractRoom.GetAttachedFields().textureOffset; // better
        //     // Vector2 vector = cameraPosition; // needs offset // does not work
        //     // Vector2 offset = Vector2.Lerp(blizzardGraphics.lastCamPos, blizzardGraphics.room.game.rainWorld.options.ScreenSize * 0.5f, timeStacker); // constant?
        //     // Debug.Log("blizzardGraphics.lastCamPos " + blizzardGraphics.lastCamPos); // yes, constant...

        //     // Vector2 offset = blizzardGraphics.room.game.rainWorld.options.ScreenSize * 0.5f; 
        //     // jumps but I can't cover the whole room otherwise..
        //     Vector2 offset = blizzardGraphics.room.cameraPositions[roomCamera.currentCameraPosition] + blizzardGraphics.room.game.rainWorld.options.ScreenSize * 0.5f;
        //     // Vector2 offset = 0.5f * new Vector2(roomCamera.levelTexture.width, roomCamera.levelTexture.height); // better
        //     // Vector2 offset = 0.5f * new Vector2(roomCamera.levelTexture.width, roomCamera.levelTexture.height); // better

        //     spriteLeaser.sprites[0].x = offset.x - vector.x;
        //     spriteLeaser.sprites[0].y = offset.y * 2f - vector.y;
        //     spriteLeaser.sprites[1].x = offset.x - vector.x;
        //     spriteLeaser.sprites[1].y = offset.y - vector.y;

        //     float num = Mathf.Lerp(blizzardGraphics.oldSnowFallIntensity, blizzardGraphics.SnowfallIntensity, blizzardGraphics.upDeLerp);
        //     float num2 = Mathf.Lerp(blizzardGraphics.oldBlizzardIntensity, blizzardGraphics.BlizzardIntensity, blizzardGraphics.upDeLerp);
        //     float num3 = Mathf.Lerp(blizzardGraphics.oldWhiteOut, blizzardGraphics.WhiteOut, blizzardGraphics.upDeLerp);
        //     float num4 = Mathf.Lerp(blizzardGraphics.oldWindStrength, blizzardGraphics.WindStrength, blizzardGraphics.upDeLerp);
        //     float num5 = Mathf.Lerp(blizzardGraphics.oldWindAngle, blizzardGraphics.WindAngle, blizzardGraphics.upDeLerp);

        //     Shader.SetGlobalFloat("_windAngle", num5);
        //     Shader.SetGlobalFloat("_windStrength", num4);

        //     spriteLeaser.sprites[0].isVisible = num > 0f;
        //     spriteLeaser.sprites[1].isVisible = num2 > 0f;

        //     spriteLeaser.sprites[0].color = new Color(num, 0f, 0f);
        //     spriteLeaser.sprites[0].scaleY = 170f * (1f + (num4 + (1f - Mathf.Abs(num5)) * num4) * (4f + 4f * num3)); // scales the snow but not the whole thing
        //     spriteLeaser.sprites[0].rotation = Mathf.Lerp(blizzardGraphics.oldSnowAngle, blizzardGraphics.snowAngle, blizzardGraphics.upDeLerp);

        //     spriteLeaser.sprites[1].scaleX = 170f * (1f + 0.8f * num3);
        //     spriteLeaser.sprites[1].scaleX = 170f * (1f + 0.4f * num3);
        //     spriteLeaser.sprites[1].color = new Color(num2, num3, 0f);
        //     spriteLeaser.sprites[1].rotation = Mathf.Lerp(blizzardGraphics.oldBlizzardAngle, blizzardGraphics.blizzardAngle, blizzardGraphics.upDeLerp);

        //     blizzardGraphics.lastCamPos = offset;
        //     blizzardGraphics.UpdateWindMap();
        // }

        private static void BlizzardGraphics_Update(On.MoreSlugcats.BlizzardGraphics.orig_Update orig, MoreSlugcats.BlizzardGraphics blizzardGraphics, bool eu)
        {
            RoomCamera roomCamera = blizzardGraphics.rCam;
            if (roomCamera.room == null)
            {
                orig(blizzardGraphics, eu);
                return;
            }

            if (roomCamera.GetAttachedFields().isRoomBlacklisted || roomCamera.voidSeaMode)
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
            // these here seem to don't have that;
            Vector2 roomCameraPosition = roomCamera.room.cameraPositions[roomCamera.currentCameraPosition];
            roomCamera.room.cameraPositions[roomCamera.currentCameraPosition] = roomCamera.room.cameraPositions[0];
            orig(blizzardGraphics, eu);
            roomCamera.room.cameraPositions[roomCamera.currentCameraPosition] = roomCameraPosition;

            // Vector4 value = new Vector4(roomCamera.levelTexture.width / ((float)roomCamera.room.TileWidth * 20f) * (1366f / roomCamera.levelTexture.width) * 1.02f, roomCamera.levelTexture
            // .height / ((float)roomCamera.room.TileHeight * 20f) * 1.04f, roomCamera.room.cameraPositions[0].x / ((float)roomCamera.room.TileWidth * 20f), roomCamera.room.cameraPositions[0].y / ((float)roomCamera.room.TileHeight * 20f));
            // Shader.SetGlobalVector("_tileCorrection", value);
            // blizzardGraphics.vel = new Vector2(100000f, 10000f);
            // blizzardGraphics.pos = roomCamera.pos;
        }

        //
        //
        //

        // private static int SnowSource_CheckVisibility(On.MoreSlugcats.SnowSource.orig_CheckVisibility orig, MoreSlugcats.SnowSource snowSource, int cameraIndex)
        // {
        //     // add check if room is blacklisted
        //     Vector2 cameraPosition = snowSource.room.game.cameras[0].pos;
        //     if (snowSource.pos.x > cameraPosition.x - snowSource.rad && snowSource.pos.x < cameraPosition.x + snowSource.rad + 1400f && snowSource.pos.y > cameraPosition.y - snowSource.rad && snowSource.pos.y < cameraPosition.y + snowSource.rad + 800f)
        //     {
        //         return 1;
        //     }
        //     return 0;
        // }

        // private static Vector4[] SnowSource_PackSnowData(On.MoreSlugcats.SnowSource.orig_PackSnowData orig, MoreSlugcats.SnowSource snowSource)
        // {
        //     // maybe use fixed anchor instead of roomCamera.pos
        //     // and use the whole room instead of 1400x800; TODO?
        //     if (snowSource.room == null) return orig(snowSource);

        //     RoomCamera roomCamera = snowSource.room.game.cameras[0];
        //     if (roomCamera.GetAttachedFields().isRoomBlacklisted) return orig(snowSource);
        //     if (roomCamera.voidSeaMode) return orig(snowSource);

        //     // what does this exactly do?
        //     // generating red green values out of one float?
        //     // simply a better storage thing?
        //     Vector2 xRedGreen = Custom.EncodeFloatRG((snowSource.pos.x - roomCamera.pos.x) / 1400f * 0.3f + 0.3f);
        //     Vector2 yRedGreen = Custom.EncodeFloatRG((snowSource.pos.y - roomCamera.pos.y) / 800f * 0.3f + 0.3f);
        //     Vector2 rRedGreen = Custom.EncodeFloatRG(snowSource.rad / 1600f);

        //     Vector4[] array = new Vector4[3];
        //     array[0] = new Vector4(xRedGreen.x, xRedGreen.y, yRedGreen.x, yRedGreen.y);
        //     array[1] = new Vector4(rRedGreen.x, rRedGreen.y, snowSource.intensity, snowSource.noisiness);
        //     array[2] = new Vector4(0f, 0f, 0f, (int)snowSource.shape / 5f);
        //     return array;
        // }

        private static void SnowSource_Update(On.MoreSlugcats.SnowSource.orig_Update orig, MoreSlugcats.SnowSource snowSource, bool eu)
        {
            orig(snowSource, eu);

            // snowChange updates the overlay texture for fallen snow;
            // skip that for now;
            // jumps too much;
            if (snowSource.room?.game.cameras[0] is not RoomCamera roomCamera) return;
            if (roomCamera.GetAttachedFields().isRoomBlacklisted || roomCamera.voidSeaMode) return;
            snowSource.room.game.cameras[0].snowChange = false;
        }
    }
}
