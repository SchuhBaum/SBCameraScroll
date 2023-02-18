using System;
using UnityEngine;

namespace SBCameraScroll;

public class PositionTypeCamera
{
    //
    // parameters
    //

    public static float camera_box_x = 40f;
    public static float camera_box_y = 40f;
    public static float offset_speed_multiplier = 0.2f;

    public static float Smoothing_Factor_X => RoomCameraMod.smoothing_factor_x;
    public static float Smoothing_Factor_Y => RoomCameraMod.smoothing_factor_y;

    private readonly RoomCamera room_camera;
    private readonly RoomCameraMod.AttachedFields attached_fields;

    //
    // variables
    //

    public EntityID? follow_abstract_creature_id = null;
    public Vector2 camera_offset = new();


    //
    //
    //

    public PositionTypeCamera(RoomCamera roomCamera, RoomCameraMod.AttachedFields attachedFields)
    {
        room_camera = roomCamera;
        attached_fields = attachedFields;
    }

    //
    // public
    //

    private void Move_Camera_Towards_Target(Vector2 targetPosition, Vector2 at_border_difference)
    {
        float camera_box_minus_border_x = Mathf.Max(0.0f, camera_box_x - Mathf.Abs(at_border_difference.x));
        float camera_box_minus_border_y = Mathf.Max(0.0f, camera_box_y - Mathf.Abs(at_border_difference.y));

        float distance_x = Mathf.Abs(targetPosition.x - room_camera.lastPos.x);
        float distance_y = Mathf.Abs(targetPosition.y - room_camera.lastPos.y);

        if (distance_x > camera_box_minus_border_x)
        {
            // the goal is to reach innerCameraBoxX_minus_border-close to targetPosition.x
            // the result is the same as:
            // roomCamera.pos.x = Mathf.Lerp(roomCamera.lastPos.x, innerCameraBoxX_minus_border-close to targetPosition.x, t = smoothingFactorX);
            room_camera.pos.x = Mathf.Lerp(room_camera.lastPos.x, targetPosition.x, Smoothing_Factor_X * (distance_x - camera_box_minus_border_x) / distance_x);

            // stop when moving too slow;
            // downside is that the targetposition might not be reached exactly;
            // depending on smoothingFactorX this can be a couple of pixels far away from targetPosition;
            if (Mathf.Abs(room_camera.pos.x - room_camera.lastPos.x) < 1f)
            {
                room_camera.pos.x = room_camera.lastPos.x;
            }
        }
        else
        {
            room_camera.pos.x = room_camera.lastPos.x;
        }

        if (distance_y > camera_box_minus_border_y)
        {
            room_camera.pos.y = Mathf.Lerp(room_camera.lastPos.y, targetPosition.y, Smoothing_Factor_Y * (distance_y - camera_box_minus_border_y) / distance_y);
            if (Mathf.Abs(room_camera.pos.y - room_camera.lastPos.y) < 1f)
            {
                room_camera.pos.y = room_camera.lastPos.y;
            }
        }
        else
        {
            room_camera.pos.y = room_camera.lastPos.y;
        }
    }

    public void Move_Camera_Without_Offset()
    {
        Vector2 targetPosition = attached_fields.onScreenPosition;
        RoomCameraMod.CheckBorders(room_camera, ref targetPosition);

        Vector2 at_border_difference = attached_fields.onScreenPosition - targetPosition;
        Move_Camera_Towards_Target(targetPosition, at_border_difference);
    }

    public void Move_Camera_With_Offset_Using_Player_Input(in Player player)
    {
        Vector2 targetPosition = attached_fields.onScreenPosition + camera_offset;
        RoomCameraMod.CheckBorders(room_camera, ref targetPosition);

        Vector2 at_border_difference = attached_fields.onScreenPosition + camera_offset - targetPosition;
        Move_Camera_Towards_Target(targetPosition, at_border_difference);
        Update_Camera_Offset_Using_Player_Input(player, at_border_difference);
    }

    public void Move_Camera_With_Offset_Using_Position_Input()
    {
        Vector2 targetPosition = attached_fields.onScreenPosition + camera_offset;
        RoomCameraMod.CheckBorders(room_camera, ref targetPosition);

        Vector2 at_border_difference = attached_fields.onScreenPosition + camera_offset - targetPosition;
        Move_Camera_Towards_Target(targetPosition, at_border_difference);
        Update_Camera_Offset_Using_Position_Input(at_border_difference);
    }

    public void Reset()
    {
        RoomCameraMod.UpdateOnScreenPosition(room_camera);
        RoomCameraMod.CheckBorders(room_camera, ref attached_fields.onScreenPosition); // do not move past room boundaries

        // center camera on player
        room_camera.lastPos = attached_fields.onScreenPosition;
        room_camera.pos = attached_fields.onScreenPosition;
        follow_abstract_creature_id = room_camera.followAbstractCreature?.ID;
        camera_offset = new();
    }

    public void Update()
    {
        if (room_camera.followAbstractCreature == null) return;
        if (room_camera.room == null) return;
        RoomCameraMod.UpdateOnScreenPosition(room_camera);

        // is_in_transition
        if (follow_abstract_creature_id != room_camera.followAbstractCreature.ID && room_camera.followAbstractCreature?.realizedCreature is Creature creature)
        {
            follow_abstract_creature_id = room_camera.followAbstractCreature.ID;
            room_camera.pos = room_camera.lastPos; // just wait this frame and resume next frame;

            {
                if (creature is Player player)
                {
                    camera_offset.x = player.input[0].x * camera_box_x;
                    camera_offset.y = player.input[0].y * camera_box_y;
                }
            }
            return;
        }

        if (!MainMod.Option_CameraOffset)
        {
            Move_Camera_Without_Offset();
            return;
        }

        // scope the variable player; otherwise I need to re-name stuff;
        {
            if (room_camera.followAbstractCreature?.realizedObject is Player player)
            {
                // same as the other function but uses player inputs
                // instead of position changes in order to update
                // the offset;
                Move_Camera_With_Offset_Using_Player_Input(player);
                return;
            }
        }
        Move_Camera_With_Offset_Using_Position_Input();
    }

    private void Update_Camera_Offset_Using_Player_Input(in Player player, Vector2 at_border_difference)
    {
        bool has_target_turned_around_x = player.input[0].x != 0 && player.input[0].x == -Math.Sign(camera_offset.x);
        bool has_target_turned_around_y = player.input[0].y != 0 && player.input[0].y == -Math.Sign(camera_offset.y);

        bool has_target_and_camera_moved_x = player.input[0].x != 0 && room_camera.pos.x != room_camera.lastPos.x;
        bool has_target_and_camera_moved_y = player.input[0].y != 0 && room_camera.pos.y != room_camera.lastPos.y;

        Update_Camera_Offset_XY(ref camera_offset.x, at_border_difference.x, camera_box_x, has_target_turned_around_x, has_target_and_camera_moved_x);
        Update_Camera_Offset_XY(ref camera_offset.y, at_border_difference.y, camera_box_y, has_target_turned_around_y, has_target_and_camera_moved_y);
    }

    private void Update_Camera_Offset_Using_Position_Input(Vector2 at_border_difference)
    {
        float buffer = 2f;

        bool has_target_moved_x = Mathf.Abs(attached_fields.onScreenPosition.x - attached_fields.lastOnScreenPosition.x) > buffer;
        bool has_target_moved_y = Mathf.Abs(attached_fields.onScreenPosition.y - attached_fields.lastOnScreenPosition.y) > buffer;

        bool has_target_turned_around_x = has_target_moved_x && Math.Sign(attached_fields.onScreenPosition.x - attached_fields.lastOnScreenPosition.x) == -Math.Sign(camera_offset.x);
        bool has_target_turned_around_y = has_target_moved_y && Math.Sign(attached_fields.onScreenPosition.y - attached_fields.lastOnScreenPosition.y) == -Math.Sign(camera_offset.y);

        bool has_target_and_camera_moved_x = has_target_moved_x && room_camera.pos.x != room_camera.lastPos.x;
        bool has_target_and_camera_moved_y = has_target_moved_y && room_camera.pos.y != room_camera.lastPos.y;

        Update_Camera_Offset_XY(ref camera_offset.x, at_border_difference.x, camera_box_x, has_target_turned_around_x, has_target_and_camera_moved_x);
        Update_Camera_Offset_XY(ref camera_offset.y, at_border_difference.y, camera_box_y, has_target_turned_around_y, has_target_and_camera_moved_y);
    }

    // probably not worth the refactor;
    // plus now these things are coupled;
    private void Update_Camera_Offset_XY(ref float camera_offset, float at_border_difference, float camera_box, bool has_target_turned_around, bool has_target_and_camera_moved)
    {
        bool is_at_border = Mathf.Abs(at_border_difference) > 0.1f;
        float buffer = 10f;

        if (is_at_border)
        {
            if (at_border_difference > camera_box + buffer)
            {
                camera_offset = Mathf.Clamp(camera_offset - at_border_difference + camera_box + buffer, -2f * camera_box, 2f * camera_box);
            }
            else if (at_border_difference < -camera_box - buffer)
            {
                camera_offset = Mathf.Clamp(camera_offset - at_border_difference - camera_box - buffer, -2f * camera_box, 2f * camera_box);
            }
            return;
        }

        if (has_target_turned_around)
        {
            camera_offset = 0f;
            return;
        }

        if (!has_target_and_camera_moved) return;
        camera_offset = Mathf.Clamp(camera_offset + offset_speed_multiplier * (attached_fields.onScreenPosition.x - attached_fields.lastOnScreenPosition.x), -2f * camera_box, 2f * camera_box);
    }
}