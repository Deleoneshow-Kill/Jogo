using UnityEngine;
namespace CleanRPG.Systems{
public class PalaceArenaAutoBuilder:MonoBehaviour{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void OnAfterSceneLoad(){ SceneBuilder.BuildPalaceArena(); }
}}
