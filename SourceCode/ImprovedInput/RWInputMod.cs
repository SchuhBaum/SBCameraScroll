using ImprovedInput;
using UnityEngine;

using static SBCameraScroll.PlayerMod;

namespace SBCameraScroll;

public static class RWInputMod
{
    //
    // parameters
    //

    public static PlayerKeybind center_keybinding = null!;
    public static PlayerKeybind switch_keybinding = null!;

    //
    // main
    //

    public static void Initialize_Custom_Keybindings()
    {
        // initialize after ImprovedInput has;
        center_keybinding = PlayerKeybind.Register("SBCameraScroll-Center_Vanilla_Type_Camera", "SBCameraScroll", "Center", KeyCode.None, KeyCode.None);
        switch_keybinding = PlayerKeybind.Register("SBCameraScroll-Switch_Type_Camera", "SBCameraScroll", "Switch", KeyCode.None, KeyCode.None);
    }

    //
    // public
    //

    public static InputPackageMod Get_Input(Player player)
    {
        InputPackageMod custom_input = new();
        int player_number = player.playerState.playerNumber;

        if (center_keybinding.Unbound(player_number))
        {
            custom_input.center_camera = player.input[0].mp;
        }
        else
        {
            custom_input.center_camera = center_keybinding.CheckRawPressed(player_number);
        }

        if (switch_keybinding.Unbound(player_number))
        {
            custom_input.switch_camera = player.input[0].mp;
        }
        else
        {
            custom_input.switch_camera = switch_keybinding.CheckRawPressed(player_number);
        }
        return custom_input;
    }
}