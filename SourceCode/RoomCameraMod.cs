

// TODO: there is some distortion based on distance going on that can be very extreme; check if I can reduce it;

namespace SBCameraScroll;

public static class RoomCameraMod {
    // Maybe I can use this for debugging purposes more often.
    // public static FSprite sprite_0 = new("Futile_White");
    // public static FSprite sprite_1 = new("Futile_White");
    // public static DebugSprite debug_sprite_0 = new(new(), sprite_0, null);
    // public static DebugSprite debug_sprite_1 = new(new(), sprite_1, null);

    //
    // parameters
    //

    public static CameraType camera_type = CameraType.Position;
    public static float smoothing_factor = 0.16f;

    // used in CoopTweaks; don't rename;
    public static float number_of_frames_per_shortcut_udpate = 3f;
    public static List<string> blacklisted_rooms = new List<string>() { "RM_AI", "GW_ARTYSCENES", "GW_ARTYNIGHTMARE", "SB_E05SAINT", "SL_AI", "WRSA_WEAVER" };

    // makes some shader glitch out more;
    // not recommended;
    public static float camera_zoom = 1f;
    public static float Half_Inverse_Camera_Zoom_XY => 0.5f * (1f / camera_zoom - 1f);
    public static bool Is_Camera_Zoom_Enabled => !is_split_screen_coop_enabled && camera_zoom != 1f;
    public static bool Is_Dynamic_Zoom_Enabled => !is_split_screen_coop_enabled && Option_DynamicZoom;

    //
    // variables
    //

    [Obsolete]
    internal static readonly Dictionary<RoomCamera, Attached_Fields> _all_attached_fields = new();
    [Obsolete]
    public static Attached_Fields Get_Attached_Fields(this RoomCamera room_camera) => _all_attached_fields[room_camera];

    internal static readonly Dictionary<RoomCamera, RoomCameraFields> _all_room_camera_fields = new();
    public static RoomCameraFields GetFields(this RoomCamera room_camera) {
        _all_room_camera_fields.TryGetValue(room_camera, out RoomCameraFields room_camera_fields);
        return room_camera_fields;
    }

    public static bool Is_Camera_Scroll_Enabled(this RoomCamera room_camera) => room_camera.room?.cameraPositions.Length > 1 || Option_ScrollOneScreenRooms || camera_zoom > 1f || room_camera.GetFields() is RoomCameraFields room_camera_fields && room_camera_fields.is_camera_scroll_forced_by_split_screen;

    // You want to access the non-null room in most cases. This does not save
    // code.
    [Obsolete("Use IsRoomBlacklisted() instead.")]
    public static bool
    Is_Type_Camera_Not_Used(
        this RoomCamera room_camera)
    {
        return room_camera.room is not Room room || room_camera.IsRoomBlacklisted(room.abstractRoom);
    }

    public static bool
    IsRoomBlacklisted(
        this RoomCamera room_camera,
        AbstractRoom abstract_room)
    {
        return room_camera.voidSeaMode || abstract_room.IsRoomBlacklisted();
    }

    public static bool
    IsRoomBlacklisted(
        this RoomCamera room_camera,
        String room_name)
    {
        return room_camera.voidSeaMode || IsRoomBlacklisted(room_name);
    }

    public static bool
    IsRoomBlacklisted(
        this AbstractRoom abstract_room)
    {
        return IsRoomBlacklisted(abstract_room.FileName);
    }

    public static bool
    IsRoomBlacklisted(
        string room_name)
    {
        return blacklisted_rooms.Contains(room_name);
    }

    public static string? next_text_prompt_message = null;

    public static Hook? hook_RoomCamera_LevelTexture = null;

    // The variable new(10) does not allocate memory yet. As soon as the first
    // element is added it will reserve memory for the other 9 spots.
    public static List<Texture2D>[] level_texture_lists = {
        new(10), new(10), new(10), new(10)
    };
    public static List<string>[] level_texture_room_name_lists = {
        new(10), new(10), new(10), new(10)
    };
    // The naming is bad. Level texture can mean two different things.
    //   1) One camera screen texture.
    //   2) The whole room texture.
    // In vanilla only 1) is used.
    public static Texture2D Get_Level_Texture(int camera_number, int cam_pos_index) {
        if (Option_ReducedMemoryUsage) {
            Debug.Log($"{mod_id}.Get_Level_Texture: [WARNING] This function should never be called with Option_ReducedMemoryUsage turned on. That option is useless now.");
        }

        if (camera_number < 0 || camera_number > 3) {
            Debug.Log($"{mod_id}.Get_Level_Texture: [WARNING] I got the invalid camera number {camera_number}. I will use 0 instead.");
            camera_number = 0;
        }
        while (cam_pos_index >= level_texture_lists[camera_number].Count) {
            Texture2D level_texture = new Texture2D(1400, 800, TextureFormat.ARGB32, mipChain: false) {
                anisoLevel = 0,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            level_texture_lists[camera_number].Add(level_texture);
        }
        return level_texture_lists[camera_number][cam_pos_index];
    }
    public static string Get_Level_Texture_Room_Name(int camera_number, int cam_pos_index) {
        if (camera_number < 0 || camera_number > 3) {
            Debug.Log($"{mod_id}.Get_Level_Texture_Room_Name: [WARNING] I got the invalid camera number {camera_number}. I will use 0 instead.");
            camera_number = 0;
        }
        while (cam_pos_index >= level_texture_room_name_lists[camera_number].Count) {
            level_texture_room_name_lists[camera_number].Add("");
        }
        return level_texture_room_name_lists[camera_number][cam_pos_index];
    }
    public static void Set_Level_Texture_Room_Name(string room_name, int camera_number, int cam_pos_index) {
        if (camera_number < 0 || camera_number > 3) return;
        while (cam_pos_index >= level_texture_room_name_lists[camera_number].Count) {
            level_texture_room_name_lists[camera_number].Add("");
        }
        level_texture_room_name_lists[camera_number][cam_pos_index] = room_name;
    }

    public static RenderTexture[] render_texture_array = {
        new RenderTexture(1, 1, 0, RenderTextureFormat.ARGB32) {
            anisoLevel = 0,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            useMipMap = false,
        },
        new RenderTexture(1, 1, 0, RenderTextureFormat.ARGB32) {
            anisoLevel = 0,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            useMipMap = false,
        },
        new RenderTexture(1, 1, 0, RenderTextureFormat.ARGB32) {
            anisoLevel = 0,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            useMipMap = false,
        },
        new RenderTexture(1, 1, 0, RenderTextureFormat.ARGB32) {
            anisoLevel = 0,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            useMipMap = false,
        }
    };
    public static RenderTexture Render_Texture(this RoomCamera room_camera) {
        int camera_number = room_camera.cameraNumber;
        if (camera_number < 0 || camera_number > 3) {
            Debug.Log($"{mod_id}.Render_Texture: [WARNING] I got the invalid camera number {camera_number}. I will use 0 instead.");
            camera_number = 0;
        }
        return render_texture_array[camera_number];
    }

    //
    //
    //

    internal static void OnEnable() {
        IL.RoomCamera.DrawUpdate += IL_RoomCamera_DrawUpdate;
        IL.RoomCamera.Update     += IL_RoomCamera_Update;

        On.RoomCamera.ApplyDepth                  += RoomCamera_ApplyDepth;
        On.RoomCamera.ApplyPalette                += RoomCamera_ApplyPalette;
        On.RoomCamera.ApplyPositionChange         += RoomCamera_ApplyPositionChange;
        On.RoomCamera.ctor                        += RoomCamera_Ctor;
        On.RoomCamera.IsViewedByCameraPosition    += RoomCamera_IsViewedByCameraPosition;
        On.RoomCamera.IsVisibleAtCameraPosition   += RoomCamera_IsVisibleAtCameraPosition;
        On.RoomCamera.MoveCamera_int              += RoomCamera_MoveCamera;
        On.RoomCamera.PositionCurrentlyVisible    += RoomCamera_PositionCurrentlyVisible;
        On.RoomCamera.PositionVisibleInNextScreen += RoomCamera_PositionVisibleInNextScreen;
        On.RoomCamera.PreLoadTexture              += RoomCamera_PreLoadTexture;
        On.RoomCamera.RectCurrentlyVisible        += RoomCamera_RectCurrentlyVisible;
        On.RoomCamera.ScreenMovement              += RoomCamera_ScreenMovement;

        //
        // just-in-time merging
        //

        for (int camera_number = 0; camera_number < render_texture_array.Length; ++camera_number) {
            RenderTexture render_texture = render_texture_array[camera_number];
            Replace_Or_Add_Atlas($"LevelTexture{(camera_number == 0 ? "" : camera_number.ToString())}", render_texture);
        }

        // Trying to hook On.PersistentData.ctor does not work. The mod is not
        // loaded when that function is called.
        if (Type.GetType("RoomCamera, Assembly-CSharp") is Type RoomCamera) {
            try {
                hook_RoomCamera_LevelTexture = new Hook(RoomCamera.GetMethod("get_levelTexture", BindingFlags.Public | BindingFlags.Instance), typeof(RoomCameraMod).GetMethod("RoomCamera_LevelTexture"));
            } catch (Exception exception) {
                Debug.Log($"{mod_id}: {exception}");
            }
        }

        //
        // These are not all option unrelated hooks. The IL-hook
        // IL_RoomCamera_Update contains a small section only for just-in-time
        // merging.
        //

        IL.RoomCamera.ApplyPositionChange += IL_RoomCamera_ApplyPositionChange;

        On.RoomCamera.ChangeCameraToPlayer   += RoomCamera_ChangeCameraToPlayer;
        On.RoomCamera.DepthAtCoordinate      += RoomCamera_DepthAtCoordinate;
        On.RoomCamera.LitAtCoordinate        += RoomCamera_LitAtCoordinate;
        On.RoomCamera.PixelColorAtCoordinate += RoomCamera_PixelColorAtCoordinate;
        On.RoomCamera.UpdateSnowLight        += RoomCamera_UpdateSnowLight;

        //
        //
    }

    //
    // public
    //

    public static void Apply_Camera_Zoom(RoomCamera room_camera) {
        // The screens overlap if you mess with the zoom when SplitScreen Coop
        // is enabled.
        if (is_split_screen_coop_enabled) {
            return;
        }

        if (camera_zoom == 1f) {
            Reset_Camera_Zoom(room_camera);
            return;
        }

        // copied from SlugcatEyebrowRaise mod
        for (int sprite_layer_index = 0; sprite_layer_index < 11; ++sprite_layer_index) {
            FContainer sprite_layer = room_camera.SpriteLayers[sprite_layer_index];
            sprite_layer.scale = 1f;
            sprite_layer.SetPosition(Vector2.zero);

            // this makes it such that the graphics are centered;
            // 
            // still, there are scaling issues:
            // for example, the underwater glow is only aligned with
            // slugcat when in the center of the screen;
            // when zoomed out it will move faster away from the
            // center compared to slugcat which can only reach the
            // border of the level visuals; the glow seem to reach
            // the border of the screen instead;
            sprite_layer.ScaleAroundPointRelative(0.5f * room_camera.sSize, camera_zoom, camera_zoom);
        }
    }

    public static void AddFadeTransition(RoomCamera room_camera) {
        if (room_camera.room is not Room room) return;
        if (room.roomSettings.fadePalette == null) return;

        // the day-night fade effect does not update paletteBlend in all cases;
        // so this can otherwise reset it sometimes;
        // priotize day-night over this;
        if (room_camera.effect_dayNight > 0f && room.world.rainCycle.timer >= room.world.rainCycle.cycleLength) return;
        if (ModManager.Expedition && room.game.rainWorld.ExpeditionMode && ExpeditionGame.activeUnlocks.Contains("bur-blinded")) return;

        // the fade is automatically applied in RoomCamera.Update();
        room_camera.paletteBlend = Mathf.Lerp(room_camera.paletteBlend, room_camera.room.roomSettings.fadePalette.fades[room_camera.currentCameraPosition], 0.01f);
    }

    public static void CheckBorders(RoomCamera room_camera, ref Vector2 position) {
        if (room_camera.room == null) return;
        Vector2 screen_size = room_camera.sSize;
        Vector2 min_camera_position = room_camera.room.abstractRoom.GetFields().min_camera_position; // regionGate's min_camera_position might be unitialized => RegionGateMod

        // half of the camera screen is not visible; the other half is centered; let the
        // non-visible part move past room borders;
        Vector2 screen_offset = is_split_screen_coop_enabled ? Get_Screen_Offset(room_camera, screen_size) : new();

        // Half_Inverse_Camera_Zoom_XY:
        // in percent; how much screen space is added left and right, top and bottom;
        // example: camera_zoom = 0.8f increases the screen size in x and y by 25% each; Half_Inverse_Camera_Zoom_XY = 0.5 * 25%;
        Vector2 screen_size_increase = Is_Camera_Zoom_Enabled ? Half_Inverse_Camera_Zoom_XY * room_camera.sSize : Vector2.zero;
        float min_x = min_camera_position.x - screen_offset.x + screen_size_increase.x;
        float max_x = min_camera_position.x + screen_offset.x - screen_size_increase.x + room_camera.levelGraphic.width - screen_size.x;

        if (min_x < max_x) {
            // stop position at room texture borders;
            position.x = Mathf.Clamp(position.x, min_x, max_x);
        } else {
            // keep the position centered in case the camera is zoomed;
            position.x = 0.5f * (min_x + max_x);
        }

        // not sure why I have to decrease max_y by a constant;
        // I picked 18f bc room_camera.seekPos.y gets changed by 18f in Update();
        // seems to work, i.e. I don't see black bars;
        float min_y = min_camera_position.y - screen_offset.y + screen_size_increase.y;
        float max_y = min_camera_position.y + screen_offset.y - screen_size_increase.y + room_camera.levelGraphic.height - screen_size.y - 18f;

        if (min_y < max_y) {
            position.y = Mathf.Clamp(position.y, min_y, max_y);
        } else {
            position.y = 0.5f * (min_y + max_y);
        }
    }

    public static Vector2 DrawUpdate_GetCameraPosition(RoomCamera room_camera, Vector2 camera_position) {
        if (room_camera.room is not Room room || room_camera.IsRoomBlacklisted(room.abstractRoom)) {
            // This is called instead of `CamPos(currentCameraPosition)` inside
            // functions like this:
            //   Mathf.Clamp(vector.x, CamPos(currentCameraPosition).x + hDisplace + 8f - 20f, CamPos(currentCameraPosition).x + hDisplace + 8f + 20f);
            return room_camera.CamPos(room_camera.currentCameraPosition);
        }

        // Given the place where this is called, we need to undo adding
        // hDisplace. We don't want offsets. We just want to skip the clamping.
        camera_position.x -= room_camera.hDisplace;
        return camera_position;
    }

    public static Vector2 DrawUpdate_GetMainBodyChunkOrOnScreenPosition(RoomCamera room_camera) {
        if (room_camera.room is not Room room || room_camera.IsRoomBlacklisted(room.abstractRoom)) {
            // vanilla:
            // The null check was already done.
            return room_camera.followAbstractCreature.realizedCreature.mainBodyChunk.pos;
        }
        return room_camera.GetFields().on_screen_position + 0.5f * room_camera.sSize;
    }

    public static void DrawUpdate_UpdateLevelTextureGameObject(RoomCamera room_camera, Vector2 camera_position) {
        if (room_camera.room is not Room room || room_camera.IsRoomBlacklisted(room.abstractRoom)) {
            room_camera.levelGraphic.x = room_camera.CamPos(room_camera.currentCameraPosition).x - camera_position.x;
            room_camera.levelGraphic.y = room_camera.CamPos(room_camera.currentCameraPosition).y - camera_position.y;
            room_camera.backgroundGraphic.x = room_camera.CamPos(room_camera.currentCameraPosition).x - camera_position.x;
            room_camera.backgroundGraphic.y = room_camera.CamPos(room_camera.currentCameraPosition).y - camera_position.y;
            return;
        }

        // not sure what this does // seems to visually darken stuff (apply shader or something) when offscreen
        // I think that textureOffset is only needed(?) for compatibility reasons with room.cameraPositions
        Vector2 min_camera_position = room_camera.room.abstractRoom.GetFields().min_camera_position;
        room_camera.levelGraphic.SetPosition(min_camera_position - camera_position);
        room_camera.backgroundGraphic.SetPosition(min_camera_position - camera_position);
    }

    public static void DrawUpdate_UpdateShadPropSpriteRect(RoomCamera room_camera, Vector2 camera_position) {
        if (room_camera.room is not Room room || room_camera.IsRoomBlacklisted(room.abstractRoom)) {
            Vector4 sprite_rect = new Vector4(
                (-camera_position.x - 0.5f + room_camera.CamPos(room_camera.currentCameraPosition).x) / room_camera.sSize.x,
                (-camera_position.y + 0.5f + room_camera.CamPos(room_camera.currentCameraPosition).y) / room_camera.sSize.y,
                (-camera_position.x - 0.5f + room_camera.levelGraphic.width + room_camera.CamPos(room_camera.currentCameraPosition).x) / room_camera.sSize.x,
                (-camera_position.y + 0.5f + room_camera.levelGraphic.height + room_camera.CamPos(room_camera.currentCameraPosition).y) / room_camera.sSize.y
            );
            Shader.SetGlobalVector(RainWorld.ShadPropSpriteRect, sprite_rect);

        } else if (!Is_Camera_Zoom_Enabled) {
            Vector4 sprite_rect = new Vector4(
                (room_camera.levelGraphic.x - 0.5f) / room_camera.sSize.x,
                (room_camera.levelGraphic.y + 0.5f) / room_camera.sSize.y,
                (room_camera.levelGraphic.x + room_camera.levelGraphic.width - 0.5f) / room_camera.sSize.x,
                (room_camera.levelGraphic.y + room_camera.levelGraphic.height + 0.5f) / room_camera.sSize.y
            );
            Shader.SetGlobalVector(RainWorld.ShadPropSpriteRect, sprite_rect);

        } else {
            // When zooming out the screen gets smaller. The offset is to
            // center the sprite rectangle. If your screen is 0.25 times as
            // big then you want to move 1.5 small screens towards bottom-left.
            // (There fit 4 small screens in total, so left margin is 1.5
            // small screens and right one as well.) If you plug that into
            // the Shader.SetGlobalVector() formula below and simplify then
            // you get the zoomed and scaled version:
            //
            // screen_offset
            // = camera_zoom * (Half_Inverse_Camera_Zoom_XY * sSize.x) / sSize.x
            // = camera_zoom * Half_Inverse_Camera_Zoom_XY
            // = 0.25f       * 1.5f // in the example
            float screen_offset = 0.5f * (1f - camera_zoom);

            // room_camera.levelGraphic.x = textureOffset.x - cameraPosition.x;
            // same for y;
            // 
            // there seem to be rounding errors when zooming;
            // in some instances you see a black outline;
            // but not in others; depends on the camera position;
            //
            // if the 0.5f is missing then you get black outlines;
            // even without zoom;
            Vector4 sprite_rect = new Vector4(
                    screen_offset + (camera_zoom * room_camera.levelGraphic.x - 0.5f) / room_camera.sSize.x,
                    screen_offset + (camera_zoom * room_camera.levelGraphic.y + 0.5f) / room_camera.sSize.y,
                    screen_offset + (camera_zoom * (room_camera.levelGraphic.x + room_camera.levelGraphic.width) - 0.5f) / room_camera.sSize.x, screen_offset + (camera_zoom * (room_camera.levelGraphic.y + room_camera.levelGraphic.height) + 0.5f) / room_camera.sSize.y
                    );
            Shader.SetGlobalVector(RainWorld.ShadPropSpriteRect, sprite_rect);
        }
    }

    public static void DrawUpdate_UpdateSlowFollowCreaturePos(RoomCamera room_camera, Vector2 target_slow_follow_creature_pos) {
        if (room_camera.room is not Room room || room_camera.IsRoomBlacklisted(room.abstractRoom)) {
            room_camera.slowFollowCreaturePos = target_slow_follow_creature_pos;
            return;
        }
        // Don't do anything. In vanilla the position snaps when the
        // cam_pos_index changes. Without this a smooth lerp is used in all
        // cases. Otherwise, the ripple effect can jump visually.
    }

    public static Vector2 GetCreaturePosition(Creature creature) {
        if (creature is Player player) {
            // reduce movement when "rolling" in place in ZeroG;
            if (player.room?.gravity == 0.0f || player.animation == Player.AnimationIndex.Roll) {
                return 0.5f * (player.bodyChunks[0].pos + player.bodyChunks[1].pos);
            }

            // use the center (of mass(?)) instead;
            // makes rolls more predictable;
            // use lower y such that crouching does not move camera;
            return new() {
                x = 0.5f * (player.bodyChunks[0].pos.x + player.bodyChunks[1].pos.x),
                y = Mathf.Min(player.bodyChunks[0].pos.y, player.bodyChunks[1].pos.y)
            };
        }
        // otherwise when the overseer jumps back and forth the camera would move as well;
        // I consider this a bug;
        // the overseer should not jump around when focusing on a shortcut;
        // because the audio stops playing as well;
        else if (creature.abstractCreature.abstractAI is OverseerAbstractAI abstract_ai && abstract_ai.safariOwner && abstract_ai.doorSelectionIndex != -1) {
            return abstract_ai.parent.Room.realizedRoom.MiddleOfTile(abstract_ai.parent.Room.realizedRoom.ShortcutLeadingToNode(abstract_ai.doorSelectionIndex).startCoord);
        }
        return creature.mainBodyChunk.pos;
    }

    [Obsolete("Use RoomCameraMod_LoadOneScreenImage(RoomCamera room_camera, string room_name, [int cam_pos_index], [byte[] byte_array]) instead.")]
    public static void Load_Image(string room_name, int camera_number, int cam_pos_index, byte[]? byte_array) {
        if (Custom.rainWorld?.processManager?.currentMainLoop is not RainWorldGame game) {
            return;
        }
        if (camera_number < 0 || camera_number > 3) {
            camera_number = 0;
        }
        RoomCamera room_camera = game.cameras[camera_number];
        RoomCameraMod_LoadOneScreenImage(room_camera, room_name, cam_pos_index, byte_array);
    }

    public static void ResetCameraPosition(RoomCamera room_camera) {
        // vanilla copy & paste stuff
        if (room_camera.room is not Room room || room_camera.IsRoomBlacklisted(room.abstractRoom)) {
            room_camera.seekPos = room_camera.CamPos(room_camera.currentCameraPosition);
            room_camera.seekPos.x += room_camera.hDisplace + 8f;
            room_camera.seekPos.y += 18f;
            room_camera.leanPos *= 0.0f;

            room_camera.lastPos = room_camera.seekPos;
            room_camera.pos = room_camera.seekPos;
            Reset_Camera_Zoom(room_camera);
            return;
        }

        var room_camera_fields = room_camera.GetFields();
        room_camera_fields.type_camera.Reset();
        Apply_Camera_Zoom(room_camera);

        room_camera.slowFollowCreaturePos = (room_camera_fields.on_screen_position - room_camera.pos) / room_camera.sSize;
        Shader.SetGlobalVector("_FollowCreatureScreenPos", room_camera.slowFollowCreaturePos);
    }

    public static void Reset_Camera_Zoom(RoomCamera room_camera) {
        // The screens overlap if you mess with the zoom when SplitScreen Coop
        // is enabled.
        if (is_split_screen_coop_enabled) {
            return;
        }

        // I need to be careful. There might be mod conflicts that I am
        // unaware of. Only apply / reset zoom when needed.
        if (!Is_Dynamic_Zoom_Enabled && camera_zoom == 1f) {
            return;
        }

        // Reset to vanilla values.
        for (int sprite_layer_index = 0; sprite_layer_index < 11; ++sprite_layer_index) {
            FContainer sprite_layer = room_camera.SpriteLayers[sprite_layer_index];
            sprite_layer.scale = 1f;
            sprite_layer.SetPosition(Vector2.zero);
            sprite_layer.ScaleAroundPointRelative(Vector2.zero, 1f, 1f);
        }
    }

    // This functions needs to be public. Otherwise, the hook creation fails.
    public static Texture2D RoomCamera_LevelTexture(Func<RoomCamera,Texture2D> orig, RoomCamera room_camera) {
        if (Option_ReducedMemoryUsage) {
            return orig(room_camera);
        }
        if (Get_Level_Texture(room_camera.cameraNumber, room_camera.currentCameraPosition) is not Texture2D texture) {
            return orig(room_camera);
        }
        return texture;
    }

    public static void RoomCameraMod_LoadOneScreenImage(RoomCamera room_camera, string room_name, int? cam_pos_index = null, byte[]? byte_array = null) {
        if (cam_pos_index == null) {
            cam_pos_index = room_camera.currentCameraPosition;
        }
        if (byte_array == null) {
            byte_array = room_camera.preLoadedTexture;
        }

        if (Option_ReducedMemoryUsage) {
            room_camera.levelTexture.LoadImage(byte_array, markNonReadable: false);

        } else {
            if (byte_array == null) return;
            if (byte_array.Length == 0) return;
            int camera_number = room_camera.cameraNumber;
            Get_Level_Texture(camera_number, (int)cam_pos_index).LoadImage(byte_array, markNonReadable: false);
            Set_Level_Texture_Room_Name(room_name, camera_number, (int)cam_pos_index);

            // This is too slow. For past Unity versions, this might have helped
            // with memory leaks from calling LoadImage().
            // Resources.UnloadUnusedAssets();
        }
    }

    public static void RoomCameraMod_LoadOneScreenOrFullRoomTexture(RoomCamera room_camera) {
        RenderTexture render_texture = room_camera.Render_Texture();
        var room_camera_fields = room_camera.GetFields();

        Room? new_room = room_camera.loadingRoom;
        new_room ??= room_camera.room;
        if (new_room is not Room room) {
            throw new Exception("RoomCamera.room and RoomCamera.loadingRoom are null.");
        }

        //
        //

        var abstract_room_fields = room.abstractRoom.GetFields();
        string room_name = room.abstractRoom.FileName;

        //
        //

        // The variable loadingRoom will be null after calling orig().
        bool is_changing_room = room_camera.loadingRoom != null;

        // We use just-in-time merging. We need to always load the room -- even
        // one-screen and blacklisted rooms. Since the vanilla call to
        // LoadImage() is removed via an IL-hook.
        if (!is_changing_room || room_camera.IsRoomBlacklisted(room.abstractRoom) || room.cameraPositions.Length < 2) {
            //
            // Case 1: Load just one screen. The room is blacklisted or has only
            // one screen.
            //

            // This is usually done in orig(). But we removed it because it
            // messes up the cache. This loads the texture into
            // room_camera.levelTexture.
            RoomCameraMod_LoadOneScreenImage(room_camera, room_name);

            if (render_texture.width != 1400 || render_texture.height != 800) {
                render_texture.Release();
                render_texture.width = 1400;
                render_texture.height = 800;
            }
            Graphics.CopyTexture(room_camera.levelTexture, render_texture);

        } else if (is_changing_room) {
            bool hasSucceeded;

            // Case 2: The whole room gets pre-loaded at once.
            if (Option_ReducedMemoryUsage) {
                // Use Util.camera_texture since it gets marked as non readable.
                // Functions like GetPixels() won't work. Therefore, don't use
                // room_camera.levelTexture since other mods might try to read
                // pixels from it.
                hasSucceeded = Util_LoadRoomTextureIntoRenderTexture(room_name, render_texture, cache: Util.camera_texture);
            } else {
                hasSucceeded = Util_LoadRoomTextureIntoRenderTexture(room_camera, room_name);
            }

            if (!hasSucceeded)
            {
                blacklisted_rooms.Add(room_name);

                // case 1
                RoomCameraMod_LoadOneScreenImage(room_camera, room_name);
                if (render_texture.width != 1400 || render_texture.height != 800) {
                    render_texture.Release();
                    render_texture.width = 1400;
                    render_texture.height = 800;
                }
                Graphics.CopyTexture(room_camera.levelTexture, render_texture);
            }
        }
    }

    public static void Send_TextPrompt_Message(RoomCamera room_camera) {
        if (next_text_prompt_message == null) return;
        if (room_camera.hud is not HUD.HUD hud) return;
        if (room_camera.game is not RainWorldGame game) return;

        if (hud.textPrompt.currentlyShowing != HUD.TextPrompt.InfoID.Nothing) {
            next_text_prompt_message = null;
            return;
        }

        hud.textPrompt.AddMessage(game.rainWorld.inGameTranslator.Translate(next_text_prompt_message), wait: 0, time: 200, darken: false, hideHud: false);
        next_text_prompt_message = null;
    }

    // accounts for room boundaries and shortcuts
    public static void UpdateOnScreenPosition(RoomCamera room_camera) {
        if (room_camera.room == null) return;
        if (room_camera.followAbstractCreature == null) return;
        if (room_camera.followAbstractCreature.Room != room_camera.room.abstractRoom) return;
        if (room_camera.followAbstractCreature.realizedCreature is not Creature creature) return;
        if (!room_camera.Is_Camera_Scroll_Enabled()) return;

        Vector2 position = -0.5f * room_camera.sSize;
        if (creature.inShortcut && GetShortcutVessel(room_camera.game.shortcuts, room_camera.followAbstractCreature) is ShortcutHandler.ShortCutVessel shortcut_vessel) {
            Vector2 current_position = room_camera.room.MiddleOfTile(shortcut_vessel.pos);
            Vector2 next_in_shortcut_position = room_camera.room.MiddleOfTile(ShortcutHandler.NextShortcutPosition(shortcut_vessel.pos, shortcut_vessel.lastPos, room_camera.room));

            // shortcuts get only updated every 3 frames => calculate exact position here // in CoopTweaks it can also be 2 frames in order to remove slowdown, i.e. compensate for the mushroom effect
            position += Vector2.Lerp(current_position, next_in_shortcut_position, room_camera.game.updateShortCut / number_of_frames_per_shortcut_udpate);
        } else {
            // use the center (of mass(?)) instead // makes rolls more predictable // use lower y such that crouching does not move camera
            position += GetCreaturePosition(creature);
        }

        var room_camera_fields = room_camera.GetFields();
        room_camera_fields.last_on_screen_position = room_camera_fields.on_screen_position;
        room_camera_fields.on_screen_position = position;
    }

    //
    // private
    //

    private static void IL_RoomCamera_ApplyPositionChange(ILContext context) {
		// LogAllInstructions(context);
		ILCursor cursor = new(context);

        if (cursor.TryGotoNext(instruction => instruction.MatchLdfld("RoomCamera", "preLoadedTexture"))) {
            if (can_log_il_hooks) {
                Debug.Log($"{mod_id}: IL_RoomCamera_ApplyPositionChange: Index {cursor.Index}");
            }

            // Prevent vanilla from overriding the cached level textures. Remove
            // the vanilla call to LoadImage().
            cursor.Index -= 3;
            cursor.RemoveRange(7);

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Action<RoomCamera>>(RoomCameraMod_LoadOneScreenOrFullRoomTexture);

        } else {
            if (can_log_il_hooks) {
                Debug.Log($"{mod_id}: IL_RoomCamera_ApplyPositionChange failed.");
            }
            return;
        }

		// LogAllInstructions(context);
	}

    private static void IL_RoomCamera_DrawUpdate(ILContext context) {
        // LogAllInstructions(context);
        ILCursor cursor = new(context);

        if (cursor.TryGotoNext(instruction => instruction.MatchCall<RoomCamera>("CamPos"))) {
            if (can_log_il_hooks) {
                Debug.Log($"{mod_id}: IL_RoomCamera_DrawUpdate: Index {cursor.Index}"); // 100 
            }

            // Vanilla only uses CamPos(currentCameraPosition). Remove it and
            // call a function that handles both -- vanilla and modded -- cases.
            // This needs to be done 4 times in total. This is the first time.
            cursor.Goto(cursor.Index - 2);
            cursor.RemoveRange(3);
            cursor.Emit(OpCodes.Ldloc_1);
            cursor.EmitDelegate<Func<RoomCamera, Vector2, Vector2>>(DrawUpdate_GetCameraPosition);

        } else {
            if (can_log_il_hooks) {
                Debug.Log($"{mod_id}: IL_RoomCamera_DrawUpdate failed.");
            }
            return;
        }

        if (cursor.TryGotoNext(instruction => instruction.MatchCall<RoomCamera>("CamPos"))) {
            if (can_log_il_hooks) {
                Debug.Log($"{mod_id}: IL_RoomCamera_DrawUpdate: Index {cursor.Index}"); // 112 
            }

            // Second.
            cursor.Goto(cursor.Index - 2);
            cursor.RemoveRange(3);
            cursor.Emit(OpCodes.Ldloc_1);
            cursor.EmitDelegate<Func<RoomCamera, Vector2, Vector2>>(DrawUpdate_GetCameraPosition);

        } else {
            if (can_log_il_hooks) {
                Debug.Log($"{mod_id}: IL_RoomCamera_DrawUpdate failed.");
            }
            return;
        }

        if (cursor.TryGotoNext(instruction => instruction.MatchCall<RoomCamera>("CamPos"))) {
            if (can_log_il_hooks) {
                Debug.Log($"{mod_id}: IL_RoomCamera_DrawUpdate: Index {cursor.Index}"); // 129 
            }

            // Third.
            cursor.Goto(cursor.Index - 2);
            cursor.RemoveRange(3);
            cursor.Emit(OpCodes.Ldloc_1);
            cursor.EmitDelegate<Func<RoomCamera, Vector2, Vector2>>(DrawUpdate_GetCameraPosition);

        } else {
            if (can_log_il_hooks) {
                Debug.Log($"{mod_id}: IL_RoomCamera_DrawUpdate failed.");
            }
            return;
        }

        if (cursor.TryGotoNext(instruction => instruction.MatchCall<RoomCamera>("CamPos"))) {
            if (can_log_il_hooks) {
                Debug.Log($"{mod_id}: IL_RoomCamera_DrawUpdate: Index {cursor.Index}"); // 145 
            }

            // Fourth.
            cursor.Goto(cursor.Index - 2);
            cursor.RemoveRange(3);
            cursor.Emit(OpCodes.Ldloc_1);
            cursor.EmitDelegate<Func<RoomCamera, Vector2, Vector2>>(DrawUpdate_GetCameraPosition);

        } else {
            if (can_log_il_hooks) {
                Debug.Log($"{mod_id}: IL_RoomCamera_DrawUpdate failed.");
            }
            return;
        }

        //
        //

        if (cursor.TryGotoNext(instruction => instruction.MatchCall<RoomCamera>("CamPos"))) {
            if (can_log_il_hooks) {
                Debug.Log($"{mod_id}: IL_RoomCamera_DrawUpdate: Index {cursor.Index}"); // 321
            }

            //
            // The LevelTexture is bundled to the GameObject(?) levelGraphic.
            // You can use levelGraphic to squeeze or stretch the texture. We
            // want it to have original size.
            //

            // Leave the RoomCamera argument unchanged.
            cursor.Goto(cursor.Index - 4);
            cursor.RemoveRange(43); // 317-359

            cursor.Emit(OpCodes.Ldloc_1); // camera_position
            cursor.EmitDelegate<Action<RoomCamera, Vector2>>(DrawUpdate_UpdateLevelTextureGameObject);
        } else {
            if (can_log_il_hooks) {
                Debug.Log($"{mod_id}: IL_RoomCamera_DrawUpdate failed.");
            }
            return;
        }

        if (cursor.TryGotoNext(instruction => instruction.MatchCallvirt("Creature", "get_mainBodyChunk"))) {
            if (can_log_il_hooks) {
                Debug.Log($"{mod_id}: IL_RoomCamera_DrawUpdate: Index {cursor.Index}");
            }

            // Used when updating the slowFollowCreaturePosition in the function
            // DrawUpdate. But the body chunk position does not update when in
            // shortcuts. Use on_screen_position instead.

            cursor.Index -= 2;
            cursor.RemoveRange(4);
            cursor.EmitDelegate<Func<RoomCamera, Vector2>>(DrawUpdate_GetMainBodyChunkOrOnScreenPosition);

        } else {
            if (can_log_il_hooks) {
                Debug.Log($"{mod_id}: IL_RoomCamera_DrawUpdate failed.");
            }
            return;
        }

        if (cursor.TryGotoNext(instruction => instruction.MatchStfld("RoomCamera", "slowFollowCreaturePos"))) {
            if (can_log_il_hooks) {
                Debug.Log($"{mod_id}: IL_RoomCamera_DrawUpdate: Index {cursor.Index}");
            }

            // Reuse the arguments from the removed instruction.
            cursor.RemoveRange(1);
            cursor.EmitDelegate<Action<RoomCamera, Vector2>>(DrawUpdate_UpdateSlowFollowCreaturePos);

        } else {
            if (can_log_il_hooks) {
                Debug.Log($"{mod_id}: IL_RoomCamera_DrawUpdate failed.");
            }
            return;
        }

        if (cursor.TryGotoNext(instruction => instruction.MatchLdsfld("RainWorld", "ShadPropSpriteRect"))) {
            if (can_log_il_hooks) {
                Debug.Log($"{mod_id}: IL_RoomCamera_DrawUpdate: Index {cursor.Index}");
            }

            //
            // Delete and replace the whole Shader.SetGlobalVector() call that
            // sets RainWorld.ShadPropSpriteRect.
            //

            // Keep the label to the first instruction intact.
            cursor.Index += 1;
            cursor.Prev.OpCode = OpCodes.Ldarg_0; // room_camera
            cursor.Prev.Operand = null;

            cursor.RemoveRange(70);

            cursor.Emit(OpCodes.Ldloc_1); // camera_position
            cursor.EmitDelegate<Action<RoomCamera, Vector2>>(DrawUpdate_UpdateShadPropSpriteRect);

        } else {
            if (can_log_il_hooks) {
                Debug.Log($"{mod_id}: IL_RoomCamera_DrawUpdate failed.");
            }
            return;
        }
        // LogAllInstructions(context);
    }

    private static void IL_RoomCamera_Update(ILContext context) {
        // LogAllInstructions(context);
        ILCursor cursor = new(context);

        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate<Action<RoomCamera>>(room_camera => {
            Send_TextPrompt_Message(room_camera);
        });

        // maybe it is just me or is stuff noticeably slower when using On-Hooks + GPU stuff?
        // IL_RoomCamera_DrawUpdate() seems to do a lot..
        // maybe it is better to do Update as an IL-Hook as well;

        if (cursor.TryGotoNext(instruction => instruction.MatchCall<RoomCamera>("UpdateDayNightPalette"))) {
            if (can_log_il_hooks) {
                Debug.Log($"{mod_id}: IL_RoomCamera_Update: Index {cursor.Index}"); // 400
            }

            // put before UpdateDayNightPalette()
            cursor.EmitDelegate<Action<RoomCamera>>(room_camera => {
                // in four player split screen you can zoom the camera out by double tapping the
                // map-button; to better transition when doing so I want the scroll to be enabled
                // in both cases => simply check Is_Split; otherwise it teleports to the target 
                // location immediately;
                room_camera.GetFields().is_camera_scroll_forced_by_split_screen = is_split_screen_coop_enabled && Is_Split;
                if (room_camera.room is not Room room || room_camera.IsRoomBlacklisted(room.abstractRoom)) return;
                AddFadeTransition(room_camera);
            });
            cursor.Emit(OpCodes.Ldarg_0);
        } else {
            if (can_log_il_hooks) {
                Debug.Log($"{mod_id}: IL_RoomCamera_Update failed.");
            }
            return;
        }

        if (cursor.TryGotoNext(instruction => instruction.MatchLdfld("HUD.HUD", "owner"))) {
            cursor.Goto(cursor.Index + 2);

            if (can_log_il_hooks) {
                Debug.Log($"{mod_id}: IL_RoomCamera_Update: Index {cursor.Index}.");
            }

            // For just-in-time merging. The hud owner can be null when the room
            // loads too slowly. Add missing null check.
            cursor.Next.OpCode = OpCodes.Brtrue;
            cursor.EmitDelegate<Func<HUD.IOwnAHUD?, Player, bool>>((hud_owner, player) => {
                return hud_owner == null || hud_owner == player;
            });
        } else {
            if (can_log_il_hooks) {
                Debug.Log($"{mod_id}: IL_RoomCamera_Update failed.");
            }
            return;
        }

        // Update the modded camera after the position updates and before the
        // screen shake effect. Previously, I did it after and the screen shake
        // did nothing.
        if (cursor.TryGotoNext(instruction => instruction.MatchCall<RoomCamera>("get_screenShake"))) {
            if (can_log_il_hooks) {
                Debug.Log($"{mod_id}: IL_RoomCamera_Update: Index {cursor.Index}"); // before: 916 // after: 920
            }

            cursor.EmitDelegate<Action<RoomCamera>>(room_camera => {
                if (room_camera.room is not Room room || room_camera.IsRoomBlacklisted(room.abstractRoom)) return;
                room_camera.GetFields().type_camera.Update();
            });
            cursor.Emit(OpCodes.Ldarg_0);
        } else {
            if (can_log_il_hooks) {
                Debug.Log($"{mod_id}: IL_RoomCamera_Update failed.");
            }
            return;
        }
        // LogAllInstructions(context);
    }

    //
    //
    //

    private static Vector2 RoomCamera_ApplyDepth(On.RoomCamera.orig_ApplyDepth orig, RoomCamera room_camera, Vector2 position, float depth) {
        if (room_camera.room is not Room room || room_camera.IsRoomBlacklisted(room.abstractRoom)) return orig(room_camera, position, depth);
        int cam_pos_index = CameraViewingPoint(room, position);
        if (cam_pos_index == -1) return orig(room_camera, position, depth);

        // before this I would change the depth based on the camera position; but it is 
        // static and only needs to match the pre-rendered visuals of the room;
        return Custom.ApplyDepthOnVector(position, room_camera.CamPos(cam_pos_index) + new Vector2(700f, 1600f / 3f), depth);
    }

    private static void RoomCamera_ApplyPalette(On.RoomCamera.orig_ApplyPalette orig, RoomCamera room_camera) {
        orig(room_camera);

        if (room_camera.fullScreenEffect == null) return;
        if (Option_FullScreenEffects) return;
        room_camera.fullScreenEffect.RemoveFromContainer();
        room_camera.fullScreenEffect = null;
    }

    private static void RoomCamera_ApplyPositionChange(On.RoomCamera.orig_ApplyPositionChange orig, RoomCamera room_camera) {
        var room_camera_fields = room_camera.GetFields();

        // There is a bug when using SplitScreen Co-op where you would get a
        // black screen. For example, when spawning in a shelter. Setting the
        // active render texture seems to fix it.
        RenderTexture render_texture = room_camera.Render_Texture();
        RenderTexture.active = render_texture;

        //
        //

        if (Is_Dynamic_Zoom_Enabled && room_camera.loadingRoom != null) {
            var loading_room_fields = room_camera.loadingRoom.abstractRoom.GetFields();
            float dynamic_zoom_x = room_camera.sSize.x / loading_room_fields.total_width;
            if (dynamic_zoom_x < 1f)
                dynamic_zoom_x = 1f;
            float dynamic_zoom_y = room_camera.sSize.y / loading_room_fields.total_height;
            if (dynamic_zoom_y < 1f)
                dynamic_zoom_y = 1f;
            camera_zoom = Mathf.Max(dynamic_zoom_x, dynamic_zoom_y);
        }

        //
        //

        Util_ClearCachedPixelColors();

        // The variable loadingRoom will be null after calling orig().
        bool is_changing_room = room_camera.loadingRoom != null;
        orig(room_camera);

        if (room_camera.room is not Room room) {
            throw new Exception("RoomCamera.room is null.");
        }

        //
        //

        var abstract_room_fields = room.abstractRoom.GetFields();

        // If I blacklist too early then the camera might jump in the current
        // room. Do it after calling orig() / ChangeRoom().
        if (is_changing_room && room_camera.IsRoomBlacklisted(room.abstractRoom)) {
            string room_name = room.abstractRoom.FileName;
            Debug.Log($"{mod_id}: The room {room_name} is blacklisted.");
        }

        //
        //

        // SetGlobalTexture needs to happen here. Otherwise, this can mess up
        // shaders if it is set only before the resizing (and merging?)
        // happened.
        Shader.SetGlobalTexture(RainWorld.ShadPropLevelTex, render_texture);

        room_camera.levelGraphic.width       = render_texture.width;
        room_camera.levelGraphic.height      = render_texture.height;
        room_camera.backgroundGraphic.width  = room_camera.backgroundTexture.width;
        room_camera.backgroundGraphic.height = room_camera.backgroundTexture.height;

        // The combiner is used mainly in the Watcher campaign. It overlaps
        // textures for custom effects. The combined texture replaces the level
        // texture in the shaders while active.
        //
        // The visuals are pixelated without resizing.
        LevelTexCombiner combiner = room_camera.levelTexCombiner;
        if (combiner.combinedLevelTex != null) {
            combiner.combinedLevelTex.Release();
            combiner.combinedLevelTex.width  = render_texture.width;
            combiner.combinedLevelTex.height = render_texture.height;
        }
        if (combiner.intermediateTex != null) {
            combiner.intermediateTex.Release();
            combiner.intermediateTex.width  = render_texture.width;
            combiner.intermediateTex.height = render_texture.height;
        }

        if (is_changing_room) {
            // Graphics.Blit() creates this texture again; but only when UpdateSnowLight() is called;
            // this is forced for example when the room is changed in orig(); therefore, only do
            // this when this happens;

            RenderTexture snow_texture = room_camera.SnowTexture;
            snow_texture.Release();
            snow_texture.width  = render_texture.width;
            snow_texture.height = render_texture.height;
        }

        // This is needed for some shader. The camera textures contain additional
        // color information (palette pixels). The offsets are used to find them 
        // again inside the shader.
        if (room.cameraPositions.Length > 30 || render_texture.width <= 1400 && render_texture.height <= 800) {
            Shader.SetGlobalInt(TextureOffsetArrayLength, 0);
            Shader.SetGlobalVectorArray(TextureOffsetArray, new Vector4[30]);
        } else {
            Vector2 min_camera_position = abstract_room_fields.min_camera_position;
            Vector4[] texture_offset_array = new Vector4[30];

            for (int cam_pos_index = 0; cam_pos_index < room.cameraPositions.Length; ++cam_pos_index) {
                texture_offset_array[cam_pos_index] = (Vector4)(room.cameraPositions[cam_pos_index] - min_camera_position);
            }

            Shader.SetGlobalInt(TextureOffsetArrayLength, room.cameraPositions.Length);
            Shader.SetGlobalVectorArray(TextureOffsetArray, texture_offset_array);
        }

        // uses currentCameraPosition;
        ResetCameraPosition(room_camera);
    }

    private static void RoomCamera_ChangeCameraToPlayer(On.RoomCamera.orig_ChangeCameraToPlayer orig, RoomCamera room_camera, AbstractCreature camera_target) {
        // The room can be null when it loads too slowly. Add missing null check.
        if (room_camera.room == null) return;
        orig(room_camera, camera_target);
    }

    private static void RoomCamera_Ctor(On.RoomCamera.orig_ctor orig, RoomCamera room_camera, RainWorldGame game, int camera_number) {
        orig(room_camera, game, camera_number);
        if (_all_room_camera_fields.ContainsKey(room_camera)) return;
        _all_room_camera_fields.Add(room_camera, new(room_camera));
    }

    private static float RoomCamera_DepthAtCoordinate(On.RoomCamera.orig_DepthAtCoordinate orig, RoomCamera room_camera, Vector2 position) {
        // similar to RoomCamera_PixelColorAtCoordinate();
        float depth = orig(room_camera, position);
        if (room_camera.room is not Room room || room_camera.IsRoomBlacklisted(room.abstractRoom)) {
            return depth;
        }

        if (!Option_ReducedMemoryUsage) {
            int cam_pos_index = CameraViewingPoint(room, position);
            if (cam_pos_index != -1) {
                int current_camera_index = room_camera.currentCameraPosition;
                room_camera.currentCameraPosition = cam_pos_index;
                depth = orig(room_camera, position);
                room_camera.currentCameraPosition = current_camera_index;
            }
            return depth;

        } else {
            var abstract_room_fields = room.abstractRoom.GetFields();
            position = position - abstract_room_fields.min_camera_position;
            Color pixel_color = Util_ReadPixelColorFromCacheOrGPU(room_camera, position);

            if (pixel_color.r == 1f && pixel_color.g == 1f && pixel_color.b == 1f)
            {
                return 1f;
            }

            int red = Mathf.FloorToInt(pixel_color.r * 255f);
            if (red > 90)
            {
                red -= 90;
            }
            return (float)((red - 1) % 30) / 30f;
        }
    }

    private static bool RoomCamera_IsViewedByCameraPosition(On.RoomCamera.orig_IsViewedByCameraPosition orig, RoomCamera room_camera, int cam_pos_index, Vector2 test_position) {
        if (room_camera.room is not Room room || room_camera.IsRoomBlacklisted(room.abstractRoom)) {
            return orig(room_camera, cam_pos_index, test_position);
        }

        // snow can be fall down into screens that should be outside of the visibility range;
        // changing this back to vanilla + room_camera.pos didn't help with the snow;
        // but for consistency it might be better to leave it as is;
        return test_position.x > room_camera.pos.x - 188f && test_position.x < room_camera.pos.x + 188f + 1024f && test_position.y > room_camera.pos.y - 18f && test_position.y < room_camera.pos.y + 18f + 768f;
        // buffer: 200f
        // return test_position.x > room_camera.pos.x - 200f - 188f && test_position.x < room_camera.pos.x + 200f + 188f + 1024f && test_position.y > room_camera.pos.y - 200f - 18f && test_position.y < room_camera.pos.y + 200f + 18f + 768f;
        // return test_position.x > room_camera.pos.x - 380f && test_position.x < room_camera.pos.x + 380f + 1400f && test_position.y > room_camera.pos.y - 20f && test_position.y < room_camera.pos.y + 20f + 800f;
    }

    // looking at the source code this seems to be only used with currentCameraPosition at this point;
    // => treat is like RoomCamera_PositionCurrentlyVisible();
    private static bool RoomCamera_IsVisibleAtCameraPosition(On.RoomCamera.orig_IsVisibleAtCameraPosition orig, RoomCamera room_camera, int cam_pos_index, Vector2 test_position) {
        if (room_camera.room is not Room room || room_camera.IsRoomBlacklisted(room.abstractRoom)) {
            return orig(room_camera, cam_pos_index, test_position);
        }

        Vector2 screen_size_increase = Is_Camera_Zoom_Enabled ? Half_Inverse_Camera_Zoom_XY * room_camera.sSize : Vector2.zero;
        return test_position.x > room_camera.pos.x - 188f - screen_size_increase.x && test_position.x < room_camera.pos.x + 188f + room_camera.game.rainWorld.options.ScreenSize.x + screen_size_increase.x && test_position.y > room_camera.pos.y - 18f - screen_size_increase.y && test_position.y < room_camera.pos.y + 18f + 768f + screen_size_increase.y;
        // return test_position.x > room_camera.pos.x - 200f - 188f && test_position.x < room_camera.pos.x + 200f + 188f + room_camera.game.rainWorld.options.ScreenSize.x && test_position.y > room_camera.pos.y - 200f - 18f && test_position.y < room_camera.pos.y + 200f + 18f + 768f;
        // return test_position.x > room_camera.pos.x - 380f && test_position.x < room_camera.pos.x + 380f + 1400f && test_position.y > room_camera.pos.y - 20f && test_position.y < room_camera.pos.y + 20f + 800f;
    }

    private static bool? RoomCamera_LitAtCoordinate(On.RoomCamera.orig_LitAtCoordinate orig, RoomCamera room_camera, Vector2 position) {
        bool? is_lit = orig(room_camera, position);
        if (room_camera.room is not Room room || room_camera.IsRoomBlacklisted(room.abstractRoom)) {
            return is_lit;
        }

        if (!Option_ReducedMemoryUsage) {
            int cam_pos_index = CameraViewingPoint(room, position);
            if (cam_pos_index != -1) {
                int current_camera_index = room_camera.currentCameraPosition;
                room_camera.currentCameraPosition = cam_pos_index;
                is_lit = orig(room_camera, position);
                room_camera.currentCameraPosition = current_camera_index;
            }
            return is_lit;

        } else {
            var abstract_room_fields = room.abstractRoom.GetFields();
            position = position - abstract_room_fields.min_camera_position;
            Color pixel_color = Util_ReadPixelColorFromCacheOrGPU(room_camera, position);

            if (pixel_color.r == 1f && pixel_color.g == 1f && pixel_color.b == 1f)
            {
                return null;
            }
            return Mathf.FloorToInt(pixel_color.r * 255f) > 90;
        }
    }

    private static void RoomCamera_MoveCamera(On.RoomCamera.orig_MoveCamera_int orig, RoomCamera room_camera, int cam_pos_index) {
        // only called when moving camera positions inside the same room 
        // if the ID changed then do a smooth transition instead 
        // the logic for that is done in UpdateCameraPosition()

        if (room_camera.room is not Room room || room_camera.IsRoomBlacklisted(room.abstractRoom)) {
            orig(room_camera, cam_pos_index);
            return;
        }

        room_camera.currentCameraPosition = cam_pos_index;
        if (room_camera.followAbstractCreature != null && room_camera.GetFields().type_camera is VanillaTypeCamera vanilla_type_camera && vanilla_type_camera.are_vanilla_positions_used && vanilla_type_camera.follow_abstract_creature_id == room_camera.followAbstractCreature.ID) {
            // Otherwise, the camera moves after a vanilla transition. But
            // ignore is during a smooth transition, i.e. when follow_abstract_creature_id
            // is set to null (kinda ugly to not have a separate variable for that).
            ResetCameraPosition(room_camera);
        }
    }

    private static Color RoomCamera_PixelColorAtCoordinate(On.RoomCamera.orig_PixelColorAtCoordinate orig, RoomCamera room_camera, Vector2 position) {
        Color pixel_color = orig(room_camera, position);
        if (room_camera.room is not Room room || room_camera.IsRoomBlacklisted(room.abstractRoom)) 
        {
            return pixel_color;
        }

        if (!Option_ReducedMemoryUsage) {
            int cam_pos_index = CameraViewingPoint(room, position);
            if (cam_pos_index != -1) {
                int current_camera_index = room_camera.currentCameraPosition;
                room_camera.currentCameraPosition = cam_pos_index;
                pixel_color = orig(room_camera, position);
                room_camera.currentCameraPosition = current_camera_index;
            }
            return pixel_color;

        } else {
            var abstract_room_fields = room.abstractRoom.GetFields();
            position = position - abstract_room_fields.min_camera_position;
            pixel_color = Util_ReadPixelColorFromCacheOrGPU(room_camera, position);

            if (pixel_color.r == 1f && pixel_color.g == 1f && pixel_color.b == 1f)
            {
                return room_camera.paletteTexture.GetPixel(0, 7);
            }

            int red = Mathf.FloorToInt(pixel_color.r * 255f);
            float t = 0f;
            if (red > 90)
            {
                red -= 90;
            }
            else
            {
                t = 1f;
            }

            int div = Mathf.FloorToInt((float)red / 30f);
            int rem = (red - 1) % 30;
            return Color.Lerp(Color.Lerp(room_camera.paletteTexture.GetPixel(rem, div + 3), room_camera.paletteTexture.GetPixel(rem, div), t), room_camera.paletteTexture.GetPixel(1, 7), (float)rem * (1f - room_camera.paletteTexture.GetPixel(9, 7).r) / 30f);
        }
    }

    // use room_camera.pos as reference instead of camPos(..) // seems to be important for unloading graphics and maybe other things
    private static bool RoomCamera_PositionCurrentlyVisible(On.RoomCamera.orig_PositionCurrentlyVisible orig, RoomCamera room_camera, Vector2 test_position, float margin, bool widescreen) {
        if (room_camera.room is not Room room || room_camera.IsRoomBlacklisted(room.abstractRoom)) {
            return orig(room_camera, test_position, margin, widescreen);
        }

        Vector2 screen_size_increase = Is_Camera_Zoom_Enabled ? Half_Inverse_Camera_Zoom_XY * room_camera.sSize : Vector2.zero;
        return test_position.x > room_camera.pos.x - 188f - margin - (widescreen ? 190f : 0f) - screen_size_increase.x && test_position.x < room_camera.pos.x + 188f + (ModManager.MMF ? room_camera.game.rainWorld.options.ScreenSize.x : 1024f) + margin + (widescreen ? 190f : 0f) + screen_size_increase.x && test_position.y > room_camera.pos.y - 18f - margin - screen_size_increase.y && test_position.y < room_camera.pos.y + 18f + 768f + margin + screen_size_increase.y;
        // return test_position.x > room_camera.pos.x - 200f - 188f - margin - (widescreen ? 190f : 0f) && test_position.x < room_camera.pos.x + 200f + 188f + (ModManager.MMF ? room_camera.game.rainWorld.options.ScreenSize.x : 1024f) + margin + (widescreen ? 190f : 0f) && test_position.y > room_camera.pos.y - 200f - 18f - margin && test_position.y < room_camera.pos.y + 200f + 18f + 768f + margin;
        // return test_position.x > room_camera.pos.x - 380f - margin && test_position.x < room_camera.pos.x + 380f + 1400f + margin && test_position.y > room_camera.pos.y - 20f - margin && test_position.y < room_camera.pos.y + 20f + 800f + margin;
    }

    private static bool RoomCamera_PositionVisibleInNextScreen(On.RoomCamera.orig_PositionVisibleInNextScreen orig, RoomCamera room_camera, Vector2 test_position, float margin, bool widescreen) {
        if (room_camera.room is not Room room || room_camera.IsRoomBlacklisted(room.abstractRoom)) {
            return orig(room_camera, test_position, margin, widescreen);
        }

        Vector2 screen_size_increase = Is_Camera_Zoom_Enabled ? Half_Inverse_Camera_Zoom_XY * room_camera.sSize : Vector2.zero;
        float screen_size_x = ModManager.MMF ? room_camera.game.rainWorld.options.ScreenSize.x : 1024f;
        return test_position.x > room_camera.pos.x - screen_size_x - 188f - margin - (widescreen ? 190f : 0f) - screen_size_increase.x && test_position.x < room_camera.pos.x + 2f * screen_size_x + 188f + margin + (widescreen ? 190f : 0f) + screen_size_increase.x && test_position.y > room_camera.pos.y - 768f - 18f - margin - screen_size_increase.y && test_position.y < room_camera.pos.y + 2f * 768f + 18f + margin + screen_size_increase.y;
        // return test_position.x > room_camera.pos.x - (ModManager.MMF ? room_camera.game.rainWorld.options.ScreenSize.x : 1024f) - 200f - 188f - margin - (widescreen ? 190f : 0f) && test_position.x < room_camera.pos.x + 2f * (ModManager.MMF ? room_camera.game.rainWorld.options.ScreenSize.x : 1024f) + 200f + 188f + margin + (widescreen ? 190f : 0f) && test_position.y > room_camera.pos.y - 768f - 200f - 18f - margin && test_position.y < room_camera.pos.y + 2f * 768f + 200f + 18f + margin;
        // return test_position.x > room_camera.pos.x - 380f - 1400f - margin && test_position.x < room_camera.pos.x + 380f + 2800f + margin && test_position.y > room_camera.pos.y - 20f - 800f - margin && test_position.y < room_camera.pos.y + 20f + 1600f + margin;
    }

    private static void RoomCamera_PreLoadTexture(On.RoomCamera.orig_PreLoadTexture orig, RoomCamera room_camera, Room room, int cam_pos_index) {
        // PreLoadTexture() is only called when changing camera positions inside
        // the same room.
        if (room_camera.IsRoomBlacklisted(room.abstractRoom)) {
            orig(room_camera, room, cam_pos_index);
        }
    }

    private static bool RoomCamera_RectCurrentlyVisible(On.RoomCamera.orig_RectCurrentlyVisible orig, RoomCamera room_camera, Rect test_rectangle, float margin, bool widescreen) {
        if (room_camera.room is not Room room || room_camera.IsRoomBlacklisted(room.abstractRoom)) {
            return orig(room_camera, test_rectangle, margin, widescreen);
        }

        Vector2 screen_size_increase = Is_Camera_Zoom_Enabled ? Half_Inverse_Camera_Zoom_XY * room_camera.sSize : Vector2.zero;
        Rect other_rectangle = default;

        other_rectangle.xMin = room_camera.pos.x - 188f - margin - (widescreen ? 190f : 0f) - screen_size_increase.x;
        other_rectangle.xMax = room_camera.pos.x + 188f + (ModManager.MMF ? room_camera.game.rainWorld.options.ScreenSize.x : 1024f) + margin + (widescreen ? 190f : 0f) + screen_size_increase.x;
        other_rectangle.yMin = room_camera.pos.y - 18f - margin - screen_size_increase.y;
        other_rectangle.yMax = room_camera.pos.y + 18f + 768f + margin + screen_size_increase.y;

        // other_rectangle.xMin = room_camera.pos.x - 200f - 188f - margin - (widescreen ? 190f : 0f);
        // other_rectangle.xMax = room_camera.pos.x + 200f + 188f + (ModManager.MMF ? room_camera.game.rainWorld.options.ScreenSize.x : 1024f) + margin + (widescreen ? 190f : 0f);
        // other_rectangle.yMin = room_camera.pos.y - 200f - 18f - margin;
        // other_rectangle.yMax = room_camera.pos.y + 200f + 18f + 768f + margin;

        // other_rectangle.xMin = room_camera.pos.x - 380f - margin;
        // other_rectangle.xMax = room_camera.pos.x + 380f + 1400f + margin;
        // other_rectangle.yMin = room_camera.pos.y - 20f - margin;
        // other_rectangle.yMax = room_camera.pos.y + 20f + 800f + margin;

        return test_rectangle.CheckIntersect(other_rectangle);
    }

    private static void RoomCamera_ScreenMovement(On.RoomCamera.orig_ScreenMovement orig, RoomCamera room_camera, Vector2? source_position, Vector2 bump, float shake) {
        // should remove effects on camera like camera shakes caused by other creatures // feels weird otherwise
        if (room_camera.room is not Room room || room_camera.IsRoomBlacklisted(room.abstractRoom)) {
            orig(room_camera, source_position, bump, shake);
        }
    }

    private static void RoomCamera_UpdateSnowLight(On.RoomCamera.orig_UpdateSnowLight orig, RoomCamera room_camera) {
        orig(room_camera);
        if (room_camera.Render_Texture() is not RenderTexture render_texture) return;
        Graphics.Blit(render_texture, room_camera.SnowTexture, new Material(room_camera.game.rainWorld.Shaders["LevelSnowShader"].shader));
    }

    //
    //
    //

    [Obsolete]
    public sealed class Attached_Fields {
        public bool is_camera_scroll_forced_by_split_screen = false;

        public Vector2 last_on_screen_position = new();
        public Vector2 on_screen_position = new();

        public IAmATypeCamera? type_camera;

        public Attached_Fields(RoomCamera room_camera) {
            // if (camera_type == CameraType.Position) {
            //     type_camera = new PositionTypeCamera(room_camera, this);
            //     return;
            // }

            // if (camera_type == CameraType.Vanilla) {
            //     type_camera = new VanillaTypeCamera(room_camera, this);
            //     return;
            // }
            // type_camera = new SwitchTypeCamera(room_camera, this);
        }
    }

    public sealed class RoomCameraFields {
        public bool is_camera_scroll_forced_by_split_screen = false;

        public Vector2 last_on_screen_position = new();
        public Vector2 on_screen_position = new();

        public IAmATypeCamera type_camera;

        public RoomCameraFields(RoomCamera room_camera) {
            if (camera_type == CameraType.Position) {
                type_camera = new PositionTypeCamera(room_camera, this);
                return;
            }

            if (camera_type == CameraType.Vanilla) {
                type_camera = new VanillaTypeCamera(room_camera, this);
                return;
            }
            type_camera = new SwitchTypeCamera(room_camera, this);
        }
    }

    public enum CameraType {
        Position,
        Vanilla,
        Switch
    }
}
