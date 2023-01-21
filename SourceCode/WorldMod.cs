using System.Collections.Generic;
using UnityEngine;

namespace SBCameraScroll
{
    internal static class WorldMod
    {
        internal static void OnEnable()
        {
            On.World.LoadWorld += World_LoadWorld;
        }

        // ----------------- //
        // private functions //
        // ----------------- //

        private static void World_LoadWorld(On.World.orig_LoadWorld orig, World world, SlugcatStats.Name slugcatName, List<AbstractRoom> abstractRoomsList, int[] swarmRooms, int[] shelters, int[] gates)
        {
            // AbstractRoomMod.textureOffset.Clear(); // this freezes gate transitions when using SplitScreenMod
            // WormGrassMod.cosmeticWormsOnTile.Clear(); // probably caused freezes as well // too risky to do stuff like this while rooms still being updated(?)
            // RainWorldGameMod.ClearAllWormGrass(); // safer but too slow // there might worm grass already been created for the (new) world

            orig(world, slugcatName, abstractRoomsList, swarmRooms, shelters, gates);
            if (MainModOptions.isMergeWhileLoadingEnabled.Value && world.game?.IsStorySession == true) // regionState is a function and needs world.game != null
            {
                Debug.Log("SBCameraScroll: Check rooms for missing merged textures.");
                foreach (AbstractRoom abstractRoom in abstractRoomsList)
                {
                    AbstractRoomMod.MergeCameraTextures(abstractRoom, world.regionState?.regionName); // regionState can be null (at least in arena)
                }
            }
        }
    }
}