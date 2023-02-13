using System.IO;
using UnityEngine;

namespace SBCameraScroll
{
    internal static class RainWorldMod
    {
        //
        // parameters
        //

        public static readonly AssetBundle? assetBundle = AssetBundle.LoadFromFile(MainMod.modDirectoryPath + "AssetBundles" + Path.DirectorySeparatorChar + "shaders");

        //
        // public
        //

        public static void ReplaceShader(this RainWorld rainWorld, string name)
        {
            if (assetBundle == null) return;
            if (!rainWorld.Shaders.ContainsKey(name)) return;

            Shader? shader = assetBundle.LoadAsset<Shader>(name);
            if (shader == null) return;

            // rainWorld.Shaders[name].shader = new Material(shader).shader;
            rainWorld.Shaders[name].shader = shader;
        }
    }
}