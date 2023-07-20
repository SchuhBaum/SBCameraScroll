using UnityEngine;

namespace SBCameraScroll;

internal static class AboveCloudsViewMod {
    // copy & paste from bee's mod // removes visible "jumps" of clouds
    internal static void OnEnable() {
        On.AboveCloudsView.CloseCloud.DrawSprites += CloseCloud_DrawSprites;
        On.AboveCloudsView.DistantCloud.DrawSprites += DistantCloud_DrawSprites;
        On.AboveCloudsView.DistantLightning.DrawSprites += DistantLightning_DrawSprites; // only for adjusting alpha?
        On.AboveCloudsView.FlyingCloud.DrawSprites += FlyingCloud_DrawSprites;
    }

    //
    // private
    //

    private static void CloseCloud_DrawSprites(On.AboveCloudsView.CloseCloud.orig_DrawSprites orig, AboveCloudsView.CloseCloud close_cloud, RoomCamera.SpriteLeaser sprite_leaser, RoomCamera room_camera, float time_stacker, Vector2 camera_position) {
        if (room_camera.room == null) {
            orig(close_cloud, sprite_leaser, room_camera, time_stacker, camera_position);
            return;
        }

        if (room_camera.Is_Type_Camera_Not_Used()) {
            orig(close_cloud, sprite_leaser, room_camera, time_stacker, camera_position);
            return;
        }

        Vector2 room_camera_position = room_camera.room.cameraPositions[room_camera.currentCameraPosition];
        room_camera.room.cameraPositions[room_camera.currentCameraPosition] = room_camera.room.cameraPositions[0];
        orig(close_cloud, sprite_leaser, room_camera, time_stacker, camera_position);
        room_camera.room.cameraPositions[room_camera.currentCameraPosition] = room_camera_position;

        // this makes the position as it should be // i.e. it respects a moving camera (even without scrolling the camera moves);
        // however this messes with the light of the cloud shader // the clouds look like light bulbs turning on and off when scrolling very quickly;
        // I need to preserve the offset (683f) for some rooms;
        //
        // at this point I think that these cloud background overlay(?) objects are too small;
        // scrolling them leads to problems one way or another;
        // I probably need to change the cloud shader instead?;
        // spriteLeaser.sprites[1].x += closeCloud.DrawPos(cameraPosition, roomCamera.hDisplace).x;
    }

    private static void DistantCloud_DrawSprites(On.AboveCloudsView.DistantCloud.orig_DrawSprites orig, AboveCloudsView.DistantCloud distant_cloud, RoomCamera.SpriteLeaser sprite_leaser, RoomCamera room_camera, float time_stacker, Vector2 camera_position) {
        if (room_camera.room == null) {
            orig(distant_cloud, sprite_leaser, room_camera, time_stacker, camera_position);
            return;
        }

        if (room_camera.Is_Type_Camera_Not_Used()) {
            orig(distant_cloud, sprite_leaser, room_camera, time_stacker, camera_position);
            return;
        }

        Vector2 room_camera_position = room_camera.room.cameraPositions[room_camera.currentCameraPosition];
        room_camera.room.cameraPositions[room_camera.currentCameraPosition] = room_camera.room.cameraPositions[0];
        orig(distant_cloud, sprite_leaser, room_camera, time_stacker, camera_position);
        room_camera.room.cameraPositions[room_camera.currentCameraPosition] = room_camera_position;

        // spriteLeaser.sprites[1].x += distantCloud.DrawPos(cameraPosition, roomCamera.hDisplace).x;
    }

    private static void DistantLightning_DrawSprites(On.AboveCloudsView.DistantLightning.orig_DrawSprites orig, AboveCloudsView.DistantLightning distant_lightning, RoomCamera.SpriteLeaser sprite_leaser, RoomCamera room_camera, float time_stacker, Vector2 camera_position) {
        if (room_camera.room == null) {
            orig(distant_lightning, sprite_leaser, room_camera, time_stacker, camera_position);
            return;
        }

        if (room_camera.Is_Type_Camera_Not_Used()) {
            orig(distant_lightning, sprite_leaser, room_camera, time_stacker, camera_position);
            return;
        }

        Vector2 room_camera_position = room_camera.room.cameraPositions[room_camera.currentCameraPosition];
        room_camera.room.cameraPositions[room_camera.currentCameraPosition] = room_camera.room.cameraPositions[0];
        orig(distant_lightning, sprite_leaser, room_camera, time_stacker, camera_position);
        room_camera.room.cameraPositions[room_camera.currentCameraPosition] = room_camera_position;

        // the sprites for this are already positioned correctly;
        // spriteLeaser.sprites[0].x -= cameraPosition.x; // wrong
    }

    private static void FlyingCloud_DrawSprites(On.AboveCloudsView.FlyingCloud.orig_DrawSprites orig, AboveCloudsView.FlyingCloud flying_cloud, RoomCamera.SpriteLeaser sprite_leaser, RoomCamera room_camera, float time_stacker, Vector2 camera_position) {
        if (room_camera.room == null) {
            orig(flying_cloud, sprite_leaser, room_camera, time_stacker, camera_position);
            return;
        }

        if (room_camera.Is_Type_Camera_Not_Used()) {
            orig(flying_cloud, sprite_leaser, room_camera, time_stacker, camera_position);
            return;
        }

        Vector2 room_camera_position = room_camera.room.cameraPositions[room_camera.currentCameraPosition];
        room_camera.room.cameraPositions[room_camera.currentCameraPosition] = room_camera.room.cameraPositions[0];
        orig(flying_cloud, sprite_leaser, room_camera, time_stacker, camera_position);
        room_camera.room.cameraPositions[room_camera.currentCameraPosition] = room_camera_position;

        // spriteLeaser.sprites[0].x += flyingCloud.DrawPos(cameraPosition, roomCamera.hDisplace).x;
    }
}
