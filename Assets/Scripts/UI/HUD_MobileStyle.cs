
using UnityEngine;

namespace CleanRPG.UI
{
    /// <summary>
    /// HUD antigo (mobile-style) mantido apenas para compatibilidade. Hoje ele não gera interface nenhuma.
    /// </summary>
    public class HUD_MobileStyle : MonoBehaviour
    {
        void Awake()
        {
            var legacy = GameObject.Find("HUD_MobileStyle");
            if (legacy != null) Destroy(legacy);
            Debug.Log("HUD_MobileStyle foi descontinuado. Use CanvasHUD_Basic ou CanvasHUD_TMP_v6.");
            Destroy(this);
        }
    }
}
