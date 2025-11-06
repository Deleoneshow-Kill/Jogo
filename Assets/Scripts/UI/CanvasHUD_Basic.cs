using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using CleanRPG.Battle;
using CleanRPG.Core;

namespace CleanRPG.UI
{
    public class CanvasHUD_Basic : MonoBehaviour
    {
        public CleanRPG.Battle.BattleBootstrap3D bootstrap;
        private Canvas canvas;
        private RectTransform root;
    private Image energyWheelPlayer, energyWheelEnemy;
    private Transform turnRow;
    private Button btn1, btn2, btn3, btnAuto, btnSpeed;
    private Image ico1, ico2, ico3;
    private Image btnAutoImage, btnSpeedImage;
    private Text txtRound, txtEnergyPlayer, txtEnergyEnemy, txtCurrent, txtHP, txtStats, txtCosmos, txtSynergy, txtTooltip;
        private Sprite uiSprite;
        private Sprite knobSprite;
    private Text[] abilityLabels;
    private TooltipRelay[] abilityTooltipHooks;
    private TooltipRelay tooltipAuto, tooltipSpeed;
    private Text txtAutoLabel, txtSpeedLabel;

        void Awake()
        {
            EnsureBootstrapReference();
            uiSprite = Resources.Load<Sprite>("UISprite");
            if (!uiSprite)
            {
                uiSprite = Resources.GetBuiltinResource<Sprite>("UISprite.psd");
            }
            knobSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");
            canvas = new GameObject("HUD").AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvas.gameObject.AddComponent<GraphicRaycaster>();
            canvas.sortingOrder = 500;
            if (EventSystem.current == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }
            root = canvas.transform as RectTransform;

            var panel = CreatePanel("Top", new Vector2(0,-28), new Vector2(520,92), new Vector2(0.5f,1f));
            var panelImage = panel.GetComponent<Image>(); panelImage.color = new Color(0f,0f,0f,0.32f);
            txtRound = CreateText(panel, "Round", "Round 1", new Vector2(20,-48), 24, true);
            txtRound.rectTransform.sizeDelta = new Vector2(200,26);
            txtEnergyPlayer = CreateText(panel, "EnergyPlayer", "Jogador: 0/10 (base 3)", new Vector2(20,-12));
            txtEnergyEnemy = CreateText(panel, "EnergyEnemy", "Inimigo: 0/10", new Vector2(260,-12));
            txtEnergyPlayer.rectTransform.sizeDelta = new Vector2(220,22);
            txtEnergyEnemy.rectTransform.sizeDelta = new Vector2(220,22);
            energyWheelPlayer = CreateImage(panel,"EnergyPlayerWheel", new Vector2(360,-6), new Vector2(48,48));
            energyWheelPlayer.type = Image.Type.Filled; energyWheelPlayer.fillMethod = Image.FillMethod.Radial360; energyWheelPlayer.fillOrigin = (int)Image.Origin360.Top; energyWheelPlayer.fillClockwise=false; energyWheelPlayer.sprite = uiSprite; energyWheelPlayer.color = new Color(0.2f,0.75f,0.35f,0.95f);
            energyWheelEnemy = CreateImage(panel,"EnergyEnemyWheel", new Vector2(420,-6), new Vector2(48,48));
            energyWheelEnemy.type = Image.Type.Filled; energyWheelEnemy.fillMethod = Image.FillMethod.Radial360; energyWheelEnemy.fillOrigin = (int)Image.Origin360.Top; energyWheelEnemy.fillClockwise=false; energyWheelEnemy.sprite = uiSprite; energyWheelEnemy.color = new Color(0.85f,0.25f,0.25f,0.95f);

            turnRow = CreatePanel("TurnRow", new Vector2(0,-136), new Vector2(620,68), new Vector2(0.5f,1f));
            var turnImage = turnRow.GetComponent<Image>(); turnImage.color = new Color(0f,0f,0f,0.18f);

            var skills = CreatePanel("Controls", new Vector2(-20,24), new Vector2(320,420), new Vector2(1f,0f));
            var skillsImage = skills.GetComponent<Image>(); skillsImage.color = new Color(0,0,0,0f);
            abilityLabels = new Text[3]; abilityTooltipHooks = new TooltipRelay[3];
            btn1 = CreateAbilityButton(skills,"B1","1", new Vector2(-20,300), out abilityLabels[0], 96f); btn1.onClick.AddListener(()=>{ if (!EnsureBootstrapReference()) return; bootstrap.TriggerAbilityKey(1); }); abilityTooltipHooks[0] = EnsureTooltipRelay(btn1.gameObject); ico1 = CreateCenteredImage(btn1.transform, "Ico1", new Vector2(76,76));
            btn2 = CreateAbilityButton(skills,"B2","2", new Vector2(-20,180), out abilityLabels[1], 110f); btn2.onClick.AddListener(()=>{ if (!EnsureBootstrapReference()) return; bootstrap.TriggerAbilityKey(2); }); abilityTooltipHooks[1] = EnsureTooltipRelay(btn2.gameObject); ico2 = CreateCenteredImage(btn2.transform, "Ico2", new Vector2(88,88));
            btn3 = CreateAbilityButton(skills,"B3","3", new Vector2(-20,20), out abilityLabels[2], 140f); btn3.onClick.AddListener(()=>{ if (!EnsureBootstrapReference()) return; bootstrap.TriggerAbilityKey(3); }); abilityTooltipHooks[2] = EnsureTooltipRelay(btn3.gameObject); ico3 = CreateCenteredImage(btn3.transform, "Ico3", new Vector2(110,110));
            foreach(var relay in abilityTooltipHooks) relay.Init(SetTooltip, ClearTooltip);

            btnAuto = CreateAbilityButton(skills,"Auto","Auto", new Vector2(-150,100), out txtAutoLabel, 76f);
            btnAuto.onClick.AddListener(()=>
            {
                if (!EnsureBootstrapReference()) return;
                bootstrap.ToggleAutoBattle();
            });
            btnAutoImage = btnAuto.GetComponent<Image>();
            tooltipAuto = EnsureTooltipRelay(btnAuto.gameObject);
            tooltipAuto.Init(SetTooltip, ClearTooltip);
            tooltipAuto.SetContent("Alterna o modo automático da batalha.");
            btnAuto.transform.SetAsLastSibling();
            if (txtAutoLabel){ txtAutoLabel.fontSize = 10; txtAutoLabel.alignment = TextAnchor.MiddleCenter; }

            btnSpeed = CreateAbilityButton(skills,"Speed","x1.0", new Vector2(-150,20), out txtSpeedLabel, 76f);
            btnSpeed.onClick.AddListener(()=>
            {
                if (!EnsureBootstrapReference()) return;
                bootstrap.CycleSpeed();
            });
            btnSpeedImage = btnSpeed.GetComponent<Image>(); tooltipSpeed = EnsureTooltipRelay(btnSpeed.gameObject); tooltipSpeed.Init(SetTooltip, ClearTooltip); tooltipSpeed.SetContent("Altera a velocidade das animações da batalha.");
            if (txtSpeedLabel){ txtSpeedLabel.fontSize = 10; txtSpeedLabel.alignment = TextAnchor.MiddleCenter; }

            txtTooltip = CreateText(root,"Tooltip","Descrição da habilidade", new Vector2(20,-120));
            var tooltipRT = txtTooltip.rectTransform;
            tooltipRT.sizeDelta = new Vector2(520,48);
            tooltipRT.anchorMin = new Vector2(0f,1f);
            tooltipRT.anchorMax = new Vector2(0f,1f);
            tooltipRT.pivot = new Vector2(0f,1f);
            tooltipRT.anchoredPosition = new Vector2(20,-120);
            ClearTooltip();

            var info = CreatePanel("Info", new Vector2(-200,40), new Vector2(360,230), new Vector2(1f,0.5f));
            var infoImage = info.GetComponent<Image>(); infoImage.color = new Color(0f,0f,0f,0.28f);
            txtCurrent = CreateText(info,"Current","Turno de: —", new Vector2(10,-10), 20, true);
            txtHP = CreateText(info,"HP","HP: —", new Vector2(10,-44));
            txtStats = CreateText(info,"Stats","Crit: — | Seventh Sense: —", new Vector2(10,-78));
            txtCosmos = CreateText(info,"Cosmos","Cosmos: —", new Vector2(10,-112));
            txtSynergy = CreateText(info,"Synergy","Sinergias: —", new Vector2(10,-146));
            txtSynergy.rectTransform.sizeDelta = new Vector2(320,96);
        }

        void Update()
        {
            if(!EnsureBootstrapReference()) return;
            var st = bootstrap.GetStateTMP();
            int maxEnergy = Mathf.Max(1, st.energyMax);
            txtRound.text = $"Round {st.round}";
            if (txtEnergyPlayer) txtEnergyPlayer.text = $"Jogador: {st.energyPlayer}/{maxEnergy} (base {st.energyBaseline})";
            if (txtEnergyEnemy) txtEnergyEnemy.text = $"Inimigo: {st.energyEnemy}/{maxEnergy}";
            if (energyWheelPlayer) energyWheelPlayer.fillAmount = maxEnergy > 0 ? Mathf.Clamp01(st.energyPlayer/(float)maxEnergy) : 0f;
            if (energyWheelEnemy) energyWheelEnemy.fillAmount = maxEnergy > 0 ? Mathf.Clamp01(st.energyEnemy/(float)maxEnergy) : 0f;

            foreach(Transform t in turnRow) UnityEngine.Object.Destroy(t.gameObject);
            int i=0; foreach(var chip in st.turnChipsTMP.Take(8)){ var p = CreateImage(turnRow,"chip"+i, new Vector2(10+i*60,-10), new Vector2(50,50)); p.color = chip.isEnemy? new Color(0.3f,0.6f,1f): new Color(0.3f,1f,0.6f); i++; }

            if (st.current.name!=null)
            {
                txtCurrent.text = $"Turno de: {st.current.name} Lv{st.current.level} R{st.current.rank} SPD {st.current.spd}";
                txtHP.text = $"HP: {st.current.hp}/{st.current.maxHP}  Shield: {st.current.shield}";
                txtStats.text = $"Crit: {st.current.crit}% | Seventh Sense: {(st.current.seventhSense?"Ativo":"Inativo")}";
                txtCosmos.text = FormatCosmos(st.current.cosmos);
            }
            else
            {
                txtCurrent.text = "Turno de: —";
                txtHP.text = "HP: —";
                txtStats.text = "Crit: — | Seventh Sense: —";
                txtCosmos.text = "Cosmos: —";
            }
            txtSynergy.text = FormatSynergies(st.synergies);
            var ab = bootstrap.GetAbilityBar();
            bool autoMode = st.autoEnabled;
            UpdateAbility(btn1, abilityLabels[0], abilityTooltipHooks[0], ab.FirstOrDefault(a=>a.index==1), autoMode);
            UpdateAbility(btn2, abilityLabels[1], abilityTooltipHooks[1], ab.FirstOrDefault(a=>a.index==2), autoMode);
            UpdateAbility(btn3, abilityLabels[2], abilityTooltipHooks[2], ab.FirstOrDefault(a=>a.index==3), autoMode);

            if (btnAuto && txtAutoLabel)
            {
                txtAutoLabel.text = st.autoEnabled ? "Auto\nOn" : "Auto\nOff";
                if (btnAutoImage) btnAutoImage.color = st.autoEnabled ? new Color(0.1f,0.7f,0.35f,0.95f) : new Color(0.25f,0.25f,0.25f,0.9f);
            }

            if (btnSpeed && txtSpeedLabel)
            {
                txtSpeedLabel.text = $"x{st.speedMultiplier:0.0}";
                if (btnSpeedImage) btnSpeedImage.color = st.speedMultiplier > 1f ? new Color(0.1f,0.45f,0.8f,0.95f) : new Color(0.25f,0.25f,0.35f,0.9f);
            }

            // update icons
            void SetIcon(UnityEngine.UI.Image img, string path){ if (img==null) return; if (string.IsNullOrEmpty(path)){ img.enabled=false; return; } var sp = Resources.Load<Sprite>(path); if (sp){ img.enabled=true; img.sprite = sp; } else img.enabled=false; }
            var a1 = ab.FirstOrDefault(a=>a.index==1);
            var a2 = ab.FirstOrDefault(a=>a.index==2);
            var a3 = ab.FirstOrDefault(a=>a.index==3);
            SetIcon(ico1, a1.iconPath);
            SetIcon(ico2, a2.iconPath);
            SetIcon(ico3, a3.iconPath);
        }

        void UpdateAbility(Button btn, Text label, TooltipRelay tooltipRelay, CleanRPG.Battle.BattleBootstrap3D.AbilityUI ab, bool autoMode)
        {
            if (label==null) return;
            if (string.IsNullOrEmpty(ab.id))
            {
                label.text = "—";
                btn.interactable=false;
                tooltipRelay?.SetContent(string.Empty);
                return;
            }
            var costLine = ab.cost>0? $"⛁{ab.cost}" : string.Empty;
            label.text = string.IsNullOrEmpty(costLine)? $"{ab.index}. {ab.name}" : $"{ab.index}. {ab.name}\n{costLine}";
            btn.interactable = !autoMode && ab.canUse;
            tooltipRelay?.SetContent(ab.tooltip);
        }

        void SetTooltip(string text){ if (txtTooltip!=null) txtTooltip.text = string.IsNullOrEmpty(text)? "Descrição da habilidade" : text; }
        void ClearTooltip(){ SetTooltip(string.Empty); }

        string FormatCosmos(string[] cosmos){ if (cosmos==null || cosmos.Length==0) return "Cosmos: —"; return "Cosmos: " + string.Join(", ", cosmos); }

        string FormatSynergies(CleanRPG.Battle.BattleBootstrap3D.HUDState.SynergyInfo[] synergies)
        {
            if (synergies==null || synergies.Length==0) return "Sinergias: —";
            var lines = new List<string>();
            foreach (var s in synergies)
            {
                var prefix = s.active?"[Ativa]":"[Bloqueada]";
                lines.Add($"{prefix} {s.name} {FormatBonus(s.bonus)}");
            }
            return string.Join("\n", lines);
        }

        string FormatBonus(StatBundle bonus)
        {
            var parts = new List<string>();
            if (bonus.hp != 0) parts.Add($"HP+{bonus.hp}");
            if (bonus.atk != 0) parts.Add($"ATK+{bonus.atk}");
            if (bonus.defStat != 0) parts.Add($"DEF+{bonus.defStat}");
            if (bonus.spd != 0) parts.Add($"SPD+{bonus.spd}");
            if (bonus.crit != 0) parts.Add($"CRIT+{bonus.crit}%");
            return parts.Count>0?"("+string.Join(" ", parts)+")":"";
        }

        TooltipRelay EnsureTooltipRelay(GameObject go)
        {
            var relay = go.GetComponent<TooltipRelay>();
            if (relay==null) relay = go.AddComponent<TooltipRelay>();
            return relay;
        }

        bool EnsureBootstrapReference()
        {
            if (bootstrap) return true;
            bootstrap = FindObjectOfType<CleanRPG.Battle.BattleBootstrap3D>();
            return bootstrap != null;
        }

    RectTransform CreatePanel(string n, Vector2 pos, Vector2 size, Vector2 anchor){ var go=new GameObject(n, typeof(RectTransform), typeof(Image)); go.transform.SetParent(root,false); var rt=go.GetComponent<RectTransform>(); rt.sizeDelta=size; rt.anchorMin=anchor; rt.anchorMax=anchor; rt.pivot=anchor; rt.anchoredPosition=pos; var img=go.GetComponent<Image>(); img.sprite=uiSprite; img.type=Image.Type.Sliced; img.color=new Color(0,0,0,0.35f); return rt; }
    Image CreateImage(Transform p,string n, Vector2 pos, Vector2 size){ var go=new GameObject(n, typeof(RectTransform), typeof(Image)); go.transform.SetParent(p,false); var rt=go.GetComponent<RectTransform>(); rt.sizeDelta=size; rt.anchorMin=new Vector2(0,1); rt.anchorMax=new Vector2(0,1); rt.pivot=new Vector2(0,1); rt.anchoredPosition=pos; var img=go.GetComponent<Image>(); img.sprite=uiSprite; img.type=Image.Type.Filled; return img; }
    Image CreateCenteredImage(Transform p,string n, Vector2 size){ var go=new GameObject(n, typeof(RectTransform), typeof(Image)); go.transform.SetParent(p,false); var rt=go.GetComponent<RectTransform>(); rt.sizeDelta=size; rt.anchorMin=new Vector2(0.5f,0.5f); rt.anchorMax=new Vector2(0.5f,0.5f); rt.pivot=new Vector2(0.5f,0.5f); rt.anchoredPosition=Vector2.zero; var img=go.GetComponent<Image>(); img.type=Image.Type.Simple; img.preserveAspect = true; return img; }
    Button CreateAbilityButton(Transform parent,string name,string label, Vector2 pos, out Text labelRef, float size = 96f, bool anchorFromRight = true){ var go=new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(parent,false); var rt=go.GetComponent<RectTransform>(); rt.sizeDelta=new Vector2(size,size); if(anchorFromRight){ rt.anchorMin=new Vector2(1f,0f); rt.anchorMax=new Vector2(1f,0f); rt.pivot=new Vector2(1f,0f); } else { rt.anchorMin=new Vector2(0f,0f); rt.anchorMax=new Vector2(0f,0f); rt.pivot=new Vector2(0f,0f); } rt.anchoredPosition=pos; var img=go.GetComponent<Image>(); img.sprite = knobSprite ? knobSprite : uiSprite; img.type=Image.Type.Simple; img.color=new Color(1f,1f,1f,0.9f); var btn=go.GetComponent<Button>(); var txtObj = new GameObject("Label", typeof(RectTransform), typeof(Text)); txtObj.transform.SetParent(go.transform,false); var tr=txtObj.GetComponent<RectTransform>(); tr.sizeDelta=new Vector2(size-12f, Mathf.Max(24f, size*0.35f)); tr.anchorMin=new Vector2(0.5f,0f); tr.anchorMax=new Vector2(0.5f,0f); tr.pivot=new Vector2(0.5f,0f); tr.anchoredPosition=new Vector2(0,8); var t=txtObj.GetComponent<Text>(); t.text=label; t.fontSize=12; t.color=Color.white; t.alignment = TextAnchor.MiddleCenter; t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow; labelRef = t; return btn; }
    Button CreateButton(Transform p,string n,string label, Vector2 pos, out Text labelRef){ var go=new GameObject(n, typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(p,false); var rt=go.GetComponent<RectTransform>(); rt.sizeDelta=new Vector2(150,44); rt.anchorMin=new Vector2(0,1); rt.anchorMax=new Vector2(0,1); rt.pivot=new Vector2(0,1); rt.anchoredPosition=pos; var img=go.GetComponent<Image>(); img.sprite=uiSprite; img.type=Image.Type.Sliced; img.color=new Color(0.15f,0.15f,0.2f,0.9f); var btn=go.GetComponent<Button>(); var txt= new GameObject("Label", typeof(RectTransform), typeof(Text)); txt.transform.SetParent(go.transform,false); var tr=txt.GetComponent<RectTransform>(); tr.sizeDelta=new Vector2(110,28); tr.anchorMin=new Vector2(0,1); tr.anchorMax=new Vector2(0,1); tr.pivot=new Vector2(0,1); tr.anchoredPosition=new Vector2(46,-8); var t=txt.GetComponent<Text>(); t.text=label; t.fontSize=15; t.color=Color.white; t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow; labelRef = t; return btn; }
    Text CreateText(Transform parent, string name, string content, Vector2 pos, int size=16, bool bold=false){ var go=new GameObject(name, typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent,false); var rt=go.GetComponent<RectTransform>(); rt.sizeDelta=new Vector2(720,28); rt.anchorMin=new Vector2(0,1); rt.anchorMax=new Vector2(0,1); rt.pivot=new Vector2(0,1); rt.anchoredPosition=pos; var t=go.GetComponent<Text>(); t.text=content; t.fontSize=size; if(bold) t.fontStyle = FontStyle.Bold; t.color=Color.white; t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow; return t; }

        class TooltipRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            Action<string> onEnter;
            Action onExit;
            string message;
            public void Init(Action<string> enter, Action exit){ onEnter = enter; onExit = exit; }
            public void SetContent(string value){ message = value; }
            public void OnPointerEnter(PointerEventData eventData){ onEnter?.Invoke(message); }
            public void OnPointerExit(PointerEventData eventData){ onExit?.Invoke(); }
        }
    }
}