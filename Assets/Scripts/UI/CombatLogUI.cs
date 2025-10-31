
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CleanRPG.UI
{
    using CleanRPG.Battle;

    public class CombatLogUI : MonoBehaviour
    {
        private Canvas canvas;
        private RectTransform panel;
        private ScrollRect scroll;
        private RectTransform content;
        private Sprite uiSprite;
        private CombatWatcher watcher;

        void Awake()
        {
            uiSprite = Resources.GetBuiltinResource<Sprite>("UISprite.psd");
            canvas = new GameObject("CombatLogCanvas").AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvas.gameObject.AddComponent<GraphicRaycaster>();

            panel = CreatePanel(new Vector2(-10, 10), new Vector2(380, 220), new Vector2(1,0));

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(panel, false);
            var vpRT = viewport.GetComponent<RectTransform>(); vpRT.sizeDelta = new Vector2(360, 180); vpRT.pivot = new Vector2(1,0); vpRT.anchorMin = new Vector2(1,0); vpRT.anchorMax = new Vector2(1,0); vpRT.anchoredPosition = new Vector2(-10, 10);
            viewport.GetComponent<Image>().sprite = uiSprite; viewport.GetComponent<Image>().type = Image.Type.Sliced; viewport.GetComponent<Image>().color = new Color(0,0,0,0.25f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            var sr = new GameObject("ScrollRect", typeof(RectTransform), typeof(ScrollRect));
            sr.transform.SetParent(panel, false);
            var srRT = sr.GetComponent<RectTransform>(); srRT.sizeDelta = new Vector2(360, 180); srRT.pivot = new Vector2(1,0); srRT.anchorMin = new Vector2(1,0); srRT.anchorMax = new Vector2(1,0); srRT.anchoredPosition = new Vector2(-10, 10);
            scroll = sr.GetComponent<ScrollRect>(); scroll.viewport = viewport.GetComponent<RectTransform>(); scroll.horizontal = false;

            var cont = new GameObject("Content", typeof(RectTransform));
            cont.transform.SetParent(viewport.transform, false);
            content = cont.GetComponent<RectTransform>(); content.sizeDelta = new Vector2(340, 10); content.pivot = new Vector2(1,1); content.anchorMin = new Vector2(1,1); content.anchorMax = new Vector2(1,1);
            scroll.content = content;

            watcher = Object.FindFirstObjectByType<CombatWatcher>();
        }

        void Update()
        {
            if (watcher == null) watcher = Object.FindFirstObjectByType<CombatWatcher>();
            if (watcher == null) return;

            var e = watcher.TryDequeue();
            while (e != null)
            {
                AddLine(e);
                if (e.type=="damage") FloatingTextManager.Instance.Spawn(e.target.transform.position, "-"+e.amount, new Color(1f,0.3f,0.3f,1f));
                if (e.type=="heal") FloatingTextManager.Instance.Spawn(e.target.transform.position, "+"+e.amount, new Color(0.3f,1f,0.5f,1f));
                e = watcher.TryDequeue();
            }
        }

        void AddLine(CombatWatcher.DeltaEvent e)
        {
            string text = e.type switch {
                "damage" => $"<color=#FF6E6E>−{e.amount}</color> em <b>{e.target.def.displayName}</b>",
                "heal"   => $"<color=#6EFF8E>+{e.amount}</color> em <b>{e.target.def.displayName}</b>",
                "status" => $"<color=#FFD54F>[{e.statusName}]</color> em <b>{e.target.def.displayName}</b>",
                "death"  => $"<color=#BBBBBB>{e.target.def.displayName} caiu</color>",
                _ => e.type
            };

            var go = new GameObject("Line", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(content, false);
            var rt = go.GetComponent<RectTransform>(); rt.sizeDelta = new Vector2(340, 20); rt.pivot = new Vector2(1,1); rt.anchoredPosition = new Vector2(0, -content.childCount*20);
            var tmp = go.GetComponent<TextMeshProUGUI>(); tmp.text = text; tmp.fontSize = 14; tmp.color = Color.white; tmp.richText = true; tmp.alignment = TextAlignmentOptions.Right;
            content.sizeDelta = new Vector2(340, content.childCount*20 + 10);
            scroll.verticalNormalizedPosition = 0f;
        }

        RectTransform CreatePanel(Vector2 anchoredPos, Vector2 size, Vector2 anchor)
        {
            var go = new GameObject("CombatLogPanel", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(canvas.transform, false);
            var rt = go.GetComponent<RectTransform>(); rt.sizeDelta = size; rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = new Vector2(1,0); rt.anchoredPosition = anchoredPos;
            var img = go.GetComponent<Image>(); img.sprite = Resources.GetBuiltinResource<Sprite>("UISprite.psd"); img.type = Image.Type.Sliced; img.color = new Color(0,0,0,0.35f);
            return rt;
        }
    }
}
