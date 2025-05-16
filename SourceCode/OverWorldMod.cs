
namespace SBCameraScroll;

internal static class OverWorldMod {
    internal static void OnEnable() {
        On.OverWorld.WorldLoaded += OverWorld_WorldLoaded;
    }

    //
    // private
    //

    private static void OverWorld_WorldLoaded(On.OverWorld.orig_WorldLoaded orig, OverWorld over_world, bool warp_used) {
        orig(over_world, warp_used);
        foreach (AbstractRoom abstract_room in _all_abstract_room_fields.Keys) {
            if (over_world.activeWorld.IsRoomInRegion(abstract_room.index)) continue;
            DestroyWormGrassInAbstractRoom(abstract_room);
        }
    }
}
