using static SBCameraScroll.MainMod;
using static SBCameraScroll.RoomCameraMod;

namespace SBCameraScroll;

public class SwitchTypeCamera : IAmATypeCamera {
    //
    // parameters
    //

    private readonly RoomCamera _room_camera;
    private readonly PositionTypeCamera _position_type_camera;
    private readonly VanillaTypeCamera _vanilla_type_camera;

    //
    // variables
    //

    public bool is_position_type_camera_active = true;

    //
    // main
    //

    public SwitchTypeCamera(RoomCamera room_camera, Attached_Fields attached_fields) {
        this._room_camera = room_camera;
        _position_type_camera = new(room_camera, attached_fields);
        _vanilla_type_camera = new(room_camera, attached_fields);
    }

    //
    // public
    //

    public bool Is_Map_Pressed(Player player) {
        if (!is_improved_input_enabled) return player.input[0].mp && !player.input[1].mp;
        return player.Wants_To_Switch_Camera();
    }

    public void Reset() {
        if (is_position_type_camera_active) {
            _position_type_camera.Reset();
            return;
        }
        _vanilla_type_camera.Reset();
    }

    public void Update() {
        if (_room_camera.followAbstractCreature == null) return;
        if (_room_camera.followAbstractCreature.realizedCreature is not Player player) {
            _position_type_camera.Update();
            return;
        }

        if (Is_Map_Pressed(player)) {
            // this might be helpful since using the vanilla_type_camera might
            // register the button press again otherwise;
            _position_type_camera.Update();
            is_position_type_camera_active = !is_position_type_camera_active;

            // start a smooth transition next frame
            // if vanilla_type_camera is active;
            _vanilla_type_camera.follow_abstract_creature_id = null;
            return;
        }

        if (is_position_type_camera_active) {
            _position_type_camera.Update();
            return;
        }
        _vanilla_type_camera.Update();
    }
}
