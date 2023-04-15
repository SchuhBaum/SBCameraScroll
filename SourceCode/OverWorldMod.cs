using static SBCameraScroll.AbstractRoomMod;

namespace SBCameraScroll;

internal static class OverWorldMod
{
    internal static void OnEnable()
    {
        On.OverWorld.WorldLoaded += OverWorld_WorldLoaded;
    }

    //
    // private
    //

    private static void OverWorld_WorldLoaded(On.OverWorld.orig_WorldLoaded orig, OverWorld over_world)
    {
        orig(over_world);
        foreach (AbstractRoom abstractRoom in all_attached_fields.Keys)
        {
            if (over_world.activeWorld.IsRoomInRegion(abstractRoom.index)) continue;
            DestroyWormGrassInAbstractRoom(abstractRoom);
        }
    }
}