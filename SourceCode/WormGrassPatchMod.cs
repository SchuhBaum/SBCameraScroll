using System.Collections.Generic;
using RWCustom;

namespace SBCameraScroll
{
    public static class WormGrassPatchMod
    {
        //
        // variables
        //

        // I couldn't get weak tables to work in this case // maybe use only for struct type values // List<WeakRef> throws an error // WeakRef<List> gets automatically cleared all the time
        // the difference between this and WormGrass.cosmeticWorms is that I can update them tile by tile // vanilla looks at every worm but only when switching screens // costs too much performance otherwise
        public static Dictionary<WormGrass.WormGrassPatch, List<WormGrass.Worm>[]> cosmeticWormsOnTiles = new Dictionary<WormGrass.WormGrassPatch, List<WormGrass.Worm>[]>();

        //
        //
        //

        internal static void OnEnable()
        {
            On.WormGrass.WormGrassPatch.SortTiles += WormGrassPatch_SortTiles; // initializes variables // in ctor tiles.Count is not up to date
        }

        // ---------------- //
        // public functions //
        // ---------------- //

        public static void UpdatePatchTile(WormGrass.WormGrassPatch wormGrassPatch, Room wormGrassRoom, int tileIndex)
        {
            // in the hunter cutscene this function is called before rainworldgame.ctor
            ref List<WormGrass.Worm> cosmeticWormsOnTile = ref WormGrassPatchMod.cosmeticWormsOnTiles[wormGrassPatch][tileIndex];

            if (cosmeticWormsOnTile.Count == 0 && wormGrassPatch.cosmeticWormPositions[tileIndex].Length > 0 && wormGrassRoom.ViewedByAnyCamera(wormGrassRoom.MiddleOfTile(wormGrassPatch.tiles[tileIndex]), margin: 200f))
            {
                for (int wormIndex = 0; wormIndex < wormGrassPatch.cosmeticWormPositions[tileIndex].Length; ++wormIndex)
                {
                    WormGrass.Worm worm = new WormGrass.Worm(wormGrassPatch.wormGrass, wormGrassPatch, wormGrassPatch.cosmeticWormPositions[tileIndex][wormIndex], wormGrassPatch.cosmeticWormLengths[tileIndex][wormIndex, 0], wormGrassPatch.sizes[tileIndex, 1], wormGrassPatch.cosmeticWormLengths[tileIndex][wormIndex, 1], true);

                    cosmeticWormsOnTile.Add(worm);
                    wormGrassPatch.wormGrass.room.AddObject(worm);
                }
            }
            else if (cosmeticWormsOnTile.Count > 0 && !wormGrassRoom.ViewedByAnyCamera(wormGrassRoom.MiddleOfTile(wormGrassPatch.tiles[tileIndex]), margin: 600f))
            {
                foreach (WormGrass.Worm worm in cosmeticWormsOnTile)
                {
                    worm.Destroy(); // should be removed automatically from room
                }
                cosmeticWormsOnTile.Clear();
            }
        }

        //
        // private
        //

        private static void WormGrassPatch_SortTiles(On.WormGrass.WormGrassPatch.orig_SortTiles orig, object obj)
        {
            // some smaller worms will always be visible // they are added directly to rooms
            // for larger worms: just their information is stored and they are created/destroyed later
            orig(obj);

            // setting up cosmeticWormsOnTile
            WormGrass.WormGrassPatch wormGrassPatch = (WormGrass.WormGrassPatch)obj;
            int tileCount = wormGrassPatch.tiles.Count;

            if (!cosmeticWormsOnTiles.ContainsKey(wormGrassPatch))
            {
                cosmeticWormsOnTiles.Add(wormGrassPatch, new List<WormGrass.Worm>[tileCount]);
            }

            for (int tileIndex = 0; tileIndex < tileCount; ++tileIndex)
            {
                cosmeticWormsOnTiles[wormGrassPatch][tileIndex] = new List<WormGrass.Worm>();
            }
        }
    }
}