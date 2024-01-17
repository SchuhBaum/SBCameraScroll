using UnityEngine;

namespace SBCameraScroll;

// use only if is_split_screen_coop_enabled is true;
public static class SplitScreenCoopMod {
    //
    // variables
    //

    public static bool Is_Split => SplitScreenCoop.SplitScreenCoop.CurrentSplitMode != SplitScreenCoop.SplitScreenCoop.SplitMode.NoSplit;
    public static bool Is_Split_4Screen => SplitScreenCoop.SplitScreenCoop.CurrentSplitMode == SplitScreenCoop.SplitScreenCoop.SplitMode.Split4Screen;
    public static bool Is_Split_Horizontally => SplitScreenCoop.SplitScreenCoop.CurrentSplitMode == SplitScreenCoop.SplitScreenCoop.SplitMode.SplitHorizontal;
    public static bool Is_Split_Vertically => SplitScreenCoop.SplitScreenCoop.CurrentSplitMode == SplitScreenCoop.SplitScreenCoop.SplitMode.SplitVertical;

    //
    // public
    //

    public static Vector2 Get_Screen_Offset(RoomCamera room_camera, in Vector2 screen_size) {
        if (Is_Split_Horizontally) return new(0.0f, 0.25f * screen_size.y);
        if (Is_Split_Vertically) return new(0.25f * screen_size.x, 0.0f);

        if (Is_Split_4Screen) {
            if (SplitScreenCoop.SplitScreenCoop.cameraZoomed[room_camera.cameraNumber]) return new();
            return new(0.25f * screen_size.x, 0.25f * screen_size.y);
        }
        return new();
    }

    public static bool Is_4Screen_Zoomed_Out(RoomCamera room_camera) => SplitScreenCoop.SplitScreenCoop.cameraZoomed[room_camera.cameraNumber];
}
