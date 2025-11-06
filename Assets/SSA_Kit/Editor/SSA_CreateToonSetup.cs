using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using System.Linq;

public static class SSA_CreateToonSetup
{
    [MenuItem("SSA/Setup/1) Instalar Toon Shader + Material Default")]
    public static void Install()
    {
        string matDir = "Assets/SSA_Kit/Materials";
        if (!AssetDatabase.IsValidFolder("Assets/SSA_Kit")) AssetDatabase.CreateFolder("Assets", "SSA_Kit");
        if (!AssetDatabase.IsValidFolder(matDir)) AssetDatabase.CreateFolder("Assets/SSA_Kit", "Materials");

        var shader = Shader.Find("SSA/ToonMatcapOutlineRamp");
        if (!shader) { Debug.LogError("Shader SSA/ToonMatcapOutlineRamp não encontrado."); return; }

        var mat = AssetDatabase.LoadAssetAtPath<Material>(matDir + "/SSA_Toon_Default.mat");
        if (!mat) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, matDir + "/SSA_Toon_Default.mat"); }

        var ramp = Resources.Load<Texture2D>("ToonRamps/skin_ramp");
        var mc = Resources.Load<Texture2D>("Matcaps/Matcap_Default");
        if (ramp && mat.HasProperty("_RampTex")) mat.SetTexture("_RampTex", ramp);
        if (mc && mat.HasProperty("_MatCapTex")) mat.SetTexture("_MatCapTex", mc);
        if (mat.HasProperty("_MatCapIntensity")) mat.SetFloat("_MatCapIntensity", 0.55f);
        if (mat.HasProperty("_RimPower")) mat.SetFloat("_RimPower", 2.2f);
        EditorUtility.SetDirty(mat); AssetDatabase.SaveAssets();
        Selection.activeObject = mat;
        Debug.Log("[SSA] Toon instalado em Assets/SSA_Kit/Materials/SSA_Toon_Default.mat");
    }

    [MenuItem("SSA/Fix/Forçar TOON em TODOS os renderers da cena")]
    public static void ForceToonScene(){
        var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/SSA_Kit/Materials/SSA_Toon_Default.mat");
        if (!mat) { Debug.LogError("Crie o material primeiro (Setup/1)."); return; }
        int n=0;
        foreach(var r in Object.FindObjectsOfType<Renderer>(true)){
            var arr=r.sharedMaterials;
            for(int i=0;i<arr.Length;i++) arr[i]=mat;
            r.sharedMaterials=arr; n++;
        }
        Debug.Log($"[SSA] Toon aplicado em {n} renderers.");
    }

    [MenuItem("SSA/UI/Desativar todos os Canvas exceto 'SSA_HUD'")]
    public static void DisableLegacyCanvas(){
        int off=0;
        foreach(var c in Object.FindObjectsOfType<Canvas>(true)){
            if(c.gameObject.name=="SSA_HUD") continue;
            if(c.gameObject.activeSelf){ c.gameObject.SetActive(false); off++; }
        }
        Debug.Log($"[SSA] Canvas desativados: {off}. Mantido 'SSA_HUD'.");
    }

    [MenuItem("SSA/Camera/Preset SSA (FOV 24° + ângulo)")]
    public static void CameraPreset(){
        var cam=Camera.main; if(!cam){ Debug.LogError("Main Camera não encontrada."); return; }
        cam.fieldOfView=24f; cam.transform.position=new Vector3(-7.5f,5f,-9.5f); cam.transform.rotation=Quaternion.Euler(12f,25f,0f);
        var data=cam.GetUniversalAdditionalCameraData(); if(data!=null){ data.antialiasing=AntialiasingMode.FastApproximateAntialiasing; data.renderPostProcessing=true; }
        if(!GameObject.Find("SSA_GlobalPost")){
            var go=new GameObject("SSA_GlobalPost"); var vol=go.AddComponent<Volume>(); vol.isGlobal=true; vol.priority=20;
            var profile=ScriptableObject.CreateInstance<VolumeProfile>();
            var tm=profile.Add<Tonemapping>(true); tm.mode.value=TonemappingMode.ACES;
            var bloom=profile.Add<Bloom>(true); bloom.intensity.value=0.35f; bloom.threshold.value=1.1f;
            vol.profile=profile;
        }
        RenderSettings.fog=true; RenderSettings.fogMode=FogMode.Linear; RenderSettings.fogStartDistance=18f; RenderSettings.fogEndDistance=90f; RenderSettings.fogColor=new Color(0.52f,0.60f,0.78f,1f);
        Debug.Log("[SSA] Câmera e pós configurados.");
    }
}
