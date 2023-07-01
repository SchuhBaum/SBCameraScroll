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

    public static AssetBundle? modded_shaders_bundle = null;

    //
    // public
    //

    public static void Load_Asset_Bundle()
    {
        if (modded_shaders_bundle != null) return;
        try
        {
            modded_shaders_bundle = AssetBundle.LoadFromFile(mod_directory_path + "AssetBundles" + Path.DirectorySeparatorChar + "modded_shaders");
        }
        catch (Exception exception)
        {
            Debug.Log("SBCameraScroll: Could not load the asset bundle with modded shaders.\n  " + exception);
            modded_shaders_bundle = null;
        }
    }

    public static void Replace_Shader(this RainWorld rain_world, string shader_name)
    {
        if (modded_shaders_bundle == null) return;
        if (!rain_world.Shaders.ContainsKey(shader_name)) return;
        Shader? modded_shader = modded_shaders_bundle.LoadAsset<Shader>(shader_name);
        if (modded_shader == null) return;

        Debug.Log("SBCameraScroll: Replace the shader '" + shader_name + "'.");
        rain_world.Shaders[shader_name].shader = modded_shader;
    }
}