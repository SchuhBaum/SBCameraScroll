using RWCustom;
using System.Collections.Generic;
using UnityEngine;

namespace SBCameraScroll
{
    public static class WormGrassMod
    {
        public static Dictionary<WormGrass.WormGrassPatch, List<WormGrass.Worm>[]> cosmeticWormsOnTile = new Dictionary<WormGrass.WormGrassPatch, List<WormGrass.Worm>[]>();

        internal static void OnEnable()
        {
            On.WormGrass.Explosion += WormGrass_Explosion;
            On.WormGrass.Update += WormGrass_Update;

            On.WormGrass.WormGrassPatch.SortTiles += WormGrassPatch_SortTiles;
        }

        // ---------------- //
        // public functions //
        // ---------------- //

        public static void ExplodeWorm(WormGrass.Worm worm, Explosion explosion)
        {
            if (Custom.DistLess(worm.pos, explosion.pos, explosion.rad * 2f))
            {
                float distance = Mathf.InverseLerp(explosion.rad * 2f, explosion.rad, Vector2.Distance(worm.pos, explosion.pos)); // between 0 and 1
                if (Random.value < distance)
                {
                    worm.vel += Custom.DirVec(explosion.pos, worm.pos) * explosion.force * 2f * distance;
                    worm.excitement = 0.0f;
                    worm.focusCreature = null;
                    worm.dragForce = 0.0f;
                    worm.attachedChunk = null;
                }
            }
        }

        public static void UpdatePatchTile(Room wormGrassRoom, WormGrass.WormGrassPatch wormGrassPatch, int tileIndex)
        {
            // in the hunter cutscene this function is called before rainworldgame.ctor
            ref List<WormGrass.Worm> cosmeticWorms = ref cosmeticWormsOnTile[wormGrassPatch][tileIndex];

            if (cosmeticWorms.Count == 0 && wormGrassPatch.cosmeticWormPositions[tileIndex].Length > 0 && wormGrassRoom.ViewedByAnyCamera(wormGrassRoom.MiddleOfTile(wormGrassPatch.tiles[tileIndex]), margin: 200f))
            {
                for (int wormIndex = 0; wormIndex < wormGrassPatch.cosmeticWormPositions[tileIndex].Length; ++wormIndex)
                {
                    WormGrass.Worm worm = new WormGrass.Worm(wormGrassPatch.wormGrass, wormGrassPatch, wormGrassPatch.cosmeticWormPositions[tileIndex][wormIndex], wormGrassPatch.cosmeticWormLengths[tileIndex][wormIndex, 0], wormGrassPatch.sizes[tileIndex, 1], wormGrassPatch.cosmeticWormLengths[tileIndex][wormIndex, 1], true);

                    cosmeticWorms.Add(worm);
                    wormGrassPatch.wormGrass.room.AddObject(worm);
                }
            }
            else if (cosmeticWorms.Count > 0 && !wormGrassRoom.ViewedByAnyCamera(wormGrassRoom.MiddleOfTile(wormGrassPatch.tiles[tileIndex]), margin: 600f))
            {
                foreach (WormGrass.Worm worm in cosmeticWorms)
                {
                    worm.Destroy(); // should be removed automatically from room
                }
                cosmeticWorms.Clear();
            }
        }

        // ----------------- //
        // private functions //
        // ----------------- //

        private static void WormGrass_Explosion(On.WormGrass.orig_Explosion orig, WormGrass wormGrass, Explosion explosion)
        {
            foreach (WormGrass.WormGrassPatch wormGrassPatch in wormGrass.patches)
            {
                foreach (WormGrass.Worm worm in wormGrassPatch.worms) // smaller worms
                {
                    ExplodeWorm(worm, explosion);
                }

                for (int tileIndex = 0; tileIndex < wormGrassPatch.tiles.Count; ++tileIndex)
                {
                    foreach (WormGrass.Worm worm in cosmeticWormsOnTile[wormGrassPatch][tileIndex]) // loaded worms
                    {
                        ExplodeWorm(worm, explosion);
                    }
                }
            }
        }

        private static void WormGrass_Update(On.WormGrass.orig_Update orig, WormGrass wormGrass, bool eu)
        {
            wormGrass.evenUpdate = eu;
            foreach (WormGrass.WormGrassPatch wormGrassPatch in wormGrass.patches)
            {
                wormGrassPatch.Update();
            }

            // different update logic for worms // update each tile based on distance // not when currentCameraIndex changes
            foreach (WormGrass.WormGrassPatch wormGrassPatch in wormGrass.patches)
            {
                for (int tileIndex = 0; tileIndex < wormGrassPatch.tiles.Count; ++tileIndex) // update all tiles at once
                {
                    UpdatePatchTile(wormGrass.room, wormGrassPatch, tileIndex);
                }
            }
        }

        private static void WormGrassPatch_SortTiles(On.WormGrass.WormGrassPatch.orig_SortTiles orig, WormGrass.WormGrassPatch wormGrassPatch)
        {
            // some smaller worms will always be visible // they are added directly to rooms
            // for larger worms: just their information is stored and they are created/destroyed later
            orig(wormGrassPatch);

            // setting up cosmeticWormsOnTile
            int tileCount = wormGrassPatch.tiles.Count;
            if (!cosmeticWormsOnTile.ContainsKey(wormGrassPatch))
            {
                cosmeticWormsOnTile.Add(wormGrassPatch, new List<WormGrass.Worm>[tileCount]);
            }

            for (int tileIndex = 0; tileIndex < tileCount; ++tileIndex)
            {
                cosmeticWormsOnTile[wormGrassPatch][tileIndex] = new List<WormGrass.Worm>();
            }
        }
    }
}