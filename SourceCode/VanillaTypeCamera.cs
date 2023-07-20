using RWCustom;
using System;
using UnityEngine;
using static SBCameraScroll.MainMod;
using static SBCameraScroll.RoomCameraMod;

namespace SBCameraScroll;

public class VanillaTypeCamera : IAmATypeCamera {
    //
    // parameters
    //

    // distance from border instead of camera position;
    public static float camera_box_from_border_x = 180f;
    public static float camera_box_from_border_y = 20f;

    private readonly RoomCamera _room_camera;
    private readonly Attached_Fields _attached_fields;

    //
    // variables
    //

    public EntityID? follow_abstract_creature_id = null;

    public Vector2 seek_position = new();
    public Vector2 vanilla_type_position = new();

    public bool is_centered = false;
    public bool use_vanilla_positions;

    public int transition_counter = 0;

    //
    // main
    //

    public VanillaTypeCamera(RoomCamera room_camera, Attached_Fields attached_fields) {
        _room_camera = room_camera;
        _attached_fields = attached_fields;
        use_vanilla_positions = !is_split_screen_coop_enabled && camera_zoom <= 1.33f;
    }

    //
    // public
    //

    public bool Is_Map_Pressed(Player player) {
        if (!is_improved_input_enabled) return player.input[0].mp && !player.input[1].mp;
        return player.Wants_To_Center_Camera();
    }

    public void Move_Camera() {
        Vector2 half_screen_size = 0.5f * _room_camera.sSize;
        float camera_box_multiplier_x = 1f;
        float camera_box_multiplier_y = 1f;

        if (is_split_screen_coop_enabled) {
            if (Is_Split_Horizontally) {
                half_screen_size.y -= 0.25f * _room_camera.sSize.y;
                camera_box_multiplier_y = 0.5f;
            } else if (Is_Split_Vertically) {
                half_screen_size.x -= 0.25f * _room_camera.sSize.x;
                camera_box_multiplier_x = 0.5f;
            }
        } else if (Is_Camera_Zoom_Enabled) {
            half_screen_size += Half_Inverse_Camera_Zoom_XY * _room_camera.sSize;
            camera_box_multiplier_x = 1f / camera_zoom;
            camera_box_multiplier_y = 1f / camera_zoom;
        }

        float direction_x = Math.Sign(_attached_fields.on_screen_position.x - vanilla_type_position.x);
        float distance_x = direction_x * (_attached_fields.on_screen_position.x - vanilla_type_position.x);
        float start_lean_distance_x = 2f * Mathf.Abs(_room_camera.followCreatureInputForward.x);

        if (distance_x > half_screen_size.x - camera_box_multiplier_x * camera_box_from_border_x) {
            // I cannot use ResetCameraPosition() because it sets vanilla_type_position to on_screen_position and useVanillaPositions is set to true;
            seek_position.x = 0.0f;

            // new distance to the center of the screen: outerCameraBoxX + 50f;
            // leanStartDistanceX can be up to 40f;
            vanilla_type_position.x += direction_x * (distance_x + half_screen_size.x - camera_box_multiplier_x * camera_box_from_border_x - 50f);
            _room_camera.lastPos.x = vanilla_type_position.x; // prevent transition with in-between frames
            _room_camera.pos.x = vanilla_type_position.x;
        } else {
            // lean effect; 
            // 20f for start_lean_distance_x is a simplification;
            // vanilla scales this instead; 
            if (distance_x > half_screen_size.x - camera_box_multiplier_x * camera_box_from_border_x - start_lean_distance_x) {
                seek_position.x = direction_x * 8f;
            } else {
                seek_position.x *= 0.9f;
            }

            // mimic what vanilla is doing with roomCamera.leanPos in Update();
            _room_camera.pos.x = Mathf.Lerp(_room_camera.lastPos.x, vanilla_type_position.x + seek_position.x, 0.1f);
        }

        float direction_y = Math.Sign(_attached_fields.on_screen_position.y - vanilla_type_position.y);
        float distance_y = direction_y * (_attached_fields.on_screen_position.y - vanilla_type_position.y);
        float start_lean_distance_y = 2f * Mathf.Abs(_room_camera.followCreatureInputForward.y);

        if (distance_y > half_screen_size.y - camera_box_multiplier_y * camera_box_from_border_y) {
            seek_position.y = 0.0f;
            vanilla_type_position.y += direction_y * (distance_y + half_screen_size.y - camera_box_multiplier_y * camera_box_from_border_y - 50f);
            _room_camera.lastPos.y = vanilla_type_position.y;
            _room_camera.pos.y = vanilla_type_position.y;
        } else {
            // vanilla does not do the lean effect for both;
            if (distance_y > half_screen_size.y - camera_box_multiplier_y * camera_box_from_border_y - start_lean_distance_y && seek_position.x < 8f) {
                seek_position.y = direction_y * 8f;
            } else {
                seek_position.y *= 0.9f;
            }
            _room_camera.pos.y = Mathf.Lerp(_room_camera.lastPos.y, vanilla_type_position.y + seek_position.y, 0.1f);
        }

        CheckBorders(_room_camera, ref vanilla_type_position);
        CheckBorders(_room_camera, ref _room_camera.lastPos);
        CheckBorders(_room_camera, ref _room_camera.pos);
    }

    public bool Move_Camera_Transition() {
        Vector2 target_position;
        if (use_vanilla_positions) {
            // only in case when the player is not the target
            // seekPos can change during a transition
            // this extends the transition until the player stops changing screens
            target_position = _room_camera.seekPos;
        } else {
            target_position = _attached_fields.on_screen_position;
            CheckBorders(_room_camera, ref target_position); // stop at borders
        }

        // 3f is not enough to reach the player that is walking away from the camera;
        // use a counter as well;
        _room_camera.pos = Vector2.Lerp(_room_camera.lastPos, target_position, smoothing_factor);
        _room_camera.pos = Custom.MoveTowards(_room_camera.pos, target_position, 3f);
        return _room_camera.pos == target_position;
    }

    public void Reset() {
        UpdateOnScreenPosition(_room_camera);
        CheckBorders(_room_camera, ref _attached_fields.on_screen_position); // do not move past room boundaries

        _room_camera.seekPos = _room_camera.CamPos(_room_camera.currentCameraPosition);
        _room_camera.seekPos.x += _room_camera.hDisplace + 8f;
        _room_camera.seekPos.y += 18f;
        _room_camera.leanPos *= 0.0f;

        use_vanilla_positions = !is_split_screen_coop_enabled && camera_zoom <= 1.33f;
        if (use_vanilla_positions) {
            // center camera on vanilla position;
            _room_camera.lastPos = _room_camera.seekPos;
            _room_camera.pos = _room_camera.seekPos;
        } else {
            _room_camera.lastPos = _attached_fields.on_screen_position;
            _room_camera.pos = _attached_fields.on_screen_position;
        }

        follow_abstract_creature_id = null; // do a smooth transition // this actually makes a difference for the vanilla type camera // otherwise the map input would immediately be processed
        seek_position *= 0.0f;
        vanilla_type_position = _attached_fields.on_screen_position;
        is_centered = false;
    }

    public void Update() {
        if (_room_camera.followAbstractCreature == null) return;
        if (_room_camera.room == null) return;
        UpdateOnScreenPosition(_room_camera);

        // smooth transition when switching cameras in the same room
        if (follow_abstract_creature_id != _room_camera.followAbstractCreature.ID && _room_camera.followAbstractCreature?.realizedCreature is Creature creature) {
            // keep transition going even when switching back;
            follow_abstract_creature_id = null;

            // needs follow_abstract_creature_id = null;
            // updates camera_offset;
            if (Move_Camera_Transition() || transition_counter > 20) {
                follow_abstract_creature_id = _room_camera.followAbstractCreature.ID;
                vanilla_type_position = _room_camera.pos;
                is_centered = true;
                transition_counter = 0;
                return;
            }

            ++transition_counter;
            if (is_split_screen_coop_enabled || camera_zoom > 1.33f) {
                // vanilla positions don't respect split screen;
                // otherwise you can move off-screen between camera positions;
                use_vanilla_positions = false;
            } else if (creature is Player player && Is_Map_Pressed(player)) {
                use_vanilla_positions = !use_vanilla_positions;
            }
            return;
        }

        if (is_centered && (Mathf.Abs(_attached_fields.on_screen_position.x - _attached_fields.last_on_screen_position.x) > 1f || Mathf.Abs(_attached_fields.on_screen_position.y - _attached_fields.last_on_screen_position.y) > 1f)) {
            is_centered = false;
        }

        if (!use_vanilla_positions) {
            Move_Camera();
        }

        // in Safari mode the camera might follow other creatures;
        // this means that inputs are ignored;
        // this means that you can't center the camera and it is
        // just the vanilla camera;
        {
            if (_room_camera.followAbstractCreature?.realizedCreature is not Player player) return;
            if (!Is_Map_Pressed(player)) return;

            if (is_split_screen_coop_enabled || camera_zoom > 1.33f) {
                use_vanilla_positions = false;
            } else if (use_vanilla_positions || is_centered) {
                use_vanilla_positions = !use_vanilla_positions;
            }
            follow_abstract_creature_id = null; // start a smooth transition
        }
    }
}
