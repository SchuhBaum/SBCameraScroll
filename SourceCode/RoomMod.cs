using static SBCameraScroll.AbstractRoomMod;
using static SBCameraScroll.MainMod;
using static SBCameraScroll.RoomCameraMod;

namespace SBCameraScroll;

public static class RoomMod {
    public static bool CanScrollCamera(this Room room) => Option_ScrollOneScreenRooms || is_split_screen_coop_enabled && room.game.IsStorySession || room.cameraPositions.Length > 1;

    internal static void OnEnable() {
        On.Room.Loaded += Room_Loaded; // removes DeathFallFocus objects (which create fall focal points);
        On.Room.LoadFromDataString += Room_LoadFromDataString;
    }

    //
    // private
    //

    private static void Room_Loaded(On.Room.orig_Loaded orig, Room room) {
        orig(room);

        // these focal points change the height of death fall indicators;
        // even when I use the camera height to create a full screen effect 
        // that moves with the camera, they will pop in and out when in or
        // out of range;
        // => remove for now;
        room.deathFallFocalPoints = new();
    }

    private static void Room_LoadFromDataString(On.Room.orig_LoadFromDataString orig, Room room, string[] lines) {
        orig(room, lines);

        if (room?.game == null) return;
        if (room.abstractRoom is not AbstractRoom abstract_room) return;
        if (blacklisted_rooms.Contains(abstract_room.name)) return;

        CheckCameraPositions(ref room.cameraPositions);
        UpdateTextureOffset(abstract_room, room.cameraPositions); // update for one-screen rooms as well
        MergeCameraTextures(abstract_room, room.abstractRoom.world?.regionState?.regionName, room.cameraPositions); // warping might mess with world or region state => check for nulls // regionState is a function and needs game != null
    }
}
