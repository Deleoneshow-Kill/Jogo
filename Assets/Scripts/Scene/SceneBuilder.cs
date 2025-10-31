
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CleanRPG.Systems {
  public static class SceneBuilder {
    public static void BuildPalaceArena(GameObject host=null) {
      if (Camera.main == null) {
        var camGO = new GameObject("Main Camera");
        var cam = camGO.AddComponent<Camera>();
        cam.tag = "MainCamera";
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.03f,0.03f,0.06f,1f);
        cam.transform.position = new Vector3(0, 6.5f, -10f);
        cam.transform.rotation = Quaternion.Euler(20, 0, 0);
      }
      if (Object.FindObjectOfType<Light>() == null) {
        var l = new GameObject("Directional Light").AddComponent<Light>();
        l.type = LightType.Directional; l.color = new Color(1f,0.92f,0.78f,1f); l.intensity=1.2f;
        l.transform.rotation = Quaternion.Euler(45,30,0);
      }
      var floor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
      floor.name="ArenaFloor"; floor.transform.position=Vector3.zero;
      floor.transform.localScale=new Vector3(6f,0.2f,6f);
      floor.GetComponent<Renderer>().sharedMaterial = UnlitColor(new Color(0.1f,0.2f,0.5f,1f));
      var back = GameObject.CreatePrimitive(PrimitiveType.Plane);
      back.name="Backdrop"; back.transform.position=new Vector3(0,3f,7.5f);
      back.transform.rotation=Quaternion.Euler(90,0,0); back.transform.localScale=new Vector3(0.7f,1f,0.4f);
      back.GetComponent<Renderer>().sharedMaterial = UnlitColor(new Color(0.05f,0.08f,0.15f,1f));
      for (int i=-1;i<=1;i+=2){ var col=GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        col.name="Column_"+(i>0?"R":"L"); col.transform.position=new Vector3(5.5f*i,2.5f,3f);
        col.transform.localScale=new Vector3(0.4f,2.5f,0.4f);
        col.GetComponent<Renderer>().sharedMaterial=UnlitColor(new Color(0.75f,0.65f,0.45f,1f));
      }
    }
    static Material UnlitColor(Color c){ var m=new Material(Shader.Find("Unlit/Color")); m.color=c; return m; }
  }
}
