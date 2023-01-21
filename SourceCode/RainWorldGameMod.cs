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
            RoomCameraMod.allAttachedFields.Clear();
            WormGrassMod.allAttachedFields.Clear();

            Debug.Log("SBCameraScroll: Initialize variables.");
            MainModOptions.instance.MainModOptions_OnConfigChanged(); //TODO // remporary fix for events not working
            orig(game, manager);

            foreach (AbstractCreature abstractPlayer in game.Players)
            {
                int playerNumber = ((PlayerState)abstractPlayer.state).playerNumber;
                EntityID entityID = new(-1, playerNumber);

                if (abstractPlayer.ID != entityID) // copied from JollyCoopFixesAndStuff // I had multiple player with the ID of player 2
                {
                    abstractPlayer.ID = entityID;
                }
            }
        }

        private static void RainWorldGame_ShutDownProcess(On.RainWorldGame.orig_ShutDownProcess orig, RainWorldGame game)
        {
            Debug.Log("SBCameraScroll: Cleanup.");
            orig(game);

            AbstractRoomMod.allAttachedFields.Clear();
            RoomCameraMod.allAttachedFields.Clear();
            WormGrassMod.allAttachedFields.Clear();
        }
    }
}