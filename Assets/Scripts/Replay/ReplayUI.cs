
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CleanRPG.Battle;

namespace CleanRPG.Replay
{
    public class ReplayUI : MonoBehaviour
    {
        private Canvas canvas; private RectTransform panel; private Sprite uiSprite;
        private ReplaySystem sys; private CleanRPG.Battle.BattleBootstrap3D boot;

        void Awake()
        {
            uiSprite = Resources.Load<Sprite>("UISprite");
            if (!uiSprite)
            {
                uiSprite = Resources.GetBuiltinResource<Sprite>("UISprite.psd");
            }
            canvas = new GameObject("ReplayCanvas").AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvas.gameObject.AddComponent<GraphicRaycaster>();
            panel = CreatePanel(new Vector2(-10,-10), new Vector2(300,110), new Vector2(1,1));
            Build();
        }

        void Build()
        {
            boot = FindFirstObjectByType<CleanRPG.Battle.BattleBootstrap3D>();
            sys = gameObject.AddComponent<ReplaySystem>();

            var txt = CreateTMP(panel, "Title", "Replay (F5 abre/fecha)", new Vector2(10,-10), 18, true);
            var bRec = CreateButton(panel, "Gravar", new Vector2(10,-40)); bRec.onClick.AddListener(()=> sys.StartRecording());
            var bStop = CreateButton(panel, "Parar", new Vector2(110,-40)); bStop.onClick.AddListener(()=> sys.StopRecording());
            var bPlay = CreateButton(panel, "Play 0.25x", new Vector2(10,-80)); bPlay.onClick.AddListener(()=> sys.Play(boot));
            var bClr = CreateButton(panel, "Limpar", new Vector2(110,-80)); bClr.onClick.AddListener(()=> sys.Clear());
        }

        void Update(){ if (Input.GetKeyDown(KeyCode.F5)) panel.gameObject.SetActive(!panel.gameObject.activeSelf); }

        RectTransform CreatePanel(Vector2 pos, Vector2 size, Vector2 anchor){ var go=new GameObject("ReplayPanel", typeof(RectTransform), typeof(Image)); go.transform.SetParent(canvas.transform,false); var rt=go.GetComponent<RectTransform>(); rt.sizeDelta=size; rt.anchorMin=anchor; rt.anchorMax=anchor; rt.pivot=new Vector2(1,1); rt.anchoredPosition=pos; var img=go.GetComponent<Image>(); img.sprite=uiSprite; img.type=Image.Type.Sliced; img.color=new Color(0,0,0,0.45f); return rt; }
        Button CreateButton(Transform p,string label, Vector2 pos){ var go=new GameObject("Btn", typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(p,false); var rt=go.GetComponent<RectTransform>(); rt.sizeDelta=new Vector2(90,28); rt.pivot=new Vector2(0,1); rt.anchoredPosition=pos; var img=go.GetComponent<Image>(); img.sprite=uiSprite; img.type=Image.Type.Sliced; img.color=new Color(0.2f,0.2f,0.2f,0.9f); var btn=go.GetComponent<Button>(); var tgo=new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)); tgo.transform.SetParent(go.transform,false); var tr=tgo.GetComponent<RectTransform>(); tr.sizeDelta=new Vector2(80,24); tr.pivot=new Vector2(0,1); tr.anchoredPosition=new Vector2(6,-2); var t=tgo.GetComponent<TextMeshProUGUI>(); t.text=label; t.fontSize=14; t.color=Color.white; return btn; }
        TextMeshProUGUI CreateTMP(Transform parent,string name,string content, Vector2 pos,int size=16, bool bold=false){ var go=new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(parent,false); var rt=go.GetComponent<RectTransform>(); rt.sizeDelta=new Vector2(280,24); rt.pivot=new Vector2(0,1); rt.anchoredPosition=pos; var t=go.GetComponent<TextMeshProUGUI>(); t.text=content; t.fontSize=size; if(bold) t.fontStyle = FontStyles.Bold; t.color=Color.white; return t; }
    }
}
