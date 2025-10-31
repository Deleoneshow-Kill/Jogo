
using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using CleanRPG.Core;
using CleanRPG.Systems;
using CleanRPG.UI;
namespace CleanRPG.Battle
{
    public partial class BattleBootstrap3D : MonoBehaviour
    {
        public List<CharacterRuntime> all = new List<CharacterRuntime>();
        public CharacterRuntime current;
        public CharacterRuntime selectedTarget;
        public TurnController turn;
        public EnergySystem energy;
        public System.Collections.Generic.Dictionary<string, CharacterDefinition> charDB = new System.Collections.Generic.Dictionary<string, CharacterDefinition>();
        public System.Collections.Generic.Dictionary<string, AbilityDefinition> abDB = new System.Collections.Generic.Dictionary<string, AbilityDefinition>();
        public List<SynergyReport> playerSynergies = new List<SynergyReport>();
        public List<SynergyReport> enemySynergies = new List<SynergyReport>();
        public event Action StateChanged;
        private CombatWatcher watcher;
        private BattleAbilityTimeline abilityTimeline;
        private bool isPerformingAbility;
        private int lastEnergyRound = 1;
    private bool autoBattleEnabled;
    private bool autoQueued;
    private Team autoQueuedTeam;
    private float nextAutoDecisionTime;
    private const float AutoDecisionDelay = 0.35f;
        private readonly float[] speedSteps = new float[]{1f, 1.5f, 2f};
        private int speedIndex;
        private void Start()
        {
            LoadDB();
            ProgressionSystem.LoadAll();
            CleanRPG.Systems.InventorySystem.InitAllOwned(charDB.Keys.ToArray());
            SetupDefault();
            watcher = gameObject.AddComponent<CombatWatcher>(); watcher.Initialize(all);
            var hudTmp = GetComponent<CanvasHUD_TMP_v6>();
            if (hudTmp == null)
                hudTmp = gameObject.AddComponent<CanvasHUD_TMP_v6>();
            hudTmp.bootstrap = this;

            var legacyHud = GetComponent<CanvasHUD_Basic>();
            if (legacyHud != null)
                legacyHud.enabled = false;
            gameObject.AddComponent<CombatLogUI>();
            gameObject.AddComponent<TeamSelectUI>().bootstrap = this;
            gameObject.AddComponent<GachaBannerUI>();
            gameObject.AddComponent<TurnTimelineUI>().bootstrap = this;
            gameObject.AddComponent<CleanRPG.Systems.ComboSystem>();
            var pickBan = GetComponent<ArenaPickBanUI>();
            if (pickBan == null) pickBan = gameObject.AddComponent<ArenaPickBanUI>();
            pickBan.bootstrap = this;
            gameObject.AddComponent<CleanRPG.Replay.ReplayUI>();
            InitializeComboHooks();
            abilityTimeline = GetComponent<BattleAbilityTimeline>();
            if (abilityTimeline == null) abilityTimeline = gameObject.AddComponent<BattleAbilityTimeline>();
            abilityTimeline.Initialize(this);
            Time.timeScale = speedSteps[Mathf.Clamp(speedIndex, 0, speedSteps.Length - 1)];
        }
        void LoadDB()
        {
            charDB.Clear();
            abDB.Clear();

            foreach (var t in Resources.LoadAll<TextAsset>("Characters"))
            {
                if (t == null || string.IsNullOrEmpty(t.text)) continue;
                var c = JsonUtility.FromJson<CharacterDefinition>(t.text);
                if (c == null || string.IsNullOrEmpty(c.id)) continue;
                NormalizeCharacterStats(c);
                charDB[c.id] = c;
            }

            foreach (var t in Resources.LoadAll<TextAsset>("Abilities"))
            {
                if (t == null || string.IsNullOrEmpty(t.text)) continue;
                var a = JsonUtility.FromJson<AbilityDefinition>(t.text);
                if (a == null || string.IsNullOrEmpty(a.id)) continue;
                abDB[a.id] = a;
            }

            foreach (var def in charDB.Values)
            {
                NormalizeCharacterAbilities(def);
            }
        }

        void NormalizeCharacterStats(CharacterDefinition def)
        {
            if (def == null) return;

            if (string.IsNullOrEmpty(def.displayName) && !string.IsNullOrEmpty(def.name))
                def.displayName = def.name;

            if (string.IsNullOrEmpty(def.portraitSpritePath) && !string.IsNullOrEmpty(def.iconSpritePath))
                def.portraitSpritePath = def.iconSpritePath;

            if (def.maxHP <= 0 && def.hp > 0)
                def.maxHP = Mathf.Max(1, def.hp);

            if (def.maxHP <= 0)
                def.maxHP = 1200;

            if (def.atk <= 0)
            {
                if (def.attack > 0) def.atk = Mathf.Max(1, def.attack);
                else def.atk = 120;
            }

            if (def.defStat <= 0)
            {
                if (def.def > 0) def.defStat = Mathf.Max(0, def.def);
                else if (def.defense > 0) def.defStat = Mathf.Max(0, def.defense);
                else def.defStat = 80;
            }

            if (def.speed <= 0)
            {
                if (def.spd > 0) def.speed = Mathf.Max(1, def.spd);
                else if (def.speedStat > 0) def.speed = Mathf.Max(1, def.speedStat);
                else def.speed = 100;
            }

            if (!string.IsNullOrEmpty(def.@class))
                def.role = ParseRole(def.@class, def.role);

            if (string.IsNullOrEmpty(def.rarity) && def.rarityNumeric > 0)
                def.rarity = def.rarityNumeric.ToString();

            if (def.tags == null)
                def.tags = Array.Empty<string>();

            def.displayName = string.IsNullOrEmpty(def.displayName) ? def.id : def.displayName;
        }

        void NormalizeCharacterAbilities(CharacterDefinition def)
        {
            if (def == null) return;

            def.basic = NormalizeAbilityField(def, def.basic, AbilityKind.Basic, 0);
            def.special = NormalizeAbilityField(def, def.special, AbilityKind.Special, 1);
            def.ultimate = NormalizeAbilityField(def, def.ultimate, AbilityKind.Ultimate, 2);
            if (!string.IsNullOrEmpty(def.passive) || HasAbilitySlot(def, 3))
                def.passive = NormalizeAbilityField(def, def.passive, AbilityKind.Passive, 3);
        }

        string NormalizeAbilityField(CharacterDefinition def, string current, AbilityKind kind, int slotIndex)
        {
            var slot = GetAbilitySlot(def, slotIndex);
            string candidate = string.IsNullOrEmpty(current) ? slot?.id : current;
            candidate = ResolveAbilityIdentifier(candidate, def, kind, slotIndex);
            return EnsureAbilityExists(candidate, def, slot, kind);
        }

        CharacterDefinition.AbilitySlot GetAbilitySlot(CharacterDefinition def, int index)
        {
            if (def?.abilities == null || def.abilities.Length == 0) return null;
            if (index < 0 || index >= def.abilities.Length) return null;
            return def.abilities[index];
        }

        bool HasAbilitySlot(CharacterDefinition def, int index) => GetAbilitySlot(def, index) != null;

        string ResolveAbilityIdentifier(string rawId, CharacterDefinition def, AbilityKind kind, int slotIndex)
        {
            string trimmed = (rawId ?? string.Empty).Trim();

            var candidates = new List<string>();
            if (!string.IsNullOrEmpty(trimmed))
            {
                candidates.Add(trimmed);
                if (!trimmed.StartsWith("ab_", StringComparison.OrdinalIgnoreCase))
                    candidates.Add("ab_" + trimmed);
                else if (trimmed.Length > 3)
                    candidates.Add(trimmed.Substring(3));
            }

            var generated = GenerateAbilityId(def, kind);
            if (!candidates.Contains(generated))
                candidates.Add(generated);

            foreach (var entry in candidates)
            {
                if (!string.IsNullOrEmpty(entry) && abDB.ContainsKey(entry))
                    return entry;
            }

            return candidates.First(c => !string.IsNullOrEmpty(c));
        }

        string EnsureAbilityExists(string identifier, CharacterDefinition def, CharacterDefinition.AbilitySlot slot, AbilityKind kind)
        {
            string id = string.IsNullOrEmpty(identifier) ? GenerateAbilityId(def, kind) : identifier.Trim();
            if (!abDB.ContainsKey(id))
            {
                var ability = new AbilityDefinition
                {
                    id = id,
                    displayName = !string.IsNullOrEmpty(slot?.name) ? slot.name : GenerateAbilityDisplayName(def, kind),
                    kind = kind,
                    effect = ParseEffect(slot?.effect, kind, slot?.description),
                    power = slot?.power > 0 ? slot.power : DefaultPower(kind),
                    potency = slot?.potency ?? 0,
                    duration = slot?.duration ?? 0,
                    cost = slot?.cost ?? DefaultCost(kind),
                    cooldown = Mathf.Max(0, slot?.cooldown ?? DefaultCooldown(kind)),
                    target = InferTarget(slot?.description),
                    description = !string.IsNullOrEmpty(slot?.description) ? slot.description : GenerateAbilityDescription(def, kind),
                    iconSpritePath = slot?.iconSpritePath ?? def.iconSpritePath ?? def.portraitSpritePath
                };
                abDB[id] = ability;
            }
            return id;
        }

        string GenerateAbilityId(CharacterDefinition def, AbilityKind kind)
        {
            string suffix = kind switch
            {
                AbilityKind.Basic => "basic",
                AbilityKind.Special => "special",
                AbilityKind.Ultimate => "ultimate",
                AbilityKind.Passive => "passive",
                _ => "ability"
            };
            return $"ab_{def.id}_{suffix}";
        }

        string GenerateAbilityDisplayName(CharacterDefinition def, AbilityKind kind)
        {
            return $"{def.displayName} {kind}";
        }

        string GenerateAbilityDescription(CharacterDefinition def, AbilityKind kind)
        {
            return $"Habilidade {kind.ToString().ToLower()} de {def.displayName}";
        }

        int DefaultCost(AbilityKind kind) => kind switch
        {
            AbilityKind.Basic => 0,
            AbilityKind.Special => 2,
            AbilityKind.Ultimate => 4,
            AbilityKind.Passive => 0,
            _ => 0
        };

        int DefaultCooldown(AbilityKind kind) => kind switch
        {
            AbilityKind.Basic => 0,
            AbilityKind.Special => 1,
            AbilityKind.Ultimate => 3,
            AbilityKind.Passive => 0,
            _ => 0
        };

        int DefaultPower(AbilityKind kind) => kind switch
        {
            AbilityKind.Basic => 100,
            AbilityKind.Special => 160,
            AbilityKind.Ultimate => 240,
            AbilityKind.Passive => 0,
            _ => 100
        };

        string InferTarget(string description)
        {
            if (string.IsNullOrEmpty(description)) return "single";
            var lower = description.ToLowerInvariant();
            if (lower.Contains("próprio") || lower.Contains("self")) return "self";
            if (lower.Contains("aliado") || lower.Contains("ally") || lower.Contains("todos") || lower.Contains("all")) return "ally";
            return "single";
        }

        EffectType ParseEffect(string effectValue, AbilityKind fallbackKind, string description)
        {
            if (!string.IsNullOrEmpty(effectValue) && Enum.TryParse(effectValue, true, out EffectType parsed))
                return parsed;

            if (!string.IsNullOrEmpty(description))
            {
                var lower = description.ToLowerInvariant();
                if (lower.Contains("cura") || lower.Contains("heal")) return EffectType.Heal;
                if (lower.Contains("escudo") || lower.Contains("shield")) return EffectType.Shield;
                if (lower.Contains("veloc") || lower.Contains("speed")) return EffectType.BuffSPD;
            }

            return fallbackKind == AbilityKind.Passive ? EffectType.BuffSPD : EffectType.Damage;
        }

        Role ParseRole(string rawValue, Role defaultRole)
        {
            if (string.IsNullOrEmpty(rawValue)) return defaultRole;
            if (Enum.TryParse(rawValue, true, out Role parsed)) return parsed;
            var lower = rawValue.ToLowerInvariant();
            if (lower.Contains("support")) return Role.Support;
            if (lower.Contains("tank")) return Role.Tank;
            if (lower.Contains("control")) return Role.Control;
            return Role.Attacker;
        }
        public void SetupDefault()
        {
            DespawnAll();
            var list = new System.Collections.Generic.List<CharacterDefinition>(charDB.Values);
            var players = list.GetRange(0, System.Math.Min(3, list.Count)).ConvertAll(x=>x.id);
            var enemies = list.Count>3? list.GetRange(3, System.Math.Min(3, list.Count-3)).ConvertAll(x=>x.id) : new System.Collections.Generic.List<string>(players);
            SetupWithTeam(players, enemies);
        }
        public void DespawnAll()
        {
            foreach (var c in all) if (c) Destroy(c.gameObject);
            all.Clear();
            playerSynergies.Clear();
            enemySynergies.Clear();
            current = null;
            selectedTarget = null;
            turn = null;
            energy = null;
            lastEnergyRound = 1;
            autoQueued = false;
            autoQueuedTeam = Team.Player;
            nextAutoDecisionTime = 0f;
        }

        void OnDisable()
        {
            autoQueued = false;
            autoQueuedTeam = Team.Player;
            nextAutoDecisionTime = 0f;
            Time.timeScale = 1f;
        }
        CharacterRuntime Spawn(CharacterDefinition def, Team team, Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.transform.position = pos;
            go.name = def.displayName;
            var rt = go.AddComponent<CharacterRuntime>(); rt.Init(def, team);
            CleanRPG.Systems.CharacterVisuals.Build(go, def, team==Team.Enemy);
            return rt;
        }
        public void SetupWithTeam(System.Collections.Generic.List<string> playerIds, System.Collections.Generic.List<string> enemyIds)
        {
            DespawnAll();
            Vector3 left = new Vector3(-3,0,0), right = new Vector3(3,0,0);
            for (int i=0;i<playerIds.Count && i<6;i++)
            { if (!charDB.TryGetValue(playerIds[i], out var d)) continue; var rt = Spawn(d, Team.Player, left + new Vector3(0,0,i*2)); all.Add(rt); }
            for (int i=0;i<enemyIds.Count && i<6;i++)
            { if (!charDB.TryGetValue(enemyIds[i], out var d)) continue; var rt = Spawn(d, Team.Enemy, right + new Vector3(0,0,i*2)); all.Add(rt); }
            ApplySynergies();
            selectedTarget = all.Find(c=>c.Team==Team.Enemy);
            energy = new EnergySystem(3, 10);
            energy.Reset(all.Select(c => c.Team));
            turn = new TurnController(all);
            current = turn.Next();
            energy.SyncRound(turn?.Round ?? 1);
            lastEnergyRound = turn?.Round ?? 1;
            var cw = GetComponent<CombatWatcher>(); if (cw!=null) cw.Initialize(all);
            RaiseStateChanged();
            ScheduleAutoForCurrentTurn();
        }
        void Update()
        {
            if (current==null) return;
            if (Input.GetKeyDown(KeyCode.Tab)) TriggerTab();
            if (Input.GetKeyDown(KeyCode.R)) SetupDefault();
            if (Input.GetKeyDown(KeyCode.Alpha1)) TriggerAbilityKey(1);
            if (Input.GetKeyDown(KeyCode.Alpha2)) TriggerAbilityKey(2);
            if (Input.GetKeyDown(KeyCode.Alpha3)) TriggerAbilityKey(3);
            if (Input.GetKeyDown(KeyCode.Space)) TriggerPass();
            bool isPlayerTurn = current.Team == Team.Player;
            if (isPlayerTurn)
            {
                if (autoBattleEnabled) TryScheduleAutoForCurrent(false);
            }
            else
            {
                TryScheduleAutoForCurrent(true);
            }
            ProcessAutoDecision();
        }
        public void EndTurn()
        {
            AbilityExecutor.ApplyCooldowns(all);
            current = turn?.Next();
            SyncEnergyWithRound();
            RaiseStateChanged();
            ScheduleAutoForCurrentTurn();
        }
        public bool TryUse(string abilityId)
        {
            if (string.IsNullOrEmpty(abilityId)) return false;
            if (current == null) return false;
            if (isPerformingAbility) return false;
            if (!abDB.TryGetValue(abilityId, out var ab)) return false;
            if (!AbilityRuntime.CanUse(current, ab, energy)) return false;

            var actor = current;
            var target = ResolveTargetForAbility(actor, ab);
            isPerformingAbility = true;

            if (abilityTimeline == null)
            {
                AbilityExecutor.Execute(this, actor, ab, target);
                isPerformingAbility = false;
                EndTurn();
                return true;
            }

            abilityTimeline?.Enqueue(new BattleAbilityTimeline.Request
            {
                actor = actor,
                target = target,
                ability = ab,
                onResolve = () =>
                {
                    AbilityExecutor.Execute(this, actor, ab, target);
                    EndTurn();
                },
                onComplete = () =>
                {
                    isPerformingAbility = false;
                    ScheduleAutoForCurrentTurn();
                }
            });
            return true;
        }

        CharacterRuntime ResolveTargetForAbility(CharacterRuntime actor, AbilityDefinition ab)
        {
            var target = selectedTarget;
            if (ab.target == "ally")
                target = all.Find(c => c.Team == actor.Team && c != actor && c.IsAlive) ?? actor;
            else if (ab.target == "self")
                target = actor;
            else if (target == null || !target.IsAlive)
                target = all.FirstOrDefault(c => c.Team != actor.Team && c.IsAlive);
            return target;
        }

        void ApplySynergies()
        {
            playerSynergies = ProgressionSystem.EvaluateSynergy(all.Where(c=>c.Team==Team.Player));
            enemySynergies = ProgressionSystem.EvaluateSynergy(all.Where(c=>c.Team==Team.Enemy));
            ApplySynergyReports(playerSynergies, Team.Player);
            ApplySynergyReports(enemySynergies, Team.Enemy);
        }

        void ApplySynergyReports(List<SynergyReport> reports, Team team)
        {
            foreach (var report in reports)
            {
                if (!report.active) continue;
                foreach (var memberId in report.members)
                {
                    var unit = all.FirstOrDefault(c => c.Team==team && c.def.id==memberId);
                    if (unit!=null) unit.ApplySynergyBonus(report.bonus);
                }
            }
        }

        void SyncEnergyWithRound()
        {
            if (turn == null || energy == null) return;
            if (turn.Round != lastEnergyRound)
            {
                lastEnergyRound = turn.Round;
                energy.SyncRound(turn.Round);
            }
        }

        string ChooseAutoAbility()
        {
            if (current == null) return null;
            var abilities = GetAbilityBar();
            if (abilities == null || abilities.Length == 0) return current.def?.basic;
            var choice = abilities
                .Where(a => a.canUse && !string.IsNullOrEmpty(a.id))
                .OrderByDescending(a => a.cost)
                .ThenBy(a => a.index)
                .Select(a => a.id)
                .FirstOrDefault();
            return choice ?? current.def?.basic;
        }

        void TryScheduleAutoForCurrent(bool force)
        {
            if (current == null || !gameObject.activeInHierarchy) return;
            if (isPerformingAbility) return;
            var team = current.Team;
            if (!force && team != Team.Player) return;
            if (!force && !autoBattleEnabled) return;
            if (autoQueued && autoQueuedTeam == team) return;
            autoQueued = true;
            autoQueuedTeam = team;
            nextAutoDecisionTime = Time.unscaledTime + AutoDecisionDelay;
        }

        void ScheduleAutoForCurrentTurn()
        {
            if (current == null) return;
            bool force = current.Team != Team.Player;
            TryScheduleAutoForCurrent(force);
        }

        void ProcessAutoDecision()
        {
            if (!autoQueued) return;
            if (Time.unscaledTime < nextAutoDecisionTime) return;
            if (current == null)
            {
                autoQueued = false;
                return;
            }
            if (isPerformingAbility) return;

            var team = current.Team;
            bool force = team != Team.Player;
            bool shouldAct = force || autoBattleEnabled;

            if (!shouldAct)
            {
                autoQueued = false;
                return;
            }

            if (team != autoQueuedTeam)
            {
                autoQueued = false;
                TryScheduleAutoForCurrent(force);
                return;
            }

            autoQueued = false;
            var abilityId = ChooseAutoAbility();
            if (string.IsNullOrEmpty(abilityId) || !TryUse(abilityId)) TriggerPass();
        }

        public void ToggleAutoBattle()
        {
            autoBattleEnabled = !autoBattleEnabled;

            bool processedImmediate = false;

            if (!autoBattleEnabled)
            {
                if (autoQueued && autoQueuedTeam == Team.Player)
                {
                    autoQueued = false;
                    nextAutoDecisionTime = 0f;
                }
            }
            else if (current != null && current.Team == Team.Player && !isPerformingAbility)
            {
                if (!autoQueued || autoQueuedTeam != current.Team)
                {
                    TryScheduleAutoForCurrent(false);
                }

                if (autoQueued && autoQueuedTeam == current.Team)
                {
                    nextAutoDecisionTime = Time.unscaledTime;
                    ProcessAutoDecision();
                    processedImmediate = true;
                }
            }

            RaiseStateChanged();

            if (!processedImmediate)
            {
                ScheduleAutoForCurrentTurn();
            }
        }

        public void CycleSpeed()
        {
            speedIndex = (speedIndex + 1) % speedSteps.Length;
            Time.timeScale = speedSteps[speedIndex];
            RaiseStateChanged();
        }

        public bool AutoBattleEnabled => autoBattleEnabled;
        public float CurrentSpeed => speedSteps[Mathf.Clamp(speedIndex, 0, speedSteps.Length - 1)];
        public int GetEnergy(Team team) => energy?.GetCurrent(team) ?? 0;
        public int GetMaxEnergy() => energy?.MaxEnergy ?? 10;
        public int GetBaselineEnergyForCurrentRound() => energy?.GetBaselineForRound(turn?.Round ?? 1) ?? 0;

        void RaiseStateChanged() => StateChanged?.Invoke();
    }
}
