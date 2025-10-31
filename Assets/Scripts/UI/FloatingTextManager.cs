
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CleanRPG.UI
{
    public class FloatingTextManager : MonoBehaviour
    {
        private static FloatingTextManager _instance;
        public static FloatingTextManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("FloatingTextManager");
                    _instance = go.AddComponent<FloatingTextManager>();
                    Object.DontDestroyOnLoad(go);
                    _instance.SetupCanvas();
                }
                return _instance;
            }
        }

        private Canvas canvas;

        void SetupCanvas()
        {
            canvas = new GameObject("FloatingCanvas").AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;
            canvas.gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvas.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        public void Spawn(Vector3 worldPos, string text, Color color)
        {
            StartCoroutine(SpawnRoutine(worldPos, text, color));
        }

        IEnumerator SpawnRoutine(Vector3 worldPos, string text, Color c)
        {
            var cam = Camera.main; if (!cam) yield break;
            var go = new GameObject("Floaty", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(canvas.transform, false);
            var rt = go.GetComponent<RectTransform>(); rt.sizeDelta = new Vector2(200, 40);
            var tmp = go.GetComponent<TextMeshProUGUI>(); tmp.text = text; tmp.color = c; tmp.fontSize = 22; tmp.alignment = TextAlignmentOptions.Center;

            float t = 0f; Vector3 screen = cam.WorldToScreenPoint(worldPos + Vector3.up * 1.5f); rt.position = screen;
            while (t < 1.2f){ t += Time.deltaTime; screen += new Vector3(0, 40*Time.deltaTime, 0); rt.position = screen; var a = Mathf.Lerp(1f, 0f, t/1.2f); tmp.color = new Color(c.r, c.g, c.b, a); yield return null; }
            Object.Destroy(go);
        }
    }
}
