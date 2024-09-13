using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;

using static SBCameraScroll.AbstractRoomMod;

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
        cursor.EmitDelegate<Action<string, AbstractRoom>>((room_name, abstract_room) => {
            if (room_name == abstract_room.name) return;
            abstract_room.Get_Attached_Fields().name_when_replaced_by_crs = room_name;
            UpdateAttachedFields(abstract_room);
        });
    }
}
