using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using RWCustom;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static SBCameraScroll.MainMod;

namespace SBCameraScroll;

internal static class LevelTexCombinerMod {
    internal static void OnEnable() {
        IL.Watcher.LevelTexCombiner.CreateBuffer += IL_LevelTexCombiner_CreateBuffer;
        IL.Watcher.LevelTexCombiner.Initialize += IL_LevelTexCombiner_Initialize;
        IL.Watcher.LevelTexCombiner.SetGlobals += IL_LevelTexCombiner_SetGlobals;
        IL.Watcher.LevelTexCombiner.UnSetGlobals += IL_LevelTexCombiner_UnSetGlobals;
    }

    //
    // public
    //

    public static bool LevelTexCombinerMod_ApplyLevelTexturePatch(ILCursor cursor, string function_name) {
        if (cursor.TryGotoNext(instruction => instruction.MatchLdfld("PersistentData", "cameraTextures"))) {
            if (can_log_il_hooks) {
                Debug.Log($"{mod_id}: IL_LevelTexCombiner_{function_name}: Index {cursor.Index}");
            }

            cursor.Index -= 2;
            cursor.RemoveRange(6);

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<Watcher.LevelTexCombiner, UnityEngine.Texture>>(LevelTexCombinerMod_GetLevelTexture);

        } else {
            if (can_log_il_hooks) {
                Debug.Log($"{mod_id}: IL_LevelTexCombiner_{function_name} failed.");
            }
            return false;
        }

        return true;
    }

    public static UnityEngine.Texture LevelTexCombinerMod_GetLevelTexture(Watcher.LevelTexCombiner combiner) {
        if (Custom.rainWorld?.processManager?.currentMainLoop is not RainWorldGame game) {
            throw new System.Exception("[ERROR] Expected to be in-game. But I did not find the instance for RainWorldGame.");
        }

        foreach (RoomCamera room_camera in game.cameras) {
            if (combiner == room_camera.levelTexCombiner) {
                UnityEngine.Texture level_texture = room_camera.levelGraphic._atlas.texture;
                return level_texture;
            }
        }

        return game.cameras[0].levelGraphic._atlas.texture;
    }

    //
    // private
    //

    private static void IL_LevelTexCombiner_CreateBuffer(ILContext context) {
        // LogAllInstructions(context);
        ILCursor cursor = new ILCursor(context);
        if (!LevelTexCombinerMod_ApplyLevelTexturePatch(cursor, function_name: "CreateBuffer")) {
            return;
        }
        // LogAllInstructions(context);
    }

    private static void IL_LevelTexCombiner_Initialize(ILContext context) {
        // LogAllInstructions(context);
        ILCursor cursor = new ILCursor(context);
        if (!LevelTexCombinerMod_ApplyLevelTexturePatch(cursor, function_name: "Initialize")) {
            return;
        }
        // LogAllInstructions(context);
    }

    private static void IL_LevelTexCombiner_SetGlobals(ILContext context) {
        // LogAllInstructions(context);
        ILCursor cursor = new ILCursor(context);
        if (!LevelTexCombinerMod_ApplyLevelTexturePatch(cursor, function_name: "SetGlobals")) {
            return;
        }
        // LogAllInstructions(context);
    }

    private static void IL_LevelTexCombiner_UnSetGlobals(ILContext context) {
        // LogAllInstructions(context);
        ILCursor cursor = new ILCursor(context);
        if (!LevelTexCombinerMod_ApplyLevelTexturePatch(cursor, function_name: "UnSetGlobals")) {
            return;
        }
        // LogAllInstructions(context);
    }
}
