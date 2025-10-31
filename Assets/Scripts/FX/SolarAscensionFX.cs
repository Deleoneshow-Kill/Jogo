
using System.Collections;
using UnityEngine;
using CleanRPG.Battle;

namespace CleanRPG.FX
{
    public static class SolarAscensionFX
    {
        public static void Play(CleanRPG.Battle.BattleBootstrap3D boot, CharacterRuntime actor, CharacterRuntime target)
        {
            var t = (target!=null? target.transform : actor.transform);
            var center = t.position + new Vector3(0,0.1f,0);

            // warm overlay (reuse Galactic overlay if present)
            var ov = boot.gameObject.GetComponent<FullscreenOverlay>(); if (ov) boot.StartCoroutine(ov.Play(0.10f, 1.0f, 0.35f, 0.28f));

            // golden rings
            MakeQuad("FX_GoldRing1", center + new Vector3(0,1.2f,0), new Vector3(3.2f,3.2f,1), 40f, 0.3f, 2.0f, 1.0f, "Art/FX/halo_rings", new Color(1f,0.95f,0.6f,0.9f));
            MakeQuad("FX_GoldRing2", center + new Vector3(0,1.2f,0), new Vector3(3.6f,3.6f,1), -55f, 0.5f, 2.0f, 1.0f, "Art/FX/halo_rings", new Color(1f,0.9f,0.5f,0.7f));

            // lotus burst
            var q = MakeQuad("FX_Lotus", center + new Vector3(0,1.0f,0), new Vector3(3.0f,3.0f,1), 0f, 0.8f, 1.8f, 0.9f, "Art/FX/lotus_petal", new Color(1f,0.9f,0.6f,1f));

            // subtle upward beads
            for (int i=0;i<16;i++){
                var bead = MakeQuad("FX_BeadSpark", center + new Vector3(Random.Range(-0.6f,0.6f), 0.1f, Random.Range(-0.6f,0.6f)), new Vector3(0.25f,0.25f,1), 90f, 0f, Random.Range(0.9f,1.2f), 0.4f, "Art/FX/bead", new Color(1f,0.95f,0.7f,1f));
                bead.velocity = new Vector3(0, Random.Range(0.8f,1.2f), 0);
            }

            // camera nudge
            var dir = boot.gameObject.GetComponent<CameraDirector>(); if (dir!=null) boot.StartCoroutine(dir.Shake(0.14f, 0.24f, 36f));
        }

        static FXAuto MakeQuad(string name, Vector3 pos, Vector3 scale, float spin, float grow, float life, float fade, string resPath, Color? tint=null){
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = name; q.transform.position = pos; q.transform.localScale = scale;
            q.transform.rotation = Quaternion.LookRotation(Camera.main? Camera.main.transform.forward : Vector3.forward);
            var m = new Material(Shader.Find("Unlit/Transparent"));
            var tex = Resources.Load<Texture2D>(resPath); if (tex) m.mainTexture = tex;
            if (tint.HasValue) m.color = tint.Value;
            var r = q.GetComponent<Renderer>(); r.material = m;
            var a = q.AddComponent<FXAuto>(); a.spin = spin; a.grow = grow; a.life = life; a.fadeStart = fade;
            return a;
        }
    }
}
