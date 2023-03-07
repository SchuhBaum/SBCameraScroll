using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Permissions;
using BepInEx;
using MonoMod.Cil;
using RWCustom;
using UnityEngine;

// temporary fix // should be added automatically //TODO
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace SBCameraScroll;

// this plugin needs to be loaded before SplitScreenMod because some methods don't always call orig()
// SplitScreenMod needs to be able to get the current cameraNumber for these methods
// if I get access to that variable directly (static) I could do that too // but I don't want to carry an instance of SplitScreenMod around => dependency
// You should be able to change load order now;
[BepInPlugin("SchuhBaum.SBCameraScroll", "SBCameraScroll", "2.4.8")]
public class MainMod : BaseUnityPlugin
{
    //
    // meta data
    //

    public static readonly string MOD_ID = "SchuhBaum.SBCameraScroll";
    public static readonly string author = "SchuhBaum";
    public static readonly string version = "2.4.8";
    public static readonly string modDirectoryPath = Directory.GetParent(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)).FullName + Path.DirectorySeparatorChar;

    //
    // options
    //

    public static bool Option_FogFullScreenEffect => MainModOptions.fogFullScreenEffect.Value;
    public static bool Option_OtherFullScreenEffects => MainModOptions.otherFullScreenEffects.Value;
    public static bool Option_MergeWhileLoading => MainModOptions.mergeWhileLoading.Value;
    public static bool Option_RegionMods => MainModOptions.regionMods.Value;
    public static bool Option_ScrollOneScreenRooms => MainModOptions.scrollOneScreenRooms.Value || is_split_screen_coop_enabled;

    public static bool Option_CameraOffset => MainModOptions.cameraOffset_Position.Value;

    //
    // other mods
    //

    public static bool is_split_screen_coop_enabled = false;

    //
    // variables
    //

    public static bool isInitialized = false;

    // 
    // main
    // 

    public MainMod() { }
    public void OnEnable()
    {
        On.RainWorld.OnModsDisabled += RainWorld_OnModsDisabled;
        On.RainWorld.OnModsEnabled += RainWorld_OnModsEnabled;
        On.RainWorld.OnModsInit += RainWorld_OnModsInit;
        On.RainWorld.PostModsInit += RainWorld_PostModsInit;
    }

    //
    // public
    //

    public static void Check_For_Newly_Activated_Or_Deactivated_Region_Mods()
    {
        List<string>? previously_active_mods = Read_Previously_Active_Mods();

        // skip if you can't read the file;
        // otherwise this would be executed always with only SBCameraScroll detected;
        if (previously_active_mods == null) return;

        List<ModManager.Mod> newly_activated_or_deactivated_mods = new();
        foreach (ModManager.Mod mod in ModManager.ActiveMods)
        {
            if (previously_active_mods.Contains(mod.id)) continue;
            newly_activated_or_deactivated_mods.Add(mod);
        }

        foreach (ModManager.Mod mod in ModManager.InstalledMods)
        {
            if (ModManager.ActiveMods.Contains(mod)) continue;
            if (!previously_active_mods.Contains(mod.id)) continue;
            newly_activated_or_deactivated_mods.Add(mod);
        }

        if (newly_activated_or_deactivated_mods.Count > 0)
        {
            Delete_Cached_Textures(newly_activated_or_deactivated_mods);
            Write_Previously_Active_Mods(ModManager.ActiveMods.ConvertAll(mod => mod.id));
        }
    }

    public static void CreateDirectory(string directoryPath)
    {
        if (Directory.Exists(directoryPath)) return;
        Directory.CreateDirectory(directoryPath);
    }

    public static void Delete_Cached_Textures(List<ModManager.Mod> potential_region_mods)
    {
        // check if mods have modified camera textures and delete the corresponding room textures;
        if (!Option_RegionMods) return;

        // there is also the folder modify/world and modify/levels but so far it seems that only
        // setting files are changed there;

        // arena
        foreach (ModManager.Mod region_mod in potential_region_mods)
        {
            string directory_path = region_mod.path + Path.DirectorySeparatorChar + "levels";
            if (!Directory.Exists(directory_path)) continue;

            foreach (string file_path_region_mod in Directory.GetFiles(directory_path, "*.png", SearchOption.AllDirectories))
            {
                FileInfo file_info = new(file_path_region_mod);

                string[] splitted_room_name = file_info.Name.Split('_');
                if (splitted_room_name.Length == 0) continue;

                string room_name;
                if (splitted_room_name.Length == 1)
                {
                    room_name = splitted_room_name[0].Replace(".png", "");
                }
                else
                {
                    // remove the camera index;
                    // example: RR_NN_1.png becomes RR_NN;
                    room_name = file_info.Name.Replace("_" + splitted_room_name[splitted_room_name.Length - 1], "");
                }

                string file_path = modDirectoryPath + "levels" + Path.DirectorySeparatorChar + room_name + "_0.png";
                string region_mod_relative_file_path = "levels" + Path.DirectorySeparatorChar + file_info.Name;

                if (!File.Exists(file_path)) continue;
                if (Is_File_Exclusive(region_mod_relative_file_path)) continue;

                Debug.Log("SBCameraScroll: The mod " + region_mod.id + " has changed one or more camera textures in the room " + room_name.ToUpper() + ". Clear the corresponding cached room texture.");
                File.Delete(file_path);
            }
        }

        // story
        foreach (ModManager.Mod region_mod in potential_region_mods)
        {
            string directory_path = region_mod.path + Path.DirectorySeparatorChar + "world";
            if (!Directory.Exists(directory_path)) continue;

            foreach (string file_path_region_mod in Directory.GetFiles(directory_path, "*.png", SearchOption.AllDirectories))
            {
                FileInfo file_info = new(file_path_region_mod);

                string region_name = file_info.Directory.Name;
                if (region_name == "world") continue;

                string[] splitted_room_name = file_info.Name.Split('_');
                if (splitted_room_name.Length == 0) continue;

                string room_name;
                if (splitted_room_name.Length == 1)
                {
                    room_name = splitted_room_name[0].Replace(".png", "");
                }
                else
                {
                    room_name = file_info.Name.Replace("_" + splitted_room_name[splitted_room_name.Length - 1], "");
                }

                string file_path = modDirectoryPath + "world" + Path.DirectorySeparatorChar + region_name + Path.DirectorySeparatorChar + room_name + "_0.png";
                string region_mod_relative_file_path = "world" + Path.DirectorySeparatorChar + region_name + Path.DirectorySeparatorChar + file_info.Name;

                if (!File.Exists(file_path)) continue;
                if (Is_File_Exclusive(region_mod_relative_file_path)) continue;

                Debug.Log("SBCameraScroll: The mod " + region_mod.id + " has changed one or more camera textures in the room " + room_name.ToUpper() + ". Clear the corresponding cached room texture.");
                File.Delete(file_path);
            }
        }
    }

    public static bool Is_File_Exclusive(string relative_file_path)
    {
        // the merged texture dimensions might stay the same but some cameras might get re-rendered by mods;
        // check for duplicates in order to detect that case; 

        int count = 0;
        if (File.Exists(Custom.RootFolderDirectory() + Path.DirectorySeparatorChar + relative_file_path))
        {
            ++count;
        }

        foreach (ModManager.Mod mod in ModManager.InstalledMods)
        {
            if (!File.Exists(mod.path + Path.DirectorySeparatorChar + relative_file_path)) continue;
            ++count;
            if (count > 1) return false;
        }
        return true;
    }

    public static List<string>? Read_Previously_Active_Mods()
    {
        string file_path = modDirectoryPath + "previously_active_mods.json";
        // if (!File.Exists(file_path)) return new() { MOD_ID };
        if (!File.Exists(file_path))
        {
            Write_Previously_Active_Mods(ModManager.ActiveMods.ConvertAll(mod => mod.id));
        }

        try
        {
            List<object> file_content = (List<object>)Json.Deserialize(File.ReadAllText(file_path));
            List<string> previously_active_mods = new();

            foreach (object obj in file_content)
            {
                previously_active_mods.Add(obj.ToString());
            }
            return previously_active_mods;
        }
        catch { }
        return null;
    }

    public static void Write_Previously_Active_Mods(List<string> active_mods)
    {
        try
        {
            string file_path = modDirectoryPath + "previously_active_mods.json";
            File.WriteAllText(file_path, Json.Serialize(active_mods));
        }
        catch { }
    }

    public static void LogAllInstructions(ILContext? context, int indexStringLength = 9, int opCodeStringLength = 14)
    {
        if (context == null) return;

        Debug.Log("-----------------------------------------------------------------");
        Debug.Log("Log all IL-instructions.");
        Debug.Log("Index:" + new string(' ', indexStringLength - 6) + "OpCode:" + new string(' ', opCodeStringLength - 7) + "Operand:");

        ILCursor cursor = new(context);
        ILCursor labelCursor = cursor.Clone();

        string cursorIndexString;
        string opCodeString;
        string operandString;

        while (true)
        {
            // this might return too early;
            // if (cursor.Next.MatchRet()) break;

            // should always break at some point;
            // only TryGotoNext() doesn't seem to be enough;
            // it still throws an exception;
            try
            {
                if (cursor.TryGotoNext(MoveType.Before))
                {
                    cursorIndexString = cursor.Index.ToString();
                    cursorIndexString = cursorIndexString.Length < indexStringLength ? cursorIndexString + new string(' ', indexStringLength - cursorIndexString.Length) : cursorIndexString;
                    opCodeString = cursor.Next.OpCode.ToString();

                    if (cursor.Next.Operand is ILLabel label)
                    {
                        labelCursor.GotoLabel(label);
                        operandString = "Label >>> " + labelCursor.Index;
                    }
                    else
                    {
                        operandString = cursor.Next.Operand?.ToString() ?? "";
                    }

                    if (operandString == "")
                    {
                        Debug.Log(cursorIndexString + opCodeString);
                    }
                    else
                    {
                        opCodeString = opCodeString.Length < opCodeStringLength ? opCodeString + new string(' ', opCodeStringLength - opCodeString.Length) : opCodeString;
                        Debug.Log(cursorIndexString + opCodeString + operandString);
                    }
                }
                else
                {
                    break;
                }
            }
            catch
            {
                break;
            }
        }
        Debug.Log("-----------------------------------------------------------------");
    }

    //
    // private
    //

    private static void RainWorld_OnModsDisabled(On.RainWorld.orig_OnModsDisabled orig, RainWorld rain_world, ModManager.Mod[] newly_disabled_mods)
    {
        orig(rain_world, newly_disabled_mods);
        Check_For_Newly_Activated_Or_Deactivated_Region_Mods();
    }

    private static void RainWorld_OnModsEnabled(On.RainWorld.orig_OnModsEnabled orig, RainWorld rain_world, ModManager.Mod[] newly_enabled_mods)
    {
        orig(rain_world, newly_enabled_mods);
        Check_For_Newly_Activated_Or_Deactivated_Region_Mods();
    }

    private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld rainWorld)
    {
        orig(rainWorld);

        // if used after isInitialized then disabling and enabling the mod
        // without applying removes access to the options menu;
        MachineConnector.SetRegisteredOI(MOD_ID, MainModOptions.instance);

        if (isInitialized) return;
        isInitialized = true;

        Debug.Log("SBCameraScroll: version " + version);
        Debug.Log("SBCameraScroll: maxTextureSize " + SystemInfo.maxTextureSize);
        Debug.Log("SBCameraScroll: modDirectoryPath " + modDirectoryPath);

        CreateDirectory(modDirectoryPath + "levels");
        CreateDirectory(modDirectoryPath + "world");

        // somehow this is in front of creatures and hides them;
        // besides that I need the current shader instead of the one from RW v1.5;
        // it seems to have been modified to contain some changes for the Gutter area;
        // rainWorld.ReplaceShader("DeepWater");

        foreach (ModManager.Mod mod in ModManager.ActiveMods)
        {
            if (mod.id == "henpemaz_splitscreencoop")
            {
                is_split_screen_coop_enabled = true;
                break;
            }
        }

        if (!is_split_screen_coop_enabled)
        {
            Debug.Log("SBCameraScroll: SplitScreen Co-op not found.");
        }
        else
        {
            Debug.Log("SBCameraScroll: SplitScreen Co-op found. Enable scrolling one-screen rooms.");
        }

        AboveCloudsViewMod.OnEnable();
        AbstractRoomMod.OnEnable();
        GhostWorldPresenceMod.OnEnable();
        GoldFlakesMod.OnEnable();

        MoreSlugcatsMod.OnEnable();
        OverWorldMod.OnEnable();
        RainWorldGameMod.OnEnable();
        RegionGateMod.OnEnable();

        RoomCameraMod.OnEnable();
        RoomMod.OnEnable();
        SuperStructureProjectorMod.OnEnable();
        WorldMod.OnEnable();

        WormGrassPatchMod.OnEnable();
        WormGrassMod.OnEnable();
    }

    private void RainWorld_PostModsInit(On.RainWorld.orig_PostModsInit orig, RainWorld rain_world)
    {
        orig(rain_world); // loads options;
        Check_For_Newly_Activated_Or_Deactivated_Region_Mods();
    }
}