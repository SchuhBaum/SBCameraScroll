using static SBCameraScroll.AbstractRoomMod;

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

    private static void RegionGate_Update(On.RegionGate.orig_Update orig, RegionGate region_gate, bool eu)
    {
        if (!region_gate.room.abstractRoom.Get_Attached_Fields().is_initialized)
        {
            UpdateTextureOffset(region_gate.room.abstractRoom, region_gate.room.cameraPositions);
        }
        orig(region_gate, eu);
    }
}