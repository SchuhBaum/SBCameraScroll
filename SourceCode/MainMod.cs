using System;
using System.IO;
using System.Reflection;
using RWCustom;
using UnityEngine;

namespace SBCameraScroll
{
    public class MainMod : Partiality.Modloader.PartialityMod
    {
        public string updateURL = "http://beestuff.pythonanywhere.com/audb/api/mods/8/6";
        public int version = 21;
        public string keyE = "AQAB";
        public string keyN = "0Sb8AUUh0jkFOuNDGJti4jL0iTB4Oug0pM8opATxJH8hfAt6FW3//Q4wb4VfTHZVP3+zHMX6pxcqjdvN0wt/0SWyccfoFhx2LupmT3asV4UDPBdQNmDeA/XMfwmwYb23yxp0apq3kVJNJ3v1SExvo+EPQP4/74JueNBiYshKysRK1InJfkrO1pe1WxtcE7uIrRBVwIgegSVAJDm4PRCODWEp533RxA4FZjq8Hc4UP0Pa0LxlYlSI+jJ+hUrdoA6wd+c/R+lRqN2bjY9OE/OktAxqgthEkSXTtmZwFkCjds0RCqZTnzxfJLN7IheyZ69ptzcB6Zl7kFTEofv4uDjCYNic52/C8uarj+hl4O0yU4xpzdxhG9Tq9SAeNu7h6Dt4Impbr3dAonyVwOhA/HNIz8TUjXldRs0THcZumJ/ZvCHO3qSh7xKS/D7CWuwuY5jWzYZpyy14WOK55vnEFS0GmTwjR+zZtSUy2Y7m8hklllqHZNqRYejoORxTK4UkL4GFOk/uLZKVtOfDODwERWz3ns/eOlReeUaCG1Tole7GhvoZkSMyby/81k3Fh16Z55JD+j1HzUCaoKmT10OOmLF7muV7RV2ZWG0uzvN2oUfr5HSN3TveNw7JQPd5DvZ56whr5ExLMS7Gs6fFBesmkgAwcPTkU5pFpIjgbyk07lDI81k=";

        public static MainMod? instance;

        public static bool isCustomRegionsModEnabled = false;
        public static bool isSplitScreenModEnabled = false;
        public static string modDirectoryPath = "";

        public static bool isFogFullScreenEffectOptionEnabled = true;
        public static bool isOtherFullScreenEffectsOptionEnabled = true;
        public static bool isMergeWhileLoadingOptionEnabled = true;
        public static bool isScrollOneScreenRoomsOptionEnabled = false;

        public MainMod()
        {
            ModID = "SBCameraScroll";
            Version = "0.60";
            author = "SchuhBaum";
            instance = this;
        }

        // ---------------- //
        // public functions //
        // ---------------- //

        public override void OnEnable()
        {
            base.OnEnable();
            On.RainWorld.Start += RainWorld_Start;
        }

        public static OptionalUI.OptionInterface LoadOI() // requires ConfigMachine.dll
        {
            return new MainModOptions();
        }

        // -------------- //
        // public methods //
        // -------------- //

        public static void CreateDirectory(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

        public static void CreateModRootDirectory()
        {
            string directoryPath = Custom.RootFolderDirectory() + "Mods";
            CreateDirectory(directoryPath);
            directoryPath += Path.DirectorySeparatorChar + "SBCameraScroll";
            CreateDirectory(directoryPath);

            modDirectoryPath = directoryPath + Path.DirectorySeparatorChar;
            directoryPath += Path.DirectorySeparatorChar + "World";
            CreateDirectory(directoryPath);

            directoryPath += Path.DirectorySeparatorChar + "Regions";
            CreateDirectory(directoryPath);
        }

        // --------------- //
        // private methods //
        // --------------- //

        private void RainWorld_Start(On.RainWorld.orig_Start orig, RainWorld rainWorld)
        {
            CreateModRootDirectory();
            Debug.Log("SBCameraScroll: Version " + Version);

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                string assemblyName = assembly.GetName().Name;
                if (assemblyName == "CustomRegions")
                {
                    isCustomRegionsModEnabled = true;
                }
                else if (assemblyName == "SplitScreenMod")
                {
                    isSplitScreenModEnabled = true;
                }
            }

            if (!isCustomRegionsModEnabled)
            {
                Debug.Log("SBCameraScroll: CustomRegions not found.");
            }
            else
            {
                Debug.Log("SBCameraScroll: CustomRegions found. Adept merging camera textures.");
            }

            if (!isSplitScreenModEnabled)
            {
                Debug.Log("SBCameraScroll: SplitScreenMod not found.");
            }
            else
            {
                Debug.Log("SBCameraScroll: SplitScreenMod found. Enable option for scrolling one-screen rooms.");
            }

            AboveCloudsViewMod.OnEnable();
            GhostWorldPresenceMod.OnEnable();
            GoldFlakesMod.OnEnable();
            RainWorldGameMod.OnEnable();

            RegionGateMod.OnEnable();
            RoomCameraMod.OnEnable();
            RoomMod.OnEnable();

            SuperStructureProjectorMod.OnEnable();
            WorldMod.OnEnable();
            WormGrassMod.OnEnable();

            orig(rainWorld);
        }
    }
}