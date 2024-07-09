using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

using static SBCameraScroll.MainMod;

namespace SBCameraScroll;

public static class RainWorldMod {
    //
    // parameters
    //

    public static AssetBundle? modded_shaders_bundle = null;

    //
    // public
    //

    public static int? Get_Atlas_Index(string name) {
		FAtlasManager atlas_manager = Futile.atlasManager;
		List<FAtlas> _atlases = atlas_manager._atlases;
		int count = Futile.atlasManager._atlases.Count;
		for (int i = 0; i < count; i++) {
			if (_atlases[i].name == name) {
				return i;
			}
		}
		return null;
    }

    public static void Load_Asset_Bundle() {
        if (modded_shaders_bundle != null) return;
        try {
            modded_shaders_bundle = AssetBundle.LoadFromFile(mod_directory_path + "AssetBundles" + Path.DirectorySeparatorChar + "modded_shaders");
        } catch (Exception exception) {
            Debug.Log("SBCameraScroll: Could not load the asset bundle with modded shaders.\n  " + exception);
            modded_shaders_bundle = null;
        }
    }

    public static ComputeShader? Load_Compute_Shader(string shader_name) {
        if (modded_shaders_bundle == null) return null;
        ComputeShader? compute_shader = modded_shaders_bundle.LoadAsset<ComputeShader>(shader_name);
        if (compute_shader == null) return null;
        Debug.Log(mod_id + ": Loaded the compute shader '" + shader_name + "'.");
        return compute_shader;
    }

    public static void Replace_Or_Add_Atlas(string name, Texture texture) {
        if (Get_Atlas_Index(name) is not int index) {
            Futile.atlasManager.LoadAtlasFromTexture(name, texture, textureFromAsset: false);
            return;
        }

        FAtlasManager atlas_manager = Futile.atlasManager;
        if (atlas_manager._atlases[index].texture == texture) return;

		FAtlas atlas = new FAtlas(name, texture, index, false); // don't use index++;
        atlas_manager._atlases[index] = atlas;
        Replace_Or_Add_Atlas_Elements(atlas);
        Debug.Log(mod_id + ": Replaced atlas for Texture " + name + ".");
    }

    public static void Replace_Or_Add_Atlas_Elements(FAtlas atlas) {
        int count = atlas.elements.Count;
        for (int i = 0; i < count; i++)
        {
            FAtlasElement atlas_element = atlas.elements[i];
            atlas_element.atlas = atlas;
            atlas_element.atlasIndex = atlas.index;

            FAtlasManager atlas_manager = Futile.atlasManager;
            if (atlas_manager._allElementsByName.ContainsKey(atlas_element.name)) {
                atlas_manager._allElementsByName[atlas_element.name] = atlas_element;
            } else {
                atlas_manager._allElementsByName.Add(atlas_element.name, atlas_element);
            }
        }
    }

    public static void Replace_Shader(this RainWorld rain_world, string shader_name) {
        if (modded_shaders_bundle == null) return;
        if (!rain_world.Shaders.ContainsKey(shader_name)) return;
        Shader? modded_shader = modded_shaders_bundle.LoadAsset<Shader>(shader_name);
        if (modded_shader == null) return;

        Debug.Log("SBCameraScroll: Replaced the shader '" + shader_name + "'.");
        rain_world.Shaders[shader_name].shader = modded_shader;
    }
}
