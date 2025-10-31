#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class AssignURP
{
    [MenuItem("Jogo/Ativar URP (Assign)", priority = 35)]
    public static void Assign()
    {
        var guids = AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset");
        if (guids.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "URP",
                "Nenhum UniversalRenderPipelineAsset encontrado.\nCrie um: Assets > Create > Rendering > URP Asset (Forward Renderer).",
                "OK");
            return;
        }

        var path = AssetDatabase.GUIDToAssetPath(guids[0]);
        var urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
        if (!urp)
        {
            EditorUtility.DisplayDialog("URP", "Falha ao carregar o URP Asset.", "OK");
            return;
        }

        GraphicsSettings.defaultRenderPipeline = urp;
        for (int i = 0; i < QualitySettings.names.Length; i++)
        {
            QualitySettings.SetQualityLevel(i, false);
            QualitySettings.renderPipeline = urp;
        }
        Debug.Log($"URP ativado: {urp.name}. Agora rode Jogo > SSA: Corrigir Cena Agora.");
    }
}
#endif
