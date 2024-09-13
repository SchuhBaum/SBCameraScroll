using BepInEx;
using MonoMod.Cil;
using RWCustom;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Permissions;
using UnityEngine;

using static SBCameraScroll.AbstractRoomMod;
using static SBCameraScroll.MainModOptions;
using static SBCameraScroll.RainWorldMod;

// allows access to private members;
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace SBCameraScroll;

[BepInPlugin("SBCameraScroll", "SBCameraScroll", "2.9.0")]
public class MainMod : BaseUnityPlugin {
    //
    // meta data
    //

    public static readonly string mod_id = "SBCameraScroll";
    public static readonly string author = "SchuhBaum";
    public static readonly string version = "2.9.0";
    public static readonly string mod_directory_path = Directory.GetParent(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)).FullName + Path.DirectorySeparatorChar;

    //
    // options
    //

    public static bool Option_JIT_Merging => jit_merging.Value && HasCopyTextureSupport;

    public static bool Option_FillEmptySpaces => fill_empty_spaces.Value;
    public static bool Option_FullScreenEffects => full_screen_effects.Value;
    public static bool Option_MergeWhileLoading => merge_while_loading.Value;

    public static bool Option_RegionMods => region_mods.Value;
    public static bool Option_ScrollOneScreenRooms => scroll_one_screen_rooms.Value;
    public static bool Option_CameraOffset => cameraoffset_position.Value;

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

    // does not work for some reason;
    // private MonoBehaviour _create_cache_coroutine_wrapper = new GameObject().AddComponent<MonoBehaviour>();
    private class CoroutineWrapper : MonoBehaviour { }
    internal static MonoBehaviour? _coroutine_wrapper = null;

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
        On.RainWorld.OnModsDisabled += RainWorld_OnModsDisabled;
        On.RainWorld.OnModsEnabled += RainWorld_OnModsEnabled;
        On.RainWorld.OnModsInit += RainWorld_OnModsInit;
        On.RainWorld.PostModsInit += RainWorld_PostModsInit;
    }

    //
    // public
    //

    public static void Check_For_Newly_Activated_Or_Deactivated_Region_Mods() {
        List<string>? previously_active_mods = Read_Previously_Active_Mods();

        // skip if you can't read the file;
        // otherwise this would be executed always with only SBCameraScroll detected;
        if (previously_active_mods == null) return;

        List<ModManager.Mod> newly_activated_or_deactivated_mods = new();
        foreach (ModManager.Mod mod in ModManager.ActiveMods) {
            if (previously_active_mods.Contains(mod.id)) continue;
            newly_activated_or_deactivated_mods.Add(mod);
        }

        foreach (ModManager.Mod mod in ModManager.InstalledMods) {
            if (ModManager.ActiveMods.Contains(mod)) continue;
            if (!previously_active_mods.Contains(mod.id)) continue;
            newly_activated_or_deactivated_mods.Add(mod);
        }

        if (newly_activated_or_deactivated_mods.Count > 0) {
            Delete_Cached_Textures(newly_activated_or_deactivated_mods);
            Write_Previously_Active_Mods(ModManager.ActiveMods.ConvertAll(mod => mod.id));
        }
    }

    public static void CreateDirectory(string directory_path) {
        if (Directory.Exists(directory_path)) return;
        Directory.CreateDirectory(directory_path);
    }

    public static void Delete_Cached_Textures(List<ModManager.Mod> potential_region_mods) {
        // check if mods have modified camera textures and delete the corresponding room textures;
        if (!Option_RegionMods) return;

        // there is also the folder modify/world and modify/levels but so far it seems that only
        // setting files are changed there;

        // arena
        foreach (ModManager.Mod region_mod in potential_region_mods) {
            string directory_path = region_mod.path + Path.DirectorySeparatorChar + "levels";
            if (!Directory.Exists(directory_path)) continue;

            foreach (string file_path_region_mod in Directory.GetFiles(directory_path, "*.png", SearchOption.AllDirectories)) {
                FileInfo file_info = new(file_path_region_mod);

                string[] splitted_room_name = file_info.Name.Split('_');
                if (splitted_room_name.Length == 0) continue;

                string room_name;
                if (splitted_room_name.Length == 1) {
                    room_name = splitted_room_name[0].Replace(".png", "");
                } else {
                    // remove the camera index;
                    // example: RR_NN_1.png becomes RR_NN;
                    room_name = file_info.Name.Replace("_" + splitted_room_name[splitted_room_name.Length - 1], "");
                }

                string file_path = mod_directory_path + "levels" + Path.DirectorySeparatorChar + room_name + "_0.png";
                string region_mod_relative_file_path = "levels" + Path.DirectorySeparatorChar + file_info.Name;

                if (!File.Exists(file_path)) continue;
                if (Is_File_Exclusive(region_mod_relative_file_path)) continue;

                Debug.Log("SBCameraScroll: The mod " + region_mod.id + " has changed one or more camera textures in the room " + room_name.ToUpper() + ". Clear the corresponding cached room texture.");
                File.Delete(file_path);
            }
        }

        // story
        foreach (ModManager.Mod region_mod in potential_region_mods) {
            string directory_path = region_mod.path + Path.DirectorySeparatorChar + "world";
            if (!Directory.Exists(directory_path)) continue;

            foreach (string file_path_region_mod in Directory.GetFiles(directory_path, "*.png", SearchOption.AllDirectories)) {
                FileInfo file_info = new(file_path_region_mod);

                string region_name = file_info.Directory.Name;
                if (region_name == "world") continue;

                string[] splitted_room_name = file_info.Name.Split('_');
                if (splitted_room_name.Length == 0) continue;

                string room_name;
                if (splitted_room_name.Length == 1) {
                    room_name = splitted_room_name[0].Replace(".png", "");
                } else {
                    room_name = file_info.Name.Replace("_" + splitted_room_name[splitted_room_name.Length - 1], "");
                }

                string file_path = mod_directory_path + "world" + Path.DirectorySeparatorChar + region_name + Path.DirectorySeparatorChar + room_name + "_0.png";
                string region_mod_relative_file_path = "world" + Path.DirectorySeparatorChar + region_name + Path.DirectorySeparatorChar + file_info.Name;

                if (!File.Exists(file_path)) continue;
                if (Is_File_Exclusive(region_mod_relative_file_path)) continue;

                Debug.Log("SBCameraScroll: The mod " + region_mod.id + " has changed one or more camera textures in the room " + room_name.ToUpper() + ". Clear the corresponding cached room texture.");
                File.Delete(file_path);
            }
        }
    }

    public static void Initialize_Custom_Input() {
        // wrap it in order to make it a soft dependency only;
        Debug.Log("SBCameraScroll: Initialize custom input.");
        RWInputMod.Initialize_Custom_Keybindings();
        PlayerMod.OnEnable();
    }

    public static bool Is_File_Exclusive(string relative_file_path) {
        // the merged texture dimensions might stay the same but some cameras might get re-rendered by mods;
        // check for duplicates in order to detect that case; 

        int count = 0;
        if (File.Exists(Custom.RootFolderDirectory() + Path.DirectorySeparatorChar + relative_file_path)) {
            ++count;
        }

        foreach (ModManager.Mod mod in ModManager.InstalledMods) {
            if (!File.Exists(mod.path + Path.DirectorySeparatorChar + relative_file_path)) continue;
            ++count;
            if (count > 1) return false;
        }
        return true;
    }

    public static List<string>? Read_Previously_Active_Mods() {
        string file_path = mod_directory_path + "previously_active_mods.json";
        // if (!File.Exists(file_path)) return new() { MOD_ID };
        if (!File.Exists(file_path)) {
            Write_Previously_Active_Mods(ModManager.ActiveMods.ConvertAll(mod => mod.id));
        }

        try {
            List<object> file_content = (List<object>)Json.Deserialize(File.ReadAllText(file_path));
            List<string> previously_active_mods = new();

            foreach (object obj in file_content) {
                previously_active_mods.Add(obj.ToString());
            }
            return previously_active_mods;
        } catch { }
        return null;
    }

    public static void Write_Previously_Active_Mods(List<string> active_mods) {
        try {
            string file_path = mod_directory_path + "previously_active_mods.json";
            File.WriteAllText(file_path, Json.Serialize(active_mods));
        } catch { }
    }

    public static void LogAllInstructions(ILContext? context, int index_string_length = 9, int op_code_string_length = 14) {
        if (context == null) return;

        Debug.Log("-----------------------------------------------------------------");
        Debug.Log("Log all IL-instructions.");
        Debug.Log("Index:" + new string(' ', index_string_length - 6) + "OpCode:" + new string(' ', op_code_string_length - 7) + "Operand:");

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
                        operand_string = "Label >>> " + label_cursor.Index;
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

    //
    // private
    //

    private static void RainWorld_OnModsDisabled(On.RainWorld.orig_OnModsDisabled orig, RainWorld rain_world, ModManager.Mod[] newly_disabled_mods) {
        orig(rain_world, newly_disabled_mods);
        Check_For_Newly_Activated_Or_Deactivated_Region_Mods();
    }

    private static void RainWorld_OnModsEnabled(On.RainWorld.orig_OnModsEnabled orig, RainWorld rain_world, ModManager.Mod[] newly_enabled_mods) {
        orig(rain_world, newly_enabled_mods);
        Check_For_Newly_Activated_Or_Deactivated_Region_Mods();
    }

    private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld rain_world) {
        orig(rain_world);

        // if used after isInitialized then disabling and enabling the mod
        // without applying removes access to the options menu;
        MachineConnector.SetRegisteredOI(mod_id, main_mod_options);

        if (is_on_mods_init_initialized) return;
        is_on_mods_init_initialized = true;
        _coroutine_wrapper ??= new GameObject("SBCameraScroll").AddComponent<CoroutineWrapper>();

        Debug.Log("SBCameraScroll: version " + version);
        Debug.Log("SBCameraScroll: HasCopyTextureSupport " + HasCopyTextureSupport);
        Debug.Log("SBCameraScroll: max_texture_size " + SystemInfo.maxTextureSize);
        Debug.Log("SBCameraScroll: mod_directory_path " + mod_directory_path);

        CreateDirectory(mod_directory_path + "levels");
        CreateDirectory(mod_directory_path + "world");

        Load_Asset_Bundle();
        rain_world.Replace_Shader("Decal");
        rain_world.Replace_Shader("DeepWater");
        rain_world.Replace_Shader("DisplaySnowShader");
        rain_world.Replace_Shader("Fog");

        rain_world.Replace_Shader("LevelColor");
        rain_world.Replace_Shader("LevelHeat");
        rain_world.Replace_Shader("SporesSnow");
        rain_world.Replace_Shader("UnderWaterLight");
        fill_empty_spaces_compute_shader = Load_Compute_Shader("FillEmptySpaces");

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
        //     Debug.Log("SBCameraScroll: CRS found. Adept merging when the `REPLACEROOM` feature is used.");
        // } else {
        //     Debug.Log("SBCameraScroll: CRS not found.");
        // }

        if (is_improved_input_enabled) {
            Debug.Log("SBCameraScroll: Improved Input Config found. Use custom keybindings.");
        } else {
            Debug.Log("SBCameraScroll: Improved Input Config not found.");
        }

        if (is_split_screen_coop_enabled) {
            Debug.Log("SBCameraScroll: SplitScreen Co-op found. Enable scrolling one-screen rooms.");
        } else {
            Debug.Log("SBCameraScroll: SplitScreen Co-op not found.");
        }

        can_log_il_hooks = true;
        AboveCloudsViewMod.OnEnable();
        AbstractRoomMod.OnEnable();
        FScreenMod.OnEnable();

        GhostWorldPresenceMod.OnEnable();
        GoldFlakesMod.OnEnable();
        MoreSlugcatsMod.OnEnable();
        OverWorldMod.OnEnable();

        ProcessManagerMod.OnEnable();
        RainWorldGameMod.OnEnable();
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
        Check_For_Newly_Activated_Or_Deactivated_Region_Mods();

        // this function is called again when applying mods;
        // only initialize once;
        if (is_post_mod_init_initialized) return;
        is_post_mod_init_initialized = true;
        if (!is_improved_input_enabled) return;
        Initialize_Custom_Input();
    }
}
