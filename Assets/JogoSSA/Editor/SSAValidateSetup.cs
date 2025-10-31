#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class SSAValidate
{
    private const string RampFolder = "Assets/Shaders/Toon/Ramps";
    private const string GlobalProfilePath = "Assets/JogoSSA/Profiles/SSA_GlobalProfile.asset";

    [MenuItem("Jogo/Validate SSA Setup", priority = 30)]
    public static void Validate()
    {
        var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urp == null)
        {
            Debug.LogError("URP NÃO está ativo. Atribua um UniversalRenderPipelineAsset em Project Settings > Graphics.");
        }

        if (PlayerSettings.colorSpace != ColorSpace.Linear)
        {
            PlayerSettings.colorSpace = ColorSpace.Linear;
            Debug.Log("Color Space -> Linear.");
        }

        FixRampImport(RampFolder);
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Jogo/Apply SSA Post/Lighting", priority = 31)]
    public static void ApplyPostAndLighting()
    {
        var gv = GameObject.Find("SSA Global Volume");
        if (!gv)
        {
            gv = new GameObject("SSA Global Volume");
        }

        var volume = gv.GetComponent<Volume>();
        if (!volume)
        {
            volume = gv.AddComponent<Volume>();
        }
        volume.isGlobal = true;

        Directory.CreateDirectory(Path.GetDirectoryName(GlobalProfilePath));
        var profile = volume.sharedProfile;
        if (!profile)
        {
            profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(GlobalProfilePath);
            if (!profile)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, GlobalProfilePath);
            }
            volume.sharedProfile = profile;
        }

        AddOrGet(profile, out Tonemapping tonemap);
        tonemap.mode.value = TonemappingMode.ACES;

        AddOrGet(profile, out Bloom bloom);
        bloom.threshold.value = 1.2f;
        bloom.intensity.value = 0.5f;

        AddOrGet(profile, out ColorAdjustments ca);
        ca.postExposure.value = 0.2f;
        ca.contrast.value = 10f;
        ca.saturation.value = 8f;

        var light = Object.FindObjectOfType<Light>();
        if (light == null || light.type != LightType.Directional)
        {
            var go = new GameObject("SSA Sun Light");
            light = go.AddComponent<Light>();
            light.type = LightType.Directional;
        }
        light.intensity = 1.2f;
        light.color = Color.white;
        light.shadows = LightShadows.Soft;
        light.shadowBias = 0.05f;
        light.shadowNormalBias = 0.4f;
        light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        var cam = Camera.main;
        if (cam != null)
        {
            cam.allowHDR = true;
            var ucam = cam.GetUniversalAdditionalCameraData();
            if (ucam != null)
            {
                ucam.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
                ucam.renderPostProcessing = true;
            }
            cam.fieldOfView = 42f;
        }

        Debug.Log("Post/Lighting aplicados (ACES+Bloom+CA, Sol, Câmera HDR/FXAA, FOV 42).");
    }

    private static void FixRampImport(string folder)
    {
        if (!AssetDatabase.IsValidFolder(folder))
        {
            Debug.LogWarning($"Pasta de ramps não encontrada: {folder}");
            return;
        }

        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer))
            {
                continue;
            }

            bool changed = false;
            if (importer.sRGBTexture)
            {
                importer.sRGBTexture = false;
                changed = true;
            }
            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                changed = true;
            }
            if (importer.filterMode != FilterMode.Point)
            {
                importer.filterMode = FilterMode.Point;
                changed = true;
            }
            if (importer.wrapMode != TextureWrapMode.Clamp)
            {
                importer.wrapMode = TextureWrapMode.Clamp;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
                Debug.Log($"Ramp ajustada: {path}");
            }
        }
        Debug.Log("Ramps OK (sRGB off, Point, Clamp, Uncompressed).");
    }

    private static void AddOrGet<T>(VolumeProfile profile, out T component) where T : VolumeComponent
    {
        if (!profile.TryGet(out component))
        {
            component = profile.Add<T>(true);
        }
    }
}
#endif
