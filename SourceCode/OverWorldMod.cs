namespace SBCameraScroll
{
    internal static class OverWorldMod
    {
        internal static void OnEnable()
        {
            On.OverWorld.WorldLoaded += OverWorld_WorldLoaded;
        }

        //
        // private
        //

        private static void OverWorld_WorldLoaded(On.OverWorld.orig_WorldLoaded orig, OverWorld overWorld)
        {
            orig(overWorld);
            foreach (AbstractRoom abstractRoom in AbstractRoomMod.all_attached_fields.Keys)
            {
                if (!overWorld.activeWorld.IsRoomInRegion(abstractRoom.index))
                {
                    AbstractRoomMod.DestroyWormGrassInAbstractRoom(abstractRoom);
                }
            }
        }
    }
}