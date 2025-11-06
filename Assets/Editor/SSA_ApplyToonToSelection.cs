using UnityEditor;
using UnityEngine;
using System.Linq;

public static class SSA_ApplyToonToSelection
{
    const string ShaderPath = "SSA/ToonMatcapOutlineRamp";

    [MenuItem("SSA/Setup/3) Aplicar Toon ao(s) Selecionado(s)")]
    public static void ApplyToSelection()
    {
        var shader = Shader.Find(ShaderPath);
        if (!shader) { Debug.LogError("[SSA] Shader não encontrado: " + ShaderPath); return; }

        string matDir = "Assets/SSA_Kit/Materials";
        if (!AssetDatabase.IsValidFolder("Assets/SSA_Kit")) AssetDatabase.CreateFolder("Assets", "SSA_Kit");
        if (!AssetDatabase.IsValidFolder(matDir)) AssetDatabase.CreateFolder("Assets/SSA_Kit", "Materials");

        var mat = AssetDatabase.LoadAssetAtPath<Material>(matDir + "/SSA_Toon_Default.mat");
        if (!mat)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, matDir + "/SSA_Toon_Default.mat");
        }

        var matcap = Resources.LoadAll<Texture2D>("Matcaps").FirstOrDefault();
        if (matcap) mat.SetTexture("_MatCapTex", matcap);

        var ramp = Resources.Load<Texture2D>("ToonRamps/skin_ramp");
        if (ramp) mat.SetTexture("_RampTex", ramp);

        foreach (var go in Selection.gameObjects)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                var arr = r.sharedMaterials;
                for (int i = 0; i < arr.Length; i++) arr[i] = mat;
                r.sharedMaterials = arr;
            }
        }

        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();
        Debug.Log("[SSA] Material aplicado aos selecionados.");
    }
}
