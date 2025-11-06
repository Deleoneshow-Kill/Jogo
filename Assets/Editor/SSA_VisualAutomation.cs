using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_RENDER_PIPELINE_URP
using JogoSSA.Editor;
#endif

public static class SSA_VisualAutomation
{
    private const string MenuPath = "SSA/Automation/0) Recuperar Visual SSA (Tudo)";

    [MenuItem(MenuPath, priority = 1)]
    public static void Run()
    {
        using var scope = new ProgressScope("SSA Visual Automation");

        scope.Report(0.05f, "Executando Pânico Fix");
        SSA_PanicFix.PanicFix();

        scope.Report(0.2f, "Garantindo URP ativo");
        var pipeline = EnsureRenderPipeline();
        if (pipeline == null)
        {
            EditorUtility.DisplayDialog("URP ausente", "Não foi possível localizar ou gerar um Universal Render Pipeline Asset. Configure manualmente em Project Settings > Graphics.", "OK");
            return;
        }

        scope.Report(0.35f, "Aplicando materiais toon");
        ApplyToonByFolderDialog();

        scope.Report(0.55f, "Executando kit visual SSA");
        SSA_FullSetup.RunAll();

        scope.Report(0.7f, "Criando HUD SSA limpo");
        SSA_CreateHUD.CreateHUD();

        scope.Report(0.9f, "Finalizando");
        Debug.Log("[SSA] Automação visual completa concluída.");
    }

    private static RenderPipelineAsset EnsureRenderPipeline()
    {
        var pipeline = GraphicsSettings.currentRenderPipeline;
        if (pipeline == null)
        {
            pipeline = FindRenderPipelineAsset();
        }

#if UNITY_RENDER_PIPELINE_URP
        if (pipeline == null)
        {
            SetupSSA.Execute();
            pipeline = GraphicsSettings.currentRenderPipeline;
        }
#endif

        if (pipeline != null)
        {
            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
            EnsureDefine("UNITY_RENDER_PIPELINE_URP");
        }

        return pipeline;
    }

    private static RenderPipelineAsset FindRenderPipelineAsset()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:RenderPipelineAsset"))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(path);
            if (asset != null)
            {
                return asset;
            }
        }

        return null;
    }

    private static void ApplyToonByFolderDialog()
    {
        var absolute = EditorUtility.OpenFolderPanel("Selecione a pasta com prefabs/personagens", Application.dataPath, string.Empty);
        if (string.IsNullOrEmpty(absolute))
        {
            Debug.Log("[SSA] Etapa de pasta ignorada (cancelada).");
            return;
        }

        var relative = ToAssetRelativePath(absolute);
        if (string.IsNullOrEmpty(relative))
        {
            EditorUtility.DisplayDialog("Pasta inválida", "Escolha uma pasta dentro de 'Assets'.", "OK");
            return;
        }

        var folderAsset = AssetDatabase.LoadAssetAtPath<Object>(relative);
        if (!folderAsset)
        {
            EditorUtility.DisplayDialog("Pasta não encontrada", "Não foi possível carregar a pasta selecionada.", "OK");
            return;
        }

        var previousSelection = Selection.objects;
        Selection.activeObject = folderAsset;
        SSA_ApplyToonByFolder.ApplyToFolder();
        Selection.objects = previousSelection;
    }

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

    private static string ToAssetRelativePath(string absolutePath)
    {
        if (string.IsNullOrEmpty(absolutePath))
        {
            return string.Empty;
        }

        absolutePath = absolutePath.Replace('\\', '/');
        var dataPath = Application.dataPath.Replace('\\', '/');
        if (!absolutePath.StartsWith(dataPath))
        {
            return string.Empty;
        }

        return "Assets" + absolutePath.Substring(dataPath.Length);
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
