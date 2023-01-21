using System.Collections.Generic;

namespace SBCameraScroll
{
    internal static class WormGrassPatchMod
    {
        internal static void OnEnable()
        {
            On.WormGrass.WormGrassPatch.SortTiles += WormGrassPatch_SortTiles; // initializes variables // in ctor tiles.Count is not up to date
        }

        //
        // private
        //

        private static void WormGrassPatch_SortTiles(On.WormGrass.WormGrassPatch.orig_SortTiles orig, WormGrass.WormGrassPatch wormGrassPatch)
        {
            // some smaller worms will always be visible // they are added directly to rooms
            // for larger worms: just their information is stored and they are created/destroyed later
            orig(wormGrassPatch);

            // setting up cosmeticWormsOnTile
            WormGrassMod.AttachedFields attachedFields = wormGrassPatch.wormGrass.GetAttachedFields();
            int tileCount = wormGrassPatch.tiles.Count;

            if (!attachedFields.cosmeticWormsOnTiles.ContainsKey(wormGrassPatch))
            {
                attachedFields.cosmeticWormsOnTiles.Add(wormGrassPatch, new List<WormGrass.Worm>[tileCount]);
            }

            for (int tileIndex = 0; tileIndex < tileCount; ++tileIndex)
            {
                attachedFields.cosmeticWormsOnTiles[wormGrassPatch][tileIndex] = new List<WormGrass.Worm>();
            }
        }
    }
}