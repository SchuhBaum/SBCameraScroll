
namespace SBCameraScroll;

internal static class WorldLoaderMod {
    internal static void OnEnable() {
        // CRS has a `REPLACEROOM` feature; I need to get the changed room name in order
        // to merge the textures; CRS tracks this information too; but so far I only found
        // it inside an internal class;
        IL.WorldLoader.LoadAbstractRoom += WorldLoader_LoadAbstractRoom;
    }

    //
    // private
    //

    // There should be a better way. At the end of the day I should be able to
    // hook in CRS methods even when they are private. Then I could get the
    // name when the abstract room is created.
    private static void WorldLoader_LoadAbstractRoom(ILContext context) {
        ILCursor cursor = new(context);
        cursor.Emit(OpCodes.Ldarg_1);
        cursor.Emit(OpCodes.Ldarg_2);
        cursor.EmitDelegate<Action<string, AbstractRoom>>((new_room_name, abstract_room) => {
            if (new_room_name == abstract_room.name) return;
            if (!room_name_to_crs_room_name.ContainsKey(abstract_room.name)) {
                room_name_to_crs_room_name.Add(abstract_room.name, new_room_name);
            }
            UpdateAttachedFields(abstract_room);
        });
    }
}
