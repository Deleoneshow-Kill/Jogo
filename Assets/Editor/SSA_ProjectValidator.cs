using UnityEditor;
using UnityEngine;
using System.IO;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class SSA_ProjectValidator
{
    [MenuItem("SSA/Validate/Run Checklist")]
    public static void Run()
    {
        var ok = true;

        if (!AssetDatabase.IsValidFolder("Assets/TextMesh Pro") ||
            !AssetDatabase.IsValidFolder("Assets/TextMesh Pro/Resources"))
        { Debug.LogError("TMP Essentials ausente em Assets/TextMesh Pro/Resources/."); ok = false; }

        var rp = GraphicsSettings.currentRenderPipeline;
        if (!(rp is UniversalRenderPipelineAsset))
        { Debug.LogError("URP não está ativo em Project Settings > Graphics."); ok = false; }

        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/VFX"))
            Debug.LogWarning("Crie Assets/Resources/VFX/ para seus prefabs de efeitos.");

        int missingMeta = 0;
        var root = Application.dataPath;
        foreach (var path in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            if (path.EndsWith(".meta")) continue;
            if (path.Contains(Path.DirectorySeparatorChar + "StreamingAssets" + Path.DirectorySeparatorChar)) continue;
            var meta = path + ".meta";
            if (!File.Exists(meta)) { Debug.LogError("Faltando .meta: " + path.Replace(root, "Assets")); missingMeta++; }
        }
        if (missingMeta > 0) ok = false;

        if (ok) Debug.Log("<color=#84f489>[SSA] Checklist OK</color>");
        else Debug.LogWarning("[SSA] Checklist encontrou problemas. Corrija antes de empacotar.");
    }
}
