using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;

namespace SBCameraScroll;

public static class WorldLoaderMod {
    internal static void OnEnable() {
        // CRS has a `REPLACEROOM` feature; I need to get the changed room name in order
        // to merge the textures;
        IL.WorldLoader.LoadAbstractRoom += WorldLoader_LoadAbstractRoom;
    }

    //
    // private
    //

    private static void WorldLoader_LoadAbstractRoom(ILContext context) {
        ILCursor cursor = new(context);
        cursor.Emit(OpCodes.Ldarg_1);
        cursor.Emit(OpCodes.Ldarg_2);
        cursor.EmitDelegate<Action<string, AbstractRoom>>((room_name, abstract_room) => {
            if (room_name == abstract_room.name) return;
            abstract_room.Get_Attached_Fields().name_when_replaced_by_crs = room_name;
        });
    }
}
