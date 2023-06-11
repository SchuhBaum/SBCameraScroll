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
        if (center_keybinding != null) return;

        // initialize after ImprovedInput has;
        center_keybinding = PlayerKeybind.Register("SBCameraScroll-Center_Vanilla_Type_Camera", "SBCameraScroll", "Center", KeyCode.None, KeyCode.None);
        center_keybinding.HideConflict = other_keybinding => center_keybinding.Can_Hide_Conflict_With(other_keybinding);
        switch_keybinding = PlayerKeybind.Register("SBCameraScroll-Switch_Type_Camera", "SBCameraScroll", "Switch", KeyCode.None, KeyCode.None);
        switch_keybinding.HideConflict = other_keybinding => switch_keybinding.Can_Hide_Conflict_With(other_keybinding);
    }

    //
    // public
    //

    public static bool Can_Hide_Conflict_With(this PlayerKeybind keybinding, PlayerKeybind other_keybinding)
    {
        for (int player_index_a = 0; player_index_a < maximum_number_of_players; ++player_index_a)
        {
            for (int player_index_b = player_index_a; player_index_b < maximum_number_of_players; ++player_index_b)
            {
                if (!keybinding.ConflictsWith(player_index_a, other_keybinding, player_index_b)) continue;
                if (player_index_a != player_index_b) return false;

                // this is the same as having being Unbound() for the current
                // custom keybindings;
                if (other_keybinding == PlayerKeybind.Map) continue;

                if (other_keybinding == center_keybinding) continue;
                if (other_keybinding == switch_keybinding) continue;
                return false;
            }
        }
        return true;
    }

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