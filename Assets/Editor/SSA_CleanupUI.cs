using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public static class SSA_CleanupUI
{
    [MenuItem("SSA/Fix/4) Limpar UI: desativar canvases legados e overlays")]
    public static void Cleanup()
    {
        int canvasesOff = 0, imagesOff = 0;

        foreach (var canvas in GameObject.FindObjectsOfType<Canvas>(true))
        {
            if (canvas.gameObject.name != "SSA_HUD")
            {
                canvas.gameObject.SetActive(false);
                canvasesOff++;
            }
        }

        foreach (var img in GameObject.FindObjectsOfType<Image>(true))
        {
            if (!img.enabled) continue;
            var rt = img.rectTransform;
            var size = rt.rect.size;
            if (size.x >= 600f && size.y >= 300f)
            {
                img.enabled = false;
                imagesOff++;
            }
        }

        Debug.Log($"[SSA] Canvases desativados: {canvasesOff}. Overlays (Image) ocultados: {imagesOff}. Mantenha apenas o SSA_HUD.");
    }
}
