using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CleanRPG.Core;
using CleanRPG.Battle;

namespace CleanRPG.Systems
{
    /// <summary>
    /// Loads progression templates and calculates runtime stat adjustments.
    /// </summary>
    public static class ProgressionSystem
    {
        static bool loaded;
        static readonly Dictionary<string, CosmoDefinition> cosmos = new Dictionary<string, CosmoDefinition>();
        static readonly Dictionary<string, CharacterProgressionState> states = new Dictionary<string, CharacterProgressionState>();
        static readonly Dictionary<string, CharacterConnectionDefinition> connections = new Dictionary<string, CharacterConnectionDefinition>();

        public static void LoadAll()
        {
            if (loaded) return;
            foreach (var asset in Resources.LoadAll<TextAsset>("Progression"))
            {
                if (string.IsNullOrEmpty(asset.text)) continue;
                var payload = JsonUtility.FromJson<ProgressionPayload>(asset.text);
                if (payload == null) continue;

                if (payload.cosmos != null)
                {
                    foreach (var c in payload.cosmos)
                    {
                        if (string.IsNullOrEmpty(c.id)) continue;
                        cosmos[c.id] = c;
                    }
                }

                if (payload.characters != null)
                {
                    foreach (var record in payload.characters)
                    {
                        if (string.IsNullOrEmpty(record.id)) continue;
                        var state = new CharacterProgressionState
                        {
                            id = record.id,
                            level = Mathf.Max(1, record.level),
                            rank = Mathf.Max(1, record.rank),
                            seventhSense = record.seventhSense
                        };
                        if (record.cosmos != null) state.cosmos.AddRange(record.cosmos);
                        if (record.skills != null)
                        {
                            foreach (var skill in record.skills)
                            {
                                if (!string.IsNullOrEmpty(skill.abilityId)) state.skillLevels[skill.abilityId] = Mathf.Max(1, skill.level);
                            }
                        }
                        if (record.tree != null)
                        {
                            foreach (var node in record.tree)
                            {
                                if (!string.IsNullOrEmpty(node.nodeId)) state.treeNodes[node.nodeId] = Mathf.Max(1, node.level);
                            }
                        }
                        if (record.unlockedConnections != null)
                        {
                            foreach (var conn in record.unlockedConnections) state.unlockedConnections.Add(conn);
                        }
                        states[record.id] = state;
                    }
                }

                if (payload.connections != null)
                {
                    foreach (var conn in payload.connections)
                    {
                        if (string.IsNullOrEmpty(conn.id) || conn.members == null || conn.members.Length == 0) continue;
                        connections[conn.id] = conn;
                    }
                }
            }
            loaded = true;
        }

        public static CharacterProgressionState GetState(string id)
        {
            LoadAll();
            if (!states.TryGetValue(id, out var state))
            {
                state = new CharacterProgressionState { id = id };
                states[id] = state;
            }
            return state;
        }

        public static IReadOnlyDictionary<string, CosmoDefinition> Cosmos => cosmos;
        public static IReadOnlyDictionary<string, CharacterConnectionDefinition> Connections => connections;

        public static CharacterProgressionState CloneState(string id)
        {
            return GetState(id).Clone();
        }

        public static void ApplyProgression(CharacterDefinition def, CharacterRuntime runtime)
        {
            if (def == null || runtime == null) return;
            var state = GetState(def.id);
            var factors = CalculateStatFactors(state);
            var baseHP = Mathf.RoundToInt(def.maxHP * factors.multiplier) + factors.bonus.hp;
            var baseATK = Mathf.RoundToInt(def.atk * factors.multiplier) + factors.bonus.atk;
            var baseDEF = Mathf.RoundToInt(def.defStat * factors.multiplier) + factors.bonus.defStat;
            var baseSPD = Mathf.RoundToInt(def.speed * factors.speedMultiplier) + factors.bonus.spd;
            var crit = Mathf.Clamp(state.rank * 2 + factors.bonus.crit, 0, 100);

            runtime.OverrideStats(baseHP, baseATK, baseDEF, baseSPD, crit);
            runtime.SetProgression(state.Clone());
        }

        struct FactorPack
        {
            public float multiplier;
            public float speedMultiplier;
            public StatBundle bonus;
        }

        static FactorPack CalculateStatFactors(CharacterProgressionState state)
        {
            var multiplier = 1f + (state.level - 1) * 0.02f + state.rank * 0.05f;
            float speedMultiplier = 1f + (state.level - 1) * 0.01f;
            if (state.seventhSense)
            {
                multiplier += 0.15f;
                speedMultiplier += 0.05f;
            }
            var bonus = new StatBundle();
            foreach (var cosmoId in state.cosmos)
            {
                if (string.IsNullOrEmpty(cosmoId)) continue;
                if (!cosmos.TryGetValue(cosmoId, out var cosmo)) continue;
                bonus += cosmo.stats;
            }
            return new FactorPack { multiplier = multiplier, speedMultiplier = speedMultiplier, bonus = bonus };
        }

        public static List<SynergyReport> EvaluateSynergy(IEnumerable<CharacterRuntime> actors)
        {
            LoadAll();
            var ids = actors.Where(a => a != null).Select(a => a.def?.id).Where(id => !string.IsNullOrEmpty(id)).ToList();
            return EvaluateSynergy(ids);
        }

        public static List<SynergyReport> EvaluateSynergy(IEnumerable<string> actorIds)
        {
            LoadAll();
            var list = actorIds.Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
            var reports = new List<SynergyReport>();
            foreach (var kv in connections)
            {
                var conn = kv.Value;
                var active = conn.members.All(m => list.Contains(m));
                reports.Add(new SynergyReport
                {
                    id = conn.id,
                    name = string.IsNullOrEmpty(conn.name) ? conn.id : conn.name,
                    description = conn.description,
                    active = active,
                    bonus = active ? conn.bonus : new StatBundle(),
                    members = conn.members
                });
            }
            return reports;
        }

        public static CharacterProgressionState LevelUp(string id, int levels = 1)
        {
            var state = GetState(id);
            state.level = Mathf.Clamp(state.level + Mathf.Max(1, levels), 1, 80);
            states[id] = state;
            return state;
        }

        public static CharacterProgressionState PromoteRank(string id)
        {
            var state = GetState(id);
            state.rank = Mathf.Clamp(state.rank + 1, 1, 6);
            states[id] = state;
            return state;
        }

        public static void EquipCosmo(string id, string cosmoId)
        {
            var state = GetState(id);
            if (!state.cosmos.Contains(cosmoId)) state.cosmos.Add(cosmoId);
            states[id] = state;
        }

        public static void UnlockConnection(string id, string connectionId)
        {
            var state = GetState(id);
            state.unlockedConnections.Add(connectionId);
            states[id] = state;
        }
    }
}
