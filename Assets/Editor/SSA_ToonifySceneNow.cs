using UnityEditor;
using UnityEngine;
using System.Linq;

public static class SSA_ToonifySceneNow
{
    [MenuItem("SSA/Fix/3) Forçar TOON em TODOS os renderers da cena")]
    public static void Run()
    {
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

        int renderers = 0, assigned = 0;
        foreach (var r in GameObject.FindObjectsOfType<Renderer>(true))
        {
            renderers++;
            var arr = r.sharedMaterials;
            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = mat;
                assigned++;
            }
            r.sharedMaterials = arr;
        }
        Debug.Log($"[SSA] Renderers: {renderers}. Materiais atribuídos: {assigned}. Tudo está no toon agora.");
    }
}
