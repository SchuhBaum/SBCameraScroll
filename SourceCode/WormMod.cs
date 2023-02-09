namespace SBCameraScroll
{
    internal static class WormMod
    {
        internal static void OnEnable()
        {
            On.WormGrass.Worm.ApplyPalette += Worm_ApplyPalette; // this can crash in rare cases // vanilla bug?
        }

        //
        // private
        //

        private static void Worm_ApplyPalette(On.WormGrass.Worm.orig_ApplyPalette orig, WormGrass.Worm worm, RoomCamera.SpriteLeaser spriteLeaser, RoomCamera roomCamera, RoomPalette palette)
        {
            // when a visual worm is updated it might get removed from the room;
            if (worm.room == null) return;
            orig(worm, spriteLeaser, roomCamera, palette);
        }
    }
}