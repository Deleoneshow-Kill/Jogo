using UnityEditor;
using UnityEngine;

public static class SSA_FullSetup
{
    [MenuItem("SSA/Setup/0) Rodar Tudo (Kit Visual)")]
    public static void RunAll()
    {
        SSA_GenerateRamps.Generate();
        SSA_CreateURPPostProcessing.CreatePost();
        SSA_CreateStageScene.CreateScene();
        EditorUtility.DisplayDialog("SSA Kit", "Kit visual aplicado. Use 'SSA/Setup/3) Aplicar Toon...' nos personagens.", "OK");
    }
}
