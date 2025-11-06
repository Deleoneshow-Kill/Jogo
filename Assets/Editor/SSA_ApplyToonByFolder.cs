using UnityEditor;
using UnityEngine;

public static class SSA_ApplyToonByFolder
{
    const string ShaderPath = "SSA/ToonMatcapOutlineRamp";

    [MenuItem("SSA/Setup/3b) Aplicar Toon a todos da pasta (selecionada)")]
    public static void ApplyToFolder()
    {
        var obj = Selection.activeObject;
        if (obj == null) { Debug.LogError("Selecione uma pasta no Project."); return; }
        var folder = AssetDatabase.GetAssetPath(obj);
        if (!AssetDatabase.IsValidFolder(folder)) { Debug.LogError("Seleção não é uma pasta."); return; }

        var shader = Shader.Find(ShaderPath);
        if (!shader) { Debug.LogError("[SSA] Shader não encontrado: " + ShaderPath); return; }

        string matDir = "Assets/SSA_Kit/Materials";
        if (!AssetDatabase.IsValidFolder("Assets/SSA_Kit")) AssetDatabase.CreateFolder("Assets", "SSA_Kit");
        if (!AssetDatabase.IsValidFolder(matDir)) AssetDatabase.CreateFolder("Assets/SSA_Kit", "Materials");
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matDir + "/SSA_Toon_Default.mat");
        if (!mat) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, matDir + "/SSA_Toon_Default.mat"); }

        var guids = AssetDatabase.FindAssets("t:prefab", new[] { folder });
        int changed = 0;

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var root = PrefabUtility.LoadPrefabContents(path);
            if (!root) continue;

            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                var arr = r.sharedMaterials;
                for (int i = 0; i < arr.Length; i++) arr[i] = mat;
                r.sharedMaterials = arr;
            }

            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
            changed++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[SSA] Material aplicado em {changed} prefabs na pasta: {folder}");
    }
}
