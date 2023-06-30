using System;
using System.IO;
using UnityEngine;

using static SBCameraScroll.MainMod;

namespace SBCameraScroll;

public static class RainWorldMod
{
    //
    // parameters
    //

    public static AssetBundle? asset_bundle = null;

    //
    // public
    //

    public static void Load_Asset_Bundle()
    {
        if (asset_bundle != null) return;
        try
        {
            asset_bundle = AssetBundle.LoadFromFile(mod_directory_path + "AssetBundles" + Path.DirectorySeparatorChar + "modded_shaders");
        }
        catch (Exception exception)
        {
            Debug.Log("SBCameraScroll: Could not load the asset bundle with modded shaders.\n  " + exception);
            asset_bundle = null;
        }
    }

    public static void Replace_Shader(this RainWorld rain_world, string shader_name)
    {
        if (asset_bundle == null) return;
        if (!rain_world.Shaders.ContainsKey(shader_name)) return;
        Shader? modded_shader = asset_bundle.LoadAsset<Shader>(shader_name);
        if (modded_shader == null) return;

        Debug.Log("SBCameraScroll: Replace shader " + shader_name + ".");
        rain_world.Shaders[shader_name].shader = modded_shader;
    }
}