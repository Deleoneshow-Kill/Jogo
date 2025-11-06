using UnityEditor; using UnityEngine;
public static class SSA_ToonApplyFolder
{
    [MenuItem("SSA/Characters/3) Aplicar SSA_Toon_Default em toda a pasta")]
    public static void ApplyToonInFolder(){
        var obj=Selection.activeObject; if(!obj){ Debug.LogError("Selecione uma pasta."); return; }
        var path=AssetDatabase.GetAssetPath(obj); if(!AssetDatabase.IsValidFolder(path)){ Debug.LogError("Seleção não é pasta."); return; }
        var mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/SSA_Kit/Materials/SSA_Toon_Default.mat"); if(!mat){ Debug.LogError("Material SSA_Toon_Default não encontrado. Rode Setup/1."); return; }
        var guids=AssetDatabase.FindAssets("t:Prefab t:Model t:GameObject", new[]{path}); int n=0;
        foreach(var g in guids){ var p=AssetDatabase.GUIDToAssetPath(g); var go=AssetDatabase.LoadAssetAtPath<GameObject>(p); if(!go) continue;
            foreach(var r in go.GetComponentsInChildren<Renderer>(true)){ var arr=r.sharedMaterials; for(int i=0;i<arr.Length;i++) arr[i]=mat; r.sharedMaterials=arr; n++; } }
        Debug.Log($"[SSA] Toon aplicado em {n} renderers dentro de {path}."); }
}