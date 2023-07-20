using RWCustom;
using System.Collections.Generic;
using UnityEngine;

namespace SBCameraScroll;

public static class WormGrassMod {
    //
    // variables
    //

    internal static readonly Dictionary<WormGrass, Attached_Fields> _all_attached_fields = new();
    public static Attached_Fields Get_Attached_Fields(this WormGrass worm_grass) => _all_attached_fields[worm_grass];

    //
    //
    //

    internal static void OnEnable() {
        On.WormGrass.ctor += WormGrass_Ctor; // initialize variables
        On.WormGrass.Explosion += WormGrass_Explosion;
        // On.WormGrass.NewCameraPos += WormGrass_NewCameraPos; // skip // leave cosmeticWorms empty
        On.WormGrass.Update += WormGrass_Update;
    }

    // ---------------- //
    // public functions //
    // ---------------- //

    public static void UpdatePatchTile(Attached_Fields attached_fields, WormGrass.WormGrassPatch worm_grass_patch, Room worm_grass_room, int tile_index) {
        // in the hunter cutscene this function is called before rainworldgame.ctor
        ref List<WormGrass.Worm> cosmetic_worms_on_tile = ref attached_fields.cosmetic_worms_on_tiles[worm_grass_patch][tile_index];
        if (cosmetic_worms_on_tile.Count == 0 && worm_grass_patch.cosmeticWormPositions[tile_index].Length > 0 && worm_grass_room.ViewedByAnyCamera(worm_grass_room.MiddleOfTile(worm_grass_patch.tiles[tile_index]), margin: 200f)) {
            for (int worm_index = 0; worm_index < worm_grass_patch.cosmeticWormPositions[tile_index].Length; ++worm_index) {
                WormGrass.Worm worm = new(worm_grass_patch.wormGrass, worm_grass_patch, worm_grass_patch.cosmeticWormPositions[tile_index][worm_index], worm_grass_patch.cosmeticWormLengths[tile_index][worm_index, 0], worm_grass_patch.sizes[tile_index, 1], worm_grass_patch.cosmeticWormLengths[tile_index][worm_index, 1], true);

                cosmetic_worms_on_tile.Add(worm);
                worm_grass_patch.wormGrass.room.AddObject(worm);
            }
        } else if (cosmetic_worms_on_tile.Count > 0 && !worm_grass_room.ViewedByAnyCamera(worm_grass_room.MiddleOfTile(worm_grass_patch.tiles[tile_index]), margin: 600f)) {
            foreach (WormGrass.Worm worm in cosmetic_worms_on_tile) {
                worm.Destroy(); // should be removed automatically from room
            }
            cosmetic_worms_on_tile.Clear();
        }
    }

    // ----------------- //
    // private functions //
    // ----------------- //

    private static void WormGrass_Ctor(On.WormGrass.orig_ctor orig, WormGrass worm_grass, Room room, List<IntVector2> tiles) {
        if (!_all_attached_fields.ContainsKey(worm_grass)) {
            _all_attached_fields.Add(worm_grass, new Attached_Fields());
        }
        orig(worm_grass, room, tiles); // needs attachedFields for wormGrass

        if (worm_grass.patches.Count == 0) {
            Debug.Log("SBCameraScroll: This worm grass for room " + room.abstractRoom.name + " has no patches. Destroy.");
            worm_grass.Destroy();
            _all_attached_fields.Remove(worm_grass);
        } else {
            AbstractRoomMod.Attached_Fields abstract_room_fields = room.abstractRoom.Get_Attached_Fields();
            if (abstract_room_fields.worm_grass is WormGrass worm_grass_) {
                Debug.Log("SBCameraScroll: There is already worm grass in " + room.abstractRoom.name + ". Destroy the old one.");
                worm_grass_.Destroy();
                _all_attached_fields.Remove(worm_grass_);
            }
            abstract_room_fields.worm_grass = worm_grass;
        }
    }

    private static void WormGrass_Explosion(On.WormGrass.orig_Explosion orig, WormGrass worm_grass, Explosion explosion) {
        orig(worm_grass, explosion); // takes care of small worms // cosmeticWorms is empty because NewCameraPos() is skipped

        if (worm_grass.slatedForDeletetion) return;

        Attached_Fields attached_fields = worm_grass.Get_Attached_Fields();
        foreach (List<WormGrass.Worm>[] cosmetic_worms_on_tiles in attached_fields.cosmetic_worms_on_tiles.Values) {
            foreach (List<WormGrass.Worm> cosmetic_worms_on_tile in cosmetic_worms_on_tiles) {
                foreach (WormGrass.Worm worm in cosmetic_worms_on_tile) // loaded worms
                {
                    // vanilla copy & paste
                    if (Custom.DistLess(worm.pos, explosion.pos, explosion.rad * 2f)) {
                        float distance = Mathf.InverseLerp(explosion.rad * 2f, explosion.rad, Vector2.Distance(worm.pos, explosion.pos)); // between 0 and 1
                        if (Random.value < distance) {
                            worm.vel += Custom.DirVec(explosion.pos, worm.pos) * explosion.force * 2f * distance;
                            worm.excitement = 0.0f;
                            worm.focusCreature = null;
                            worm.dragForce = 0.0f;
                            worm.attachedChunk = null;
                        }
                    }
                }
            }
        }
    }

    // not need atm because of WormGrass_Update()
    // private static void WormGrass_NewCameraPos(On.WormGrass.orig_NewCameraPos orig, UpdatableAndDeletable updatableAndDeletable)
    // {
    //     // don't add Worms to WormGrass.cosmeticWorms // save performance
    //     // use cosmeticWormsOnTiles instead 
    //     return;
    // }

    private static void WormGrass_Update(On.WormGrass.orig_Update orig, UpdatableAndDeletable updatable_and_deletable, bool eu) {
        // I could also call orig() instead // this way NewCameraPos() and related things are skipped
        // currently that can pose problems with SplitScreenMod 
        // because WormGrass.cameraPositions might not be initialized for camera two

        WormGrass worm_grass = (WormGrass)updatable_and_deletable;
        worm_grass.evenUpdate = eu;

        foreach (WormGrass.WormGrassPatch worm_grass_patch in worm_grass.patches) {
            worm_grass_patch.Update();
        }

        if (worm_grass.slatedForDeletetion) return;

        // different update logic for worms
        // update each tile based on distance
        // not when currentCameraIndex changes

        Attached_Fields attached_fields = worm_grass.Get_Attached_Fields();
        foreach (WormGrass.WormGrassPatch worm_grass_patch in attached_fields.cosmetic_worms_on_tiles.Keys) {
            for (int tile_index = 0; tile_index < worm_grass_patch.tiles.Count; ++tile_index) // update all tiles at once
            {
                UpdatePatchTile(attached_fields, worm_grass_patch, worm_grass.room, tile_index);
            }
        }
    }

    //
    //
    //

    public sealed class Attached_Fields {
        // the difference between this and WormGrass.cosmeticWorms is that I can update them tile by tile
        // vanilla looks at every worm but only when switching screens // costs too much performance otherwise
        public Dictionary<WormGrass.WormGrassPatch, List<WormGrass.Worm>[]> cosmetic_worms_on_tiles = new();
    }
}
