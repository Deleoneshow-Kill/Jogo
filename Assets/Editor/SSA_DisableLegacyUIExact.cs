using UnityEditor;
using UnityEngine;

public static class SSA_DisableLegacyUIExact
{
    [MenuItem("SSA/Look/Disable Legacy UI (exact names)")]
    public static void Run()
    {
        string[] names = {
            "Game", "AutoScreenshot", "HUD_Bootstrap",
            "TeamSelectCanvas", "CombatLogCanvas", "GachaCanvas",
            "ReplayCanvas", "ArenaCanvas", "FloatingCanvas", "SSA_StageSample"
        };
        int off = 0;
        foreach (var n in names)
        {
            var go = GameObject.Find(n);
            if (go && go.activeSelf) { go.SetActive(false); off++; }
        }
        Debug.Log($"[SSA] Canvases/objetos legados desativados: {off}");
    }
}
