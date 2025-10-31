using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CleanRPG.Battle;
using CleanRPG.Core;
using UnityEngine.EventSystems;

namespace CleanRPG.UI
{
    /// <summary>
    /// TMP-based battle HUD matching the mobile layout reference while keeping all game hooks intact.
    /// </summary>
    public class CanvasHUD_TMP_v6 : MonoBehaviour
    {
        public BattleBootstrap3D bootstrap;

        Canvas canvas;
        RectTransform root;

    Image energyWheelPlayer;
    Image energyWheelEnemy;
    Image timerRing;
    TMP_Text txtRound;
    TMP_Text txtTimer;
    TMP_Text txtEnergyPlayer;
    TMP_Text txtEnergyEnemy;

        RectTransform turnRow;
    RectTransform abilityColumn;

        readonly Button[] abilityButtons = new Button[3];
        readonly TMP_Text[] abilityLabels = new TMP_Text[3];
        readonly Image[] abilityIcons = new Image[3];
        readonly TooltipRelay[] abilityTooltips = new TooltipRelay[3];
    readonly Image[] abilityConnectors = new Image[3];

        Button btnAuto;
        TMP_Text txtAutoLabel;
        Image btnAutoImage;
        TooltipRelay tooltipAuto;

        Button btnSpeed;
        TMP_Text txtSpeedLabel;
        Image btnSpeedImage;
        TooltipRelay tooltipSpeed;

        Image abilityRingOuter;
        Image abilityRingInner;
        Image currentPortrait;

        TMP_Text txtCurrent;
        TMP_Text txtHP;
        TMP_Text txtStats;
        TMP_Text txtCosmos;
        TMP_Text txtSynergy;
        TMP_Text txtTooltip;

        readonly Dictionary<string, Sprite> spriteCache = new();

        Sprite uiSprite;
        Sprite knobSprite;

        const int MaxTurnChips = 7;
    const float RoundTimerMax = 30f;
    float roundTimerValue = RoundTimerMax;
    int lastRoundDisplayed = -1;

        void Awake()
        {
            EnsureBootstrapReference();

            uiSprite = Resources.GetBuiltinResource<Sprite>("UISprite.psd");
            knobSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");

            var canvasGO = new GameObject("CanvasHUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(transform, false);

            canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;

            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            if (EventSystem.current == null)
            {
                _ = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }

            root = canvasGO.GetComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            BuildTopBar();
            BuildTurnTimeline();
            BuildControls();
            BuildLeftMenu();
            BuildInfoPanel();
            BuildTooltip();
        }

        void Update()
        {
            if (!EnsureBootstrapReference())
                return;

            var state = bootstrap.GetStateTMP();
            int maxEnergy = Mathf.Max(1, state.energyMax);

            if (lastRoundDisplayed != state.round)
            {
                lastRoundDisplayed = state.round;
                roundTimerValue = RoundTimerMax;
            }
            else
            {
                roundTimerValue = Mathf.Max(0f, roundTimerValue - Time.deltaTime);
            }

            if (txtTimer != null)
                txtTimer.text = Mathf.Clamp(Mathf.CeilToInt(roundTimerValue), 0, (int)RoundTimerMax).ToString("00");
            if (timerRing != null)
                timerRing.fillAmount = Mathf.Clamp01(roundTimerValue / RoundTimerMax);

            if (txtRound != null)
                txtRound.text = $"Rodada {state.round}";
            if (txtEnergyPlayer != null)
                txtEnergyPlayer.text = $"Equipe: {state.energyPlayer}/{maxEnergy}\nBase {state.energyBaseline}";
            if (txtEnergyEnemy != null)
                txtEnergyEnemy.text = $"Inimigos: {state.energyEnemy}/{maxEnergy}";

            if (energyWheelPlayer != null)
                energyWheelPlayer.fillAmount = Mathf.Clamp01(state.energyPlayer / (float)maxEnergy);
            if (energyWheelEnemy != null)
                energyWheelEnemy.fillAmount = Mathf.Clamp01(state.energyEnemy / (float)maxEnergy);

            if (abilityRingOuter != null)
                abilityRingOuter.color = state.current.isEnemy ? new Color(0.82f, 0.25f, 0.25f, 0.32f) : new Color(0.2f, 0.55f, 0.95f, 0.32f);
            if (abilityRingInner != null)
                abilityRingInner.color = state.current.isEnemy ? new Color(0.18f, 0.05f, 0.08f, 0.78f) : new Color(0f, 0.08f, 0.2f, 0.78f);

            UpdateCurrentPortrait(state);
            UpdateTurnTimeline(state);
            UpdateInfoPanel(state);
            UpdateAbilities(state);
            UpdateControls(state);
            if (txtSynergy != null)
                txtSynergy.text = FormatSynergies(state.synergies);
        }

        void BuildTopBar()
        {
            var containerGO = new GameObject("TopHUD", typeof(RectTransform));
            containerGO.transform.SetParent(root, false);
            var container = containerGO.GetComponent<RectTransform>();
            container.anchorMin = new Vector2(0.5f, 1f);
            container.anchorMax = new Vector2(0.5f, 1f);
            container.pivot = new Vector2(0.5f, 1f);
            container.sizeDelta = new Vector2(720f, 200f);
            container.anchoredPosition = new Vector2(0f, -24f);

            var timerBackGO = new GameObject("TimerFrame", typeof(RectTransform), typeof(Image));
            timerBackGO.transform.SetParent(container, false);
            var timerBack = timerBackGO.GetComponent<RectTransform>();
            timerBack.anchorMin = new Vector2(0.5f, 1f);
            timerBack.anchorMax = new Vector2(0.5f, 1f);
            timerBack.pivot = new Vector2(0.5f, 0.5f);
            timerBack.sizeDelta = new Vector2(180f, 180f);
            timerBack.anchoredPosition = new Vector2(0f, -74f);

            var timerFrameImage = timerBackGO.GetComponent<Image>();
            timerFrameImage.sprite = knobSprite != null ? knobSprite : uiSprite;
            timerFrameImage.type = Image.Type.Sliced;
            timerFrameImage.color = new Color(0f, 0.06f, 0.16f, 0.75f);

            timerRing = CreateCenteredImage(timerBack, "TimerRing", new Vector2(176f, 176f));
            if (timerRing != null)
            {
                timerRing.sprite = knobSprite != null ? knobSprite : uiSprite;
                timerRing.type = Image.Type.Filled;
                timerRing.fillMethod = Image.FillMethod.Radial360;
                timerRing.fillOrigin = (int)Image.Origin360.Top;
                timerRing.fillClockwise = false;
                timerRing.color = new Color(0.25f, 0.55f, 0.95f, 0.8f);
            }

            txtTimer = CreateTMP(timerBack, "TimerValue", "30", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(140f, 80f), Vector2.zero, 44f, TextAlignmentOptions.Center, true);

            energyWheelPlayer = CreateImage(container, "EnergyWheelPlayer", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(60f, 60f), new Vector2(-200f, -136f));
            ConfigureEnergyWheel(energyWheelPlayer, new Color(0.2f, 0.75f, 0.35f, 0.95f));
            txtEnergyPlayer = CreateTMP(container, "EnergyPlayer", "Equipe 0/10", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(180f, 60f), new Vector2(-200f, -192f), 18f, TextAlignmentOptions.Center);

            energyWheelEnemy = CreateImage(container, "EnergyWheelEnemy", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(60f, 60f), new Vector2(200f, -136f));
            ConfigureEnergyWheel(energyWheelEnemy, new Color(0.85f, 0.25f, 0.25f, 0.95f));
            txtEnergyEnemy = CreateTMP(container, "EnergyEnemy", "Inimigos 0/10", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(180f, 60f), new Vector2(200f, -192f), 18f, TextAlignmentOptions.Center);

            var roundPillarGO = new GameObject("RoundPillar", typeof(RectTransform), typeof(Image));
            roundPillarGO.transform.SetParent(root, false);
            var roundPillar = roundPillarGO.GetComponent<RectTransform>();
            roundPillar.anchorMin = new Vector2(1f, 0.5f);
            roundPillar.anchorMax = new Vector2(1f, 0.5f);
            roundPillar.pivot = new Vector2(1f, 0.5f);
            roundPillar.sizeDelta = new Vector2(96f, 280f);
            roundPillar.anchoredPosition = new Vector2(-12f, 140f);

            var pillarImage = roundPillarGO.GetComponent<Image>();
            pillarImage.sprite = uiSprite;
            pillarImage.type = Image.Type.Sliced;
            pillarImage.color = new Color(0f, 0.08f, 0.18f, 0.55f);

            var roundTextGO = new GameObject("RoundLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            roundTextGO.transform.SetParent(roundPillar, false);
            var roundTextRT = roundTextGO.GetComponent<RectTransform>();
            roundTextRT.anchorMin = Vector2.zero;
            roundTextRT.anchorMax = Vector2.one;
            roundTextRT.pivot = new Vector2(0.5f, 0.5f);
            roundTextRT.offsetMin = new Vector2(12f, 12f);
            roundTextRT.offsetMax = new Vector2(-12f, -12f);
            roundTextRT.localEulerAngles = new Vector3(0f, 0f, 90f);

            txtRound = roundTextGO.GetComponent<TextMeshProUGUI>();
            txtRound.text = "Rodada 1";
            txtRound.fontSize = 28f;
            txtRound.color = Color.white;
            txtRound.alignment = TextAlignmentOptions.Center;
            txtRound.textWrappingMode = TextWrappingModes.NoWrap;
        }

        void BuildTurnTimeline()
        {
            var go = new GameObject("TurnTimeline", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(root, false);
            turnRow = go.GetComponent<RectTransform>();
            turnRow.anchorMin = new Vector2(0.5f, 0f);
            turnRow.anchorMax = new Vector2(0.5f, 0f);
            turnRow.pivot = new Vector2(0.5f, 0f);
            turnRow.sizeDelta = new Vector2(920f, 110f);
            turnRow.anchoredPosition = new Vector2(0f, 32f);

            var img = go.GetComponent<Image>();
            img.sprite = uiSprite;
            img.type = Image.Type.Sliced;
            img.color = new Color(0f, 0f, 0f, 0f);
        }

        void BuildControls()
        {
            var controlsGO = new GameObject("RightControls", typeof(RectTransform));
            controlsGO.transform.SetParent(root, false);
            var controls = controlsGO.GetComponent<RectTransform>();
            controls.anchorMin = new Vector2(1f, 0f);
            controls.anchorMax = new Vector2(1f, 0f);
            controls.pivot = new Vector2(1f, 0f);
            controls.sizeDelta = new Vector2(520f, 520f);
            controls.anchoredPosition = new Vector2(-20f, 24f);

            var portraitRootGO = new GameObject("PortraitRoot", typeof(RectTransform));
            portraitRootGO.transform.SetParent(controls, false);
            var portraitRoot = portraitRootGO.GetComponent<RectTransform>();
            portraitRoot.anchorMin = new Vector2(1f, 0f);
            portraitRoot.anchorMax = new Vector2(1f, 0f);
            portraitRoot.pivot = new Vector2(1f, 0f);
            portraitRoot.sizeDelta = new Vector2(260f, 260f);
            portraitRoot.anchoredPosition = new Vector2(0f, 0f);

            abilityRingOuter = CreateCenteredImage(portraitRoot, "AbilityRingOuter", new Vector2(260f, 260f));
            abilityRingOuter.sprite = knobSprite != null ? knobSprite : uiSprite;
            abilityRingOuter.color = new Color(0.25f, 0.55f, 0.95f, 0.28f);

            abilityRingInner = CreateCenteredImage(abilityRingOuter.rectTransform, "AbilityRingInner", new Vector2(216f, 216f));
            abilityRingInner.sprite = uiSprite;
            abilityRingInner.type = Image.Type.Sliced;
            abilityRingInner.color = new Color(0f, 0.06f, 0.14f, 0.8f);

            currentPortrait = CreateCenteredImage(abilityRingInner.rectTransform, "CurrentPortrait", new Vector2(200f, 200f));
            currentPortrait.enabled = false;
            currentPortrait.preserveAspect = true;

            var columnGO = new GameObject("AbilityColumn", typeof(RectTransform), typeof(Image));
            columnGO.transform.SetParent(controls, false);
            abilityColumn = columnGO.GetComponent<RectTransform>();
            abilityColumn.anchorMin = new Vector2(1f, 0f);
            abilityColumn.anchorMax = new Vector2(1f, 0f);
            abilityColumn.pivot = new Vector2(1f, 0f);
            abilityColumn.sizeDelta = new Vector2(360f, 360f);
            abilityColumn.anchoredPosition = new Vector2(-48f, 128f);

            var columnImage = columnGO.GetComponent<Image>();
            columnImage.sprite = uiSprite;
            columnImage.type = Image.Type.Sliced;
            columnImage.color = new Color(0f, 0f, 0f, 0f);

            abilityButtons[0] = CreateAbilityButton(abilityColumn, "AbilityUltimate", new Vector2(-28f, 248f), 118f, out abilityLabels[0], out abilityIcons[0], out abilityConnectors[0], out abilityTooltips[0]);
            abilityButtons[1] = CreateAbilityButton(abilityColumn, "AbilitySpecial", new Vector2(-8f, 136f), 110f, out abilityLabels[1], out abilityIcons[1], out abilityConnectors[1], out abilityTooltips[1]);
            abilityButtons[2] = CreateAbilityButton(abilityColumn, "AbilityBasic", new Vector2(-36f, 32f), 102f, out abilityLabels[2], out abilityIcons[2], out abilityConnectors[2], out abilityTooltips[2]);

            var slotUltimate = 3;
            abilityButtons[0].onClick.AddListener(() => { if (EnsureBootstrapReference()) bootstrap.TriggerAbilityKey(slotUltimate); });
            var slotSpecial = 2;
            abilityButtons[1].onClick.AddListener(() => { if (EnsureBootstrapReference()) bootstrap.TriggerAbilityKey(slotSpecial); });
            var slotBasic = 1;
            abilityButtons[2].onClick.AddListener(() => { if (EnsureBootstrapReference()) bootstrap.TriggerAbilityKey(slotBasic); });
        }

        void BuildLeftMenu()
        {
            var menuGO = new GameObject("LeftMenu", typeof(RectTransform), typeof(Image));
            menuGO.transform.SetParent(root, false);
            var menu = menuGO.GetComponent<RectTransform>();
            menu.anchorMin = new Vector2(0f, 0.5f);
            menu.anchorMax = new Vector2(0f, 0.5f);
            menu.pivot = new Vector2(0f, 0.5f);
            menu.sizeDelta = new Vector2(190f, 460f);
            menu.anchoredPosition = new Vector2(36f, -24f);

            var menuImage = menuGO.GetComponent<Image>();
            menuImage.sprite = uiSprite;
            menuImage.type = Image.Type.Sliced;
            menuImage.color = new Color(0f, 0f, 0f, 0.32f);

            float nextOffset = 36f;

            var retreatButton = CreateControlButton(menu, "RetreatButton", new Vector2(12f, -nextOffset), out var retreatLabel, out var retreatImage, out var retreatRelay);
            retreatButton.interactable = false;
            retreatLabel.text = "Recuar";
            retreatLabel.color = new Color(1f, 1f, 1f, 0.5f);
            retreatImage.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);
            retreatRelay.SetContent("Retirada ainda não disponível.");
            nextOffset += 92f;

            var pauseButton = CreateControlButton(menu, "PauseButton", new Vector2(12f, -nextOffset), out var pauseLabel, out var pauseImage, out var pauseRelay);
            pauseButton.interactable = false;
            pauseLabel.text = "Pausar";
            pauseLabel.color = new Color(1f, 1f, 1f, 0.5f);
            pauseImage.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);
            pauseRelay.SetContent("Função de pausa não implementada.");
            nextOffset += 92f;

            btnAuto = CreateControlButton(menu, "AutoButton", new Vector2(12f, -nextOffset), out txtAutoLabel, out btnAutoImage, out tooltipAuto);
            btnAuto.onClick.AddListener(() => { if (EnsureBootstrapReference()) bootstrap.ToggleAutoBattle(); });
            txtAutoLabel.text = "Auto";
            tooltipAuto.SetContent("Alterna o modo automático da batalha.");
            nextOffset += 92f;

            btnSpeed = CreateControlButton(menu, "SpeedButton", new Vector2(12f, -nextOffset), out txtSpeedLabel, out btnSpeedImage, out tooltipSpeed);
            btnSpeed.onClick.AddListener(() => { if (EnsureBootstrapReference()) bootstrap.CycleSpeed(); });
            txtSpeedLabel.text = "Rápido x1.0";
            tooltipSpeed.SetContent("Altera a velocidade das animações da batalha.");
            nextOffset += 92f;

            var reportButton = CreateControlButton(menu, "ReportButton", new Vector2(12f, -nextOffset), out var reportLabel, out var reportImage, out var reportRelay);
            reportButton.interactable = false;
            reportLabel.text = "Denunciar";
            reportLabel.color = new Color(1f, 1f, 1f, 0.5f);
            reportImage.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);
            reportRelay.SetContent("Canal de denúncia indisponível nesta versão.");
        }

        void BuildInfoPanel()
        {
            var info = CreatePanel(root, "InfoPanel", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(380f, 260f), new Vector2(36f, 28f), 0.32f);

            txtCurrent = CreateTMP(info, "Current", "Turno de: —", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(340f, 28f), new Vector2(20f, -20f), 20f, TextAlignmentOptions.Left, true);
            txtHP = CreateTMP(info, "HP", "HP: —", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(340f, 24f), new Vector2(20f, -58f), 18f, TextAlignmentOptions.Left);
            txtStats = CreateTMP(info, "Stats", "Crit: — | Seventh Sense: —", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(340f, 24f), new Vector2(20f, -92f), 18f, TextAlignmentOptions.Left);
            txtCosmos = CreateTMP(info, "Cosmos", "Cosmos: —", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(340f, 24f), new Vector2(20f, -126f), 18f, TextAlignmentOptions.Left);
            txtSynergy = CreateTMP(info, "Synergy", "Sinergias: —", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(340f, 96f), new Vector2(20f, -160f), 17f, TextAlignmentOptions.TopLeft);
            txtSynergy.textWrappingMode = TextWrappingModes.Normal;
        }

        void BuildTooltip()
        {
            txtTooltip = CreateTMP(root, "Tooltip", "Descrição da habilidade", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(520f, 80f), new Vector2(-360f, 140f), 18f, TextAlignmentOptions.MidlineRight);
            ClearTooltip();
        }

        void UpdateInfoPanel(BattleBootstrap3D.HUDState state)
        {
            if (string.IsNullOrEmpty(state.current.name))
            {
                txtCurrent.text = "Turno de: —";
                txtHP.text = "HP: —";
                txtStats.text = "Crit: — | Seventh Sense: —";
                txtCosmos.text = "Cosmos: —";
                return;
            }

            txtCurrent.text = $"Turno de: {state.current.name} Lv{state.current.level} R{state.current.rank} SPD {state.current.spd}";
            txtHP.text = $"HP: {state.current.hp}/{state.current.maxHP}  Shield: {state.current.shield}";
            txtStats.text = $"Crit: {state.current.crit}% | Seventh Sense: {(state.current.seventhSense ? "Ativo" : "Inativo")}";
            txtCosmos.text = FormatCosmos(state.current.cosmos);
        }

        void UpdateAbilities(BattleBootstrap3D.HUDState state)
        {
            var abilities = bootstrap.GetAbilityBar();
            int[] order = { 3, 2, 1 };
            for (int i = 0; i < abilityButtons.Length; i++)
            {
                var abilityData = abilities.FirstOrDefault(a => a.index == order[i]);
                BattleBootstrap3D.AbilityUI? ability = string.IsNullOrEmpty(abilityData.id) ? (BattleBootstrap3D.AbilityUI?)null : abilityData;
                UpdateAbilitySlot(i, ability, state.autoEnabled);
            }
        }

        void UpdateControls(BattleBootstrap3D.HUDState state)
        {
            if (btnAutoImage != null)
                btnAutoImage.color = state.autoEnabled ? new Color(0.12f, 0.7f, 0.35f, 0.95f) : new Color(0.18f, 0.22f, 0.28f, 0.92f);
            if (txtAutoLabel != null)
            {
                txtAutoLabel.text = "Auto";
                txtAutoLabel.color = state.autoEnabled ? Color.white : new Color(0.85f, 0.85f, 0.85f, 1f);
            }

            if (btnSpeedImage != null)
                btnSpeedImage.color = state.speedMultiplier > 1f ? new Color(0.18f, 0.45f, 0.9f, 0.95f) : new Color(0.18f, 0.18f, 0.22f, 0.92f);
            if (txtSpeedLabel != null)
            {
                txtSpeedLabel.text = $"Rápido x{state.speedMultiplier:0.0}";
                txtSpeedLabel.color = state.speedMultiplier > 1f ? Color.white : new Color(0.85f, 0.85f, 0.85f, 1f);
            }
        }

        void UpdateAbilitySlot(int index, BattleBootstrap3D.AbilityUI? ability, bool autoMode)
        {
            var button = abilityButtons[index];
            var label = abilityLabels[index];
            var icon = abilityIcons[index];
            var relay = abilityTooltips[index];

            if (!ability.HasValue || string.IsNullOrEmpty(ability.Value.id))
            {
                label.text = "<b>Bloqueada</b>";
                button.interactable = false;
                relay.SetContent(string.Empty);
                SetIcon(icon, null, false);
                var connectorLocked = abilityConnectors[index];
                if (connectorLocked != null)
                    connectorLocked.color = new Color(0.35f, 0.35f, 0.35f, 0.25f);
                return;
            }

            var data = ability.Value;
            string status = data.canUse ? (data.cost > 0 ? $"Custo: {data.cost}" : "Pronto") : (data.cooldownRemaining > 0 ? $"Recarga: {data.cooldownRemaining}" : "Bloqueada");
            if (autoMode && data.canUse)
                status = "Modo Auto";
            label.text = $"<b>{data.name}</b>\n{status}";
            button.interactable = !autoMode && data.canUse;
            relay.SetContent(data.tooltip ?? string.Empty);

            var background = button.GetComponent<Image>();
            if (background != null)
                background.color = button.interactable ? new Color(1f, 1f, 1f, 0.95f) : new Color(0.35f, 0.35f, 0.35f, 0.6f);

            if (label != null)
                label.color = button.interactable ? Color.white : new Color(0.75f, 0.75f, 0.75f, 1f);

            SetIcon(icon, data.iconPath, true);
            if (icon != null)
            {
                bool hasCustomSprite = icon.sprite != null && icon.sprite != uiSprite;
                if (hasCustomSprite)
                    icon.color = button.interactable ? Color.white : new Color(0.75f, 0.75f, 0.75f, 0.7f);
            }

            var connector = abilityConnectors[index];
            if (connector != null)
                connector.color = button.interactable ? new Color(0.45f, 0.75f, 1f, 0.35f) : new Color(0.35f, 0.35f, 0.35f, 0.25f);
        }

        void UpdateCurrentPortrait(BattleBootstrap3D.HUDState state)
        {
            if (currentPortrait == null)
                return;

            var sprite = LoadSprite(state.current.portraitPath);
            if (sprite != null)
            {
                currentPortrait.enabled = true;
                currentPortrait.sprite = sprite;
                currentPortrait.type = Image.Type.Simple;
                currentPortrait.preserveAspect = true;
                currentPortrait.color = Color.white;
            }
            else
            {
                currentPortrait.enabled = true;
                currentPortrait.sprite = uiSprite;
                currentPortrait.type = Image.Type.Sliced;
                currentPortrait.preserveAspect = false;
                currentPortrait.color = state.current.isEnemy ? new Color(0.7f, 0.24f, 0.24f, 0.7f) : new Color(0.2f, 0.5f, 0.9f, 0.7f);
            }
        }

        void UpdateTurnTimeline(BattleBootstrap3D.HUDState state)
        {
            if (turnRow == null)
                return;

            foreach (Transform child in turnRow)
                Destroy(child.gameObject);

            int index = 0;
            var chipList = state.turnChipsTMP?.Take(MaxTurnChips).ToList();
            if (chipList == null || chipList.Count == 0)
                return;

            float spacing = 96f;
            float startX = -(chipList.Count - 1) * 0.5f * spacing;

            foreach (var chip in chipList)
            {
                var holder = new GameObject($"Chip_{index}", typeof(RectTransform));
                holder.transform.SetParent(turnRow, false);
                var rt = holder.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(84f, 84f);
                rt.anchoredPosition = new Vector2(startX + index * spacing, 4f);

                var frame = CreateCenteredImage(rt, "Frame", new Vector2(72f, 72f));
                frame.sprite = knobSprite != null ? knobSprite : uiSprite;
                frame.color = chip.isEnemy ? new Color(0.82f, 0.28f, 0.28f, 0.55f) : new Color(0.25f, 0.6f, 0.95f, 0.55f);

                var portrait = CreateCenteredImage(rt, "Portrait", new Vector2(58f, 58f));
                var sprite = LoadSprite(chip.portraitPath);
                if (sprite != null)
                {
                    portrait.sprite = sprite;
                    portrait.type = Image.Type.Simple;
                    portrait.preserveAspect = true;
                    portrait.color = Color.white;
                }
                else
                {
                    portrait.sprite = uiSprite;
                    portrait.type = Image.Type.Sliced;
                    portrait.preserveAspect = false;
                    portrait.color = chip.isEnemy ? new Color(0.75f, 0.35f, 0.35f, 0.6f) : new Color(0.3f, 0.6f, 0.95f, 0.6f);
                }

                index++;
            }
        }

        void ConfigureEnergyWheel(Image wheel, Color color)
        {
            if (wheel == null)
                return;

            wheel.sprite = knobSprite != null ? knobSprite : uiSprite;
            wheel.type = Image.Type.Filled;
            wheel.fillMethod = Image.FillMethod.Radial360;
            wheel.fillOrigin = (int)Image.Origin360.Top;
            wheel.fillClockwise = false;
            wheel.color = color;
        }

        void SetIcon(Image target, string path, bool allowFallback)
        {
            if (target == null)
                return;

            var sprite = LoadSprite(path);
            if (sprite != null)
            {
                target.enabled = true;
                target.sprite = sprite;
                target.type = Image.Type.Simple;
                target.preserveAspect = true;
                target.color = Color.white;
            }
            else if (allowFallback)
            {
                target.enabled = true;
                target.sprite = uiSprite;
                target.type = Image.Type.Sliced;
                target.preserveAspect = false;
                target.color = new Color(0.18f, 0.24f, 0.32f, 0.7f);
            }
            else
            {
                target.enabled = false;
            }
        }

        void SetTooltip(string message)
        {
            if (txtTooltip == null)
                return;

            bool hasContent = !string.IsNullOrEmpty(message);
            txtTooltip.text = hasContent ? message : string.Empty;
            txtTooltip.enabled = hasContent;
        }

        void ClearTooltip()
        {
            if (txtTooltip == null)
                return;

            txtTooltip.text = string.Empty;
            txtTooltip.enabled = false;
        }

        string FormatCosmos(string[] cosmos)
        {
            if (cosmos == null || cosmos.Length == 0)
                return "Cosmos: —";

            return "Cosmos: " + string.Join(", ", cosmos);
        }

        string FormatSynergies(BattleBootstrap3D.HUDState.SynergyInfo[] synergies)
        {
            if (synergies == null || synergies.Length == 0)
                return "Sinergias: —";

            var lines = new List<string>();
            foreach (var entry in synergies)
            {
                var prefix = entry.active ? "[Ativa]" : "[Bloqueada]";
                lines.Add($"{prefix} {entry.name} {FormatBonus(entry.bonus)}");
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
            return parts.Count > 0 ? "(" + string.Join(" ", parts) + ")" : string.Empty;
        }

        bool EnsureBootstrapReference()
        {
            if (bootstrap != null)
                return true;

            #if UNITY_2023_1_OR_NEWER
            bootstrap = FindFirstObjectByType<BattleBootstrap3D>();
            #else
            bootstrap = FindObjectOfType<BattleBootstrap3D>();
            #endif
            return bootstrap != null;
        }

        RectTransform CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size, Vector2 anchoredPosition, float alpha)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPosition;

            var img = go.GetComponent<Image>();
            img.sprite = uiSprite;
            img.type = Image.Type.Sliced;
            img.color = new Color(0f, 0f, 0f, alpha);
            return rt;
        }

        Image CreateImage(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size, Vector2 anchoredPosition)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPosition;

            return go.GetComponent<Image>();
        }

        Image CreateCenteredImage(RectTransform parent, string name, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;

            var image = go.GetComponent<Image>();
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            return image;
        }

        TMP_Text CreateTMP(Transform parent, string name, string initial, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size, Vector2 anchoredPosition, float fontSize, TextAlignmentOptions alignment, bool bold = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPosition;

            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = initial;
            tmp.fontSize = fontSize;
            tmp.color = Color.white;
            tmp.alignment = alignment;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.overflowMode = TextOverflowModes.Overflow;
            if (bold)
                tmp.fontStyle = FontStyles.Bold;
            return tmp;
        }

    Button CreateAbilityButton(RectTransform parent, string name, Vector2 position, float size, out TMP_Text label, out Image icon, out Image connector, out TooltipRelay relay)
        {
            var entryGO = new GameObject(name, typeof(RectTransform));
            entryGO.transform.SetParent(parent, false);
            var entry = entryGO.GetComponent<RectTransform>();
            entry.anchorMin = new Vector2(1f, 0f);
            entry.anchorMax = new Vector2(1f, 0f);
            entry.pivot = new Vector2(1f, 0f);
            entry.sizeDelta = new Vector2(320f, size + 36f);
            entry.anchoredPosition = position;

            var buttonGO = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGO.transform.SetParent(entry, false);
            var buttonRT = buttonGO.GetComponent<RectTransform>();
            buttonRT.anchorMin = new Vector2(0f, 0f);
            buttonRT.anchorMax = new Vector2(0f, 0f);
            buttonRT.pivot = new Vector2(0f, 0f);
            buttonRT.sizeDelta = new Vector2(size, size);
            buttonRT.anchoredPosition = new Vector2(0f, 12f);

            var img = buttonGO.GetComponent<Image>();
            img.sprite = knobSprite != null ? knobSprite : uiSprite;
            img.type = Image.Type.Sliced;
            img.color = new Color(1f, 1f, 1f, 0.92f);

            var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGO.transform.SetParent(buttonGO.transform, false);
            var iconRT = iconGO.GetComponent<RectTransform>();
            iconRT.anchorMin = new Vector2(0.5f, 0.5f);
            iconRT.anchorMax = new Vector2(0.5f, 0.5f);
            iconRT.pivot = new Vector2(0.5f, 0.5f);
            iconRT.sizeDelta = new Vector2(size - 28f, size - 28f);
            icon = iconGO.GetComponent<Image>();
            icon.type = Image.Type.Simple;
            icon.preserveAspect = true;

            var connectorGO = new GameObject("Connector", typeof(RectTransform), typeof(Image));
            connectorGO.transform.SetParent(entry, false);
            var connectorRT = connectorGO.GetComponent<RectTransform>();
            connectorRT.anchorMin = new Vector2(0f, 0.5f);
            connectorRT.anchorMax = new Vector2(0f, 0.5f);
            connectorRT.pivot = new Vector2(0f, 0.5f);
            connectorRT.sizeDelta = new Vector2(size + 24f, 4f);
            connectorRT.anchoredPosition = new Vector2(size, size * 0.5f + 12f);
            var connectorImg = connectorGO.GetComponent<Image>();
            connectorImg.sprite = uiSprite;
            connectorImg.type = Image.Type.Sliced;
            connectorImg.color = new Color(0.45f, 0.75f, 1f, 0.35f);

            var labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGO.transform.SetParent(entry, false);
            var labelRT = labelGO.GetComponent<RectTransform>();
            labelRT.anchorMin = new Vector2(0f, 0f);
            labelRT.anchorMax = new Vector2(1f, 1f);
            labelRT.pivot = new Vector2(0f, 1f);
            labelRT.offsetMin = new Vector2(size + 32f, 16f);
            labelRT.offsetMax = new Vector2(-12f, -16f);

            var tmp = labelGO.GetComponent<TextMeshProUGUI>();
            tmp.text = "—";
            tmp.fontSize = Mathf.Clamp(size * 0.18f, 16f, 22f);
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.richText = true;

            label = tmp;
            connector = connectorImg;
            relay = EnsureTooltipRelay(buttonGO);
            relay.Init(SetTooltip, ClearTooltip);
            return buttonGO.GetComponent<Button>();
        }

        Button CreateControlButton(Transform parent, string name, Vector2 anchoredPosition, out TMP_Text label, out Image background, out TooltipRelay relay)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(164f, 84f);
            rt.anchoredPosition = anchoredPosition;

            background = go.GetComponent<Image>();
            background.sprite = uiSprite;
            background.type = Image.Type.Sliced;
            background.color = new Color(0.16f, 0.22f, 0.3f, 0.9f);

            var badgeGO = new GameObject("Badge", typeof(RectTransform), typeof(Image));
            badgeGO.transform.SetParent(go.transform, false);
            var badgeRT = badgeGO.GetComponent<RectTransform>();
            badgeRT.anchorMin = new Vector2(0f, 0.5f);
            badgeRT.anchorMax = new Vector2(0f, 0.5f);
            badgeRT.pivot = new Vector2(0f, 0.5f);
            badgeRT.sizeDelta = new Vector2(44f, 44f);
            badgeRT.anchoredPosition = new Vector2(12f, 0f);
            var badgeImage = badgeGO.GetComponent<Image>();
            badgeImage.sprite = knobSprite != null ? knobSprite : uiSprite;
            badgeImage.type = Image.Type.Sliced;
            badgeImage.color = new Color(1f, 1f, 1f, 0.18f);

            var labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGO.transform.SetParent(go.transform, false);
            var labelRT = labelGO.GetComponent<RectTransform>();
            labelRT.anchorMin = new Vector2(0f, 0f);
            labelRT.anchorMax = new Vector2(1f, 1f);
            labelRT.pivot = new Vector2(0f, 0.5f);
            labelRT.offsetMin = new Vector2(64f, 12f);
            labelRT.offsetMax = new Vector2(-12f, -12f);

            label = labelGO.GetComponent<TextMeshProUGUI>();
            label.text = "—";
            label.fontSize = 20f;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.textWrappingMode = TextWrappingModes.Normal;

            relay = EnsureTooltipRelay(go);
            relay.Init(SetTooltip, ClearTooltip);
            return go.GetComponent<Button>();
        }

        TooltipRelay EnsureTooltipRelay(GameObject go)
        {
            var relay = go.GetComponent<TooltipRelay>();
            if (relay == null)
                relay = go.AddComponent<TooltipRelay>();
            return relay;
        }

        Sprite LoadSprite(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            if (spriteCache.TryGetValue(path, out var cached))
                return cached;

            var sprite = Resources.Load<Sprite>(path);
            spriteCache[path] = sprite;
            return sprite;
        }

        sealed class TooltipRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            Action<string> onEnter;
            Action onExit;
            string message;

            public void Init(Action<string> enter, Action exit)
            {
                onEnter = enter;
                onExit = exit;
            }

            public void SetContent(string value) => message = value;

            public void OnPointerEnter(PointerEventData eventData) => onEnter?.Invoke(message);

            public void OnPointerExit(PointerEventData eventData) => onExit?.Invoke();
        }
    }
}
