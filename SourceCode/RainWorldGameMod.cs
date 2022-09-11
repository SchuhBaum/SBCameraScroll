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
            Debug.Log("SBCameraScroll: Initialize variables.");
            WormGrassMod.cosmeticWormsOnTiles.Clear();
            orig(game, manager);

            foreach (AbstractCreature abstractPlayer in game.Players)
            {
                int playerNumber = ((PlayerState)abstractPlayer.state).playerNumber;
                EntityID entityID = new EntityID(-1, playerNumber);

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
            WormGrassMod.cosmeticWormsOnTiles.Clear();
        }
    }
}