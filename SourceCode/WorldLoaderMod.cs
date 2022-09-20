namespace SBCameraScroll
{
    internal static class WorldLoaderMod
    {
        internal static void OnEnable()
        {
            On.WorldLoader.NextActivity += WorldLoader_NextActivity;
        }

        private static void WorldLoader_NextActivity(On.WorldLoader.orig_NextActivity orig, object obj)
        {
            orig(obj); // increases activity
            if (((WorldLoader)obj).activity == WorldLoader.Activity.CreatingAbstractRooms) // called before abstract rooms are created
            {
                // don't delete the whole thing // some abstract rooms might still be in use
                // just clear the worm grass
                foreach (AbstractRoom abstractRoom in AbstractRoomMod.allAttachedFields.Keys)
                {
                    AbstractRoomMod.DestroyWormGrassInAbstractRoom(abstractRoom);
                }
            }
        }
    }
}