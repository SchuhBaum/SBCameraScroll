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
            On.AboveCloudsView.FlyingCloud.DrawSprites += FlyingCloud_DrawSprites;
        }

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

            // this makes the position as it should be // i.e. it respects a moving camera (even without scrolling the camera moves)
            // however this messes with the light of the cloud shader // the clouds look like light bulbs turning on and off when scrolling very quickly
            spriteLeaser.sprites[1].x = closeCloud.DrawPos(cameraPosition, roomCamera.hDisplace).x;
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
            spriteLeaser.sprites[1].x = distantCloud.DrawPos(cameraPosition, roomCamera.hDisplace).x;
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
            spriteLeaser.sprites[0].x = flyingCloud.DrawPos(cameraPosition, roomCamera.hDisplace).x;
        }
    }
}
