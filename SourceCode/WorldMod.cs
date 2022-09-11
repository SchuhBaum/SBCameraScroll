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

        private static void World_LoadWorld(On.World.orig_LoadWorld orig, World world, int slugcatNumber, List<AbstractRoom> abstractRoomsList, int[] swarmRooms, int[] shelters, int[] gates)
        {
            //AbstractRoomMod.textureOffset.Clear(); // this freezes gate transitions when using SplitScreenMod
            //WormGrassMod.cosmeticWormsOnTile.Clear(); // probably caused freezes as well // too risky to do stuff like this while rooms still being updated(?)

            orig(world, slugcatNumber, abstractRoomsList, swarmRooms, shelters, gates);

            if (MainMod.isMergeWhileLoadingOptionEnabled && world.game?.IsStorySession == true) // regionState is a function and needs world.game != null
            {
                Debug.Log("SBCameraScroll: Check rooms for missing merged textures.");
                foreach (AbstractRoom abstractRoom in abstractRoomsList)
                {
                    AbstractRoomMod.MergeCameraTextures(abstractRoom.name, world.regionState?.regionName); // regionState can be null (at least in arena)
                }
            }
        }
    }
}