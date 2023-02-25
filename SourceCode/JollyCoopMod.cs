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
        playerArrow.bodyPos.x += 0.5f * (playerArrow.jollyHud.hud.rainWorld.options.ScreenSize.x - RoomCameraMod.default_screen_size_x);
    }
}