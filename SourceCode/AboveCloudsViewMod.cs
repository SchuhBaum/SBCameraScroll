using UnityEngine;

namespace SBCameraScroll
{
    internal static class AboveCloudsViewMod
    {
        // copy & paste from bee's mod // removes visible "jumps" of clouds
        internal static void OnEnable()
        {
            On.AboveCloudsView.CloseCloud.DrawSprites += CloseCloud_DrawSprites;
            On.AboveCloudsView.DistantCloud.DrawSprites += DistantCloud_DrawSprites;
            On.AboveCloudsView.DistantLightning.DrawSprites += DistantLightning_DrawSprites; // only for adjusting alpha?
            On.AboveCloudsView.FlyingCloud.DrawSprites += FlyingCloud_DrawSprites;

            // On.AboveCloudsView.Fog.DrawSprites += Fog_DrawSprites; // what does this do initially?
        }

        // private static void Fog_DrawSprites(On.AboveCloudsView.Fog.orig_DrawSprites orig, AboveCloudsView.Fog fog, RoomCamera.SpriteLeaser spriteLeaser, RoomCamera roomCamera, float timeStacker, Vector2 cameraPosition)
        // {
        //     if (roomCamera.room == null || fog.room.game.IsArenaSession)
        //     {
        //         orig(fog, spriteLeaser, roomCamera, timeStacker, cameraPosition);
        //         return;
        //     }

        //     Vector2 roomCameraPosition = roomCamera.room.cameraPositions[roomCamera.currentCameraPosition];
        //     roomCamera.room.cameraPositions[roomCamera.currentCameraPosition] = roomCamera.room.cameraPositions[0];
        //     orig(fog, spriteLeaser, roomCamera, timeStacker, cameraPosition);
        //     roomCamera.room.cameraPositions[roomCamera.currentCameraPosition] = roomCameraPosition;
        // }

        // ----------------- //
        // private functions //
        // ----------------- //

        private static void CloseCloud_DrawSprites(On.AboveCloudsView.CloseCloud.orig_DrawSprites orig, AboveCloudsView.CloseCloud closeCloud, RoomCamera.SpriteLeaser spriteLeaser, RoomCamera roomCamera, float timeStacker, Vector2 cameraPosition)
        {
            if (roomCamera.room == null)
            {
                orig(closeCloud, spriteLeaser, roomCamera, timeStacker, cameraPosition);
                return;
            }

            Vector2 roomCameraPosition = roomCamera.room.cameraPositions[roomCamera.currentCameraPosition];
            roomCamera.room.cameraPositions[roomCamera.currentCameraPosition] = roomCamera.room.cameraPositions[0];
            orig(closeCloud, spriteLeaser, roomCamera, timeStacker, cameraPosition);
            roomCamera.room.cameraPositions[roomCamera.currentCameraPosition] = roomCameraPosition;
        }

        private static void DistantCloud_DrawSprites(On.AboveCloudsView.DistantCloud.orig_DrawSprites orig, AboveCloudsView.DistantCloud distantCloud, RoomCamera.SpriteLeaser spriteLeaser, RoomCamera roomCamera, float timeStacker, Vector2 cameraPosition)
        {
            if (roomCamera.room == null)
            {
                orig(distantCloud, spriteLeaser, roomCamera, timeStacker, cameraPosition);
                return;
            }

            Vector2 roomCameraPosition = roomCamera.room.cameraPositions[roomCamera.currentCameraPosition];
            roomCamera.room.cameraPositions[roomCamera.currentCameraPosition] = roomCamera.room.cameraPositions[0];
            orig(distantCloud, spriteLeaser, roomCamera, timeStacker, cameraPosition);
            roomCamera.room.cameraPositions[roomCamera.currentCameraPosition] = roomCameraPosition;
        }

        private static void DistantLightning_DrawSprites(On.AboveCloudsView.DistantLightning.orig_DrawSprites orig, AboveCloudsView.DistantLightning distantLightning, RoomCamera.SpriteLeaser spriteLeaser, RoomCamera roomCamera, float timeStacker, Vector2 cameraPosition)
        {
            if (roomCamera.room == null)
            {
                orig(distantLightning, spriteLeaser, roomCamera, timeStacker, cameraPosition);
                return;
            }

            Vector2 roomCameraPosition = roomCamera.room.cameraPositions[roomCamera.currentCameraPosition];
            roomCamera.room.cameraPositions[roomCamera.currentCameraPosition] = roomCamera.room.cameraPositions[0];
            orig(distantLightning, spriteLeaser, roomCamera, timeStacker, cameraPosition);
            roomCamera.room.cameraPositions[roomCamera.currentCameraPosition] = roomCameraPosition;
        }

        private static void FlyingCloud_DrawSprites(On.AboveCloudsView.FlyingCloud.orig_DrawSprites orig, AboveCloudsView.FlyingCloud flyingCloud, RoomCamera.SpriteLeaser spriteLeaser, RoomCamera roomCamera, float timeStacker, Vector2 cameraPosition)
        {
            if (roomCamera.room == null)
            {
                orig(flyingCloud, spriteLeaser, roomCamera, timeStacker, cameraPosition);
                return;
            }

            Vector2 roomCameraPosition = roomCamera.room.cameraPositions[roomCamera.currentCameraPosition];
            roomCamera.room.cameraPositions[roomCamera.currentCameraPosition] = roomCamera.room.cameraPositions[0];
            orig(flyingCloud, spriteLeaser, roomCamera, timeStacker, cameraPosition);
            roomCamera.room.cameraPositions[roomCamera.currentCameraPosition] = roomCameraPosition;
        }
    }
}
