
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CleanRPG.Battle;
using CleanRPG.Systems;

namespace CleanRPG.UI
{
    public class TeamSelectUI : MonoBehaviour
    {
    public CleanRPG.Battle.BattleBootstrap3D bootstrap;
        private Canvas canvas; private RectTransform panel; private Sprite uiSprite;
        private List<string> pickPlayer = new List<string>(); private List<string> pickEnemy = new List<string>();

        void Awake(){ uiSprite = Resources.GetBuiltinResource<Sprite>("UISprite.psd"); canvas = new GameObject("TeamSelectCanvas").AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; canvas.gameObject.AddComponent<GraphicRaycaster>(); panel = CreatePanel(new Vector2(10,-100), new Vector2(760,420), new Vector2(0,1)); panel.gameObject.SetActive(false); BuildUI(); }
        void Update(){ if (Input.GetKeyDown(KeyCode.F2)) panel.gameObject.SetActive(!panel.gameObject.activeSelf); }

        void BuildUI()
        {
            CreateTMP(panel,"Title","Seleção de Equipe (F2 fecha)", new Vector2(10,-10), 20, true);
            var listPanel = CreateSub(panel, "Owned", new Vector2(10,-40), new Vector2(360,360)); var (scroll, content) = NewScroll(listPanel);
            int y=-10; foreach (var kv in bootstrap.charDB){ var id=kv.Key; var def=kv.Value; if (!CleanRPG.Systems.InventorySystem.IsOwned(id)) continue; var b = CreateButton(content, def.displayName, new Vector2(10,y)); b.onClick.AddListener(()=> AddToPlayer(id)); y-=36; }
            var slotsP = CreateSub(panel, "SlotsP", new Vector2(380,-40), new Vector2(180,200)); CreateTMP(slotsP,"LblP","Time do Jogador", new Vector2(10,-10),18,true);
            for (int i=0;i<6;i++){ var b = CreateButton(slotsP, $"P{i+1}: —", new Vector2(10,-40 - i*28)); int idx=i; b.onClick.AddListener(()=> RemoveFromPlayer(idx)); }
            var slotsE = CreateSub(panel, "SlotsE", new Vector2(570,-40), new Vector2(180,200)); CreateTMP(slotsE,"LblE","Time Inimigo", new Vector2(10,-10),18,true);
            for (int i=0;i<6;i++){ var b = CreateButton(slotsE, $"E{i+1}: —", new Vector2(10,-40 - i*28)); int idx=i; b.onClick.AddListener(()=> RemoveFromEnemy(idx)); }
            var ctrl = CreateSub(panel, "Ctrl", new Vector2(380,-260), new Vector2(370,140));
            var btnStart = CreateButton(ctrl, "Iniciar Batalha", new Vector2(10,-10)); btnStart.onClick.AddListener(StartBattle);
            var btnFillE = CreateButton(ctrl, "Auto Inimigos", new Vector2(10,-50)); btnFillE.onClick.AddListener(()=> AutoEnemies());
            var btnClear = CreateButton(ctrl, "Limpar", new Vector2(10,-90)); btnClear.onClick.AddListener(()=>{ pickPlayer.Clear(); pickEnemy.Clear(); RefreshSlots(); });
        }

        void AddToPlayer(string id){ if (pickPlayer.Count>=6) return; pickPlayer.Add(id); RefreshSlots(); }
        void RemoveFromPlayer(int idx){ if (idx>=0 && idx<pickPlayer.Count){ pickPlayer.RemoveAt(idx); RefreshSlots(); } }
        void RemoveFromEnemy(int idx){ if (idx>=0 && idx<pickEnemy.Count){ pickEnemy.RemoveAt(idx); RefreshSlots(); } }
        void AutoEnemies(){ pickEnemy.Clear(); foreach (var kv in bootstrap.charDB.Values){ if (pickEnemy.Count>=6) break; if (!pickPlayer.Contains(kv.id)) pickEnemy.Add(kv.id); } RefreshSlots(); }
        void StartBattle(){ if (pickPlayer.Count==0) return; if (pickEnemy.Count==0) AutoEnemies(); bootstrap.SetupWithTeam(new List<string>(pickPlayer), new List<string>(pickEnemy)); panel.gameObject.SetActive(false); }
        void RefreshSlots(){ var sp = panel.Find("SlotsP"); for (int i=0;i<6;i++){ var b = sp.GetChild(i+1).GetComponent<Button>(); var t = b.transform.Find("Label").GetComponent<TextMeshProUGUI>(); t.text = $"P{i+1}: " + (i<pickPlayer.Count? bootstrap.charDB[pickPlayer[i]].displayName : "—"); } var se = panel.Find("SlotsE"); for (int i=0;i<6;i++){ var b = se.GetChild(i+1).GetComponent<Button>(); var t = b.transform.Find("Label").GetComponent<TextMeshProUGUI>(); t.text = $"E{i+1}: " + (i<pickEnemy.Count? bootstrap.charDB[pickEnemy[i]].displayName : "—"); } }

        RectTransform CreatePanel(Vector2 pos, Vector2 size, Vector2 anchor){ var go=new GameObject("TeamSelectPanel", typeof(RectTransform), typeof(Image)); go.transform.SetParent(canvas.transform,false); var rt=go.GetComponent<RectTransform>(); rt.sizeDelta=size; rt.anchorMin=anchor; rt.anchorMax=anchor; rt.pivot=new Vector2(0,1); rt.anchoredPosition=pos; var img=go.GetComponent<Image>(); img.sprite=Resources.GetBuiltinResource<Sprite>("UISprite.psd"); img.type=Image.Type.Sliced; img.color=new Color(0,0,0,0.55f); return rt; }
        RectTransform CreateSub(Transform parent,string name, Vector2 pos, Vector2 size){ var go=new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent,false); var rt=go.GetComponent<RectTransform>(); rt.sizeDelta=size; rt.pivot=new Vector2(0,1); rt.anchoredPosition=pos; var img=go.GetComponent<Image>(); img.sprite=Resources.GetBuiltinResource<Sprite>("UISprite.psd"); img.type=Image.Type.Sliced; img.color=new Color(0,0,0,0.35f); return rt; }
        (ScrollRect scroll, RectTransform content) NewScroll(Transform parent){ var vp=new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask)); vp.transform.SetParent(parent,false); var vpRT=vp.GetComponent<RectTransform>(); vpRT.sizeDelta=new Vector2(340,330); vpRT.anchoredPosition=new Vector2(10,-10); vp.GetComponent<Image>().sprite=Resources.GetBuiltinResource<Sprite>("UISprite.psd"); vp.GetComponent<Image>().type=Image.Type.Sliced; vp.GetComponent<Image>().color=new Color(0,0,0,0.25f); vp.GetComponent<Mask>().showMaskGraphic=false; var sr=new GameObject("ScrollRect", typeof(RectTransform), typeof(ScrollRect)); sr.transform.SetParent(parent,false); var srRT=sr.GetComponent<RectTransform>(); srRT.sizeDelta=new Vector2(340,330); srRT.anchoredPosition=new Vector2(10,-10); var scroll=sr.GetComponent<ScrollRect>(); scroll.viewport=vpRT; scroll.horizontal=false; var cont=new GameObject("Content", typeof(RectTransform)); cont.transform.SetParent(vp.transform,false); var cRT=cont.GetComponent<RectTransform>(); cRT.pivot=new Vector2(0,1); cRT.sizeDelta=new Vector2(320,10); scroll.content=cRT; return (scroll, cRT); }
        Button CreateButton(Transform parent, string label, Vector2 pos){ var go=new GameObject("Btn", typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(parent,false); var rt=go.GetComponent<RectTransform>(); rt.sizeDelta=new Vector2(320,28); rt.pivot=new Vector2(0,1); rt.anchoredPosition=pos; var img=go.GetComponent<Image>(); img.sprite=Resources.GetBuiltinResource<Sprite>("UISprite.psd"); img.type=Image.Type.Sliced; img.color=new Color(0.2f,0.2f,0.2f,0.9f); var btn=go.GetComponent<Button>(); var txt=new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)); txt.transform.SetParent(go.transform,false); var tr=txt.GetComponent<RectTransform>(); tr.sizeDelta=new Vector2(300,24); tr.pivot=new Vector2(0,1); tr.anchoredPosition=new Vector2(10,-2); var t=txt.GetComponent<TextMeshProUGUI>(); t.text=label; t.fontSize=16; t.color=Color.white; return btn; }
        TextMeshProUGUI CreateTMP(Transform parent,string name,string content, Vector2 pos,int size=16, bool bold=false){ var go=new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(parent,false); var rt=go.GetComponent<RectTransform>(); rt.sizeDelta=new Vector2(720,28); rt.pivot=new Vector2(0,1); rt.anchoredPosition=pos; var t=go.GetComponent<TextMeshProUGUI>(); t.text=content; t.fontSize=size; if(bold) t.fontStyle = FontStyles.Bold; t.color=Color.white; return t; }
    }
}
