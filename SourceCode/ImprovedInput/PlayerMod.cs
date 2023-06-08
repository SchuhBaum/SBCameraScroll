using System.Collections.Generic;

namespace SBCameraScroll;

public static class PlayerMod
{
    //
    // parameters
    //

    public static int number_of_players = 4;

    //
    // variables
    //

    public static List<InputPackageMod[]> custom_input_list = null!;

    //
    // main
    //

    internal static void OnEnable()
    {
        Initialize_Custom_Inputs();
        On.Player.checkInput -= Player_CheckInput;
        On.Player.checkInput += Player_CheckInput;
    }

    public static void Initialize_Custom_Inputs()
    {
        if (custom_input_list != null) return;
        custom_input_list = new();

        for (int player_number = 0; player_number < number_of_players; ++player_number)
        {
            InputPackageMod[] custom_input = new InputPackageMod[2];
            custom_input[0] = new();
            custom_input[1] = new();
            custom_input_list.Add(custom_input);
        }
    }

    //
    // public
    //

    public static bool Wants_To_Center_Camera(this Player player)
    {
        int player_number = player.playerState.playerNumber;
        if (player_number < 0) return player.input[0].mp && !player.input[1].mp;
        if (player_number >= number_of_players) return player.input[0].mp && !player.input[1].mp;

        InputPackageMod[] custom_input = custom_input_list[player_number];
        return custom_input[0].center_camera && !custom_input[1].center_camera;
    }

    public static bool Wants_To_Switch_Camera(this Player player)
    {
        int player_number = player.playerState.playerNumber;
        if (player_number < 0) return player.input[0].mp && !player.input[1].mp;
        if (player_number >= number_of_players) return player.input[0].mp && !player.input[1].mp;

        InputPackageMod[] custom_input = custom_input_list[player_number];
        return custom_input[0].switch_camera && !custom_input[1].switch_camera;
    }

    //
    // private
    //

    private static void Player_CheckInput(On.Player.orig_checkInput orig, Player player)
    {
        // update player.input first;
        orig(player);

        int player_number = player.playerState.playerNumber;
        if (player_number < 0) return;
        if (player_number >= number_of_players) return;

        InputPackageMod[] custom_input = custom_input_list[player_number];
        custom_input[1] = custom_input[0];

        if (player.stun == 0 && !player.dead)
        {
            custom_input[0] = RWInputMod.Get_Input(player);
            return;
        }
        custom_input[0] = new();
    }

    //
    //
    //

    public struct InputPackageMod
    {
        public bool center_camera = false;
        public bool switch_camera = false;
        public InputPackageMod() { }
    }
}