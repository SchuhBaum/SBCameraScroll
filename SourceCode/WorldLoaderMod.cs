
namespace SBCameraScroll;

internal static class WorldLoaderMod {
    internal static void OnEnable() {
        // CRS has a `REPLACEROOM` feature; I need to get the changed room name in order
        // to merge the textures; CRS tracks this information too; but so far I only found
        // it inside an internal class;
        //
        // This is part of vanilla now. But the alt name is set after the room
        // is created. Update the attached fields anyways.
        IL.WorldLoader.LoadAbstractRoom += WorldLoader_LoadAbstractRoom;
    }

    //
    // public
    //

    public static void WorldLoaderMod_CheckForAltRoomName(AbstractRoom abstract_room) {
        if (abstract_room.altFileName != null) {
            UpdateAttachedFields(abstract_room);
        }
    }

    //
    // private
    //

    private static void WorldLoader_LoadAbstractRoom(ILContext context) {
        ILCursor cursor = new ILCursor(context);
        cursor.Emit(OpCodes.Ldarg_2);
        cursor.EmitDelegate(WorldLoaderMod_CheckForAltRoomName);
    }
}
