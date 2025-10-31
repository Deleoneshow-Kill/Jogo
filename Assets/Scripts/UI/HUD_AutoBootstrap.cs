using UnityEngine;

namespace CleanRPG.UI
{
    public static class HUD_AutoBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnAfterSceneLoad()
        {
            var go = new GameObject("HUD_Bootstrapper");
            go.AddComponent<HUD_Bootstrap>(); // seu script existente
        }
    }
}
