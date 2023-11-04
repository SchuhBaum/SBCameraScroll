using System.Collections.Generic;
using UnityEngine;

using static SBCameraScroll.AbstractRoomMod;
using static SBCameraScroll.RoomCameraMod;

namespace SBCameraScroll;

internal static class WorldMod {
    internal static void OnEnable() {
        On.World.LoadWorld += World_LoadWorld;
    }

    // ----------------- //
    // private functions //
    // ----------------- //

    private static void World_LoadWorld(On.World.orig_LoadWorld orig, World world, SlugcatStats.Name slugcat_name, List<AbstractRoom> abstract_rooms_list, int[] swarm_rooms, int[] shelters, int[] gates) {
        // textureOffset.Clear(); // this freezes gate transitions when using SplitScreenMod
        // cosmeticWormsOnTile.Clear(); // probably caused freezes as well // too risky to do stuff like this while rooms still being updated(?)
        // ClearAllWormGrass(); // safer but too slow // there might worm grass already been created for the (new) world

        // regionState is a function and needs world.game != null
        orig(world, slugcat_name, abstract_rooms_list, swarm_rooms, shelters, gates);
        if (!MainMod.Option_MergeWhileLoading) return;
        if (world.game == null) return;
        if (!world.game.IsStorySession) return;

        Debug.Log("SBCameraScroll: Checking rooms for missing merged textures.");
        can_send_message_now = true;
        has_to_send_message_later = false;

        foreach (AbstractRoom abstract_room in abstract_rooms_list) {
            // regionState can be null (at least in arena);
            MergeCameraTextures(abstract_room, world.regionState?.regionName);
        }
        can_send_message_now = false;
    }
}
