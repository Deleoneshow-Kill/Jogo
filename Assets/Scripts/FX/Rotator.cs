
using UnityEngine;
namespace CleanRPG.FX {
  public class Rotator : MonoBehaviour {
    public Vector3 speed = new Vector3(0,45f,0);
    void Update(){ transform.Rotate(speed * Time.deltaTime, Space.Self); }
  }
}
