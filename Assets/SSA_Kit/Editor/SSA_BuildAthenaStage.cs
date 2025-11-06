using UnityEditor; using UnityEngine;
public static class SSA_BuildAthenaStage
{
    [MenuItem("SSA/Stage/Build Athena Garden Stage")]
    public static void Build(){
        var root=new GameObject("SSA_AthenaStage");
        // ground disc
        var ground=new GameObject("StageGround"); ground.transform.SetParent(root.transform);
        var mf=ground.AddComponent<MeshFilter>(); var mr=ground.AddComponent<MeshRenderer>();
        mf.sharedMesh=MakeDisc(6f,64); mr.sharedMaterial=new Material(Shader.Find("SSA/ToonMatcapOutlineRamp"));
        // steps
        for(int i=0;i<3;i++){ var step=GameObject.CreatePrimitive(PrimitiveType.Cube); step.name="Step_"+i; step.transform.SetParent(root.transform);
            step.transform.localScale=new Vector3(8.0f+i*0.8f,0.2f,1.4f+i*0.3f); step.transform.position=new Vector3(0,0.1f*i,-2.2f+i*0.45f);
            step.GetComponent<Renderer>().sharedMaterial=new Material(Shader.Find("SSA/ToonMatcapOutlineRamp")); }
        // columns
        float[] colX={-5.6f,-3.6f,3.6f,5.6f};
        for(int i=0;i<colX.Length;i++){ var col=GameObject.CreatePrimitive(PrimitiveType.Cylinder); col.name="Column_"+i; col.transform.SetParent(root.transform);
            col.transform.localScale=new Vector3(0.4f,3.0f,0.4f); col.transform.position=new Vector3(colX[i],3.0f,-1.0f);
            col.GetComponent<Renderer>().sharedMaterial=new Material(Shader.Find("SSA/ToonMatcapOutlineRamp")); }
        // parallax quads
        CreateBG("BG_Far","SSA_Backgrounds/layer_far",50f,new Vector3(0,10,20), new Vector2(0.01f,0.005f), root.transform);
        CreateBG("BG_Mid","SSA_Backgrounds/layer_mid",42f,new Vector3(0,8,15), new Vector2(0.02f,0.008f), root.transform);
        CreateBG("BG_Near_Temple","SSA_Backgrounds/layer_near_temple",36f,new Vector3(0,6,12), new Vector2(0.03f,0.012f), root.transform);
        // fog & camera
        RenderSettings.fog=true; RenderSettings.fogMode=FogMode.Linear; RenderSettings.fogStartDistance=18f; RenderSettings.fogEndDistance=90f; RenderSettings.fogColor=new Color(0.52f,0.60f,0.78f,1f);
        var cam=Camera.main; if(cam){ cam.fieldOfView=24f; cam.transform.position=new Vector3(-7.5f,5.0f,-9.5f); cam.transform.rotation=Quaternion.Euler(12f,25f,0f);
            foreach(var p in root.GetComponentsInChildren<SSA_ParallaxLayer>(true)) p.cam=cam.transform; }
        Selection.activeObject=root; Debug.Log("[SSA] Athena Stage criado.");
    }
    static Mesh MakeDisc(float r,int seg){ var m=new Mesh(); var v=new Vector3[seg+1]; var uv=new Vector2[seg+1]; var t=new int[seg*3];
        v[0]=Vector3.zero; uv[0]=new Vector2(0.5f,0.5f);
        for(int i=0;i<seg;i++){ float a=(i/(float)seg)*Mathf.PI*2f; v[i+1]=new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r); uv[i+1]=new Vector2(Mathf.Cos(a)*0.5f+0.5f, Mathf.Sin(a)*0.5f+0.5f);
            t[i*3+0]=0; t[i*3+1]=i+1; t[i*3+2]= i==seg-1 ? 1 : i+2; }
        m.vertices=v; m.uv=uv; m.triangles=t; m.RecalculateNormals(); return m; }
    static void CreateBG(string name,string res,float width,Vector3 pos,Vector2 par,Transform parent){
        var go=GameObject.CreatePrimitive(PrimitiveType.Quad); go.name=name; go.transform.SetParent(parent);
        float aspect=16f/9f; go.transform.localScale=new Vector3(width, width/aspect,1); go.transform.position=pos;
        var tex=Resources.Load<Texture2D>(res); var mat=new Material(Shader.Find("Unlit/Texture")); mat.SetTexture("_MainTex",tex); go.GetComponent<Renderer>().sharedMaterial=mat;
        Object.DestroyImmediate(go.GetComponent<Collider>()); var p=go.AddComponent<SSA_ParallaxLayer>(); p.parallaxStrength=par;
    }
}
