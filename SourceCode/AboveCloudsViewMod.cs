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

        private static void CloseCloud_DrawSprites(On.AboveCloudsView.CloseCloud.orig_DrawSprites orig, AboveCloudsView.CloseCloud closeCloud, RoomCamera.SpriteLeaser sLeaser, RoomCamera roomCamera, float timeStacker, Vector2 camPos)
        {
            if (roomCamera.room == null)
            {
                orig(closeCloud, sLeaser, roomCamera, timeStacker, camPos);
                return;
            }

            Vector2 cameraPosition = roomCamera.room.cameraPositions[roomCamera.currentCameraPosition];
            roomCamera.room.cameraPositions[roomCamera.currentCameraPosition] = roomCamera.room.cameraPositions[0];
            orig(closeCloud, sLeaser, roomCamera, timeStacker, camPos);
            roomCamera.room.cameraPositions[roomCamera.currentCameraPosition] = cameraPosition;
        }

        private static void DistantCloud_DrawSprites(On.AboveCloudsView.DistantCloud.orig_DrawSprites orig, AboveCloudsView.DistantCloud distantCloud, RoomCamera.SpriteLeaser sLeaser, RoomCamera roomCamera, float timeStacker, Vector2 camPos)
        {
            if (roomCamera.room == null)
            {
                orig(distantCloud, sLeaser, roomCamera, timeStacker, camPos);
                return;
            }

            Vector2 cameraPosition = roomCamera.room.cameraPositions[roomCamera.currentCameraPosition];
            roomCamera.room.cameraPositions[roomCamera.currentCameraPosition] = roomCamera.room.cameraPositions[0];
            orig(distantCloud, sLeaser, roomCamera, timeStacker, camPos);
            roomCamera.room.cameraPositions[roomCamera.currentCameraPosition] = cameraPosition;
        }

        private static void FlyingCloud_DrawSprites(On.AboveCloudsView.FlyingCloud.orig_DrawSprites orig, AboveCloudsView.FlyingCloud flyingCloud, RoomCamera.SpriteLeaser sLeaser, RoomCamera roomCamera, float timeStacker, Vector2 camPos)
        {
            if (roomCamera.room == null)
            {
                orig(flyingCloud, sLeaser, roomCamera, timeStacker, camPos);
                return;
            }

            Vector2 cameraPosition = roomCamera.room.cameraPositions[roomCamera.currentCameraPosition];
            roomCamera.room.cameraPositions[roomCamera.currentCameraPosition] = roomCamera.room.cameraPositions[0];
            orig(flyingCloud, sLeaser, roomCamera, timeStacker, camPos);
            roomCamera.room.cameraPositions[roomCamera.currentCameraPosition] = cameraPosition;
        }
    }
}