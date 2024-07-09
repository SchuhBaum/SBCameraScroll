using UnityEngine;
using static ProcessManager;
using static SBCameraScroll.MainMod;
using static SBCameraScroll.MainModOptions;

namespace SBCameraScroll;

internal static class ProcessManagerMod {
    internal static void OnEnable() {
        On.ProcessManager.RequestMainProcessSwitch_ProcessID += ProcessManager_RequestMainProcessSwitch;
    }

    //
    // public
    //

    public static void Initialize_Option_Specific_Hooks() {
        // Without using the variable can_log_il_hooks the logs are repeated
        // for every other mod that adds the corresponding IL hook.

        main_mod_options.Apply_And_Log_All_Options();
        Debug.Log(mod_id + ": Initialize option specific hooks.");
        can_log_il_hooks = true;
        RoomMod.On_Config_Changed();
        RoomCameraMod.On_Config_Changed();
        WorldMod.On_Config_Changed();
        can_log_il_hooks = false;
    }

    //
    // private
    //

    private static void ProcessManager_RequestMainProcessSwitch(On.ProcessManager.orig_RequestMainProcessSwitch_ProcessID orig, ProcessManager process_manager, ProcessID next_process_id) {
        ProcessID current_process_id = process_manager.currentMainLoop.ID;
        orig(process_manager, next_process_id);
        if (current_process_id != ProcessID.Initialization) return;
        Initialize_Option_Specific_Hooks();
    }
}
