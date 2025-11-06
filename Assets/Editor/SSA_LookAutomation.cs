using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_RENDER_PIPELINE_URP
using UnityEngine.Rendering.Universal;
#endif

public static class SSA_LookAutomation
{
    private const string MenuPath = "SSA/Automation/2) Aplicar Look SSA Completo";
    private const string PipelineFolder = "Assets/SSA_Kit/Rendering";
    private const string PipelineAssetPath = PipelineFolder + "/SSA_URP.asset";

    [MenuItem(MenuPath, priority = 3)]
    public static void Run()
    {
        using var progress = new ProgressScope("SSA Look Automation");

        progress.Report(0.05f, "Garantindo Universal Render Pipeline");
        if (!EnsureUniversalRenderPipeline())
        {
            return;
        }

        progress.Report(0.2f, "Instalando shader/material toon");
        SSA_CreateToonSetup.Install();

        progress.Report(0.35f, "Desativando canvases legados");
        SSA_CreateToonSetup.DisableLegacyCanvas();

        progress.Report(0.5f, "Aplicando toon em todos os renderers da cena");
        SSA_ToonifySceneNow.Run();

        progress.Report(0.65f, "Ajustando câmera principal");
        SSA_CreateToonSetup.CameraPreset();

        progress.Report(0.8f, "Aplicando pós-processamento SSA");
        SSA_CreateURPPostProcessing.CreatePost();

        progress.Report(0.95f, "Finalizando");
        Debug.Log("[SSA] Automação de look concluída. Materiais, câmera e pós atualizados.");
        EditorUtility.DisplayDialog("SSA Look", "Fluxo visual SSA aplicado com sucesso.", "OK");
    }

    private static bool EnsureUniversalRenderPipeline()
    {
        var pipeline = GraphicsSettings.defaultRenderPipeline ?? GraphicsSettings.defaultRenderPipeline ?? QualitySettings.renderPipeline;
#if UNITY_RENDER_PIPELINE_URP
        if (pipeline is UniversalRenderPipelineAsset)
        {
            EnsureDefine("UNITY_RENDER_PIPELINE_URP");
            return true;
        }

        Directory.CreateDirectory(PipelineFolder);
        var existing = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelineAssetPath);
        if (!existing)
        {
            existing = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            existing.name = Path.GetFileNameWithoutExtension(PipelineAssetPath);

            var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            renderer.name = existing.name + "_Renderer";

            AssetDatabase.CreateAsset(existing, PipelineAssetPath);
            AssetDatabase.AddObjectToAsset(renderer, existing);
            ConfigureRenderer(existing, renderer);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        GraphicsSettings.defaultRenderPipeline = existing;
        GraphicsSettings.defaultRenderPipeline = existing;
        ApplyToAllQualityLevels(existing);
        EnsureDefine("UNITY_RENDER_PIPELINE_URP");
        return true;
#else
        EditorUtility.DisplayDialog(
            "URP ausente",
            "Instale o pacote 'Universal Render Pipeline' via Package Manager antes de executar este fluxo.",
            "OK");
        return false;
#endif
    }

#if UNITY_RENDER_PIPELINE_URP
    private static void ConfigureRenderer(UniversalRenderPipelineAsset pipeline, UniversalRendererData renderer)
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

    private static void EnsureDefine(string symbol)
    {
        var activeTarget = EditorUserBuildSettings.activeBuildTarget;
        var group = BuildPipeline.GetBuildTargetGroup(activeTarget);
        if (group == BuildTargetGroup.Unknown)
        {
            group = EditorUserBuildSettings.selectedBuildTargetGroup;
        }
        if (group == BuildTargetGroup.Unknown)
        {
            group = BuildTargetGroup.Standalone;
        }

        var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
        var list = new List<string>(defines.Split(';').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)));
        if (list.Contains(symbol))
        {
            return;
        }

        list.Add(symbol);
        var combined = string.Join(";", list);
        PlayerSettings.SetScriptingDefineSymbolsForGroup(group, combined);
        Debug.Log($"[SSA] Define adicionado: {symbol} -> {combined}");
    }

    private readonly struct ProgressScope : System.IDisposable
    {
        private readonly string _title;

        public ProgressScope(string title)
        {
            _title = title;
            EditorUtility.DisplayProgressBar(_title, string.Empty, 0f);
        }

        public void Report(float progress, string message)
        {
            EditorUtility.DisplayProgressBar(_title, message, Mathf.Clamp01(progress));
        }

        public void Dispose()
        {
            EditorUtility.ClearProgressBar();
        }
    }
}
