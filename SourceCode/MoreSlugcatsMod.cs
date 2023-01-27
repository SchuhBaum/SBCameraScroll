using UnityEngine;

namespace SBCameraScroll
{
    internal static class MoreSlugcatsMod
    {
        internal static void OnEnable()
        {
            // make sprites invisible
            On.MoreSlugcats.BlizzardGraphics.DrawSprites += BlizzardGraphics_DrawSprites;

            // only useful when snowChange changes in Update();
            // On.MoreSlugcats.SnowSource.CheckVisibility += SnowSource_CheckVisibility;
            On.MoreSlugcats.SnowSource.Update += SnowSource_Update;
        }

        //
        // private
        //

        private static void BlizzardGraphics_DrawSprites(On.MoreSlugcats.BlizzardGraphics.orig_DrawSprites orig, MoreSlugcats.BlizzardGraphics blizzardGraphics, RoomCamera.SpriteLeaser spriteLeaser, RoomCamera roomCamera, float timeStacker, Vector2 cameraPosition)
        {
            orig(blizzardGraphics, spriteLeaser, roomCamera, timeStacker, cameraPosition);

            // simply hide the falling snow;
            // I don't want to disable everything since there might be 
            // gameplay implications;
            if (roomCamera.GetAttachedFields().isRoomBlacklisted || roomCamera.voidSeaMode) return;
            spriteLeaser.sprites[0].isVisible = false;
            spriteLeaser.sprites[1].isVisible = false;
        }

        // private static int SnowSource_CheckVisibility(On.MoreSlugcats.SnowSource.orig_CheckVisibility orig, MoreSlugcats.SnowSource snowSource, int cameraIndex)
        // {
        //     Vector2 cameraPosition = snowSource.room.game.cameras[0].pos;
        //     if (snowSource.pos.x > cameraPosition.x - snowSource.rad && snowSource.pos.x < cameraPosition.x + snowSource.rad + 1400f && snowSource.pos.y > cameraPosition.y - snowSource.rad && snowSource.pos.y < cameraPosition.y + snowSource.rad + 800f)
        //     {
        //         return 1;
        //     }
        //     return 0;
        // }

        private static void SnowSource_Update(On.MoreSlugcats.SnowSource.orig_Update orig, MoreSlugcats.SnowSource snowSource, bool eu)
        {
            orig(snowSource, eu);

            // happens once when entering room anyways;
            // the problem with this is that it changes only
            // when the currentCameraPositionIndex changes;
            // this results in visible "jumps";
            // if I do this constantly then I have performance issues;
            // I also couldn't get it to work that the result
            // before jumps is the same as after jumps;
            snowSource.room.game.cameras[0].snowChange = false;
        }
    }
}
