
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
            modded_shaders_bundle = AssetBundle.LoadFromFile($"{mod_directory_path}AssetBundles{Path.DirectorySeparatorChar}modded_shaders");
        } catch (Exception exception) {
            Debug.Log($"{mod_id}: Could not load the asset bundle with modded shaders.\n  {exception}");
            modded_shaders_bundle = null;
        }
    }

    public static ComputeShader? Load_Compute_Shader(string shader_name) {
        if (modded_shaders_bundle == null) return null;
        ComputeShader? compute_shader = modded_shaders_bundle.LoadAsset<ComputeShader>(shader_name);
        if (compute_shader == null) return null;
        Debug.Log($"{mod_id}: Loaded the compute shader '{shader_name}'.");
        return compute_shader;
    }

    // We use only render textures. This function is used to replace the vanilla
    // Texture2D.
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
        Debug.Log($"{mod_id}: Replaced atlas for Texture {name}.");
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

    public static void Replace_Shader(this RainWorld rain_world, string shader_name, string? modded_shader_name = null) {
        if (modded_shaders_bundle == null) return;
        var f_shader = FShader._shaders.Find(s => s.name == shader_name);
        if (f_shader == null) {
            Debug.Log($"{mod_id}: Didn't find the shader '{shader_name}'.");
            return;
        }

        modded_shader_name ??= shader_name;
        var modded_shader = modded_shaders_bundle.LoadAsset<Shader>(modded_shader_name);
        if (modded_shader == null) {
            Debug.Log($"{mod_id}: Didn't find the modded shader for '{modded_shader_name}'.");
            return;
        }

        f_shader.shader = modded_shader;
        f_shader.name = modded_shader.name;
        Debug.Log($"{mod_id}: Replaced the shader '{shader_name}'.");
    }

    public static void Replace_Shader_LevelBlend() {
        if (modded_shaders_bundle == null) return;

        string shader_name = "LevelBlend";
        Shader? modded_shader = modded_shaders_bundle.LoadAsset<Shader>(shader_name);
        if (modded_shader == null) {
            Debug.Log($"{mod_id}: Didn't find the modded shader for '{shader_name}'.");
            return;
        }

        UnityEngine.Object.Destroy(RippleCameraData.combinerMaterial);
        RippleCameraData.combinerMaterial = new Material(modded_shader);
        Debug.Log($"{mod_id}: Replaced the shader '{shader_name}'.");
    }

    public static void Replace_Shader_SavePlayerCamoMask(RippleCameraData ripple_data) {
        if (modded_shaders_bundle == null) return;

        string shader_name = "SavePlayerCamoMask";
        Shader? modded_shader = modded_shaders_bundle.LoadAsset<Shader>(shader_name);
        if (modded_shader == null) {
            Debug.Log($"{mod_id}: Didn't find the modded shader for '{shader_name}'.");
            return;
        }

        UnityEngine.Object.Destroy(ripple_data.playerCamoMaskSaver);
        ripple_data.playerCamoMaskSaver = new Material(modded_shader);
        Debug.Log($"{mod_id}: Replaced the shader '{shader_name}' for RippleCameraData instance {ripple_data.GetHashCode()}.");
    }
}
