
namespace SBCameraScroll;

internal static class MainMenuMod {
    internal static void OnEnable() {
        On.Menu.MainMenu.ctor += MainMenu_Ctor;
    }

    //
    // private

    private static void
    MainMenu_Ctor(
        On.Menu.MainMenu.orig_ctor orig,
        MainMenu main_menu,
        ProcessManager process_manager,
        bool show_region_specific_background)
    {
        orig(main_menu, process_manager, show_region_specific_background);

        // NOTE: The Steam Deck resolution 1229x768 is still not applied
        // correclty. For some reason, it gets mapped to 1280x800 during
        // OnLoadFinished(). This messes up alignment.
        main_mod_options.Set_Resolution(resolution.Value);
    }
}
