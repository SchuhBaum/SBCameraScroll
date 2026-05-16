
namespace SBCameraScroll;

public static class AbstractRoomMod {
    //
    // parameters
    //

    public static readonly int maximum_texture_width = 16384;
    public static readonly int maximum_texture_height = 16384;
    public static bool HasCopyTextureSupport => SystemInfo.copyTextureSupport >= UnityEngine.Rendering.CopyTextureSupport.TextureToRT;

    //
    // variables
    //

    [Obsolete("Use _all_abstract_room_fields instead.")]
    internal static readonly Dictionary<AbstractRoom, Attached_Fields> _all_attached_fields = new();
    [Obsolete("Use GetFields() instead.")]
    public static Attached_Fields Get_Attached_Fields(this AbstractRoom abstract_room) => _all_attached_fields[abstract_room];

    internal static readonly Dictionary<AbstractRoom, AbstractRoomFields>
    _all_abstract_room_fields = new();

    public static AbstractRoomFields
    GetFields(
        this AbstractRoom abstract_room)
    {
        _all_abstract_room_fields.TryGetValue(abstract_room, out AbstractRoomFields abstract_room_fields);
        return abstract_room_fields;
    }

    //
    //
    //

    internal static void OnEnable() {
        On.AbstractRoom.ctor += AbstractRoom_Ctor;
        On.AbstractRoom.Abstractize += AbstractRoom_Abstractize;
    }

    // ---------------- //
    // public functions //
    // ---------------- //

    public static RectInt? CalculateLevelTextureRectangle(string room_name) {
        Vector2[]? camera_positions = LoadCameraPositions(room_name);
        if (camera_positions == null) return null;

        CheckCameraPositions(ref camera_positions);
        if (camera_positions == null || camera_positions.Length == 0) {
            return null;
        }

        int total_width  = 0;
        int total_height = 0;
        Vector2 min_camera_position = camera_positions[0];

        foreach (Vector2 camera_position in camera_positions) {
            min_camera_position.x = Mathf.Min(min_camera_position.x, camera_position.x);
            min_camera_position.y = Mathf.Min(min_camera_position.y, camera_position.y);
            total_width = Mathf.Max(total_width, (int)camera_position.x + 1400);
            total_height = Mathf.Max(total_height, (int)camera_position.y + 800);
        }

        // Ignore the effect of any position modifiers here.
        total_width  -= (int)min_camera_position.x;
        total_height -= (int)min_camera_position.y;

        return new RectInt((int)min_camera_position.x, (int)min_camera_position.y, total_width, total_height);
    }

    public static void CheckCameraPositions(ref Vector2[] camera_positions) {
        bool is_faulty_camera_found = false;
        foreach (Vector2 camera_position in camera_positions) {
            if (Mathf.Abs(camera_position.x) > 20000f || Mathf.Abs(camera_position.y) > 20000f) {
                is_faulty_camera_found = true;
            }
        }

        if (is_faulty_camera_found) {
            // SL_C01 has two cameras which are in outer space or something => needed too much memory
            Debug.Log($"{mod_id}: One or more camera screen positions are out of bounds. Remove them from cameraPositions.");

            List<Vector2> camera_positions_ = new();
            foreach (Vector2 camera_position in camera_positions) {
                if (Mathf.Abs(camera_position.x) <= 20000f && Mathf.Abs(camera_position.y) <= 20000f) {
                    camera_positions_.Add(camera_position);
                }
            }
            camera_positions = camera_positions_.ToArray();
        }
    }

    public static void DestroyWormGrassInAbstractRoom(AbstractRoom abstract_room) {
        var abstract_room_fields = abstract_room.GetFields();
        if (abstract_room_fields.worm_grass is WormGrass worm_grass) {
            Debug.Log($"{mod_id}: Remove worm grass from {abstract_room.name}.");

            // I expect only one wormGrass per room
            // wormGrass can have multiple patches with multiple tiles each

            worm_grass.Destroy();
            WormGrassMod._all_worm_grass_fields.Remove(worm_grass);
        }
        abstract_room_fields.worm_grass = null;
    }

    public static Vector2[]? LoadCameraPositions(string? room_name) {
        if (room_name == null) return null;

        string file_path = WorldLoader.FindRoomFile(room_name, false, ".txt");
        if (!File.Exists(file_path)) return null;

        // copy and paste from vanilla code
        string[] lines = File.ReadAllLines(file_path);
        int height = Convert.ToInt32(lines[1].Split('|')[0].Split('*')[1]);
        string[] line3 = lines[3].Split('|');
        Vector2[] camera_positions = new Vector2[line3.Length];

        for (int index = 0; index < line3.Length; ++index) {
            camera_positions[index] = new Vector2(Convert.ToSingle(line3[index].Split(',')[0]), height * 20f - 800f - Convert.ToSingle(line3[index].Split(',')[1]));
        }
        return camera_positions;
    }

    // I need to initialize the fields again if CRS changes the room name.
    // Otherwise, the camera textures are misaligned or not merged.
    public static void UpdateAttachedFields(AbstractRoom abstract_room) {
        var abstract_room_fields = abstract_room.GetFields();

        // This changes the name if the REPLACEROOM feature is used.
        string room_name = abstract_room.FileName;
        if (room_name.Contains("OffScreenDen") || room_name.Contains("offscreenden")) {
            return;
        }

        if (CalculateLevelTextureRectangle(room_name) is not RectInt rect) {
            Debug.Log($"{mod_id}: Failed to initialize abstract_room_fields for room {room_name}.");
            return;
        }

        int total_width  = rect.width;
        int total_height = rect.height;
        abstract_room_fields.min_camera_position = new Vector2(rect.x, rect.y);

        if (total_width > maximum_texture_width || total_height > maximum_texture_height) {
            Debug.Log($"{mod_id}: Warning! Merged texture width or height is too large. Setting to the maximum and hoping for the best.");
            total_width  = Mathf.Min(total_width, maximum_texture_width);
            total_height = Mathf.Min(total_height, maximum_texture_height);
        }

        abstract_room_fields.total_width  = total_width;
        abstract_room_fields.total_height = total_height;

        // Debug.Log($"{mod_id}: Initialized abstract_room_fields for room {room_name}.");
    }

    //
    // private
    //

    private static void AbstractRoom_Ctor(On.AbstractRoom.orig_ctor orig, AbstractRoom abstract_room, string room_name, int[] connections, int index, int swarm_room_index, int shelter_index, int gate_index) {
        orig(abstract_room, room_name, connections, index, swarm_room_index, shelter_index, gate_index);
        if (_all_abstract_room_fields.ContainsKey(abstract_room)) return;
        _all_abstract_room_fields.Add(abstract_room, new AbstractRoomFields());
        UpdateAttachedFields(abstract_room);
    }

    private static void AbstractRoom_Abstractize(On.AbstractRoom.orig_Abstractize orig, AbstractRoom abstract_room) {
        DestroyWormGrassInAbstractRoom(abstract_room);
        orig(abstract_room);
    }

    //
    //
    //

    [Obsolete]
    public sealed class Attached_Fields {
        public int total_width = 1400;
        public int total_height = 800;

        public Vector2 min_camera_position = new();
        public WormGrass? worm_grass = null;
    }

    public sealed class AbstractRoomFields {
        public int total_width = 1400;
        public int total_height = 800;

        public Vector2 min_camera_position = new();
        public WormGrass? worm_grass = null;
    }
}
