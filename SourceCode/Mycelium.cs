namespace SBCameraScroll;

internal static class MyceliumMod {

    internal static void
    OnEnable()
    {
        On.CoralBrain.Mycelium.Update += Mycelium_Update;
    }

    //
    // Private

    private static void
    Mycelium_Update(
        On.CoralBrain.Mycelium.orig_Update orig, CoralBrain.Mycelium mycelium)
    {
        if (mycelium.owner.OwnerRoom is not Room room || room.abstractRoom.IsRoomBlacklisted())
        {
            orig(mycelium);
            return;
        }

        mycelium.lastCameraCullTick = -1;
        orig(mycelium);
    }
}
