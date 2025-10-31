
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CleanRPG.Battle;

namespace CleanRPG.UI
{
    public class ArenaPickBanUI : MonoBehaviour
    {
    public CleanRPG.Battle.BattleBootstrap3D bootstrap;
        private Canvas canvas; private RectTransform panel; private Sprite uiSprite;
        private HashSet<string> banned = new HashSet<string>();
        private List<string> picksPlayer = new List<string>();
        private List<string> picksEnemy = new List<string>();
        private int step = 0; // 0/1 Ban P1/P2, 2/3 Ban E1/E2, então picks alternados até 5x
        private TMP_Text phaseLabel, statusLabel, timerLabel;
        private RectTransform gridAll, gridP, gridE;
        private Button btnStart, btnAutoEnemy, btnFirstPick; private bool playerFirstPick = true; private float timeLeft = 20f;

        void Awake()
        {
            uiSprite = Resources.GetBuiltinResource<Sprite>("UISprite.psd");
            canvas = new GameObject("ArenaCanvas").AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvas.gameObject.AddComponent<GraphicRaycaster>();
            panel = CreatePanel(new Vector2(10,-860), new Vector2(1000,300), new Vector2(0,1));
            panel.gameObject.SetActive(false);
            Build();
        }

        void Update(){
            if (Input.GetKeyDown(KeyCode.F4)) panel.gameObject.SetActive(!panel.gameObject.activeSelf);
            if (!panel.gameObject.activeSelf) return;
            timeLeft -= Time.deltaTime;
            if (timeLeft < 0f) { AutoPhase(); }
            timerLabel.text = $"Tempo: {System.Math.Max(0f,timeLeft):0.0}s";
        }

        
        void ResetFlow(){
            banned.Clear(); picksPlayer.Clear(); picksEnemy.Clear();
            step = playerFirstPick? 0 : 2; // decide quem bane primeiro
            phaseLabel.text = playerFirstPick? "Arena — Fase: Ban do Jogador (1/2)" : "Arena — Fase: Ban do Inimigo (1/2)";
            timeLeft = 20f; RefreshLists();
        }

        void AutoPhase(){
            // choose automatically a valid entry
            if (step<=3){ // ban phases: 0,1 (player), 2,3 (enemy) depending on first pick
                foreach (var kv in bootstrap.charDB.Values){
                    var id = kv.id;
                    if (banned.Contains(id) || picksPlayer.Contains(id) || picksEnemy.Contains(id)) continue;
                    banned.Add(id);
                    step++;
                    break;
                }
                UpdatePhaseText();
            } else {
                // pick phases alternate until 10 picks (5 each)
                var pool = bootstrap.charDB.Values;
                foreach (var kv in pool){
                    var id = kv.id;
                    if (banned.Contains(id) || picksPlayer.Contains(id) || picksEnemy.Contains(id)) continue;
                    if (IsPlayerTurnToPick()) picksPlayer.Add(id); else picksEnemy.Add(id);
                    if (picksPlayer.Count + picksEnemy.Count >= 10) step = 100;
                    else step++;
                    break;
                }
                UpdatePhaseText();
            }
            RefreshLists();
            timeLeft = 20f;
        }

        bool IsPlayerTurnToPick(){
            // After bans (0..3), step 4+ : even/odd based on starting side
            int idx = step - 4;
            if (idx < 0) return playerFirstPick; // shouldn't happen
            return (idx % 2 == 0) == playerFirstPick;
        }

        void UpdatePhaseText(){
            if (step<=1) phaseLabel.text = "Arena — Fase: Ban do Jogador ("+(step+1)+"/2)";
            else if (step<=3) phaseLabel.text = "Arena — Fase: Ban do Inimigo ("+(step-1)+"/2)";
            else if (step>=4 && step<100){
                int picksDone = picksPlayer.Count + picksEnemy.Count;
                phaseLabel.text = (IsPlayerTurnToPick()? "Arena — Pick do Jogador" : "Arena — Pick do Inimigo") + $" ({picksDone+1}/10)";
            } else {
                phaseLabel.text = "Arena — Pronto para Iniciar";
            }
        }

        void Build()
        {
            phaseLabel = CreateTMP(panel,"Phase","Arena (F4) — Fase: Ban do Jogador (1/2)", new Vector2(10,-10), 22, true);
            statusLabel = CreateTMP(panel,"Status","Bane 2× por lado, picks 5×5.", new Vector2(10,-44), 16, false);
            timerLabel = CreateTMP(panel, "Timer", "Tempo: 20.0s", new Vector2(400,-44), 16, true);

            gridAll = CreateSub(panel, "Pool", new Vector2(10,-80), new Vector2(600,200));
            GridOfAll(gridAll);

            gridP = CreateSub(panel, "PicksP", new Vector2(620,-80), new Vector2(180,200));
            CreateTMP(gridP,"LblP","Jogador", new Vector2(10,-10), 18, true);

            gridE = CreateSub(panel, "PicksE", new Vector2(810,-80), new Vector2(180,200));
            CreateTMP(gridE,"LblE","Inimigo", new Vector2(10,-10), 18, true);

            btnStart = CreateButton(panel, "Iniciar Arena", new Vector2(820,-44));
            btnFirstPick = CreateButton(panel, "First: Jogador", new Vector2(560,-44)); btnFirstPick.onClick.AddListener(()=>{ playerFirstPick=!playerFirstPick; btnFirstPick.transform.Find("Label").GetComponent<TMPro.TextMeshProUGUI>().text = playerFirstPick? "First: Jogador" : "First: Inimigo"; ResetFlow(); }); btnStart.onClick.AddListener(StartArena); btnStart.interactable=false;
            btnAutoEnemy = CreateButton(panel, "Auto Enemy", new Vector2(700,-44)); btnAutoEnemy.onClick.AddListener(AutoEnemyAction);
        }

        void GridOfAll(RectTransform holder)
        {
            foreach(Transform t in holder) if (t.name!="Lbl") Destroy(t.gameObject);
            int col=0,row=0;
            foreach (var kv in bootstrap.charDB.Values.OrderBy(x=>x.displayName))
            {
                var id = kv.id;
                var b = CreateButton(holder, kv.displayName, new Vector2(10+col*190, -10 - row*30));
                b.onClick.AddListener(()=>OnPoolClicked(id, kv.displayName));
                col++; if (col>=3){ col=0; row++; }
            }
        }

        void OnPoolClicked(string id, string name)
        {
            if (step<=3){ // bans
                if (banned.Contains(id)) return;
                // placeholder
            }
            if (step<=3){
                if (!banned.Contains(id)){ banned.Add(id); step++; UpdatePhaseText(); }
            }
            else if (step>=4 && step<100){
                if (banned.Contains(id) || picksPlayer.Contains(id) || picksEnemy.Contains(id)) return;
                if (IsPlayerTurnToPick()) picksPlayer.Add(id); else picksEnemy.Add(id);
                if (picksPlayer.Count + picksEnemy.Count >= 10) step=100; else step++;
                UpdatePhaseText();
            }
            RefreshLists();
            timeLeft = 20f;
        }

        void AutoEnemyAction()
        {
            if (step<=3){
                foreach (var kv in bootstrap.charDB.Values){
                    var id = kv.id;
                    if (banned.Contains(id) || picksPlayer.Contains(id) || picksEnemy.Contains(id)) continue;
                    banned.Add(id); step++; UpdatePhaseText(); break;
                }
            } else if (step>=4 && step<100){
                foreach (var kv in bootstrap.charDB.Values){
                    var id = kv.id;
                    if (banned.Contains(id) || picksPlayer.Contains(id) || picksEnemy.Contains(id)) continue;
                    if (!IsPlayerTurnToPick()) { picksEnemy.Add(id); step++; UpdatePhaseText(); break; }
                }
            }
            RefreshLists();
            timeLeft = 20f;
        }

        void RefreshLists()
        {
            foreach (Transform t in gridP) if (t.name!="LblP") Destroy(t.gameObject);
            foreach (Transform t in gridE) if (t.name!="LblE") Destroy(t.gameObject);

            int y=-40; foreach (var id in picksPlayer) { var b = CreateButton(gridP, bootstrap.charDB[id].displayName, new Vector2(10,y)); y-=30; }
            y=-40; foreach (var id in picksEnemy) { var b = CreateButton(gridE, bootstrap.charDB[id].displayName, new Vector2(10,y)); y-=30; }

            statusLabel.text = $"Bans: {string.Join(", ", banned.Select(x=>bootstrap.charDB[x].displayName))}";
            btnStart.interactable = (picksPlayer.Count>=5 && picksEnemy.Count>=5);
        }

        void StartArena()
        {
            bootstrap.SetupWithTeam(new List<string>(picksPlayer.GetRange(0,5)), new List<string>(picksEnemy.GetRange(0,5)));
            panel.gameObject.SetActive(false);
        }

        RectTransform CreatePanel(Vector2 pos, Vector2 size, Vector2 anchor){ var go=new GameObject("ArenaPanel", typeof(RectTransform), typeof(Image)); go.transform.SetParent(canvas.transform,false); var rt=go.GetComponent<RectTransform>(); rt.sizeDelta=size; rt.anchorMin=anchor; rt.anchorMax=anchor; rt.pivot=new Vector2(0,1); rt.anchoredPosition=pos; var img=go.GetComponent<Image>(); img.sprite=Resources.GetBuiltinResource<Sprite>("UISprite.psd"); img.type=Image.Type.Sliced; img.color=new Color(0,0,0,0.55f); return rt; }
        RectTransform CreateSub(Transform parent,string name, Vector2 pos, Vector2 size){ var go=new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent,false); var rt=go.GetComponent<RectTransform>(); rt.sizeDelta=size; rt.pivot=new Vector2(0,1); rt.anchoredPosition=pos; var img=go.GetComponent<Image>(); img.sprite=Resources.GetBuiltinResource<Sprite>("UISprite.psd"); img.type=Image.Type.Sliced; img.color=new Color(0,0,0,0.35f); return rt; }
        Button CreateButton(Transform parent, string label, Vector2 pos){ var go=new GameObject("Btn", typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(parent,false); var rt=go.GetComponent<RectTransform>(); rt.sizeDelta=new Vector2(180,28); rt.pivot=new Vector2(0,1); rt.anchoredPosition=pos; var img=go.GetComponent<Image>(); img.sprite=Resources.GetBuiltinResource<Sprite>("UISprite.psd"); img.type=Image.Type.Sliced; img.color=new Color(0.2f,0.2f,0.2f,0.9f); var btn=go.GetComponent<Button>(); var txt=new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)); txt.transform.SetParent(go.transform,false); var tr=txt.GetComponent<RectTransform>(); tr.sizeDelta=new Vector2(160,24); tr.pivot=new Vector2(0,1); tr.anchoredPosition=new Vector2(10,-2); var t=txt.GetComponent<TextMeshProUGUI>(); t.text=label; t.fontSize=14; t.color=Color.white; return btn; }
        TextMeshProUGUI CreateTMP(Transform parent,string name,string content, Vector2 pos,int size=16, bool bold=false){ var go=new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(parent,false); var rt=go.GetComponent<RectTransform>(); rt.sizeDelta=new Vector2(720,28); rt.pivot=new Vector2(0,1); rt.anchoredPosition=pos; var t=go.GetComponent<TextMeshProUGUI>(); t.text=content; t.fontSize=size; if(bold) t.fontStyle = FontStyles.Bold; t.color=Color.white; return t; }
    }
}
