using System.Collections.Generic;
using RWCustom;
using UnityEngine;

namespace SBCameraScroll
{
    internal static class WormGrassMod
    {
        internal static void OnEnable()
        {
            On.WormGrass.ctor += WormGrass_ctor; // initialize variables
            On.WormGrass.Explosion += WormGrass_Explosion;
            // On.WormGrass.NewCameraPos += WormGrass_NewCameraPos; // skip // leave cosmeticWorms empty
            On.WormGrass.Update += WormGrass_Update;
        }

        // ----------------- //
        // private functions //
        // ----------------- //

        private static void WormGrass_ctor(On.WormGrass.orig_ctor orig, UpdatableAndDeletable updatableAndDeletable, Room room, List<IntVector2> tiles)
        {
            orig(updatableAndDeletable, room, tiles); // initializes WormGrassPatchMod.cosmeticWormsOnTiles
            WormGrass wormGrass = (WormGrass)updatableAndDeletable;

            if (AbstractRoomMod.abstractRoomsWithWormGrass.ContainsKey(room.abstractRoom))
            {
                if (wormGrass.patches.Count == 0)
                {
                    Debug.Log("SBCameraScroll: There is already worm grass in " + room.abstractRoom.name + ". The new one has no worm grass patches. Delete the new one.");
                    wormGrass.Destroy();
                    return;
                }
                else
                {
                    Debug.Log("SBCameraScroll: There is already worm grass in " + room.abstractRoom.name + ". Delete the old one.");
                    AbstractRoomMod.ClearWormGrassInAbstractRoom(room.abstractRoom);
                }
            }
            AbstractRoomMod.abstractRoomsWithWormGrass.Add(room.abstractRoom, (WormGrass)updatableAndDeletable);
        }

        private static void WormGrass_Explosion(On.WormGrass.orig_Explosion orig, UpdatableAndDeletable updatableAndDeletable, Explosion explosion)
        {
            orig(updatableAndDeletable, explosion); // takes care of small worms // cosmeticWorms is empty because NewCameraPos() is skipped

            WormGrass wormGrass = (WormGrass)updatableAndDeletable;
            if (!wormGrass.slatedForDeletetion)
            {
                foreach (WormGrass.WormGrassPatch wormGrassPatch in wormGrass.patches)
                {
                    for (int tileIndex = 0; tileIndex < wormGrassPatch.tiles.Count; ++tileIndex)
                    {
                        foreach (WormGrass.Worm worm in WormGrassPatchMod.cosmeticWormsOnTiles[wormGrassPatch][tileIndex]) // loaded worms
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

            // different update logic for worms
            // update each tile based on distance
            // not when currentCameraIndex changes

            if (!wormGrass.slatedForDeletetion)
            {
                foreach (WormGrass.WormGrassPatch wormGrassPatch in wormGrass.patches)
                {
                    for (int tileIndex = 0; tileIndex < wormGrassPatch.tiles.Count; ++tileIndex) // update all tiles at once
                    {
                        WormGrassPatchMod.UpdatePatchTile(wormGrassPatch, wormGrass.room, tileIndex);
                    }
                }
            }
        }
    }
}