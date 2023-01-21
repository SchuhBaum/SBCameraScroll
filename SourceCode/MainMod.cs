using System.IO;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using BepInEx;
using UnityEngine;

// temporary fix // should be added automatically //TODO
[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace SBCameraScroll
{
    // this plugin needs to be loaded before SplitScreenMod because some methods don't always call orig()
    // SplitScreenMod needs to be able to get the current cameraNumber for these methods
    // if I get access to that variable directly (static) I could do that too // but I don't want to carry an instance of SplitScreenMod around => dependency
    // You should be able to change load order now;
    [BepInPlugin("SchuhBaum.SBCameraScroll", "SBCameraScroll", "2.0.3")]
    public class MainMod : BaseUnityPlugin
    {
        //
        // meta data
        //

        public static string modDirectoryPath = "";
        public static readonly string MOD_ID = "SBCameraScroll";
        public static readonly string author = "SchuhBaum";
        public static readonly string version = "2.0.3";

        //
        // other mods
        //

        public static bool isSplitScreenModEnabled = false;

        //
        // variables
        //

        public static bool isInitialized = false;

        // 
        // main
        // 

        public MainMod() { }

        // does not work yet // game bugged?
        // public void OnModsInit()
        // {
        //     Debug.Log("Hello World!");
        // }

        public void OnEnable() => On.RainWorld.OnModsInit += RainWorld_OnModsInit;

        //
        // public
        //

        public static void CreateDirectory(string directoryPath)
        {
            if (Directory.Exists(directoryPath)) return;
            Directory.CreateDirectory(directoryPath);
        }

        public static void CreateModRootDirectory()
        {
            modDirectoryPath = Directory.GetParent(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)).FullName + Path.DirectorySeparatorChar;
            CreateDirectory(modDirectoryPath + "world");
        }

        //
        // private
        //

        private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld rainWorld)
        {
            orig(rainWorld);

            if (isInitialized) return;
            isInitialized = true;

            //TODO: fix events in MainModOptions
            MachineConnector.SetRegisteredOI(MOD_ID, MainModOptions.instance);

            CreateModRootDirectory(); // uses root folder directory // needs to be initialized at this point
            Debug.Log("SBCameraScroll: Version " + version);

            foreach (ModManager.Mod mod in ModManager.ActiveMods)
            {
                if (mod.name == "SplitScreenMod")
                {
                    isSplitScreenModEnabled = true;
                    break;
                }
            }

            if (!isSplitScreenModEnabled)
            {
                Debug.Log("SBCameraScroll: SplitScreenMod not found.");
            }
            else
            {
                Debug.Log("SBCameraScroll: SplitScreenMod found. Enable option for scrolling one-screen rooms.");
            }
            Debug.Log("SBCameraScroll: This mod needs to be loaded after SplitScreenMod. The load order can be change manually in Remix. SplitScreenMod does not exist for Rain World 1.9 yet. TODO: fix this.");

            AboveCloudsViewMod.OnEnable();
            AbstractRoomMod.OnEnable();
            GhostWorldPresenceMod.OnEnable();
            GoldFlakesMod.OnEnable();

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
    }
}