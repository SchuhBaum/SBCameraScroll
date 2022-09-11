namespace SBCameraScroll
{
    public static class RoomMod
    {
        public static bool CanScrollCamera(Room? room) => MainMod.isScrollOneScreenRoomsOptionEnabled || room?.cameraPositions.Length > 1;

        internal static void OnEnable()
        {
            On.Room.LoadFromDataString += Room_LoadFromDataString;
        }

        // ----------------- //
        // private functions //
        // ----------------- //

        private static void Room_LoadFromDataString(On.Room.orig_LoadFromDataString orig, Room room, string[] lines)
        {
            orig(room, lines);
            if (room?.game != null && room.abstractRoom.name is string roomName && !RoomCameraMod.blacklistedRooms.Contains(roomName))
            {
                AbstractRoomMod.CheckCameraPositions(ref room.cameraPositions);
                AbstractRoomMod.UpdateTextureOffset(room.abstractRoom, room.cameraPositions); // update for one-screen rooms as well
                AbstractRoomMod.MergeCameraTextures(room.abstractRoom, room.abstractRoom.world?.regionState?.regionName, room.cameraPositions); // warping might mess with world or region state => check for nulls // regionState is a function and needs game != null
            }
        }
    }
}