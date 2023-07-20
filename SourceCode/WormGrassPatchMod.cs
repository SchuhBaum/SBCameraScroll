using System.Collections.Generic;
using static SBCameraScroll.WormGrassMod;

namespace SBCameraScroll;

internal static class WormGrassPatchMod {
    internal static void OnEnable() {
        On.WormGrass.WormGrassPatch.SortTiles += WormGrassPatch_SortTiles; // initializes variables // in ctor tiles.Count is not up to date
    }

    //
    // private
    //

    private static void WormGrassPatch_SortTiles(On.WormGrass.WormGrassPatch.orig_SortTiles orig, WormGrass.WormGrassPatch worm_grass_patch) {
        // some smaller worms will always be visible // they are added directly to rooms
        // for larger worms: just their information is stored and they are created/destroyed later
        orig(worm_grass_patch);

        // setting up cosmeticWormsOnTile
        Attached_Fields attached_fields = worm_grass_patch.wormGrass.Get_Attached_Fields();
        int tile_count = worm_grass_patch.tiles.Count;

        if (!attached_fields.cosmetic_worms_on_tiles.ContainsKey(worm_grass_patch)) {
            attached_fields.cosmetic_worms_on_tiles.Add(worm_grass_patch, new List<WormGrass.Worm>[tile_count]);
        }

        for (int tile_index = 0; tile_index < tile_count; ++tile_index) {
            attached_fields.cosmetic_worms_on_tiles[worm_grass_patch][tile_index] = new List<WormGrass.Worm>();
        }
    }
}
