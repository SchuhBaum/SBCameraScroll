using UnityEngine;
using static SBCameraScroll.AbstractRoomMod;
using static SBCameraScroll.RoomCameraMod;

namespace SBCameraScroll;

public static class RoomMod {
    internal static void OnEnable() {
        On.Room.Loaded += Room_Loaded; // removes DeathFallFocus objects (which create fall focal points);
        On.Room.LoadFromDataString += Room_LoadFromDataString;
    }

    //
    // public
    //

    public static int CameraViewingPoint(Room room, Vector2 position) {
        // the original function Room.CameraViewingPoint() does not check the whole texture
        // (1400x800); it only checks what you can see of it (1366x768);
        // loop backwards to match how camera textures are merged, i.e. later ones can 
        // override parts of earlier ones;
        for (int camera_index = room.cameraPositions.Length - 1; camera_index >= 0; --camera_index) {
            Vector2 camera_position = room.cameraPositions[camera_index];
            if (position.x < camera_position.x) continue;
            if (position.x > camera_position.x + 1400f) continue;
            if (position.y < camera_position.y) continue;
            if (position.y > camera_position.y + 800f) continue;
            return camera_index;
        }
        return -1;
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
