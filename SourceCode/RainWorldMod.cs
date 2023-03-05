using System.IO;
using UnityEngine;

using static SBCameraScroll.MainMod;

namespace SBCameraScroll;

public static class RainWorldMod
{
    //
    // parameters
    //

    public static readonly AssetBundle? assetBundle = AssetBundle.LoadFromFile(modDirectoryPath + "AssetBundles" + Path.DirectorySeparatorChar + "shaders");

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