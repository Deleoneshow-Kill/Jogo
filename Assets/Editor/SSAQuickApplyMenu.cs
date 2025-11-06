#if UNITY_EDITOR
using UnityEditor; using UnityEngine;
public static class SSAQuickApplyMenu{
    [MenuItem("SSA/Aplicar Look SSA (Auto)")]
    public static void Apply(){
        var applier = Object.FindObjectOfType<SSAQuickApply>();
        if (!applier){ var go = new GameObject("SSAQuickApply"); applier = go.AddComponent<SSAQuickApply>(); }
        applier.ApplyNow(); Debug.Log("SSA Look aplicado via menu.");
    }
}
#endif
