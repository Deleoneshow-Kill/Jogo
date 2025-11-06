using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// Automates the SSA presentation setup (URP, lighting, materials, volumes, scene helpers).
public static class SSASetupWizard
{
    const string rootFolder = "Assets/SSA";
    const string renderingFolder = rootFolder + "/Rendering";
    const string materialsFolder = rootFolder + "/Materials";
    const string matcapFolder = materialsFolder + "/Matcaps";
    const string shadersFolder = rootFolder + "/Shaders";
    const string volumesFolder = rootFolder + "/Volumes";

    const string pipelineAssetPath = renderingFolder + "/SSA_Pipeline.asset";
    const string rendererAssetPath = renderingFolder + "/SSA_Renderer.asset";
    const string volumeProfilePath = renderingFolder + "/SSA_DefaultProfile.asset";
    const string globalVolumePrefabPath = volumesFolder + "/SSA_GlobalVolume.prefab";

    const string rampTexturePath = materialsFolder + "/Toon_Ramp_2Step.png";
    const string metalMatcapPath = matcapFolder + "/Matcap_Metal.png";
    const string skinMatcapPath = matcapFolder + "/Matcap_Skin.png";

    const string toonCharacterMatPath = materialsFolder + "/Toon_Character.mat";
    const string toonWeaponMatPath = materialsFolder + "/Toon_Weapon.mat";
    const string toonPropMatPath = materialsFolder + "/Toon_Prop.mat";

    [MenuItem("Tools/SSA/Run Complete SSA Setup")]
    static void RunCompleteSetup()
    {
        try
        {
            AssetDatabase.StartAssetEditing();

            EnsureFolders();

            var rendererData = EnsureRendererAsset();
            var pipelineAsset = EnsurePipelineAsset(rendererData);

            ConfigurePipelineAsset(pipelineAsset);
            ApplyPipelineToProject(pipelineAsset);

            EnsureLightingAndEnvironment();

            var rampTexture = EnsureRampTexture();
            var metalMatcap = EnsureMatcapTexture(metalMatcapPath, new Color(0.6f, 0.72f, 0.82f), new Color(0.96f, 0.98f, 1f));
            var skinMatcap = EnsureMatcapTexture(skinMatcapPath, new Color(0.92f, 0.74f, 0.68f), new Color(1f, 0.9f, 0.82f));
            var toonShader = FindMatcapOutlineShader();

            EnsureToonMaterials(toonShader, rampTexture, metalMatcap, skinMatcap);

            var volumeProfile = EnsureVolumeProfile();
            var volumePrefab = EnsureGlobalVolumePrefab(volumeProfile);

            EnsureSceneSetup(volumePrefab, toonShader);

            AssetDatabase.SaveAssets();
        }
        catch (Exception ex)
        {
            Debug.LogError("SSA setup falhou: " + ex.Message + "\n" + ex.StackTrace);
            EditorUtility.DisplayDialog("SSA Setup", "Falha ao aplicar configuracao SSA. Veja o Console.", "Ok");
            return;
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
        }

        EditorUtility.DisplayDialog("SSA Setup", "Configuracao SSA aplicada com sucesso.", "Ok");
    }

    static void EnsureFolders()
    {
        EnsureFolder(rootFolder);
        EnsureFolder(renderingFolder);
        EnsureFolder(materialsFolder);
        EnsureFolder(matcapFolder);
        EnsureFolder(shadersFolder);
        EnsureFolder(volumesFolder);
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        var parent = Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(parent))
            throw new InvalidOperationException("Pasta raiz invalida: " + path);

        EnsureFolder(parent.Replace('\\', '/'));
        AssetDatabase.CreateFolder(parent.Replace('\\', '/'), Path.GetFileName(path));
    }

    static UniversalRendererData EnsureRendererAsset()
    {
        var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererAssetPath);
        if (renderer != null)
            return renderer;

        renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
        renderer.name = "SSA_Renderer";

        var serialized = new SerializedObject(renderer);
        var postProcessDataProp = serialized.FindProperty("m_PostProcessData");
        if (postProcessDataProp != null)
        {
            var postProcessData = AssetDatabase.LoadAssetAtPath<PostProcessData>("Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset");
            postProcessDataProp.objectReferenceValue = postProcessData;
        }
        serialized.ApplyModifiedProperties();

        AssetDatabase.CreateAsset(renderer, rendererAssetPath);
        return renderer;
    }

    static UniversalRenderPipelineAsset EnsurePipelineAsset(UniversalRendererData rendererData)
    {
        var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelineAssetPath);
        if (asset != null)
            return asset;

        asset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
        asset.name = "SSA_Pipeline";
        AssetDatabase.CreateAsset(asset, pipelineAssetPath);

        var serialized = new SerializedObject(asset);
        var rendererList = serialized.FindProperty("m_RendererDataList");
        if (rendererList != null)
        {
            rendererList.arraySize = 1;
            rendererList.GetArrayElementAtIndex(0).objectReferenceValue = rendererData;
        }

        var defaultRendererIndex = serialized.FindProperty("m_DefaultRendererIndex");
        if (defaultRendererIndex != null)
            defaultRendererIndex.intValue = 0;

        serialized.ApplyModifiedProperties();
        return asset;
    }

    static void ConfigurePipelineAsset(UniversalRenderPipelineAsset asset)
    {
        if (asset == null)
            return;

        var serialized = new SerializedObject(asset);

        SetBool(serialized, "m_SupportsCameraDepthTexture", true);
        SetBool(serialized, "m_SupportsCameraOpaqueTexture", true);
        SetBool(serialized, "m_SupportsPostProcessing", true);
        SetBool(serialized, "m_UseSRPBatcher", true);

        SetInt(serialized, "m_MSAA", 1);
        SetInt(serialized, "m_MSAASampleCount", 1);
        SetInt(serialized, "m_AntialiasingMode", (int)AntialiasingMode.FastApproximateAntialiasing);
        SetInt(serialized, "m_Antialiasing", (int)AntialiasingMode.FastApproximateAntialiasing);
        SetInt(serialized, "m_AdditionalLightsPerObjectLimit", 4);
        SetInt(serialized, "m_MainLightShadowmapResolution", (int)UnityEngine.Rendering.Universal.ShadowResolution._4096);
        SetInt(serialized, "m_MainLightShadowCascadeCount", 1);
        SetFloat(serialized, "m_ShadowDistance", 60f);

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);
    }

    static void ApplyPipelineToProject(UniversalRenderPipelineAsset asset)
    {
        if (asset == null)
            return;

        GraphicsSettings.defaultRenderPipeline = asset;

        var current = QualitySettings.GetQualityLevel();
        for (int i = 0; i < QualitySettings.names.Length; i++)
        {
            QualitySettings.SetQualityLevel(i, false);
            QualitySettings.renderPipeline = asset;
        }
        QualitySettings.SetQualityLevel(current, false);
    }

    static void EnsureLightingAndEnvironment()
    {
        var light = RenderSettings.sun;
        if (light == null)
        {
            light = UnityEngine.Object.FindObjectsOfType<Light>().FirstOrDefault(l => l.type == LightType.Directional);
        }

        if (light == null)
        {
            var go = new GameObject("SSA Directional Light");
            light = go.AddComponent<Light>();
            light.type = LightType.Directional;
        }

        light.color = new Color(1f, 0.95f, 0.82f);
        light.intensity = 1.4f;
        light.shadows = LightShadows.Soft;
        light.shadowBias = 0.05f;
        light.shadowNormalBias = 0.3f;
        light.shadowResolution = LightShadowResolution.VeryHigh;

        var additionalData = light.gameObject.GetComponent<UniversalAdditionalLightData>();
        if (additionalData == null)
            additionalData = light.gameObject.AddComponent<UniversalAdditionalLightData>();

        RenderSettings.sun = light;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.73f, 0.84f, 1f);
        RenderSettings.ambientEquatorColor = new Color(0.65f, 0.77f, 0.98f);
        RenderSettings.ambientGroundColor = new Color(0.5f, 0.7f, 1f);

        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
        RenderSettings.reflectionIntensity = 1f;

        var fogData = RenderSettings.fog;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogColor = new Color(0.74f, 0.85f, 1f);
        RenderSettings.fogDensity = 0.01f;

        EditorSceneManager.MarkAllScenesDirty();
    }

    static Texture2D EnsureRampTexture()
    {
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(rampTexturePath);
        if (tex != null)
            return tex;

        tex = new Texture2D(256, 1, TextureFormat.RGBA32, false)
        {
            name = "Toon_Ramp_2Step",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        var colors = new Color[256];
        for (int x = 0; x < 256; x++)
        {
            if (x < 122)
                colors[x] = new Color(0.1f, 0.18f, 0.32f);
            else if (x < 134)
                colors[x] = new Color(0.32f, 0.45f, 0.68f);
            else
                colors[x] = new Color(0.95f, 0.97f, 1f);
        }

        tex.SetPixels(colors);
        tex.Apply();

        var png = tex.EncodeToPNG();
        File.WriteAllBytes(rampTexturePath, png);
        UnityEngine.Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(rampTexturePath, ImportAssetOptions.ForceSynchronousImport);

        tex = AssetDatabase.LoadAssetAtPath<Texture2D>(rampTexturePath);
        return tex;
    }

    static Texture2D EnsureMatcapTexture(string path, Color baseColor, Color highlight)
    {
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (tex != null)
            return tex;

        const int size = 256;
        tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = Path.GetFileNameWithoutExtension(path),
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        var center = new Vector2(size - 1, size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                var uv = new Vector2(x, y);
                var dist = Vector2.Distance(uv, center) / (size * 0.5f);
                dist = Mathf.Clamp01(dist);
                var t = Mathf.Pow(1f - dist, 2.5f);
                var color = Color.Lerp(baseColor, highlight, t);
                tex.SetPixel(x, y, color);
            }
        }

        tex.Apply();

        var png = tex.EncodeToPNG();
        File.WriteAllBytes(path, png);
        UnityEngine.Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static Shader FindMatcapOutlineShader()
    {
        var shader = Shader.Find("Custom/MatcapOutline");
        if (shader != null)
            return shader;

        var guid = AssetDatabase.FindAssets("MatcapOutline t:Shader").FirstOrDefault();
        if (!string.IsNullOrEmpty(guid))
            shader = AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath(guid));

        if (shader == null)
            Debug.LogWarning("Shader MatcapOutline nao encontrado. Ajuste as referencias de material manualmente.");

        return shader;
    }

    static void EnsureToonMaterials(Shader shader, Texture2D ramp, Texture2D metalMatcap, Texture2D skinMatcap)
    {
        if (shader == null)
            return;

        CreateMaterial(toonCharacterMatPath, shader, ramp, skinMatcap, 0.52f, 0.04f, 0.86f, 0.8f, 3.2f, 0.18f, 0.22f, 0.0025f, 0.001f);
        CreateMaterial(toonWeaponMatPath, shader, ramp, metalMatcap, 0.5f, 0.03f, 0.75f, 1.0f, 3.0f, 0.15f, 0.25f, 0.002f, 0.0008f);
        CreateMaterial(toonPropMatPath, shader, ramp, metalMatcap, 0.55f, 0.05f, 0.7f, 0.6f, 2.5f, 0.12f, 0.18f, 0.002f, 0.001f);
    }

    static void CreateMaterial(string path, Shader shader, Texture2D ramp, Texture2D matcap, float rampThreshold, float rampSoftness, float specThreshold, float specIntensity, float rimPower, float rimIntensity, float matcapBlend, float outlineNear, float outlineFar)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(shader) { name = Path.GetFileNameWithoutExtension(path) };
            AssetDatabase.CreateAsset(mat, path);
        }

        mat.SetTexture("_RampTex", ramp);
        mat.SetTexture("_MatcapTex", matcap);
        mat.SetFloat("_RampThreshold", rampThreshold);
        mat.SetFloat("_RampSoftness", rampSoftness);
        mat.SetFloat("_SpecThreshold", specThreshold);
        mat.SetFloat("_SpecIntensity", specIntensity);
        mat.SetFloat("_RimPower", rimPower);
        mat.SetFloat("_RimIntensity", rimIntensity);
        mat.SetFloat("_MatcapBlend", matcapBlend);
        mat.SetFloat("_OutlineWidthNear", outlineNear);
        mat.SetFloat("_OutlineWidthFar", outlineFar);

        EditorUtility.SetDirty(mat);
    }

    static VolumeProfile EnsureVolumeProfile()
    {
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(volumeProfilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "SSA_DefaultProfile";
            AssetDatabase.CreateAsset(profile, volumeProfilePath);
        }

        AddOrUpdate(profile, (Tonemapping t) =>
        {
            t.mode.Override(TonemappingMode.ACES);
        });

        AddOrUpdate(profile, (Bloom bloom) =>
        {
            bloom.threshold.Override(1.1f);
            bloom.intensity.Override(0.22f);
            bloom.scatter.Override(0.7f);
        });

        AddOrUpdate(profile, (ColorAdjustments color) =>
        {
            color.saturation.Override(12f);
            color.postExposure.Override(0.1f);
        });

        AddOrUpdate(profile, (ShadowsMidtonesHighlights smh) =>
        {
            smh.midtones.Override(new Vector4(0.88f, 0.94f, 1.1f, 0f));
        });

        AddOrUpdate(profile, (Vignette vignette) =>
        {
            vignette.intensity.Override(0.15f);
            vignette.smoothness.Override(0.35f);
        });

        AddOrUpdate(profile, (DepthOfField dof) =>
        {
            dof.mode.Override(DepthOfFieldMode.Bokeh);
            dof.focusDistance.Override(8f);
            dof.focalLength.Override(50f);
            dof.aperture.Override(7f);
            dof.active = false;
        });

        EditorUtility.SetDirty(profile);
        return profile;
    }

    static GameObject EnsureGlobalVolumePrefab(VolumeProfile profile)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(globalVolumePrefabPath);
        if (prefab != null)
        {
            UpdateVolumePrefab(prefab, profile);
            return prefab;
        }

        var go = new GameObject("SSA_GlobalVolume");
        var volume = go.AddComponent<UnityEngine.Rendering.Volume>();
        volume.isGlobal = true;
        volume.profile = profile;

        prefab = PrefabUtility.SaveAsPrefabAsset(go, globalVolumePrefabPath);
        UnityEngine.Object.DestroyImmediate(go);
        return prefab;
    }

    static void UpdateVolumePrefab(GameObject prefab, VolumeProfile profile)
    {
        var volume = prefab.GetComponent<UnityEngine.Rendering.Volume>();
        if (volume == null)
            volume = prefab.AddComponent<UnityEngine.Rendering.Volume>();
        volume.isGlobal = true;
        volume.profile = profile;
        EditorUtility.SetDirty(prefab);
        PrefabUtility.SavePrefabAsset(prefab);
    }

    static void EnsureSceneSetup(GameObject volumePrefab, Shader toonShader)
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
            return;

        if (volumePrefab != null)
        {
            var existing = UnityEngine.Object.FindObjectOfType<UnityEngine.Rendering.Volume>();
            if (existing == null)
            {
                var instance = PrefabUtility.InstantiatePrefab(volumePrefab) as GameObject;
                if (instance != null)
                {
                    Undo.RegisterCreatedObjectUndo(instance, "Add SSA Global Volume");
                    EditorSceneManager.MarkSceneDirty(scene);
                }
            }
        }

        AssignBuildSettings(scene);

        if (toonShader != null)
            ApplyToonMaterialsToHero(toonShader);
    }

    static void AssignBuildSettings(Scene scene)
    {
        if (!scene.IsValid() || string.IsNullOrEmpty(scene.path))
            return;

        var scenes = EditorBuildSettings.scenes.ToList();
        var currentPath = scene.path;
        var existingIndex = scenes.FindIndex(s => s.path == currentPath);
        if (existingIndex >= 0)
        {
            if (existingIndex != 0)
            {
                var record = scenes[existingIndex];
                scenes.RemoveAt(existingIndex);
                scenes.Insert(0, record);
            }
        }
        else
        {
            scenes.Insert(0, new EditorBuildSettingsScene(currentPath, true));
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }

    static void ApplyToonMaterialsToHero(Shader shader)
    {
        var hero = GameObject.Find("HeroShowcase");
        if (hero == null)
            hero = GameObject.Find("MixamoHero");
        if (hero == null)
            return;

        var characterMat = AssetDatabase.LoadAssetAtPath<Material>(toonCharacterMatPath);
        if (characterMat == null)
            return;

        foreach (var skinned in hero.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            Undo.RecordObject(skinned, "Assign SSA Toon Material");
            skinned.material = characterMat;
        }

        foreach (var mesh in hero.GetComponentsInChildren<MeshRenderer>(true))
        {
            Undo.RecordObject(mesh, "Assign SSA Toon Material");
            mesh.sharedMaterial = characterMat;
        }
    }

    static void SetBool(SerializedObject obj, string propertyPath, bool value)
    {
        var prop = obj.FindProperty(propertyPath);
        if (prop != null)
            prop.boolValue = value;
    }

    static void SetInt(SerializedObject obj, string propertyPath, int value)
    {
        var prop = obj.FindProperty(propertyPath);
        if (prop != null)
            prop.intValue = value;
    }

    static void SetFloat(SerializedObject obj, string propertyPath, float value)
    {
        var prop = obj.FindProperty(propertyPath);
        if (prop != null)
            prop.floatValue = value;
    }

    static void AddOrUpdate<T>(VolumeProfile profile, Action<T> setup) where T : VolumeComponent
    {
        if (profile == null)
            return;

        if (!profile.TryGet(out T component))
        {
            component = profile.Add<T>();
        }

        setup?.Invoke(component);
        component.active = true;
    }
}