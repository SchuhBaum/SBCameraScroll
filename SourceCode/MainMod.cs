
// allows access to private members;
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace SBCameraScroll;

[BepInPlugin("SBCameraScroll", "SBCameraScroll", "3.2.8")]
public class MainMod : BaseUnityPlugin {
    //
    // meta data
    //

    public static readonly string mod_id = "SBCameraScroll";
    public static readonly string author = "SchuhBaum";
    public static readonly string version = "3.2.8";
    public static readonly string mod_directory_path = Directory.GetParent(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)).FullName + Path.DirectorySeparatorChar;

    //
    // options
    //

    public static bool Option_DynamicZoom          => dynamic_zoom.Value;
    public static bool Option_FullScreenEffects    => full_screen_effects.Value;
    public static bool Option_ScrollOneScreenRooms => scroll_one_screen_rooms.Value;
    public static bool Option_ReducedMemoryUsage   => reduced_memory_usage.Value;
    public static bool Option_RippleTrailEffect    => ripple_trail_effect.Value;
    public static bool Option_CameraOffset         => cameraoffset_position.Value;

    //
    // other mods
    //

    // public static bool is_custom_region_support_enabled = false;
    public static bool is_improved_input_enabled = false;
    public static bool is_split_screen_coop_enabled = false;

    //
    // constants
    //

	public static readonly int TextureOffsetArray = Shader.PropertyToID("_textureOffsetArray");
	public static readonly int TextureOffsetArrayLength = Shader.PropertyToID("_textureOffsetArrayLength");

    //
    // variables
    //

    public static bool can_log_il_hooks = false;
    public static bool is_on_mods_init_initialized = false;
    public static bool is_post_mod_init_initialized = false;

    // 
    // main
    // 

    public MainMod() { }

    public void OnEnable() {
        On.RainWorld.OnModsInit   += RainWorld_OnModsInit;
        On.RainWorld.PostModsInit += RainWorld_PostModsInit;
    }

    //
    // public
    //

    public static void Initialize_Custom_Input() {
        // wrap it in order to make it a soft dependency only;
        Debug.Log($"{mod_id}: Initialize custom input.");
        RWInputMod.Initialize_Custom_Keybindings();
        PlayerMod.OnEnable();
    }

    // For debugging.
    public static void LogAllInstructions(ILContext? context, int index_string_length = 9, int op_code_string_length = 14) {
        if (context == null) return;

        Debug.Log("-----------------------------------------------------------------");
        Debug.Log("Log all IL-instructions.");
        Debug.Log($"Index:{new string(' ', index_string_length - 6)}OpCode:{new string(' ', op_code_string_length - 7)}Operand:");

        ILCursor cursor = new(context);
        ILCursor label_cursor = cursor.Clone();

        string cursor_index_string;
        string op_code_string;
        string operand_string;

        while (true) {
            // this might return too early;
            // if (cursor.Next.MatchRet()) break;

            // should always break at some point;
            // only TryGotoNext() doesn't seem to be enough;
            // it still throws an exception;
            try {
                if (cursor.TryGotoNext(MoveType.Before)) {
                    cursor_index_string = cursor.Index.ToString();
                    cursor_index_string = cursor_index_string.Length < index_string_length ? cursor_index_string + new string(' ', index_string_length - cursor_index_string.Length) : cursor_index_string;
                    op_code_string = cursor.Next.OpCode.ToString();

                    if (cursor.Next.Operand is ILLabel label) {
                        label_cursor.GotoLabel(label);
                        operand_string = $"Label >>> {label_cursor.Index}";
                    } else {
                        operand_string = cursor.Next.Operand?.ToString() ?? "";
                    }

                    if (operand_string == "") {
                        Debug.Log(cursor_index_string + op_code_string);
                    } else {
                        op_code_string = op_code_string.Length < op_code_string_length ? op_code_string + new string(' ', op_code_string_length - op_code_string.Length) : op_code_string;
                        Debug.Log(cursor_index_string + op_code_string + operand_string);
                    }
                } else {
                    break;
                }
            } catch {
                break;
            }
        }
        Debug.Log("-----------------------------------------------------------------");
    }

    // For debugging.
    public static void SaveTextureAsPNG(Texture? texture, string path) {
        Texture2D? texture_2d = null;
        if (texture is RenderTexture render_texture) {
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = render_texture;
            texture_2d = new Texture2D(render_texture.width, render_texture.height, TextureFormat.RGB24, false);
            texture_2d.ReadPixels(new Rect(0, 0, render_texture.width, render_texture.height), 0, 0);
            RenderTexture.active = previous;
        } else if (texture is Texture2D) {
            texture_2d = texture as Texture2D;
        }

        if (texture_2d == null) {
            return;
        }

        byte[] pngData = texture_2d.EncodeToPNG();
        if (pngData != null) {
            File.WriteAllBytes(path, pngData);
            Debug.Log("Texture saved to " + path);
        } else {
            Debug.LogWarning("Failed to encode texture to PNG.");
        }
    }

    //
    // private
    //

    private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld rain_world) {
        orig(rain_world);

        // if used after isInitialized then disabling and enabling the mod
        // without applying removes access to the options menu;
        MachineConnector.SetRegisteredOI(mod_id, main_mod_options);

        if (is_on_mods_init_initialized) return;
        is_on_mods_init_initialized = true;

        Debug.Log($"{mod_id}: version {version}");
        Debug.Log($"{mod_id}: HasCopyTextureSupport {HasCopyTextureSupport}");
        Debug.Log($"{mod_id}: max_texture_size {SystemInfo.maxTextureSize}");
        Debug.Log($"{mod_id}: mod_directory_path {mod_directory_path}");

        Load_Asset_Bundle();

        rain_world.Replace_Shader("Decal");
        rain_world.Replace_Shader("DeepProcessing");
        rain_world.Replace_Shader("DeepWater");
        rain_world.Replace_Shader("DisplaySnowShader");
        rain_world.Replace_Shader("Fog");
        rain_world.Replace_Shader("GlyphProjection");
        rain_world.Replace_Shader("LevelColor");

        // There is a bug, where you get heavy flickering when using portals.
        // But so far, even replacing the LevelHeat shader with itself doesn't
        // fix this. Maybe the provided version is not the same as the actual
        // version used in the Watcher DLC.
        //
        // Vanilla doesn't use the shader `Futile/LevelHeat` but rather the
        // shader `Futile/LevelColor` with the keyword `levelheat`.
        rain_world.Replace_Shader("LevelHeat", "LevelColor");

        // Unused currently.
        // rain_world.Replace_Shader("PlayerRippleTrail");
        // rain_world.Replace_Shader("ShiftMask");
        // rain_world.Replace_Shader("RippleTearMask");

        rain_world.Replace_Shader("SporesSnow");
        rain_world.Replace_Shader("UnderWaterLight");

        Replace_Shader_LevelBlend();

        foreach (ModManager.Mod mod in ModManager.ActiveMods) {
            // if (mod.id == "crs") {
            //     is_custom_region_support_enabled = true;
            //     continue;
            // }

            if (mod.id == "improved-input-config") {
                is_improved_input_enabled = true;
                continue;
            }

            if (mod.id == "henpemaz_splitscreencoop") {
                is_split_screen_coop_enabled = true;
                continue;
            }
        }

        // if (is_custom_region_support_enabled) {
        //     Debug.Log($"{mod_id}: Custom Region Support (CRS) found. Adept merging when the `REPLACEROOM` feature is used.");
        // } else {
        //     Debug.Log($"{mod_id}: Custom Region Support (CRS) not found.");
        // }

        if (is_improved_input_enabled) {
            Debug.Log($"{mod_id}: Improved Input Config found. Use custom keybindings.");
        } else {
            Debug.Log($"{mod_id}: Improved Input Config not found.");
        }

        if (is_split_screen_coop_enabled) {
            Debug.Log($"{mod_id}: SplitScreen Co-op found. Enable scrolling one-screen rooms.");
        } else {
            Debug.Log($"{mod_id}: SplitScreen Co-op not found.");
        }

        can_log_il_hooks = true;

        AboveCloudsViewMod.OnEnable();
        AbstractRoomMod.OnEnable();
        FScreenMod.OnEnable();
        GhostWorldPresenceMod.OnEnable();
        GoldFlakesMod.OnEnable();
        LevelTexCombinerMod.OnEnable();
        MoreSlugcatsMod.OnEnable();
        MyceliumMod.OnEnable();
        OverWorldMod.OnEnable();
        PlayerGraphicsMod.OnEnable();
        RainWorldGameMod.OnEnable();
        RippleCameraDataMod.OnEnable();
        RoomCameraMod.OnEnable();
        RoomMod.OnEnable();
        SuperStructureProjectorMod.OnEnable();
        WaterMod.OnEnable();
        WorldLoaderMod.OnEnable();
        WormGrassPatchMod.OnEnable();
        WormGrassMod.OnEnable();

        can_log_il_hooks = false;

    }

    private void RainWorld_PostModsInit(On.RainWorld.orig_PostModsInit orig, RainWorld rain_world) {
        orig(rain_world); // loads options;

        // this function is called again when applying mods;
        // only initialize once;
        if (is_post_mod_init_initialized) return;
        is_post_mod_init_initialized = true;

        if (is_improved_input_enabled) {
            Initialize_Custom_Input();
        }

        main_mod_options.Apply_And_Log_All_Options();
    }
}
