
using UnityEngine; using CleanRPG.Systems; using CleanRPG.UI;
namespace CleanRPG.Battle {
  public class DemoAutoSpawner : MonoBehaviour {
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot(){
      if (Object.FindObjectOfType<HUD_MobileStyle>()==null) new GameObject("HUD_Bootstrap").AddComponent<HUD_MobileStyle>();
      if (GameObject.Find("Orion")==null && GameObject.Find("Solaris")==null){
        Spawn("Orion", new Vector3(-2f,0,0), false);
        Spawn("Solaris", new Vector3( 2f,0,0), true);
      }
    }
    static void Spawn(string id, Vector3 pos, bool enemy){
      var go=GameObject.CreatePrimitive(PrimitiveType.Capsule); go.name=id; go.transform.position=pos;
      CharacterVisuals.Build(go, id, enemy);
    }
  }
}
