using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class SSA_CreateURPPostProcessing
{
    [MenuItem("SSA/Setup/2) Criar Pós-Processamento (ACES)")]
    public static void CreatePost()
    {
        string dir = "Assets/SSA_Kit/Post";
        if (!AssetDatabase.IsValidFolder("Assets/SSA_Kit")) AssetDatabase.CreateFolder("Assets", "SSA_Kit");
        if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder("Assets/SSA_Kit", "Post");

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, dir + "/SSA_GlobalProfile.asset");

        var tonemap = profile.Add<Tonemapping>(true);
        tonemap.mode.Override(TonemappingMode.ACES);

        var bloom = profile.Add<Bloom>(true);
        bloom.intensity.Override(0.2f);
        bloom.threshold.Override(1.0f);

        var vig = profile.Add<Vignette>(true);
        vig.intensity.Override(0.18f);
        vig.smoothness.Override(0.9f);

        var ca = profile.Add<ColorAdjustments>(true);
        ca.contrast.Override(10f);
        ca.saturation.Override(5f);
        ca.postExposure.Override(0f);

        AssetDatabase.SaveAssets();

        var volGO = GameObject.Find("SSA_GlobalPost") ?? new GameObject("SSA_GlobalPost");
        var volume = volGO.GetComponent<Volume>() ?? volGO.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 50;
        volume.profile = profile;

        Debug.Log("[SSA] Volume Global criado com perfil ACES/Bloom/Vignette/ColorAdjustments.");
    }
}
