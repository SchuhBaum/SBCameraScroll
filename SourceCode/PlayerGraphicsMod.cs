
namespace SBCameraScroll;

public static class PlayerGraphicsMod {
    internal static void OnEnable() {
        On.PlayerGraphics.RippleTrailUpdate -= PlayerGraphics_RippleTrailUpdate;
        On.PlayerGraphics.RippleTrailUpdate += PlayerGraphics_RippleTrailUpdate;
    }

    //
    // private
    //

    private static void PlayerGraphics_RippleTrailUpdate(On.PlayerGraphics.orig_RippleTrailUpdate orig, PlayerGraphics player_graphics) {
        orig(player_graphics);
        if (!Option_RippleTrailEffect && player_graphics.rippleTrail != null && !player_graphics.rippleTrail.beingDeleted)
        {
            player_graphics.rippleTrail.SetProperty(0, 0f);
            player_graphics.isRippleTrailDisabled = true;
        }
    }
}
