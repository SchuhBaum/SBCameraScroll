using System.Collections.Generic;
using UnityEngine;

using static SBCameraScroll.AbstractRoomMod;
using static SBCameraScroll.RoomCameraMod;

namespace SBCameraScroll;

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
        // textureOffset.Clear(); // this freezes gate transitions when using SplitScreenMod
        // cosmeticWormsOnTile.Clear(); // probably caused freezes as well // too risky to do stuff like this while rooms still being updated(?)
        // ClearAllWormGrass(); // safer but too slow // there might worm grass already been created for the (new) world

        orig(world, slugcatName, abstractRoomsList, swarmRooms, shelters, gates);
        if (MainMod.Option_MergeWhileLoading && world.game?.IsStorySession == true) // regionState is a function and needs world.game != null
        {
            Debug.Log("SBCameraScroll: Check rooms for missing merged textures.");
            can_send_message_now = true;
            has_to_send_message_later = false;

            foreach (AbstractRoom abstractRoom in abstractRoomsList)
            {
                MergeCameraTextures(abstractRoom, world.regionState?.regionName); // regionState can be null (at least in arena)
            }
            can_send_message_now = false;
        }
    }
}