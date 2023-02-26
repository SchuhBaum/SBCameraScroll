namespace SBCameraScroll;

internal static class JollyCoopMod
{
    internal static void OnEnable()
    {
        On.JollyCoop.JollyHUD.JollyPlayerSpecificHud.JollyPlayerArrow.Update += JollyPlayerArrow_Update;
    }

    //
    // private
    //

    private static void JollyPlayerArrow_Update(On.JollyCoop.JollyHUD.JollyPlayerSpecificHud.JollyPlayerArrow.orig_Update orig, JollyCoop.JollyHUD.JollyPlayerSpecificHud.JollyPlayerArrow playerArrow)
    {
        orig(playerArrow);

        if (playerArrow.jollyHud.Camera.GetAttachedFields().isRoomBlacklisted) return;

        float screen_size_x = playerArrow.jollyHud.hud.rainWorld.options.ScreenSize.x;
        if (screen_size_x == RoomCameraMod.default_screen_size_x) return;

        // 1360f seems to work better than 1366f in most cases;
        playerArrow.bodyPos.x += 0.5f * (screen_size_x - RoomCameraMod.default_screen_size_x + 6f);
    }
}