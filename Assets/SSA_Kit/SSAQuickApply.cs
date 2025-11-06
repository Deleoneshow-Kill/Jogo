using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[ExecuteAlways]
public class SSAQuickApply : MonoBehaviour
{
    public Camera mainCam; public Light sun;

    [ContextMenu("SSA/Auto-Configurar (achar por nome)")]
    public void AutoConfigure()
    {
        mainCam = Camera.main;
        sun = RenderSettings.sun ?? FindObjectOfType<Light>();

        var arena = GameObject.Find("ArenaFloor");
        arenaFloor = arena ? arena.GetComponent<Renderer>() : arenaFloor;

        columns = new Renderer[]
        {
            GameObject.Find("Column_L")?.GetComponent<Renderer>(),
            GameObject.Find("Column_R")?.GetComponent<Renderer>()
        };

        var active = GameObject.Find("Orion (Gêmeos)") ?? GameObject.Find("Orion");
        activeActor = active ? active.transform : activeActor;

        ApplyNow();
    }
    public Renderer arenaFloor; public Renderer[] columns;
    public Transform activeActor; public float ringScale = 1.4f;

    Material _floorMat, _columnMat, _ringMat; GameObject _ringGO; Volume _volume;

    static Color Hex(string hex){ ColorUtility.TryParseHtmlString(hex, out var c); return c; }
    readonly Color COL_FLOOR = new Color(0.79f,0.71f,0.62f); // #C9B59B
    readonly Color COL_COLUMN = new Color(0.84f,0.86f,0.90f); // #D6DCE5
    readonly Color SUN = new Color(1.0f,0.85f,0.60f); // #FFD79A

    [ContextMenu("SSA/Aplicar Look SSA (Cena Atual)")]
    public void ApplyNow(){
        if (!mainCam) mainCam = Camera.main;
        if (mainCam){
            mainCam.fieldOfView = 29f;
            var urpCam = mainCam.GetUniversalAdditionalCameraData();
            if (urpCam) urpCam.renderPostProcessing = true;
        }
        if (!sun) sun = RenderSettings.sun;
        if (!sun) sun = FindObjectOfType<Light>();
        if (sun){
            sun.type = LightType.Directional; sun.color = SUN; sun.intensity = 1.20f; sun.shadows = LightShadows.Soft;
        }
        RenderSettings.fog = false;

        _volume = FindObjectOfType<Volume>();
        if (!_volume){
            var go = new GameObject("SSA_GlobalVolume");
            _volume = go.AddComponent<Volume>(); _volume.isGlobal = true; _volume.priority = 10;
            _volume.sharedProfile = ScriptableObject.CreateInstance<VolumeProfile>();
        }
        var profile = _volume.sharedProfile ? _volume.sharedProfile : _volume.profile;
        if (!profile){ profile = ScriptableObject.CreateInstance<VolumeProfile>(); _volume.profile = profile; }

        if (!profile.TryGet(out Bloom bloom)){ bloom = profile.Add<Bloom>(true); }
        bloom.threshold.Override(1.0f); bloom.intensity.Override(0.12f);

        if (!profile.TryGet(out ColorAdjustments ca)){ ca = profile.Add<ColorAdjustments>(true); }
        ca.postExposure.Override(0f); ca.contrast.Override(0f); ca.saturation.Override(0f);

        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        if (arenaFloor){
            if (_floorMat == null){ _floorMat = new Material(lit); _floorMat.name = "SSA_Floor_Warm"; }
            _floorMat.SetColor("_BaseColor", COL_FLOOR); _floorMat.SetFloat("_Metallic", 0f); _floorMat.SetFloat("_Smoothness", 0.25f);
            arenaFloor.sharedMaterial = _floorMat;
        }
        if (columns != null && columns.Length > 0){
            if (_columnMat == null){ _columnMat = new Material(lit); _columnMat.name = "SSA_Column"; _columnMat.SetFloat("_Metallic", 0f); _columnMat.SetFloat("_Smoothness", 0.30f); }
            _columnMat.SetColor("_BaseColor", COL_COLUMN);
            foreach (var r in columns){
                if (!r) continue;
                var t = r.transform; var s = t.localScale; t.localScale = new Vector3(Mathf.Max(1.8f, s.x), s.y, s.z);
                r.sharedMaterial = _columnMat;
            }
        }

        if (activeActor){
            if (_ringGO == null){
                _ringGO = GameObject.CreatePrimitive(PrimitiveType.Quad);
                _ringGO.name = "SSA_SelectionRing"; _ringGO.transform.SetParent(activeActor, false);
                _ringGO.transform.localRotation = Quaternion.Euler(90,0,0); DestroyImmediate(_ringGO.GetComponent<Collider>());
                var unlit = Shader.Find("Universal Render Pipeline/Unlit");
                _ringMat = new Material(unlit); _ringMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                _ringMat.SetFloat("_Surface", 1); _ringMat.SetFloat("_Blend", 1); _ringMat.SetFloat("_ZWrite", 0);
                Texture2D ring = new Texture2D(512,512,TextureFormat.RGBA32,false,true);
                for (int y=0;y<512;y++) for (int x=0;x<512;x++){
                    float dx=(x-256)/256f, dy=(y-256)/256f; float r=Mathf.Sqrt(dx*dx+dy*dy);
                    float edge=Mathf.SmoothStep(0.42f,0.45f,r)-Mathf.SmoothStep(0.48f,0.52f,r);
                    float a=Mathf.Clamp01(edge)*0.85f; Color c=new Color(0.38f,0.86f,1f,a); ring.SetPixel(x,y,c);
                }
                ring.Apply(); _ringMat.SetTexture("_BaseMap", ring);
                _ringGO.GetComponent<MeshRenderer>().sharedMaterial = _ringMat;
            }
            _ringGO.transform.localPosition = Vector3.zero;
            _ringGO.transform.localScale = new Vector3(ringScale, ringScale, 1f);
        }
        Debug.Log("[SSAQuickApply] Look aplicado.");
    }
    void OnEnable(){ if (!Application.isPlaying) ApplyNow(); }
}
