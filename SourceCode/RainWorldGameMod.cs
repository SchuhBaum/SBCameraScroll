using UnityEngine;

namespace SBCameraScroll
{
    internal static class RainWorldGameMod
    {
        internal static void OnEnable()
        {
            On.RainWorldGame.ctor += RainWorldGame_ctor;
            On.RainWorldGame.ShutDownProcess += RainWorldGame_ShutDownProcess; // should be good practice to free all important stuff when shutting down
        }

        // ----------------- //
        // private functions //
        // ----------------- //

        private static void RainWorldGame_ctor(On.RainWorldGame.orig_ctor orig, RainWorldGame rainWorldGame, ProcessManager manager)
        {
            Debug.Log("SBCameraScroll: Initialize variables.");

            AbstractRoomMod.textureOffset.Clear(); // put before orig or it freezes
            WormGrassMod.cosmeticWormsOnTile.Clear();

            orig(rainWorldGame, manager);
            int cameraCount = rainWorldGame.cameras.Length;

            RoomCameraMod.followAbstractCreature = new AbstractCreature?[cameraCount];
            RoomCameraMod.lastOnScreenPosition = new Vector2[cameraCount];
            RoomCameraMod.onScreenPosition = new Vector2[cameraCount];
            RoomCameraMod.vanillaTypePosition = new Vector2[cameraCount];

            RoomCameraMod.isRoomBlacklisted = new bool[cameraCount];
            RoomCameraMod.useVanillaPositions = new bool[cameraCount];
            RoomCameraMod.isCentered = new bool[cameraCount];

            Debug.Log("SBCameraScroll: cameraCount " + cameraCount);
        }

        private static void RainWorldGame_ShutDownProcess(On.RainWorldGame.orig_ShutDownProcess orig, RainWorldGame game)
        {
            Debug.Log("SBCameraScroll: Cleanup.");

            orig(game);

            RoomCameraMod.followAbstractCreature = new AbstractCreature?[0];
            RoomCameraMod.lastOnScreenPosition = new Vector2[0];
            RoomCameraMod.onScreenPosition = new Vector2[0];
            RoomCameraMod.vanillaTypePosition = new Vector2[0];

            RoomCameraMod.isRoomBlacklisted = new bool[0];
            RoomCameraMod.useVanillaPositions = new bool[0];
            RoomCameraMod.isCentered = new bool[0];

            AbstractRoomMod.textureOffset.Clear();
            WormGrassMod.cosmeticWormsOnTile.Clear();
        }
    }
}