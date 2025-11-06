#if UNITY_EDITOR
using UnityEditor; using UnityEngine; using UnityEngine.Rendering; using UnityEngine.Rendering.Universal;
public static class SSAVisualVerifier{
    [MenuItem("SSA/Verificar Visual (PASS/FAIL)")]
    public static void Verify(){
        var cam=Camera.main; var sun=RenderSettings.sun ?? Object.FindObjectOfType<Light>(); var vol=Object.FindObjectOfType<Volume>();
        var floor=GameObject.Find("ArenaFloor"); var colL=GameObject.Find("Column_L"); var colR=GameObject.Find("Column_R");
        bool pass=true; void Check(string n,bool ok){ Debug.Log($"{n}: {(ok?"PASS":"FAIL")}"); pass&=ok; }
        Check("FOV==29±0.5", cam && Mathf.Abs(cam.fieldOfView-29f)<=0.5f);
        Check("Fog OFF", !RenderSettings.fog);
        Bloom bloom=null; ColorAdjustments ca=null; if (vol){ var p=vol.sharedProfile? vol.sharedProfile: vol.profile; if (p){ p.TryGet(out bloom); p.TryGet(out ca);}}
        Check("Bloom ~0.1", bloom && bloom.intensity.value>=0.08f && bloom.intensity.value<=0.15f);
        Check("PostExposure==0", ca && Mathf.Abs(ca.postExposure.value)<0.01f);
        bool lightOK = sun && sun.type==LightType.Directional && sun.intensity>=1.1f && sun.intensity<=1.3f; Check("DirectionalLight OK", lightOK);
        bool colsOK = (colL && colL.transform.localScale.x>=1.8f) && (colR && colR.transform.localScale.x>=1.8f); Check("Columns X>=1.8", colsOK);
        bool floorOK=false; if (floor && floor.TryGetComponent<Renderer>(out var fr) && fr.sharedMaterial){ var m=fr.sharedMaterial;
            floorOK = m.HasProperty("_Metallic") && m.GetFloat("_Metallic")<0.05f && m.HasProperty("_Smoothness") && Mathf.Abs(m.GetFloat("_Smoothness")-0.25f)<=0.05f; }
        Check("Floor M0 S0.25", floorOK);
        Debug.Log(pass? "✅ VISUAL OK (igual ao preview)" : "❌ AINDA NÃO: ajuste os FAIL acima.");
    }
}
#endif
