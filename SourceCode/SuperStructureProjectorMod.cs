using UnityEngine;

namespace SBCameraScroll;

internal static class SuperStructureProjectorMod {
    // same as in AboveCloudsViewMod
    internal static void OnEnable() {
        On.SuperStructureProjector.GlyphMatrix.DrawSprites += GlyphMatrix_DrawSprites;
        On.SuperStructureProjector.SingleGlyph.DrawSprites += SingleGlyph_DrawSprites;
    }

    // ----------------- //
    // private functions //
    // ----------------- //

    private static void GlyphMatrix_DrawSprites(On.SuperStructureProjector.GlyphMatrix.orig_DrawSprites orig, SuperStructureProjector.GlyphMatrix glyph_matrix, RoomCamera.SpriteLeaser sprite_leaser, RoomCamera room_camera, float time_stacker, Vector2 cam_pos) {
        if (room_camera.room == null) {
            orig(glyph_matrix, sprite_leaser, room_camera, time_stacker, cam_pos);
            return;
        }

        Vector2 camera_position = room_camera.room.cameraPositions[room_camera.currentCameraPosition];
        room_camera.room.cameraPositions[room_camera.currentCameraPosition] = room_camera.room.cameraPositions[0];
        orig(glyph_matrix, sprite_leaser, room_camera, time_stacker, cam_pos);
        room_camera.room.cameraPositions[room_camera.currentCameraPosition] = camera_position;
    }

    private static void SingleGlyph_DrawSprites(On.SuperStructureProjector.SingleGlyph.orig_DrawSprites orig, SuperStructureProjector.SingleGlyph single_glyph, RoomCamera.SpriteLeaser sprite_leaser, RoomCamera room_camera, float time_stacker, Vector2 cam_pos) {
        if (room_camera.room == null) {
            orig(single_glyph, sprite_leaser, room_camera, time_stacker, cam_pos);
            return;
        }

        Vector2 camera_position = room_camera.room.cameraPositions[room_camera.currentCameraPosition];
        room_camera.room.cameraPositions[room_camera.currentCameraPosition] = room_camera.room.cameraPositions[0];
        orig(single_glyph, sprite_leaser, room_camera, time_stacker, cam_pos);
        room_camera.room.cameraPositions[room_camera.currentCameraPosition] = camera_position;
    }
}
