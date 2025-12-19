
namespace SBCameraScroll;

public static class Util {
    public static void Util_ClearTexture(Texture2D tex, Color color) {
        Color[] pixels = new Color[tex.width * tex.height];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
        tex.SetPixels(pixels);

        // This is required. Futile doesn't handle this for us.
        tex.Apply();
    }

    public static string? Util_ExtractRoomNameFromPath(string room_path) {
        string[] split = room_path.Split(Path.DirectorySeparatorChar);
        if (split.Length == 0) {
            return null;
        }

        string[] splitted_room_name = split[split.Length-1].Split('_');
        if (splitted_room_name.Length == 0) {
            return null;
        }

        string room_name;
        if (splitted_room_name[0].ToLower() == "gate") {
            if (splitted_room_name.Length <= 2) return null;
            room_name = $"gate_{splitted_room_name[1]}_{splitted_room_name[2]}";
        } else {
            room_name = $"{splitted_room_name[0]}_{splitted_room_name[1]}";
        }

        return room_name.ToLower();
    }

    //
    //

    public static (Vector2[] camera_positions, RectInt rectangle)? Util_GetCameraPositionsAndLevelTextureRectangle(string room_name) {
        if (Custom.rainWorld?.processManager?.currentMainLoop is not RainWorldGame game) {
            Debug.Log($"{mod_id}.Util_LoadRoomTextureIntoRenderTexture: [WARNING] Expected to be in-game. But I did not find the instance for RainWorldGame. Aborting.");
            return null;
        }

        if (CalculateLevelTextureRectangle(room_name) is not RectInt rect) {
            Debug.Log($"{mod_id}.Util_LoadRoomTextureIntoRenderTexture: [WARNING] Could not calculate the level texture rectangle. Aborting.");
            return null;
        }

        Vector2[]? camera_positions = LoadCameraPositions(room_name);
        if (camera_positions == null) {
            Debug.Log($"{mod_id}.Util_LoadRoomTextureIntoRenderTexture: [WARNING] Could not load camera positions. Aborting.");
            return null;
        }

        CheckCameraPositions(ref camera_positions);
        if (camera_positions == null || camera_positions.Length == 0) {
            Debug.Log($"{mod_id}.Util_LoadRoomTextureIntoRenderTexture: [WARNING] Could not load camera positions. Aborting.");
            return null;
        }
        return (camera_positions, rect);
    }

    public static bool Util_UpdateRenderTexture(RenderTexture render_texture, RectInt rectangle) {
        int total_width  = rectangle.width;
        int total_height = rectangle.height;
        if (total_width > maximum_texture_width || total_height > maximum_texture_height) {
            Debug.Log($"{mod_id}.Util_LoadRoomTextureIntoRenderTexture: Warning! Merged texture width or height is too large. Setting to the maximum and hoping for the best.");
            total_width  = Mathf.Min(total_width, maximum_texture_width);
            total_height = Mathf.Min(total_height, maximum_texture_height);
        }

        if (total_width > SystemInfo.maxTextureSize || total_height > SystemInfo.maxTextureSize) {
            return false;
        }

        if (render_texture.width != total_width || render_texture.height != total_height) {
            render_texture.Release();
            render_texture.width  = total_width;
            render_texture.height = total_height;

            RenderTexture active_render_texture = RenderTexture.active;
            RenderTexture.active = render_texture;
            GL.Clear(clearDepth: false, clearColor: true, new Color(1f/255f, 0f, 0f));
            RenderTexture.active = active_render_texture;
        }
        return true;
    }

    //

    [Obsolete("Use Util_LoadRoomTextureIntoRenderTexture(string room_name, RenderTexture render_texture, [Texture2D cache]) or Util_LoadRoomTextureIntoRenderTexture(RoomCamera room_camera, string room_name, [RenderTexture render_texture]) instead.")]
    public static void Util_LoadRoomTextureIntoRenderTexture(string room_name, RenderTexture render_texture, int? use_cache_camera_number = null) {
        if (use_cache_camera_number == null) {
            Util_LoadRoomTextureIntoRenderTexture(room_name, render_texture, (Texture2D?)null);
        } else {
            Util_LoadRoomTextureIntoRenderTexture(room_name, render_texture, (int)use_cache_camera_number);
        }
    }

    //

    public static Texture2D camera_texture = new Texture2D(1400, 800, TextureFormat.ARGB32, mipChain: false) {
        anisoLevel = 0,
        filterMode = FilterMode.Point,
        wrapMode = TextureWrapMode.Clamp
    };

    public static bool Util_LoadRoomTextureIntoRenderTexture(string room_name, RenderTexture render_texture, Texture2D? cache = null) {
        if (Util_GetCameraPositionsAndLevelTextureRectangle(room_name) is not (Vector2[] camera_positions, RectInt rect)) {
            return false;
        }

        if (!Util_UpdateRenderTexture(render_texture, rect)) {
            return false;
        }

        if (cache == null) {
            cache = Util.camera_texture;
        }

        Vector2 min_camera_position = new Vector2(rect.x, rect.y);
        for (int cam_pos_index = 0; cam_pos_index < camera_positions.Length; ++cam_pos_index) {
            Vector2 texture_offset = camera_positions[cam_pos_index] - min_camera_position;

            int x = (int)texture_offset.x;
            int y = (int)texture_offset.y;
            int cutoff_x = 0;
            int cutoff_y = 0;

            if (x < 0) cutoff_x = -x;
            if (y < 0) cutoff_y = -y;

            if (x < maximum_texture_width && y < maximum_texture_height) {
                int width = Math.Min(1400 - cutoff_x, maximum_texture_width - x);
                int height = Math.Min(800 - cutoff_y, maximum_texture_height - y);

                // Kinda wasteful. The Texture2D is just a buffer but it lives
                // on the CPU as well as the GPU. I only need the GPU. But there
                // doesn't seem to be a direct way to upload a png file to the
                // GPU.
                string camera_texture_path = WorldLoader.FindRoomFile(room_name, includeRootDirectory: true, $"_{cam_pos_index+1}.png");
                byte[] bytes = AssetManager.PreLoadTexture(camera_texture_path);

                // Marking it prevents the copy in the RAM. But you cannot call
                // GetPixel(), etc. I get these from the GPU and cache them
                // instead.
                cache.LoadImage(bytes, markNonReadable: true);

                Graphics.CopyTexture(cache, 0, 0, cutoff_x, cutoff_y, width, height, render_texture, 0, 0, Mathf.Max(x, 0), Mathf.Max(y, 0));
            }
        }
        return true;
    }

    public static bool Util_LoadRoomTextureIntoRenderTexture(RoomCamera room_camera, string room_name, RenderTexture? render_texture = null) {
        if (Util_GetCameraPositionsAndLevelTextureRectangle(room_name) is not (Vector2[] camera_positions, RectInt rect)) {
            return false;
        }

        if (render_texture == null) {
            render_texture = room_camera.Render_Texture();
        }
        if (!Util_UpdateRenderTexture(render_texture, rect)) {
            return false;
        }

        int camera_number = room_camera.cameraNumber;
        if (camera_number < 0 || camera_number > 3) {
            Debug.Log($"{mod_id}.Util_CacheAndLoadRoomTextureIntoRenderTexture: [WARNING] Invalid camera number {camera_number}. Use 0 instead.");
            camera_number = 0;
        }

        Vector2 min_camera_position = new Vector2(rect.x, rect.y);
        for (int cam_pos_index = 0; cam_pos_index < camera_positions.Length; ++cam_pos_index) {
            Vector2 texture_offset = camera_positions[cam_pos_index] - min_camera_position;

            int x = (int)texture_offset.x;
            int y = (int)texture_offset.y;
            int cutoff_x = 0;
            int cutoff_y = 0;

            if (x < 0) cutoff_x = -x;
            if (y < 0) cutoff_y = -y;

            if (x < maximum_texture_width && y < maximum_texture_height) {
                int width = Math.Min(1400 - cutoff_x, maximum_texture_width - x);
                int height = Math.Min(800 - cutoff_y, maximum_texture_height - y);

                if (room_name != Get_Level_Texture_Room_Name(camera_number, cam_pos_index)) {
                    // Load and cache.
                    string camera_texture_path = WorldLoader.FindRoomFile(room_name, includeRootDirectory: true, $"_{cam_pos_index+1}.png");
                    byte[] bytes = AssetManager.PreLoadTexture(camera_texture_path);
                    RoomCameraMod_LoadOneScreenImage(room_camera, room_name, cam_pos_index, bytes);
                }

                Graphics.CopyTexture(Get_Level_Texture(camera_number, cam_pos_index), 0, 0, cutoff_x, cutoff_y, width, height, render_texture, 0, 0, Mathf.Max(x, 0), Mathf.Max(y, 0));
            }
        }
        return true;
    }

    //
    //

    private static readonly int _capacity = 16 * 1024;
    public static Dictionary<Vector2, Color>[] camera_number_to_cached_pixel_colors = {
        new(_capacity), new(_capacity), new(_capacity), new(_capacity)
    };
    public static Dictionary<Vector2, Color> Get_Cached_Pixel_Colors(this RoomCamera room_camera) {
        var camera_number = room_camera.cameraNumber;
        if (camera_number < 0 || camera_number > 3) {
            Debug.Log($"{mod_id}_Get_Cached_Pixel_Colors: [WARNING] Unknown camera number {camera_number}.");
            camera_number = 0;
        }
        return camera_number_to_cached_pixel_colors[camera_number];
    }

    //

    public static void Util_ClearCachedPixelColors()
    {
        foreach(var cached_pixel_colors in camera_number_to_cached_pixel_colors) {
            cached_pixel_colors.Clear();
        }
    }

    private static Texture2D pixel_texture = new Texture2D(1, 1, TextureFormat.RGB24, false);
    public static Color Util_ReadPixelColorFromCacheOrGPU(RoomCamera room_camera, Vector2 position)
    {
        Color pixel_color;

        var cached_pixel_colors = room_camera.Get_Cached_Pixel_Colors();
        if (cached_pixel_colors.TryGetValue(position, out pixel_color))
        {
            return pixel_color;
        }

        // NOTE: We need to flip the y-coordinates for Unity's rectangles.
        var render_texture = room_camera.Render_Texture();
        var height = render_texture.height;

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = render_texture;
        pixel_texture.ReadPixels(new Rect(Mathf.FloorToInt(position.x), height-Mathf.FloorToInt(position.y), 1, 1), 0, 0);
        RenderTexture.active = previous;

        pixel_color = pixel_texture.GetPixel(0,0);
        cached_pixel_colors.Add(position, pixel_color);
        return pixel_color;
    }
}
