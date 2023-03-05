namespace SBCameraScroll;

internal static class RegionGateMod
{
    internal static void OnEnable()
    {
        On.RegionGate.Update += RegionGate_Update;
    }

    //
    // private
    //

    private static void RegionGate_Update(On.RegionGate.orig_Update orig, RegionGate regionGate, bool eu)
    {
        if (!regionGate.room.abstractRoom.GetAttachedFields().isInitialized)
        {
            AbstractRoomMod.UpdateTextureOffset(regionGate.room.abstractRoom, regionGate.room.cameraPositions);
        }
        orig(regionGate, eu);
    }
}