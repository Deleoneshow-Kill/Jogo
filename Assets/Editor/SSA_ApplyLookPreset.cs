using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Linq;

public static class SSA_ApplyLookPreset
{
    [MenuItem("SSA/Look/Apply SSA Preset (Athena Garden)")]
    public static void Apply()
    {
        var key = GameObject.Find("SSA_KeyLight") ?? new GameObject("SSA_KeyLight");
        var lk = key.GetComponent<Light>() ?? key.AddComponent<Light>();
        lk.type = LightType.Directional;
        lk.intensity = 1.2f;
        lk.color = new Color(1.0f, 0.95f, 0.85f);
        key.transform.rotation = Quaternion.Euler(42f, -30f, 0f);

        var rim = GameObject.Find("SSA_RimLight") ?? new GameObject("SSA_RimLight");
        var lr = rim.GetComponent<Light>() ?? rim.AddComponent<Light>();
        lr.type = LightType.Directional;
        lr.intensity = 0.55f;
        lr.color = new Color(0.75f, 0.85f, 1.0f);
        rim.transform.rotation = Quaternion.Euler(18f, 155f, 0f);

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.60f, 0.68f, 0.85f);
        RenderSettings.ambientEquatorColor = new Color(0.45f, 0.52f, 0.66f);
        RenderSettings.ambientGroundColor = new Color(0.30f, 0.33f, 0.40f);

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 18f;
        RenderSettings.fogEndDistance = 90f;
        RenderSettings.fogColor = new Color(0.52f, 0.60f, 0.78f, 1f);

        string dir = "Assets/SSA_Kit/Post";
        if (!AssetDatabase.IsValidFolder("Assets/SSA_Kit")) AssetDatabase.CreateFolder("Assets", "SSA_Kit");
        if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder("Assets/SSA_Kit", "Post");

        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(dir + "/SSA_GlobalProfile.asset");
        if (!profile)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, dir + "/SSA_GlobalProfile.asset");
        }

        var tm = profile.components.OfType<Tonemapping>().FirstOrDefault() ?? profile.Add<Tonemapping>(true);
        tm.mode.Override(TonemappingMode.ACES);

        var bloom = profile.components.OfType<Bloom>().FirstOrDefault() ?? profile.Add<Bloom>(true);
        bloom.intensity.Override(0.35f);
        bloom.threshold.Override(1.1f);

        var vig = profile.components.OfType<Vignette>().FirstOrDefault() ?? profile.Add<Vignette>(true);
        vig.intensity.Override(0.22f);
        vig.smoothness.Override(0.9f);

        var ca = profile.components.OfType<ColorAdjustments>().FirstOrDefault() ?? profile.Add<ColorAdjustments>(true);
        ca.saturation.Override(15f);
        ca.contrast.Override(30f);
        ca.postExposure.Override(0.1f);

        var smh = profile.components.OfType<ShadowsMidtonesHighlights>().FirstOrDefault() ?? profile.Add<ShadowsMidtonesHighlights>(true);
        smh.shadows.Override(new Vector4(0.90f, 0.95f, 1.05f, 0.0f));
        smh.midtones.Override(new Vector4(1.02f, 1.02f, 1.02f, 0.0f));
        smh.highlights.Override(new Vector4(1.06f, 1.02f, 0.96f, 0.0f));

        var dof = profile.components.OfType<DepthOfField>().FirstOrDefault() ?? profile.Add<DepthOfField>(true);
        dof.mode.Override(DepthOfFieldMode.Gaussian);
        dof.gaussianStart.Override(6f);
        dof.gaussianEnd.Override(20f);
        dof.highQualitySampling.Override(true);

        var volGO = GameObject.Find("SSA_GlobalPost") ?? new GameObject("SSA_GlobalPost");
        var volume = volGO.GetComponent<Volume>() ?? volGO.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 50;
        volume.profile = profile;

        var cam = Camera.main;
        if (cam)
        {
            cam.fieldOfView = 24f;
            cam.transform.position = new Vector3(-7.5f, 5.0f, -9.5f);
            cam.transform.rotation = Quaternion.Euler(12f, 25f, 0f);

            var data = cam.GetUniversalAdditionalCameraData();
            if (data != null)
            {
                data.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
                data.renderPostProcessing = true;
            }
        }

        var shader = Shader.Find("SSA/ToonMatcapOutlineRamp");
        if (shader)
        {
            var mats = AssetDatabase.FindAssets("t:Material")
                .Select(g => AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(m => m != null && m.shader == shader)
                .ToArray();

            foreach (var m in mats)
            {
                m.SetFloat("_MatCapIntensity", 0.55f);
                m.SetColor("_ShadowColor", new Color(0.28f, 0.32f, 0.45f, 1f));
                m.SetColor("_RimColor", new Color(0.75f, 0.85f, 1.0f, 1f));
                m.SetFloat("_RimPower", 2.2f);
                if (m.HasProperty("_OutlineWidth")) m.SetFloat("_OutlineWidth", 0.0018f);
                EditorUtility.SetDirty(m);
            }
            AssetDatabase.SaveAssets();
        }

        Debug.Log("[SSA] Preset Athena aplicado.");
    }
}
