using RWCustom;
using System;
using UnityEngine;

using static SBCameraScroll.MainMod;
using static SBCameraScroll.RoomCameraMod;

namespace SBCameraScroll;

public class PositionTypeCamera : IAmATypeCamera {
    //
    // parameters
    //

    public static float camera_box_x = 40f;
    public static float camera_box_y = 40f;
    public static float offset_speed_multiplier = 0.2f;

    private readonly RoomCamera _room_camera;
    private readonly Attached_Fields _attached_fields;

    //
    // variables
    //

    public EntityID? follow_abstract_creature_id = null;
    public Vector2 camera_offset = new();

    //
    //
    //

    public PositionTypeCamera(RoomCamera room_camera, Attached_Fields attached_fields) {
        _room_camera = room_camera;
        _attached_fields = attached_fields;
    }

    //
    // public
    //

    private void Move_Camera_Towards_Target(Vector2 target_position, Vector2 at_border_difference) {
        // slow down at border; otherwise it would move at full speed and then suddenly
        // stop completely;
        float camera_box_minus_border_x = Mathf.Max(0.0f, camera_box_x - Mathf.Abs(at_border_difference.x));
        float camera_box_minus_border_y = Mathf.Max(0.0f, camera_box_y - Mathf.Abs(at_border_difference.y));

        float distance_x = Mathf.Abs(target_position.x - _room_camera.lastPos.x);
        float distance_y = Mathf.Abs(target_position.y - _room_camera.lastPos.y);

        if (distance_x > camera_box_minus_border_x) {
            // the goal is to reach camera_box_minus_border_x-close to target_position.x
            // the result is the same as:
            // roomCamera.pos.x = Mathf.Lerp(roomCamera.lastPos.x, camera_box_minus_border_x-close to target_position.x, t = smoothing_factor);
            //
            // the tick can fix a bug; every little difference to target_position can mean 
            // that your monitor screen is a pixel off; this is only noticeable when using 
            // split screen; the tick makes sure that the last 0.000-whatever difference 
            // gets reduced to zero and does not leave a line of black pixels;
            _room_camera.pos.x = Custom.LerpAndTick(_room_camera.lastPos.x, target_position.x, smoothing_factor * (distance_x - camera_box_minus_border_x) / distance_x, 0.01f);

            // stop when moving too slow; downside is that target_position might not be 
            // reached exactly => exclude when reaching the border; changing zoom in split
            // screen can mean that you move from outside the border to target_position;
            // otherwise, in that case it would leave a small black border; 
            // depending on smoothing_factor this can be a couple of pixels far away from
            // target_position;
            if (Mathf.Abs(_room_camera.pos.x - _room_camera.lastPos.x) < 1f && at_border_difference.x == 0f) {
                _room_camera.pos.x = _room_camera.lastPos.x;
            }
        } else {
            _room_camera.pos.x = _room_camera.lastPos.x;
        }

        if (distance_y > camera_box_minus_border_y) {
            _room_camera.pos.y = Custom.LerpAndTick(_room_camera.lastPos.y, target_position.y, smoothing_factor * (distance_y - camera_box_minus_border_y) / distance_y, 0.01f);
            if (Mathf.Abs(_room_camera.pos.y - _room_camera.lastPos.y) < 1f && at_border_difference.y == 0f) {
                _room_camera.pos.y = _room_camera.lastPos.y;
            }
        } else {
            _room_camera.pos.y = _room_camera.lastPos.y;
        }
    }

    public void Move_Camera_Without_Offset() {
        Vector2 target_position = _attached_fields.on_screen_position;
        CheckBorders(_room_camera, ref target_position);

        Vector2 at_border_difference = _attached_fields.on_screen_position - target_position;
        Move_Camera_Towards_Target(target_position, at_border_difference);
    }

    public void Move_Camera_With_Offset_Using_Player_Input(in Player player) {
        Vector2 target_position = _attached_fields.on_screen_position + camera_offset;
        CheckBorders(_room_camera, ref target_position);

        Vector2 at_border_difference = _attached_fields.on_screen_position + camera_offset - target_position;
        Move_Camera_Towards_Target(target_position, at_border_difference);
        Update_Camera_Offset_Using_Player_Input(player, at_border_difference);
    }

    public void Move_Camera_With_Offset_Using_Position_Input() {
        Vector2 target_position = _attached_fields.on_screen_position + camera_offset;
        CheckBorders(_room_camera, ref target_position);

        Vector2 at_border_difference = _attached_fields.on_screen_position + camera_offset - target_position;
        Move_Camera_Towards_Target(target_position, at_border_difference);
        Update_Camera_Offset_Using_Position_Input(at_border_difference);
    }

    public void Reset() {
        UpdateOnScreenPosition(_room_camera);
        CheckBorders(_room_camera, ref _attached_fields.on_screen_position); // do not move past room boundaries

        // center camera on player
        _room_camera.lastPos = _attached_fields.on_screen_position;
        _room_camera.pos = _attached_fields.on_screen_position;
        follow_abstract_creature_id = _room_camera.followAbstractCreature?.ID;
        camera_offset = new();
    }

    public void Update() {
        if (_room_camera.followAbstractCreature == null) return;
        if (_room_camera.room == null) return;
        UpdateOnScreenPosition(_room_camera);

        // is_in_transition
        if (follow_abstract_creature_id != _room_camera.followAbstractCreature.ID && _room_camera.followAbstractCreature?.realizedCreature is Creature creature) {
            follow_abstract_creature_id = _room_camera.followAbstractCreature.ID;
            _room_camera.pos = _room_camera.lastPos; // just wait this frame and resume next frame;

            {
                if (creature is Player player) {
                    camera_offset.x = player.input[0].x * camera_box_x;
                    camera_offset.y = player.input[0].y * camera_box_y;
                }
            }
            return;
        }

        if (!Option_CameraOffset) {
            Move_Camera_Without_Offset();
            return;
        }

        // scope the variable player; otherwise I need to re-name stuff;
        {
            if (_room_camera.followAbstractCreature?.realizedObject is Player player) {
                // same as the other function but uses player inputs
                // instead of position changes in order to update
                // the offset;
                Move_Camera_With_Offset_Using_Player_Input(player);
                return;
            }
        }
        Move_Camera_With_Offset_Using_Position_Input();
    }

    private void Update_Camera_Offset_Using_Player_Input(in Player player, Vector2 at_border_difference) {
        // this translated to a speed of 4 tiles per second;
        // this seems to work even when using Gourmand and being exhausted;
        float buffer = 2f;

        bool has_target_moved_x = Mathf.Abs(_attached_fields.on_screen_position.x - _attached_fields.last_on_screen_position.x) > buffer;
        bool has_target_moved_y = Mathf.Abs(_attached_fields.on_screen_position.y - _attached_fields.last_on_screen_position.y) > buffer;

        bool has_target_turned_around_x = has_target_moved_x && player.input[0].x != 0 && player.input[0].x == -Math.Sign(camera_offset.x) && player.input[0].x == Math.Sign(_attached_fields.on_screen_position.x - _attached_fields.last_on_screen_position.x);
        bool has_target_turned_around_y = has_target_moved_y && player.input[0].y != 0 && player.input[0].y == -Math.Sign(camera_offset.y) && player.input[0].y == Math.Sign(_attached_fields.on_screen_position.y - _attached_fields.last_on_screen_position.y);

        bool has_target_and_camera_moved_x = player.input[0].x != 0 && _room_camera.pos.x != _room_camera.lastPos.x;
        bool has_target_and_camera_moved_y = player.input[0].y != 0 && _room_camera.pos.y != _room_camera.lastPos.y;

        Update_Camera_Offset_XY(ref camera_offset.x, at_border_difference.x, camera_box_x, has_target_turned_around_x, has_target_and_camera_moved_x);
        Update_Camera_Offset_XY(ref camera_offset.y, at_border_difference.y, camera_box_y, has_target_turned_around_y, has_target_and_camera_moved_y);
    }

    private void Update_Camera_Offset_Using_Position_Input(Vector2 at_border_difference) {
        float buffer = 2f;

        bool has_target_moved_x = Mathf.Abs(_attached_fields.on_screen_position.x - _attached_fields.last_on_screen_position.x) > buffer;
        bool has_target_moved_y = Mathf.Abs(_attached_fields.on_screen_position.y - _attached_fields.last_on_screen_position.y) > buffer;

        bool has_target_turned_around_x = has_target_moved_x && Math.Sign(_attached_fields.on_screen_position.x - _attached_fields.last_on_screen_position.x) == -Math.Sign(camera_offset.x);
        bool has_target_turned_around_y = has_target_moved_y && Math.Sign(_attached_fields.on_screen_position.y - _attached_fields.last_on_screen_position.y) == -Math.Sign(camera_offset.y);

        bool has_target_and_camera_moved_x = has_target_moved_x && _room_camera.pos.x != _room_camera.lastPos.x;
        bool has_target_and_camera_moved_y = has_target_moved_y && _room_camera.pos.y != _room_camera.lastPos.y;

        Update_Camera_Offset_XY(ref camera_offset.x, at_border_difference.x, camera_box_x, has_target_turned_around_x, has_target_and_camera_moved_x);
        Update_Camera_Offset_XY(ref camera_offset.y, at_border_difference.y, camera_box_y, has_target_turned_around_y, has_target_and_camera_moved_y);
    }

    // probably not worth the refactor;
    // plus now these things are coupled;
    private void Update_Camera_Offset_XY(ref float camera_offset, float at_border_difference, float camera_box, bool has_target_turned_around, bool has_target_and_camera_moved) {
        bool is_at_border = Mathf.Abs(at_border_difference) > 0.1f;
        float buffer = 10f;

        // if I clamp using 2f * camera_box then there are situations where
        // the camera moves instantly when turning;
        // this can be annoying sometimes when you for example trying to pipe juke;
        float maximum_offset = 1.5f * camera_box;

        if (is_at_border) {
            if (at_border_difference > camera_box + buffer) {
                camera_offset = Mathf.Clamp(camera_offset - at_border_difference + camera_box + buffer, -maximum_offset, maximum_offset);
            } else if (at_border_difference < -camera_box - buffer) {
                camera_offset = Mathf.Clamp(camera_offset - at_border_difference - camera_box - buffer, -maximum_offset, maximum_offset);
            }
            return;
        }

        if (has_target_turned_around) {
            camera_offset = 0f;
            return;
        }

        if (!has_target_and_camera_moved) return;
        camera_offset = Mathf.Clamp(camera_offset + offset_speed_multiplier * (_attached_fields.on_screen_position.x - _attached_fields.last_on_screen_position.x), -maximum_offset, maximum_offset);
    }
}
