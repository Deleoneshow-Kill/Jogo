using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_RENDER_PIPELINE_URP
using UnityEngine.Rendering.Universal;
#endif

public static class SSA_CreateURPPipelineAsset
{
    private const string DefaultFolder = "Assets/Rendering/GeneratedURP";

    [MenuItem("SSA/Automation/1) Criar URP Pipeline Asset (Forward)", priority = 2)]
    public static void CreatePipeline()
    {
#if !UNITY_RENDER_PIPELINE_URP
        EditorUtility.DisplayDialog(
            "URP não disponível",
            "Instale o pacote Universal Render Pipeline via Package Manager antes de gerar o asset.",
            "OK");
        return;
#else
        Directory.CreateDirectory(DefaultFolder);
        var assetPath = EditorUtility.SaveFilePanelInProject(
            "Salvar URP Pipeline Asset",
            "URP-Pipeline",
            "asset",
            "Escolha o local para salvar o novo Universal Render Pipeline Asset.",
            DefaultFolder);

        if (string.IsNullOrEmpty(assetPath))
        {
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(assetPath))
        {
            if (!EditorUtility.DisplayDialog(
                    "Substituir asset",
                    "Já existe um Render Pipeline Asset nesse caminho. Deseja sobrescrevê-lo?",
                    "Sobrescrever",
                    "Cancelar"))
            {
                return;
            }
        }

        var pipeline = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
        pipeline.name = Path.GetFileNameWithoutExtension(assetPath);

        var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
        renderer.name = pipeline.name + "_Renderer";

        AssetDatabase.CreateAsset(pipeline, assetPath);
        AssetDatabase.AddObjectToAsset(renderer, pipeline);

        ConfigureRendererList(pipeline, renderer);

        EditorUtility.SetDirty(pipeline);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        GraphicsSettings.defaultRenderPipeline = pipeline;
        ApplyToAllQualityLevels(pipeline);

        Debug.Log($"[SSA] URP Pipeline criado em {assetPath} e aplicado ao projeto.");
        EditorUtility.DisplayDialog("URP configurado", "Universal Render Pipeline ativo com renderer Forward configurado.", "OK");
#endif
    }

#if UNITY_RENDER_PIPELINE_URP
    private static void ConfigureRendererList(UniversalRenderPipelineAsset pipeline, UniversalRendererData renderer)
    {
        var so = new SerializedObject(pipeline);
        var rendererList = so.FindProperty("m_RendererDataList");
        if (rendererList != null)
        {
            rendererList.arraySize = 1;
            rendererList.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
        }

        var defaultIndex = so.FindProperty("m_DefaultRendererIndex");
        if (defaultIndex != null)
        {
            defaultIndex.intValue = 0;
        }

        so.ApplyModifiedProperties();
    }

    private static void ApplyToAllQualityLevels(RenderPipelineAsset pipeline)
    {
        var currentLevel = QualitySettings.GetQualityLevel();
        for (int i = 0; i < QualitySettings.count; i++)
        {
            QualitySettings.SetQualityLevel(i, false);
            QualitySettings.renderPipeline = pipeline;
        }
        QualitySettings.SetQualityLevel(currentLevel, true);
    }
#endif
}
