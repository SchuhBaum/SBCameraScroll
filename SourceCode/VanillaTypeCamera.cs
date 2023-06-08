using System;
using RWCustom;
using UnityEngine;

using static SBCameraScroll.MainMod;
using static SBCameraScroll.RoomCameraMod;

namespace SBCameraScroll;

public class VanillaTypeCamera : IAmATypeCamera
{
    //
    // parameters
    //

    // distance from border instead of camera position;
    public static float camera_box_from_border_x = 180f;
    public static float camera_box_from_border_y = 20f;

    private readonly RoomCamera room_camera;
    private readonly AttachedFields attached_fields;

    //
    // variables
    //

    public EntityID? follow_abstract_creature_id = null;

    public Vector2 seek_position = new();
    public Vector2 vanilla_type_position = new();

    public bool is_centered = false;
    public bool use_vanilla_positions = true;

    //
    // main
    //

    public VanillaTypeCamera(RoomCamera roomCamera, AttachedFields attachedFields)
    {
        room_camera = roomCamera;
        attached_fields = attachedFields;
    }

    //
    // public
    //

    public bool Is_Map_Pressed(Player player)
    {
        if (!is_improved_input_enabled) return player.input[0].mp && !player.input[1].mp;
        return player.Wants_To_Center_Camera();
    }

    public void Move_Camera()
    {
        Vector2 half_screen_size = 0.5f * room_camera.sSize;
        float camera_box_multiplier_x = 1f;
        float camera_box_multiplier_y = 1f;

        if (is_split_screen_coop_enabled)
        {
            if (Is_Split_Horizontally)
            {
                half_screen_size.y -= 0.25f * room_camera.sSize.y;
                camera_box_multiplier_y = 0.5f;
            }
            else if (Is_Split_Vertically)
            {
                half_screen_size.x -= 0.25f * room_camera.sSize.x;
                camera_box_multiplier_x = 0.5f;
            }
        }

        float direction_x = Math.Sign(attached_fields.on_screen_position.x - vanilla_type_position.x);
        float distance_x = direction_x * (attached_fields.on_screen_position.x - vanilla_type_position.x);
        float start_lean_distance_x = 2f * Mathf.Abs(room_camera.followCreatureInputForward.x);

        if (distance_x > half_screen_size.x - camera_box_multiplier_x * camera_box_from_border_x)
        {
            // I cannot use ResetCameraPosition() because it sets vanilla_type_position to on_screen_position and useVanillaPositions is set to true;
            seek_position.x = 0.0f;

            // new distance to the center of the screen: outerCameraBoxX + 50f;
            // leanStartDistanceX can be up to 40f;
            vanilla_type_position.x += direction_x * (distance_x + half_screen_size.x - camera_box_multiplier_x * camera_box_from_border_x - 50f);
            room_camera.lastPos.x = vanilla_type_position.x; // prevent transition with in-between frames
            room_camera.pos.x = vanilla_type_position.x;
        }
        else
        {
            // lean effect; 
            // 20f for start_lean_distance_x is a simplification;
            // vanilla scales this instead; 
            if (distance_x > half_screen_size.x - camera_box_multiplier_x * camera_box_from_border_x - start_lean_distance_x)
            {
                seek_position.x = direction_x * 8f;
            }
            else
            {
                seek_position.x *= 0.9f;
            }

            // mimic what vanilla is doing with roomCamera.leanPos in Update();
            room_camera.pos.x = Mathf.Lerp(room_camera.lastPos.x, vanilla_type_position.x + seek_position.x, 0.1f);
        }

        float direction_y = Math.Sign(attached_fields.on_screen_position.y - vanilla_type_position.y);
        float distance_y = direction_y * (attached_fields.on_screen_position.y - vanilla_type_position.y);
        float start_lean_distance_y = 2f * Mathf.Abs(room_camera.followCreatureInputForward.y);

        if (distance_y > half_screen_size.y - camera_box_multiplier_y * camera_box_from_border_y)
        {
            seek_position.y = 0.0f;
            vanilla_type_position.y += direction_y * (distance_y + half_screen_size.y - camera_box_multiplier_y * camera_box_from_border_y - 50f);
            room_camera.lastPos.y = vanilla_type_position.y;
            room_camera.pos.y = vanilla_type_position.y;
        }
        else
        {
            // vanilla does not do the lean effect for both;
            if (distance_y > half_screen_size.y - camera_box_multiplier_y * camera_box_from_border_y - start_lean_distance_y && seek_position.x < 8f)
            {
                seek_position.y = direction_y * 8f;
            }
            else
            {
                seek_position.y *= 0.9f;
            }
            room_camera.pos.y = Mathf.Lerp(room_camera.lastPos.y, vanilla_type_position.y + seek_position.y, 0.1f);
        }

        CheckBorders(room_camera, ref vanilla_type_position);
        CheckBorders(room_camera, ref room_camera.lastPos);
        CheckBorders(room_camera, ref room_camera.pos);
    }

    public void Move_Camera_Transition()
    {
        Vector2 target_position;
        if (use_vanilla_positions)
        {
            // only in case when the player is not the target
            // seekPos can change during a transition
            // this extends the transition until the player stops changing screens
            target_position = room_camera.seekPos;
        }
        else
        {
            target_position = attached_fields.on_screen_position;
            CheckBorders(room_camera, ref target_position); // stop at borders
        }

        // the tick helps when the player goes back and forth
        // to stop the transition earlier;
        room_camera.pos.x = Custom.LerpAndTick(room_camera.lastPos.x, target_position.x, smoothing_factor_x, 2f);
        room_camera.pos.y = Custom.LerpAndTick(room_camera.lastPos.y, target_position.y, smoothing_factor_y, 2f);
    }

    public void Reset()
    {
        UpdateOnScreenPosition(room_camera);
        CheckBorders(room_camera, ref attached_fields.on_screen_position); // do not move past room boundaries

        room_camera.seekPos = room_camera.CamPos(room_camera.currentCameraPosition);
        room_camera.seekPos.x += room_camera.hDisplace + 8f;
        room_camera.seekPos.y += 18f;
        room_camera.leanPos *= 0.0f;

        use_vanilla_positions = !is_split_screen_coop_enabled;
        if (use_vanilla_positions)
        {
            // center camera on vanilla position;
            room_camera.lastPos = room_camera.seekPos;
            room_camera.pos = room_camera.seekPos;
        }
        else
        {
            room_camera.lastPos = attached_fields.on_screen_position;
            room_camera.pos = attached_fields.on_screen_position;
        }

        follow_abstract_creature_id = null; // do a smooth transition // this actually makes a difference for the vanilla type camera // otherwise the map input would immediately be processed
        seek_position *= 0.0f;
        vanilla_type_position = attached_fields.on_screen_position;
        is_centered = false;
    }

    public void Update()
    {
        if (room_camera.followAbstractCreature == null) return;
        if (room_camera.room == null) return;
        UpdateOnScreenPosition(room_camera);

        // smooth transition when switching cameras in the same room
        if (follow_abstract_creature_id != room_camera.followAbstractCreature.ID && room_camera.followAbstractCreature?.realizedCreature is Creature creature)
        {
            follow_abstract_creature_id = null; // keep transition going even when switching back
            Move_Camera_Transition(); // needs followAbstractCreatureID = null // updates cameraOffset

            if (is_split_screen_coop_enabled)
            {
                // vanilla positions don't respect split screen;
                // otherwise you can move off-screen between camera positions;
                use_vanilla_positions = false;
            }
            else if (creature is Player player && Is_Map_Pressed(player))
            {
                use_vanilla_positions = !use_vanilla_positions;
            }

            // stop transition earlier when player is moving && vanilla positions are not used
            if (room_camera.pos == room_camera.lastPos || !use_vanilla_positions &&
                (Mathf.Abs(creature.mainBodyChunk.vel.x) <= 1f && room_camera.pos.x == room_camera.lastPos.x || Mathf.Abs(creature.mainBodyChunk.vel.x) > 1f && Mathf.Abs(room_camera.pos.x - room_camera.lastPos.x) <= 10f) &&
                (Mathf.Abs(creature.mainBodyChunk.vel.y) <= 1f && room_camera.pos.y == room_camera.lastPos.y || Mathf.Abs(creature.mainBodyChunk.vel.y) > 1f && Mathf.Abs(room_camera.pos.y - room_camera.lastPos.y) <= 10f))
            {
                follow_abstract_creature_id = room_camera.followAbstractCreature.ID;
                vanilla_type_position = room_camera.pos;
                is_centered = true; // used for vanilla type only
            }
            return;
        }

        if (is_centered && (Mathf.Abs(attached_fields.on_screen_position.x - attached_fields.last_on_screen_position.x) > 1f || Mathf.Abs(attached_fields.on_screen_position.y - attached_fields.last_on_screen_position.y) > 1f))
        {
            is_centered = false;
        }

        if (!use_vanilla_positions)
        {
            Move_Camera();
        }

        // in Safari mode the camera might follow other creatures;
        // this means that inputs are ignored;
        // this means that you can't center the camera and it is
        // just the vanilla camera;
        {
            if (room_camera.followAbstractCreature?.realizedCreature is not Player player) return;
            if (!Is_Map_Pressed(player)) return;

            if (is_split_screen_coop_enabled)
            {
                use_vanilla_positions = false;
            }
            else if (use_vanilla_positions || is_centered)
            {
                use_vanilla_positions = !use_vanilla_positions;
            }
            follow_abstract_creature_id = null; // start a smooth transition
        }
    }
}