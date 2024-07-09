using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static SBCameraScroll.AbstractRoomMod;
using static SBCameraScroll.MainMod;

namespace SBCameraScroll;

internal static class WorldMod {
    internal static void On_Config_Changed() {
        On.World.LoadWorld -= World_LoadWorld;

        if (!Option_JIT_Merging) {
            On.World.LoadWorld += World_LoadWorld;
        }
    }

    //
    // public
    //

    public static IEnumerator MergeWhileLoading_Coroutine(List<AbstractRoom> abstract_room_list, World world) {
        Debug.Log("SBCameraScroll: Checking rooms for missing merged textures.");
        foreach (AbstractRoom abstract_room in abstract_room_list) {
            // In arena mode, the field regionState can be null.
            yield return new WaitForSeconds(0.001f);
            MergeCameraTextures(abstract_room, world.regionState?.regionName);
        }
    }

    //
    // private
    //

    private static void World_LoadWorld(On.World.orig_LoadWorld orig, World world, SlugcatStats.Name slugcat_name, List<AbstractRoom> abstract_room_list, int[] swarm_rooms, int[] shelters, int[] gates) {
        // textureOffset.Clear(); // this freezes gate transitions when using SplitScreenMod
        // cosmeticWormsOnTile.Clear(); // probably caused freezes as well // too risky to do stuff like this while rooms still being updated(?)
        // ClearAllWormGrass(); // safer but too slow // there might worm grass already been created for the (new) world

        // regionState is a function and needs world.game != null
        orig(world, slugcat_name, abstract_room_list, swarm_rooms, shelters, gates);
        if (!MainMod.Option_MergeWhileLoading) return;
        if (world.game == null) return;
        if (!world.game.IsStorySession) return;

        if (_coroutine_wrapper == null) {
            Debug.Log(mod_id + ": ERROR! The coroutine wrapper is null.");
            return;
        }

        _coroutine_wrapper.StopAllCoroutines();
        _coroutine_wrapper.StartCoroutine(MergeWhileLoading_Coroutine(abstract_room_list, world));
    }
}
