using System;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using UnityEngine;

using static SBCameraScroll.MainMod;

namespace SBCameraScroll;

internal static class WaterMod {
    internal static void OnEnable() {
        IL.Water.DrawSprites += IL_Water_DrawSprites;
    }

    //
    // private
    //

    private static void IL_Water_DrawSprites(ILContext context) {
        // LogAllInstructions(context);
        ILCursor cursor = new(context);

        if (cursor.TryGotoNext(instr => instr.MatchLdcR4(-10))) {
            // BUG:
            // In vanilla the lower edge of the deep water mesh does not scroll
            // away from the camera in y in most cases. This can lead to a bug
            // when zoomed out where the lower edge overtakes the upper edge and
            // flips the sprite / water overlay.

            if (can_log_il_hooks) {
                Debug.Log("SBCameraScroll: IL_Water_DrawSprites: Index " +
                          cursor.Index);
            }
            cursor.Goto(cursor.Index + 1);

            // Pop might be a better choice compared to RemoveRange() because it
            // leaves label targets intact.
            // cursor.Emit(OpCodes.Pop); // pop vanilla -10f
            cursor.Emit(OpCodes.Ldarg, 4);

            cursor.EmitDelegate<Func<float, Vector2, float>>(
                (y_local, camera_pos) => {
                    return y_local - camera_pos.y; // modded
                });
        } else {
            if (can_log_il_hooks) {
                Debug.Log("SBCameraScroll: IL_Water_DrawSprites failed.");
            }
            return;
        }

        if (cursor.TryGotoNext(instr => instr.MatchLdcR4(22),
                               instr => instr.MatchMul())) {
            // This is the same as before but for upside-down water.

            if (can_log_il_hooks) {
                Debug.Log("SBCameraScroll: IL_Water_DrawSprites: Index " +
                          cursor.Index);
            }

            cursor.Goto(cursor.Index + 2);
            cursor.Emit(OpCodes.Ldarg, 4);
            cursor.EmitDelegate<Func<float, Vector2, float>>(
                (y_local, camera_pos) => {
                    return y_local - camera_pos.y; // modded
                });
        } else {
            if (can_log_il_hooks) {
                Debug.Log("SBCameraScroll: IL_Water_DrawSprites failed.");
            }
            return;
        }
        // LogAllInstructions(context);
    }
}
