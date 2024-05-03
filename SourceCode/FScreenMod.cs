using RWCustom;
using UnityEngine;

namespace SBCameraScroll;

internal static class FScreenMod {
    internal static void OnEnable() {
        On.FScreen.ctor += FScreen_Ctor;
    }

    //
    // private
    //

    private static void FScreen_Ctor(On.FScreen.orig_ctor orig, FScreen f_screen, FutileParams futile_params) {
        orig(f_screen, futile_params);
        int screen_size_y = Mathf.RoundToInt(Custom.rainWorld.options.ScreenSize.y);
        if (screen_size_y == 768) return;

        f_screen.pixelHeight = screen_size_y;
        f_screen.UpdateScreenOffset();
        f_screen.renderTexture = new RenderTexture(f_screen.pixelWidth * f_screen.renderScale, f_screen.pixelHeight * f_screen.renderScale, 0);
    }
}
