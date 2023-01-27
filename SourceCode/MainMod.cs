using System.IO;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using BepInEx;
using MonoMod.Cil;
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
    [BepInPlugin("SchuhBaum.SBCameraScroll", "SBCameraScroll", "2.1.2")]
    public class MainMod : BaseUnityPlugin
    {
        //
        // meta data
        //

        public static string modDirectoryPath = "";
        public static readonly string MOD_ID = "SBCameraScroll";
        public static readonly string author = "SchuhBaum";
        public static readonly string version = "2.1.2";

        //
        // options
        //

        public static bool Option_FogFullScreenEffect => MainModOptions.fogFullScreenEffect.Value;
        public static bool Option_OtherFullScreenEffects => MainModOptions.otherFullScreenEffects.Value;
        public static bool Option_MergeWhileLoading => MainModOptions.mergeWhileLoading.Value;
        public static bool Option_ScrollOneScreenRooms => MainModOptions.scrollOneScreenRooms.Value;
        public static bool Option_ZeroG => MainModOptions.zeroG_Position.Value;

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

        public static void LogAllInstructions(ILContext? context, int indexStringLength = 9, int opCodeStringLength = 14)
        {
            if (context == null) return;

            Debug.Log("-----------------------------------------------------------------");
            Debug.Log("SBCameraScroll: Log all IL-instructions.");
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

        private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld rainWorld)
        {
            orig(rainWorld);

            //TODO: fix events in MainModOptions
            // if used after isInitialized then disabling and enabling the mod
            // without applying removes access to the options menu;
            MachineConnector.SetRegisteredOI(MOD_ID, MainModOptions.instance);

            if (isInitialized) return;
            isInitialized = true;

            Debug.Log("SBCameraScroll: Version " + version);
            CreateModRootDirectory(); // uses root folder directory // needs to be initialized at this point

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
            Debug.Log("SBCameraScroll: modDirectoryPath " + modDirectoryPath);

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
    }
}