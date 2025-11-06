using UnityEngine;
public class SSA_ParallaxLayer : MonoBehaviour
{
    public Transform cam; public Vector2 parallaxStrength = new Vector2(0.02f,0.01f);
    Vector3 startPos;
    void Start(){ startPos=transform.position; if(!cam && Camera.main) cam=Camera.main.transform; }
    void LateUpdate(){ if(!cam) return; var d=cam.position; transform.position=new Vector3(startPos.x+d.x*parallaxStrength.x, startPos.y+d.y*parallaxStrength.y, transform.position.z); }
}
