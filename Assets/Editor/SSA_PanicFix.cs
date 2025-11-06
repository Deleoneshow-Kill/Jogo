using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.IO;
using System.Linq;

public static class SSA_PanicFix
{
    [MenuItem("SSA/Fix/1) PÂNICO: consertar rosa + mancha agora")]
    public static void PanicFix()
    {
        if (!(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset))
        {
            Debug.LogError("[SSA] URP NÃO ESTÁ ATIVO. Vá em Project Settings > Graphics e selecione um UniversalRenderPipelineAsset.");
        }

        EnsureRamps();
        EnsureMatcap();

        var shader = Shader.Find("SSA/ToonMatcapOutlineRamp");
        if (!shader) { Debug.LogError("[SSA] Shader SSA/ToonMatcapOutlineRamp não encontrado."); return; }

        string matDir = "Assets/SSA_Kit/Materials";
        if (!AssetDatabase.IsValidFolder("Assets/SSA_Kit")) AssetDatabase.CreateFolder("Assets", "SSA_Kit");
        if (!AssetDatabase.IsValidFolder(matDir)) AssetDatabase.CreateFolder("Assets/SSA_Kit", "Materials");
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matDir + "/SSA_Toon_Default.mat");
        if (!mat) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, matDir + "/SSA_Toon_Default.mat"); }

        var ramp = Resources.Load<Texture2D>("ToonRamps/skin_ramp");
        if (ramp) mat.SetTexture("_RampTex", ramp);
        var matcap = Resources.LoadAll<Texture2D>("Matcaps").FirstOrDefault();
        if (matcap) mat.SetTexture("_MatCapTex", matcap);

        int renderers = 0, materialsFixed = 0;
        foreach (var r in GameObject.FindObjectsOfType<Renderer>(true))
        {
            renderers++;
            var arr = r.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < arr.Length; i++)
            {
                var sh = arr[i] ? arr[i].shader : null;
                if (arr[i] == null || (sh && sh.name == "Hidden/InternalErrorShader") || (sh && sh.name == "Standard"))
                {
                    arr[i] = mat;
                    materialsFixed++;
                    changed = true;
                }
            }
            if (changed) r.sharedMaterials = arr;
        }
        Debug.Log($"[SSA] Renderers varridos: {renderers}. Materiais corrigidos: {materialsFixed}.");

        string[] names = { "Backdrop", "Block", "Overlay", "Mask", "Painel", "Panel", "SSA_StageSample" };
        int hidden = 0;
        foreach (var go in GameObject.FindObjectsOfType<RectTransform>(true))
        {
            var n = go.name.ToLower();
            if (names.Any(x => n.Contains(x.ToLower())))
            {
                var img = go.GetComponent<UnityEngine.UI.Image>();
                if (img && img.color.a > 0.2f && go.rect.width > 300 && go.rect.height > 200)
                {
                    img.enabled = false;
                    hidden++;
                }
            }
        }
        string[] rootsToDisable = { "ReplayCanvas", "ArenaCanvas", "TeamSelectCanvas", "FloatingCanvas", "CombatLogCanvas" };
        foreach (var rootName in rootsToDisable)
        {
            var root = GameObject.Find(rootName);
            if (root) { root.SetActive(false); hidden++; }
        }
        Debug.Log($"[SSA] Painéis/overlays ocultados: {hidden}.");

        if (!GameObject.Find("Directional Light Key"))
        {
            var key = new GameObject("Directional Light Key");
            var ldKey = key.AddComponent<Light>();
            ldKey.type = LightType.Directional;
            ldKey.intensity = 1.1f;
            key.transform.rotation = Quaternion.Euler(50, -30, 0);
        }
        if (!GameObject.Find("Directional Light Rim"))
        {
            var rim = new GameObject("Directional Light Rim");
            var ldRim = rim.AddComponent<Light>();
            ldRim.type = LightType.Directional;
            ldRim.intensity = 0.4f;
            ldRim.color = new Color(0.8f, 0.9f, 1f);
            rim.transform.rotation = Quaternion.Euler(20, 150, 0);
        }

        if (!GameObject.Find("SSA_GlobalPost"))
        {
            SSA_CreateURPPostProcessing.CreatePost();
        }

        RenderSettings.skybox = AssetDatabase.GetBuiltinExtraResource<Material>("SkyboxProcedural.mat");
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.6f, 0.7f, 0.9f);
        RenderSettings.ambientEquatorColor = new Color(0.45f, 0.5f, 0.6f);
        RenderSettings.ambientGroundColor = new Color(0.3f, 0.32f, 0.35f);

        Debug.Log("<color=#84f489>[SSA] PÂNICO: ajustes aplicados. Se ainda ver MAGENTA, ative URP e rode 'Setup/3b' na pasta dos personagens.</color>");
    }

    static void EnsureRamps()
    {
        string dir = "Assets/Resources/ToonRamps";
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder("Assets/Resources", "ToonRamps");

        CreateRamp(Path.Combine(dir, "skin_ramp.png"), new float[] { 0.0f, 0.55f, 0.82f, 1.0f });
        CreateRamp(Path.Combine(dir, "metal_ramp.png"), new float[] { 0.0f, 0.4f, 0.9f, 1.0f });
        CreateRamp(Path.Combine(dir, "cloth_light_ramp.png"), new float[] { 0.0f, 0.5f, 0.8f, 1.0f });
        CreateRamp(Path.Combine(dir, "cloth_dark_ramp.png"), new float[] { 0.0f, 0.35f, 0.7f, 1.0f });
        AssetDatabase.Refresh();
    }

    static void CreateRamp(string path, float[] bands)
    {
        int w = 256, h = 1;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false, true);
        for (int x = 0; x < w; x++)
        {
            float u = x / (w - 1f);
            float v = StepBands(u, bands);
            tex.SetPixel(x, 0, new Color(v, v, v, 1));
        }
        tex.Apply();
        var bytes = tex.EncodeToPNG();
        File.WriteAllBytes(path, bytes);
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path);
        var ti = (TextureImporter)AssetImporter.GetAtPath(path);
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.mipmapEnabled = false;
        ti.wrapMode = TextureWrapMode.Clamp;
        ti.SaveAndReimport();
    }

    static float StepBands(float u, float[] b)
    {
        for (int i = 1; i < b.Length; i++)
            if (u < b[i]) return (i - 1) / (float)(b.Length - 1);
        return 1f;
    }

    static void EnsureMatcap()
    {
        string dir = "Assets/Resources/Matcaps";
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder("Assets/Resources", "Matcaps");

        var existing = AssetDatabase.FindAssets("t:Texture2D", new[] { dir });
        if (existing != null && existing.Length > 0) return;

        int size = 256;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
        tex.wrapMode = TextureWrapMode.Clamp;
        var center = (size - 1) / 2f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float nx = (x - center) / center;
            float ny = (y - center) / center;
            float r = Mathf.Sqrt(nx*nx + ny*ny);
            float rim = Mathf.Clamp01(1f - r);
            float spec = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(r - 0.5f) * 2f), 8f);
            var c = new Color(0.65f + 0.2f * rim + 0.15f * spec, 0.65f + 0.2f * rim, 0.7f + 0.2f * rim, 1f);
            if (r > 1f) c.a = 0;
            tex.SetPixel(x, y, c);
        }
        tex.Apply(false);
        var path = Path.Combine(dir, "Matcap_Default.png");
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);
        var ti = (TextureImporter)AssetImporter.GetAtPath(path);
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.mipmapEnabled = false;
        ti.wrapMode = TextureWrapMode.Clamp;
        ti.sRGBTexture = true;
        ti.SaveAndReimport();
        Debug.Log("[SSA] Matcap_Default gerado em Resources/Matcaps.");
    }
}
