
namespace SBCameraScroll;

public static class SuperStructureProjectorMod {
    //
    // Variables

    public static Hook? hook_SuperStructureProjector_IdealGlyphNumber = null;

    //
    // Initialization

    internal static void OnEnable() {
        On.SuperStructureProjector.ctor += SuperStructureProjector_Ctor;
        On.SuperStructureProjector.Update += SuperStructureProjector_Update;

        var superStructureProjector = Type.GetType("SuperStructureProjector, Assembly-CSharp");
        if (superStructureProjector != null) {
            try {
                var vanillaMethodInfo = superStructureProjector
                    .GetMethod("get_idealGlyphNumber");
                var moddedMethodInfo = typeof(SuperStructureProjectorMod)
                    .GetMethod("SuperStructureProjector_IdealGlyphNumber");
                var hook = new Hook(vanillaMethodInfo, moddedMethodInfo);
                hook_SuperStructureProjector_IdealGlyphNumber = hook;

                Debug.Log($"{mod_id}: Created hook for the property `SuperStructureProjector.idealGlyphNumber`.");
                    
            } catch (Exception exception) {
                Debug.Log($"{mod_id}: {exception}");
            }
        }

        // Same idea as in AboveCloudsViewMod. Although here, we need to take
        // the bottom-left camera position.
        On.SuperStructureProjector.GlyphMatrix.DrawSprites += GlyphMatrix_DrawSprites;
        On.SuperStructureProjector.SingleGlyph.DrawSprites += SingleGlyph_DrawSprites;
    }

    //
    // Private

    private static void
    GlyphMatrix_DrawSprites(
        On.SuperStructureProjector.GlyphMatrix.orig_DrawSprites orig,
        SuperStructureProjector.GlyphMatrix glyph_matrix,
        RoomCamera.SpriteLeaser sprite_leaser,
        RoomCamera room_camera,
        float time_stacker,
        Vector2 cam_pos)
    {
        if (room_camera.room is not Room room || room_camera.IsRoomBlacklisted(room.abstractRoom)) {
            orig(glyph_matrix, sprite_leaser, room_camera, time_stacker, cam_pos);
            return;
        }

        var abstract_room_fields = room.abstractRoom.GetFields();

        var camera_position = room_camera.room.cameraPositions[room_camera.currentCameraPosition];
        room_camera.room.cameraPositions[room_camera.currentCameraPosition] = abstract_room_fields.min_camera_position;
        orig(glyph_matrix, sprite_leaser, room_camera, time_stacker, cam_pos);
        room_camera.room.cameraPositions[room_camera.currentCameraPosition] = camera_position;
    }

    private static void
    SingleGlyph_DrawSprites(
        On.SuperStructureProjector.SingleGlyph.orig_DrawSprites orig,
        SuperStructureProjector.SingleGlyph single_glyph,
        RoomCamera.SpriteLeaser sprite_leaser,
        RoomCamera room_camera,
        float time_stacker,
        Vector2 cam_pos)
    {
        if (room_camera.room is not Room room || room_camera.IsRoomBlacklisted(room.abstractRoom)) {
            orig(single_glyph, sprite_leaser, room_camera, time_stacker, cam_pos);
            return;
        }

        var abstract_room_fields = room.abstractRoom.GetFields();

        var camera_position = room_camera.room.cameraPositions[room_camera.currentCameraPosition];
        room_camera.room.cameraPositions[room_camera.currentCameraPosition] = abstract_room_fields.min_camera_position;
        orig(single_glyph, sprite_leaser, room_camera, time_stacker, cam_pos);
        room_camera.room.cameraPositions[room_camera.currentCameraPosition] = camera_position;
    }

    private static void
    SuperStructureProjector_Ctor(
        On.SuperStructureProjector.orig_ctor orig,
        SuperStructureProjector projector,
        Room room,
        RoomSettings.RoomEffect effect)
    {
        if (room == null || room.abstractRoom.IsRoomBlacklisted()) {
            orig(projector, room, effect);
            return;
        }

        orig(projector, room, effect);

        // NOTE: SuperStructureProjector is created and initialized when the
        // room gets viewed. This means that IsRoomBlacklisted() is up-to-date.
        // E.g. when failing to load the room texture in
        // RoomCameraMod_LoadOneScreenOrFullRoomTexture().

        var abstract_room_fields = room.abstractRoom.GetFields();
        var grid_size = new IntVector2(
            Mathf.FloorToInt(abstract_room_fields.total_width / 15f) + 1,
            Mathf.FloorToInt(abstract_room_fields.total_height / 15f) + 1);
        projector.glyphGrid = new Glyph[grid_size.x, grid_size.y];

        for (int j = 0; j < projector.idealGlyphNumber / 2; j++)
		{
			projector.AddRandomGlyph();
		}
    }

    public static int
    SuperStructureProjector_IdealGlyphNumber(
        Func<SuperStructureProjector,int> orig,
        SuperStructureProjector projector)
    {
        var multiplier = (float)projector.glyphGrid.GetLength(0)/95f * (float)projector.glyphGrid.GetLength(1)/55f;
        return (int)(multiplier * 520f * projector.effect.amount);
    }

    private static void
    SuperStructureProjector_Update(
        On.SuperStructureProjector.orig_Update orig,
        SuperStructureProjector projector,
        bool eu)
    {
        if (projector.room == null || projector.room.abstractRoom.IsRoomBlacklisted()) {
            orig(projector, eu);
            return;
        }

        projector.lastCamPos = projector.room.game.cameras[0].currentCameraPosition;
        orig(projector, eu);

        var multiplier = (float)projector.glyphGrid.GetLength(0)/95f * (float)projector.glyphGrid.GetLength(1)/55f;
        for (int j = 0; j < multiplier; j++)
        {
            if (projector.glyphsList.Count < projector.idealGlyphNumber)
            {
                projector.AddRandomGlyph();
            }
        }
    }
}
