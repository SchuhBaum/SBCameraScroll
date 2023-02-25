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

        private static void RainWorldGame_ctor(On.RainWorldGame.orig_ctor orig, RainWorldGame game, ProcessManager manager)
        {
            AbstractRoomMod.allAttachedFields.Clear();
            RoomCameraMod.all_attached_fields.Clear();
            WormGrassMod.allAttachedFields.Clear();

            Debug.Log("SBCameraScroll: Initialize variables.");
            orig(game, manager);
        }

        private static void RainWorldGame_ShutDownProcess(On.RainWorldGame.orig_ShutDownProcess orig, RainWorldGame game)
        {
            Debug.Log("SBCameraScroll: Cleanup.");
            orig(game);

            AbstractRoomMod.allAttachedFields.Clear();
            RoomCameraMod.all_attached_fields.Clear();
            WormGrassMod.allAttachedFields.Clear();
        }
    }
}