using UnityEngine;
// using UnityEngine.ScreenCapture; // Unity 6000 requires Screen Capture package
namespace CleanRPG.Systems{
public class AutoScreenshot:MonoBehaviour{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AfterLoad(){ new GameObject("AutoScreenshot").AddComponent<AutoScreenshot>(); }
    float t=1.5f; bool done;
    void Update(){ 
        t-=Time.unscaledDeltaTime; 
        if(!done && t<=0f){ 
            done=true; 
            // ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath,"../Snapshots/preview.png"),1); 
            Debug.Log("Screenshot desabilitado - requer Screen Capture package no Unity 6000"); 
        }
        if(Input.GetKeyDown(KeyCode.F12)){ 
            // ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath,"../Snapshots/manual.png"),1);
            Debug.Log("Screenshot manual desabilitado - requer Screen Capture package no Unity 6000");
        }
    }
}
}
