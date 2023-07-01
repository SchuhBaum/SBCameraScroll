using System.Collections.Generic;
using RWCustom;
using UnityEngine;

namespace SBCameraScroll;

public static class WormGrassMod
{
    //
    // variables
    //

    internal static readonly Dictionary<WormGrass, AttachedFields> all_attached_fields = new();
    public static AttachedFields GetAttachedFields(this WormGrass wormGrass) => all_attached_fields[wormGrass];

    //
    //
    //

    internal static void OnEnable()
    {
        On.WormGrass.ctor += WormGrass_ctor; // initialize variables
        On.WormGrass.Explosion += WormGrass_Explosion;
        // On.WormGrass.NewCameraPos += WormGrass_NewCameraPos; // skip // leave cosmeticWorms empty
        On.WormGrass.Update += WormGrass_Update;
    }

    // ---------------- //
    // public functions //
    // ---------------- //

    public static void UpdatePatchTile(AttachedFields attachedFields, WormGrass.WormGrassPatch wormGrassPatch, Room wormGrassRoom, int tileIndex)
    {
        // in the hunter cutscene this function is called before rainworldgame.ctor
        ref List<WormGrass.Worm> cosmeticWormsOnTile = ref attachedFields.cosmeticWormsOnTiles[wormGrassPatch][tileIndex];
        if (cosmeticWormsOnTile.Count == 0 && wormGrassPatch.cosmeticWormPositions[tileIndex].Length > 0 && wormGrassRoom.ViewedByAnyCamera(wormGrassRoom.MiddleOfTile(wormGrassPatch.tiles[tileIndex]), margin: 200f))
        {
            for (int wormIndex = 0; wormIndex < wormGrassPatch.cosmeticWormPositions[tileIndex].Length; ++wormIndex)
            {
                WormGrass.Worm worm = new(wormGrassPatch.wormGrass, wormGrassPatch, wormGrassPatch.cosmeticWormPositions[tileIndex][wormIndex], wormGrassPatch.cosmeticWormLengths[tileIndex][wormIndex, 0], wormGrassPatch.sizes[tileIndex, 1], wormGrassPatch.cosmeticWormLengths[tileIndex][wormIndex, 1], true);

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

    // ----------------- //
    // private functions //
    // ----------------- //

    private static void WormGrass_ctor(On.WormGrass.orig_ctor orig, WormGrass worm_grass, Room room, List<IntVector2> tiles)
    {
        if (!all_attached_fields.ContainsKey(worm_grass))
        {
            all_attached_fields.Add(worm_grass, new AttachedFields());
        }
        orig(worm_grass, room, tiles); // needs attachedFields for wormGrass

        if (worm_grass.patches.Count == 0)
        {
            Debug.Log("SBCameraScroll: This worm grass for room " + room.abstractRoom.name + " has no patches. Destroy.");
            worm_grass.Destroy();
            all_attached_fields.Remove(worm_grass);
        }
        else
        {
            AbstractRoomMod.Attached_Fields abstractRoomAF = room.abstractRoom.Get_Attached_Fields();
            if (abstractRoomAF.worm_grass is WormGrass wormGrass_)
            {
                Debug.Log("SBCameraScroll: There is already worm grass in " + room.abstractRoom.name + ". Destroy the old one.");
                wormGrass_.Destroy();
                all_attached_fields.Remove(wormGrass_);
            }
            abstractRoomAF.worm_grass = worm_grass;
        }
    }

    private static void WormGrass_Explosion(On.WormGrass.orig_Explosion orig, WormGrass wormGrass, Explosion explosion)
    {
        orig(wormGrass, explosion); // takes care of small worms // cosmeticWorms is empty because NewCameraPos() is skipped

        if (wormGrass.slatedForDeletetion) return;

        AttachedFields attachedFields = wormGrass.GetAttachedFields();
        foreach (List<WormGrass.Worm>[] cosmeticWormsOnTiles in attachedFields.cosmeticWormsOnTiles.Values)
        {
            foreach (List<WormGrass.Worm> cosmeticWormsOnTile in cosmeticWormsOnTiles)
            {
                foreach (WormGrass.Worm worm in cosmeticWormsOnTile) // loaded worms
                {
                    // vanilla copy & paste
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

    private static void WormGrass_Update(On.WormGrass.orig_Update orig, UpdatableAndDeletable updatableAndDeletable, bool eu)
    {
        // I could also call orig() instead // this way NewCameraPos() and related things are skipped
        // currently that can pose problems with SplitScreenMod 
        // because WormGrass.cameraPositions might not be initialized for camera two

        WormGrass wormGrass = (WormGrass)updatableAndDeletable;
        wormGrass.evenUpdate = eu;

        foreach (WormGrass.WormGrassPatch wormGrassPatch in wormGrass.patches)
        {
            wormGrassPatch.Update();
        }

        if (wormGrass.slatedForDeletetion) return;

        // different update logic for worms
        // update each tile based on distance
        // not when currentCameraIndex changes

        AttachedFields attachedFields = wormGrass.GetAttachedFields();
        foreach (WormGrass.WormGrassPatch wormGrassPatch in attachedFields.cosmeticWormsOnTiles.Keys)
        {
            for (int tileIndex = 0; tileIndex < wormGrassPatch.tiles.Count; ++tileIndex) // update all tiles at once
            {
                UpdatePatchTile(attachedFields, wormGrassPatch, wormGrass.room, tileIndex);
            }
        }
    }

    //
    //
    //

    public sealed class AttachedFields
    {
        // the difference between this and WormGrass.cosmeticWorms is that I can update them tile by tile
        // vanilla looks at every worm but only when switching screens // costs too much performance otherwise
        public Dictionary<WormGrass.WormGrassPatch, List<WormGrass.Worm>[]> cosmeticWormsOnTiles = new();
    }
}