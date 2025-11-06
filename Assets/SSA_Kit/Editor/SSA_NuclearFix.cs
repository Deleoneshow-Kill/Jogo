using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Linq;

public static class SSA_NuclearFix
{
    [MenuItem("SSA/Fix/0) Nuclear: URP + Toon + HUD Limpo (Cena Atual)")]
    public static void Run()
    {
        // 1) URP (se houver asset no projeto)
        if (!(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset))
        {
            var guid = AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset").FirstOrDefault();
            if (!string.IsNullOrEmpty(guid))
            {
                var urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetDatabase.GUIDToAssetPath(guid));
                GraphicsSettings.defaultRenderPipeline = urp;
                QualitySettings.renderPipeline = urp;
                Debug.Log("[SSA] URP aplicado automaticamente.");
            }
            else
            {
                Debug.LogWarning("[SSA] URP não está ativo e nenhum asset URP foi encontrado. Ative URP em Project Settings → Graphics/Quality.");
            }
        }

        // 2) Material Toon default
        if (!AssetDatabase.IsValidFolder("Assets/SSA_Kit")) AssetDatabase.CreateFolder("Assets", "SSA_Kit");
        if (!AssetDatabase.IsValidFolder("Assets/SSA_Kit/Materials")) AssetDatabase.CreateFolder("Assets/SSA_Kit", "Materials");

        var shader = Shader.Find("SSA/ToonMatcapOutlineRamp");
        if (!shader) shader = Shader.Find("Universal Render Pipeline/Lit"); // fallback para não ficar rosa
        var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/SSA_Kit/Materials/SSA_Toon_Default.mat");
        if (!mat) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, "Assets/SSA_Kit/Materials/SSA_Toon_Default.mat"); }

        var ramp = Resources.Load<Texture2D>("ToonRamps/cloth_dark_ramp");
        var matcap = Resources.Load<Texture2D>("Matcaps/Matcap_Default");
        if (mat.HasProperty("_RampTex") && ramp) mat.SetTexture("_RampTex", ramp);
        if (mat.HasProperty("_MatCapTex") && matcap) mat.SetTexture("_MatCapTex", matcap);
        if (mat.HasProperty("_MatCapIntensity")) mat.SetFloat("_MatCapIntensity", 0.55f);
        if (mat.HasProperty("_RimPower")) mat.SetFloat("_RimPower", 2.0f);
        EditorUtility.SetDirty(mat);

        // 3) Aplicar Toon: objetos críticos + qualquer renderer com shader quebrado
        string[] targets = { "ArenaFloor", "Column_L", "Column_R", "SSA_StageSample", "Backdrop" };
        int changed = 0;

        foreach (var name in targets)
        {
            var go = GameObject.Find(name);
            if (!go) continue;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                var arr = r.sharedMaterials;
                for (int i = 0; i < arr.Length; i++) arr[i] = mat;
                r.sharedMaterials = arr; changed++;
            }
        }

        foreach (var r in Object.FindObjectsOfType<Renderer>(true))
        {
            var arr = r.sharedMaterials; bool replace = false;
            for (int i = 0; i < arr.Length; i++)
            {
                var sh = arr[i] ? arr[i].shader : null;
                if (arr[i] == null || sh == null || sh.name == "Hidden/InternalErrorShader")
                { arr[i] = mat; replace = true; }
            }
            if (replace) { r.sharedMaterials = arr; changed++; }
        }
        Debug.Log($"[SSA] Materiais aplicados em {changed} renderers.");

        // 4) Desligar Canvas legado (mancha some)
        string[] legacy = { "ArenaCanvas","CombatLogCanvas","TeamSelectCanvas","GachaCanvas","ReplayCanvas","FloatingCanvas","CanvasHUD","HUD_Bootstrap","Game","AutoScreenshot","SSA_StageSample" };
        int off = 0;
        foreach (var c in Object.FindObjectsOfType<Canvas>(true))
        {
            if (legacy.Contains(c.gameObject.name) && c.gameObject.activeSelf) { c.gameObject.SetActive(false); off++; }
        }
        Debug.Log($"[SSA] Canvas legados desativados: {off}. Mantenha apenas 'SSA_HUD'.");

        // 5) Câmera + pós + fog
        var cam = Camera.main;
        if (cam)
        {
            cam.fieldOfView = 24f;
            cam.transform.position = new Vector3(-7.5f, 5f, -9.5f);
            cam.transform.rotation = Quaternion.Euler(12f, 25f, 0f);
            var data = cam.GetUniversalAdditionalCameraData();
            if (data != null)
            { data.antialiasing = AntialiasingMode.FastApproximateAntialiasing; data.renderPostProcessing = true; }
        }

        if (!GameObject.Find("SSA_GlobalPost"))
        {
            var volGO = new GameObject("SSA_GlobalPost");
            var vol = volGO.AddComponent<Volume>(); vol.isGlobal = true; vol.priority = 20;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var tm = profile.Add<Tonemapping>(true); tm.mode.value = TonemappingMode.ACES;
            var bloom = profile.Add<Bloom>(true); bloom.intensity.value = 0.35f; bloom.threshold.value = 1.1f;
            vol.profile = profile;

            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 18f; RenderSettings.fogEndDistance = 90f;
            RenderSettings.fogColor = new Color(0.52f, 0.60f, 0.78f, 1f);
        }

        Debug.Log("<color=#83ff8a>[SSA] Nuclear Fix finalizado. Sem rosa, sem mancha, com look básico SSA.</color>");
    }
}
