#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class SSAOneButtonFix
{
    [MenuItem("SSA/One-Button Fix (URP + Materiais + Cena)")]
    public static void FixAll()
    {
        UniversalRenderPipelineAsset urp = null;
        foreach (var guid in AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset"))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
            if (urp) break;
        }
        if (urp)
        {
            GraphicsSettings.defaultRenderPipeline = urp;
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = urp;
            }
            QualitySettings.SetQualityLevel(current, true);
            Debug.Log($"[SSA OneButton] URP aplicado: {urp.name}");
        }
        else
        {
            Debug.LogWarning("[SSA OneButton] Nenhum UniversalRenderPipelineAsset encontrado.");
        }

        int changed = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Material"))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var mat  = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!mat) continue;
            string sh = mat.shader ? mat.shader.name : "Hidden/InternalErrorShader";
            Shader target = null;

            if (sh == "Hidden/InternalErrorShader" || sh == "Standard" || sh.StartsWith("Legacy Shaders/"))
                target = Shader.Find("Universal Render Pipeline/Lit");
            else if (sh.StartsWith("Unlit/") && !sh.StartsWith("Universal Render Pipeline"))
                target = Shader.Find("Universal Render Pipeline/Unlit");
            else if (sh.Contains("Particles") && !sh.Contains("Universal Render Pipeline"))
                target = Shader.Find("Universal Render Pipeline/Particles/Unlit");

            if (target != null)
            {
                var tmp = new Material(target);
                tmp.CopyPropertiesFromMaterial(mat);
                EditorUtility.CopySerialized(tmp, mat);
                Object.DestroyImmediate(tmp);
                changed++;
            }
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[SSA OneButton] Materiais do projeto convertidos: {changed}");

        int sceneChanged = 0;
        foreach (var r in Object.FindObjectsOfType<Renderer>(true))
        {
            var mats = r.sharedMaterials;
            bool any = false;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null || mats[i].shader == null || mats[i].shader.name == "Hidden/InternalErrorShader")
                {
                    mats[i] = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    any = true; sceneChanged++;
                }
            }
            if (any) r.sharedMaterials = mats;
        }
        Debug.Log($"[SSA OneButton] Renderers da cena corrigidos: {sceneChanged}");
    }
}
#endif
