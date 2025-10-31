
using System.Collections;
using UnityEngine;
using CleanRPG.Battle;

namespace CleanRPG.FX
{
    public class FXAuto : MonoBehaviour
    {
        public float life = 2.5f;
        public float spin = 60f;
        public float grow = 0f;
        public float fadeStart = 1.5f;
        public Vector3 velocity = Vector3.zero;
        private float t=0f;
        private Material mat;

        void Start(){ var r = GetComponent<Renderer>(); if (r) mat = r.material; }
        void Update(){
            t += Time.deltaTime;
            transform.Rotate(0, spin*Time.deltaTime, 0);
            if (grow!=0f) transform.localScale += Vector3.one * grow * Time.deltaTime;
            if (velocity != Vector3.zero) transform.position += velocity * Time.deltaTime;
            if (mat && t>fadeStart){
                var c = mat.color; c.a = Mathf.Lerp(1f,0f,(t-fadeStart)/(life-fadeStart)); mat.color = c;
            }
            if (t>life) Destroy(gameObject);
        }
    }

    public class CameraDirector : MonoBehaviour
    {
        public IEnumerator DollyZoom(float fovIn=35f, float fovOut=60f, float tIn=0.3f, float hold=0.6f, float tOut=0.3f)
        {
            var cam = Camera.main; if (!cam) yield break;
            float f0 = cam.fieldOfView;
            float t=0;
            while (t<tIn){ t+=Time.unscaledDeltaTime; cam.fieldOfView = Mathf.Lerp(f0, fovIn, t/tIn); yield return null; }
            float h=0; while (h<hold){ h+=Time.unscaledDeltaTime; yield return null; }
            t=0; while (t<tOut){ t+=Time.unscaledDeltaTime; cam.fieldOfView = Mathf.Lerp(fovIn, fovOut, t/tOut); yield return null; }
            cam.fieldOfView = fovOut;
        }

        public IEnumerator Shake(float amplitude=0.2f, float duration=0.25f, float frequency=40f)
        {
            var cam = Camera.main; if (!cam) yield break;
            var t0 = cam.transform.position;
            float t=0;
            while (t<duration){
                t += Time.unscaledDeltaTime;
                float s = amplitude * (1f - t/duration);
                cam.transform.position = t0 + new Vector3(
                    (Mathf.PerlinNoise(0, t*frequency)-0.5f)*2f*s,
                    (Mathf.PerlinNoise(1, t*frequency)-0.5f)*2f*s,
                    0f);
                yield return null;
            }
            cam.transform.position = t0;
        }
    }

    public class FullscreenOverlay : MonoBehaviour
    {
        private GameObject quad;
        private Material mat;
        public IEnumerator Play(float fadeIn=0.15f, float hold=0.9f, float fadeOut=0.4f, float tint=0.35f)
        {
            var cam = Camera.main; if (!cam) yield break;
            quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "FX_Overlay";
            quad.transform.SetParent(cam.transform, false);
            quad.transform.localPosition = new Vector3(0,0, 1.5f);
            quad.transform.localRotation = Quaternion.identity;
            quad.transform.localScale = new Vector3(3.2f, 1.8f, 1);
            mat = new Material(Shader.Find("Unlit/Transparent"));
            var tex = Resources.Load<Texture2D>("Art/FX/vignette");
            mat.mainTexture = tex;
            mat.color = new Color(0.6f, 0.3f, 1.0f, 0);
            quad.GetComponent<Renderer>().material = mat;
            // fade in
            float t=0; while (t<fadeIn){ t+=Time.unscaledDeltaTime; mat.color = new Color(0.6f,0.3f,1.0f, Mathf.Lerp(0, tint, t/fadeIn)); yield return null; }
            // hold
            float h=0; while (h<hold){ h+=Time.unscaledDeltaTime; yield return null; }
            // fade out
            t=0; while (t<fadeOut){ t+=Time.unscaledDeltaTime; mat.color = new Color(0.6f,0.3f,1.0f, Mathf.Lerp(tint, 0, t/fadeOut)); yield return null; }
            Destroy(quad);
        }
    }

    public class BeamBetween : MonoBehaviour
    {
        public Transform a;
        public Transform b;
        public float life = 0.45f;
        private float t=0f;
        private LineRenderer lr;

        void Start(){
            lr = gameObject.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.material = new Material(Shader.Find("Unlit/Transparent"));
            lr.material.mainTexture = Resources.Load<Texture2D>("Art/FX/trail_beam");
            lr.textureMode = LineTextureMode.Stretch;
            lr.widthMultiplier = 0.4f;
            lr.numCapVertices = 6; lr.numCornerVertices = 6;
            var grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[]{ new GradientColorKey(new Color(0.8f,0.6f,1f),0), new GradientColorKey(new Color(0.9f,0.8f,1f),1)},
                new GradientAlphaKey[]{ new GradientAlphaKey(0,0), new GradientAlphaKey(1,0.15f), new GradientAlphaKey(1,0.85f), new GradientAlphaKey(0,1)});
            lr.colorGradient = grad;
        }

        void Update(){
            t += Time.deltaTime;
            if (a && b){
                var p0 = a.position + new Vector3(0.6f,1.1f,0.35f);
                var p1 = b.position + new Vector3(0,1.0f,0);
                // slight wobble
                float w = Mathf.Sin(Time.time*20f)*0.1f;
                lr.SetPosition(0, p0 + new Vector3(0,w,0));
                lr.SetPosition(1, p1 + new Vector3(0,-w,0));
            }
            // fade width over time
            lr.widthMultiplier = Mathf.Lerp(0.4f, 0.0f, t/life);
            if (t>life) Destroy(gameObject);
        }
    }

    public static class GalacticPortalFX
    {
        public static void Play(CleanRPG.Battle.BattleBootstrap3D boot, CharacterRuntime actor, CharacterRuntime target)
        {
            var t = (target!=null? target.transform : actor.transform);
            var center = t.position + new Vector3(0,0.1f,0);

            // overlay (pseudo post-process)
            var ov = boot.gameObject.GetComponent<FullscreenOverlay>(); if (!ov) ov = boot.gameObject.AddComponent<FullscreenOverlay>();
            boot.StartCoroutine(ov.Play(0.12f, 1.1f, 0.45f, 0.32f));

            // main ring + thin ring + starburst
            var ring = MakeQuad("FX_PortalRing", center + new Vector3(0,1.1f,0), new Vector3(3f,3f,1f), 50f, 0.4f, 2.2f, 1.2f, "Art/FX/portal_ring");
            var ring2 = MakeQuad("FX_PortalRingThin", center + new Vector3(0,1.1f,0), new Vector3(3.3f,3.3f,1f), -70f, 0.6f, 2.2f, 1.2f, "Art/FX/portal_ring_thin");
            var sb = MakeQuad("FX_Starburst", center + new Vector3(0,1.1f,-0.1f), new Vector3(4f,4f,1f), -20f, 0.2f, 2.2f, 1.0f, "Art/FX/starburst", new Color(0.7f,0.8f,1f,0.85f));

            // sparks
            for (int i=0;i<24;i++){
                var off = new Vector3(Random.Range(-0.8f,0.8f), 0.1f, Random.Range(-0.8f,0.8f));
                var sp = MakeQuad("FX_Spark", center + off, new Vector3(0.3f,0.3f,1f), Random.Range(-60f,60f), 0f, Random.Range(0.9f,1.3f), 0.4f, "Art/FX/spark");
                sp.velocity = new Vector3(0, Random.Range(0.8f,1.4f), 0);
            }

            // slow-mo + camera
            boot.StartCoroutine(SlowMo(0.5f, 1.5f));
            var dir = boot.gameObject.GetComponent<CameraDirector>(); if (!dir) dir = boot.gameObject.AddComponent<CameraDirector>();
            boot.StartCoroutine(dir.DollyZoom(35f, 60f, 0.25f, 1.0f, 0.4f));

            // beam/trail from actor "hand" to target
            if (actor && target){
                var go = new GameObject("FX_Beam");
                var bm = go.AddComponent<BeamBetween>();
                bm.a = actor.transform; bm.b = target.transform; bm.life = 0.45f;
            }

            // impact: flare + shockwave + shake
            boot.StartCoroutine(ImpactFlare(center + new Vector3(0,1.1f,0)));
            boot.StartCoroutine(ImpactShockwave(center + new Vector3(0,0.02f,0)));
            boot.StartCoroutine(dir.Shake(0.22f, 0.28f, 38f));
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

        static IEnumerator SlowMo(float scale, float duration){
            float prev = Time.timeScale;
            Time.timeScale = scale;
            float t = 0f;
            while (t<duration){ t += Time.unscaledDeltaTime; yield return null; }
            Time.timeScale = prev;
        }

        static IEnumerator ImpactFlare(Vector3 pos){
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = "FX_Flare";
            q.transform.position = pos;
            q.transform.localScale = new Vector3(6f,6f,1f);
            q.transform.rotation = Quaternion.LookRotation(Camera.main? Camera.main.transform.forward : Vector3.forward);
            var m = new Material(Shader.Find("Unlit/Transparent"));
            var tex = Resources.Load<Texture2D>("Art/FX/flare"); if (tex) m.mainTexture = tex;
            m.color = new Color(1f,1f,1f,0.9f);
            var r = q.GetComponent<Renderer>(); r.material = m;
            float t=0f, life=0.25f;
            while (t<life){ t+=Time.deltaTime; float k = 1f - (t/life); m.color = new Color(1f,1f,1f,k); q.transform.localScale = Vector3.one * (6f + 2f*(t/life)); yield return null; }
            GameObject.Destroy(q);
        }

        static IEnumerator ImpactShockwave(Vector3 pos){
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = "FX_Shockwave";
            q.transform.position = pos;
            q.transform.rotation = Quaternion.Euler(90,0,0);
            q.transform.localScale = new Vector3(0.5f,0.5f,1f);
            var m = new Material(Shader.Find("Unlit/Transparent"));
            var tex = Resources.Load<Texture2D>("Art/FX/shockwave_ring"); if (tex) m.mainTexture = tex;
            m.color = new Color(1f,1f,1f,1f);
            q.GetComponent<Renderer>().material = m;

            float t=0f, dur=0.5f;
            while (t<dur){
                t += Time.deltaTime;
                float k = t/dur;
                float scale = Mathf.Lerp(0.5f, 5.0f, k);
                float alpha = 1f - k;
                q.transform.localScale = new Vector3(scale, scale, 1f);
                var c = m.color; c.a = alpha; m.color = c;
                yield return null;
            }
            GameObject.Destroy(q);
        }
    }
}
