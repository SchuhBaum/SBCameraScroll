using static SBCameraScroll.MainMod;
using static SBCameraScroll.RoomCameraMod;

namespace SBCameraScroll;

public class SwitchTypeCamera : IAmATypeCamera
{
    //
    // parameters
    //

    private readonly RoomCamera room_camera;
    private readonly PositionTypeCamera position_type_camera;
    private readonly VanillaTypeCamera vanilla_type_camera;

    //
    // variables
    //

    public bool is_position_type_camera_active = true;

    //
    // main
    //

    public SwitchTypeCamera(RoomCamera room_camera, AttachedFields attached_fields)
    {
        this.room_camera = room_camera;
        position_type_camera = new(room_camera, attached_fields);
        vanilla_type_camera = new(room_camera, attached_fields);
    }

    //
    // public
    //

    public bool Is_Map_Pressed(Player player)
    {
        if (!is_improved_input_enabled) return player.input[0].mp && !player.input[1].mp;
        return player.Wants_To_Switch_Camera();
    }

    public void Reset()
    {
        if (is_position_type_camera_active)
        {
            position_type_camera.Reset();
            return;
        }
        vanilla_type_camera.Reset();
    }

    public void Update()
    {
        if (room_camera.followAbstractCreature == null) return;
        if (room_camera.followAbstractCreature.realizedCreature is not Player player)
        {
            position_type_camera.Update();
            return;
        }

        if (Is_Map_Pressed(player))
        {
            // this might be helpful since using the vanilla_type_camera might
            // register the button press again otherwise;
            position_type_camera.Update();
            is_position_type_camera_active = !is_position_type_camera_active;

            // start a smooth transition next frame
            // if vanilla_type_camera is active;
            vanilla_type_camera.follow_abstract_creature_id = null;
            return;
        }

        if (is_position_type_camera_active)
        {
            position_type_camera.Update();
            return;
        }
        vanilla_type_camera.Update();
    }
}