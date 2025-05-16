
namespace SBCameraScroll;

public static class RoomMod {
    internal static void OnEnable() {
        // removes DeathFallFocus objects (which create fall focal points);
        On.Room.Loaded += Room_Loaded;
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
}
