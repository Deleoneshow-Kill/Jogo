
using UnityEngine;
using CleanRPG.Battle;

namespace CleanRPG.UI
{
    public class HUD_Bootstrap : MonoBehaviour
    {
        void Start()
        {
            var bootstrap = Object.FindObjectOfType<BattleBootstrap3D>();
            if (bootstrap != null)
            {
                var hudTmp = bootstrap.GetComponent<CanvasHUD_TMP_v6>();
                if (hudTmp == null)
                    hudTmp = bootstrap.gameObject.AddComponent<CanvasHUD_TMP_v6>();
                hudTmp.bootstrap = bootstrap;

                var legacyHud = bootstrap.GetComponent<CanvasHUD_Basic>();
                if (legacyHud != null)
                    legacyHud.enabled = false;

                var pickBan = bootstrap.GetComponent<ArenaPickBanUI>();
                if (pickBan == null) pickBan = bootstrap.gameObject.AddComponent<ArenaPickBanUI>();
                pickBan.bootstrap = bootstrap;
            }
            Destroy(gameObject);
        }
    }
}
