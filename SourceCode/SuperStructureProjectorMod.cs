using UnityEngine;

namespace SBCameraScroll;

internal static class SuperStructureProjectorMod
{
    // same as in AboveCloudsViewMod
    internal static void OnEnable()
    {
        On.SuperStructureProjector.GlyphMatrix.DrawSprites += GlyphMatrix_DrawSprites;
        On.SuperStructureProjector.SingleGlyph.DrawSprites += SingleGlyph_DrawSprites;
    }

    // ----------------- //
    // private functions //
    // ----------------- //

    private static void GlyphMatrix_DrawSprites(On.SuperStructureProjector.GlyphMatrix.orig_DrawSprites orig, SuperStructureProjector.GlyphMatrix glyphMatrix, RoomCamera.SpriteLeaser sLeaser, RoomCamera roomCamera, float timeStacker, Vector2 camPos)
    {
        if (roomCamera.room == null)
        {
            orig(glyphMatrix, sLeaser, roomCamera, timeStacker, camPos);
            return;
        }

        Vector2 cameraPosition = roomCamera.room.cameraPositions[roomCamera.currentCameraPosition];
        roomCamera.room.cameraPositions[roomCamera.currentCameraPosition] = roomCamera.room.cameraPositions[0];
        orig(glyphMatrix, sLeaser, roomCamera, timeStacker, camPos);
        roomCamera.room.cameraPositions[roomCamera.currentCameraPosition] = cameraPosition;
    }

    private static void SingleGlyph_DrawSprites(On.SuperStructureProjector.SingleGlyph.orig_DrawSprites orig, SuperStructureProjector.SingleGlyph singleGlyph, RoomCamera.SpriteLeaser sLeaser, RoomCamera roomCamera, float timeStacker, Vector2 camPos)
    {
        if (roomCamera.room == null)
        {
            orig(singleGlyph, sLeaser, roomCamera, timeStacker, camPos);
            return;
        }

        Vector2 cameraPosition = roomCamera.room.cameraPositions[roomCamera.currentCameraPosition];
        roomCamera.room.cameraPositions[roomCamera.currentCameraPosition] = roomCamera.room.cameraPositions[0];
        orig(singleGlyph, sLeaser, roomCamera, timeStacker, camPos);
        roomCamera.room.cameraPositions[roomCamera.currentCameraPosition] = cameraPosition;
    }
}