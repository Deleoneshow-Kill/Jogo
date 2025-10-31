using System.Linq;
using UnityEngine;
using CleanRPG.Core;
namespace CleanRPG.Battle
{
    public partial class BattleBootstrap3D : MonoBehaviour
    {
        public class HUDState
        {
            public int round;
            public int energyPlayer;
            public int energyEnemy;
            public int energyBaseline;
            public int energyMax;
            public bool autoEnabled;
            public float speedMultiplier;
            public struct TurnChip{ public string name; public bool isEnemy; public string portraitPath; }
            public TurnChip[] turnChipsTMP;
            public struct CurrentInfo
            {
                public string name;
                public int hp;
                public int maxHP;
                public int spd;
                public int shield;
                public int level;
                public int rank;
                public int crit;
                public bool seventhSense;
                public bool isEnemy;
                public string portraitPath;
                public string[] cosmos;
            }
            public struct SynergyInfo
            {
                public string id;
                public string name;
                public string description;
                public bool active;
                public CleanRPG.Core.StatBundle bonus;
                public string[] members;
            }
            public CurrentInfo current;
            public SynergyInfo[] synergies;
        }
        public HUDState GetStateTMP()
        {
            var chips = turn==null? new HUDState.TurnChip[0] : turn.Queue.ToArray().Select(c => new HUDState.TurnChip{ name=c.def.displayName, isEnemy=(c.Team==CleanRPG.Core.Team.Enemy), portraitPath=c.def.portraitSpritePath }).ToArray();
            var cur = current ? new HUDState.CurrentInfo{
                name = current.def.displayName,
                hp = current.HP,
                maxHP = current.MaxHP,
                spd = current.EffectiveSPD(),
                shield = current.ShieldHP,
                level = current.Level,
                rank = current.Rank,
                crit = current.CritChance,
                seventhSense = current.SeventhSenseAwakened,
                isEnemy = current.Team == Team.Enemy,
                portraitPath = current.def != null ? current.def.portraitSpritePath : null,
                cosmos = current.EquippedCosmos?.ToArray() ?? System.Array.Empty<string>()
            } : default;
            int round = turn?.Round ?? 1;
            int playerEnergy = energy?.GetCurrent(Team.Player) ?? 0;
            int enemyEnergy = energy?.GetCurrent(Team.Enemy) ?? 0;
            int baseline = energy?.GetBaselineForRound(round) ?? 0;
            int max = energy?.MaxEnergy ?? 0;
            var synergy = playerSynergies?.Select(s => new HUDState.SynergyInfo{
                id = s.id,
                name = s.name,
                description = s.description,
                active = s.active,
                bonus = s.bonus,
                members = s.members
            }).ToArray() ?? new HUDState.SynergyInfo[0];
            return new HUDState
            {
                round = round,
                energyPlayer = playerEnergy,
                energyEnemy = enemyEnergy,
                energyBaseline = baseline,
                energyMax = max,
                autoEnabled = AutoBattleEnabled,
                speedMultiplier = CurrentSpeed,
                turnChipsTMP = chips,
                current = cur,
                synergies = synergy
            };
        }
        public struct AbilityUI { public string id, name; public int cost, cooldownRemaining, cooldownMax, index; public bool canUse; public string tooltip, iconPath; }
        public AbilityUI[] GetAbilityBar()
        {
            if (current == null || current.def == null) return new AbilityUI[0];
            var list = new System.Collections.Generic.List<AbilityUI>();
            string[] ids = new[]{ current.def.basic, current.def.special, current.def.ultimate };
            for (int i=0;i<ids.Length;i++)
            {
                var id = ids[i];
                if (string.IsNullOrEmpty(id) || !abDB.TryGetValue(id, out var ab)) continue;
                current.cooldowns.TryGetValue(id, out var remain);
                int cdMax = ab.cooldown;
                bool can = AbilityExecutor.CanUse(current, ab, energy);
                var tooltip = string.IsNullOrEmpty(ab.description)? ab.displayName : ab.description;
                list.Add(new AbilityUI{ id=id, name=ab.displayName, cost=ab.cost, cooldownRemaining=remain, cooldownMax=cdMax, canUse=can, index=i+1, tooltip=tooltip, iconPath=ab.iconSpritePath });
            }
            return list.ToArray();
        }
        public int GetShieldOfCurrent() => current ? current.ShieldHP : 0;
        public void TriggerAbilityKey(int idx){ if (idx==1) TryUse(current?.def?.basic); else if (idx==2) TryUse(current?.def?.special); else if (idx==3) TryUse(current?.def?.ultimate); }
        public void TriggerPass(){ EndTurn(); }
        public void TriggerTab(){ var enemies = all.Where(c=>c.Team==CleanRPG.Core.Team.Enemy && c.IsAlive).ToList(); if (enemies.Count>0){ int i = enemies.IndexOf(selectedTarget); i = (i + 1) % enemies.Count; selectedTarget = enemies[i]; RaiseStateChanged(); } }
    }
}
