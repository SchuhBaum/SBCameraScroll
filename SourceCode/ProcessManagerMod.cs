using static ProcessManager;
using static SBCameraScroll.MainModOptions;

namespace SBCameraScroll;

internal static class ProcessManagerMod
{
    internal static void OnEnable()
    {
        On.ProcessManager.RequestMainProcessSwitch_ProcessID += ProcessManager_RequestMainProcessSwitch;
    }

    //
    // private
    //

    private static void ProcessManager_RequestMainProcessSwitch(On.ProcessManager.orig_RequestMainProcessSwitch_ProcessID orig, ProcessManager process_manager, ProcessID next_process_id)
    {
        ProcessID current_process_id = process_manager.currentMainLoop.ID;
        orig(process_manager, next_process_id);
        if (current_process_id != ProcessID.Initialization) return;
        main_mod_options.Log_All_Options();
    }
}