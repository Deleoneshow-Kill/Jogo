using System.Collections.Generic;
using UnityEngine;
using CleanRPG.Core;

namespace CleanRPG.Battle
{
    /// <summary>
    /// Centralised ability flow helpers so callers can share the same rules.
    /// </summary>
    public static class AbilityRuntime
    {
        public static bool CanUse(CharacterRuntime actor, AbilityDefinition ability, EnergySystem energy, bool bypassCosts = false)
        {
            if (actor == null || ability == null) return false;
            if (bypassCosts) return true;
            if (energy == null) return false;
            if (ability.cost > 0 && !energy.CanAfford(actor.Team, ability.cost)) return false;
            if (actor.cooldowns.TryGetValue(ability.id, out var turns) && turns > 0) return false;
            return true;
        }

        public static void ApplyCooldowns(List<CharacterRuntime> party)
        {
            if (party == null) return;
            foreach (var unit in party)
            {
                if (unit == null || unit.cooldowns.Count == 0) continue;
                var keys = new List<string>(unit.cooldowns.Keys);
                foreach (var key in keys)
                {
                    var remain = unit.cooldowns[key] - 1;
                    if (remain <= 0) unit.cooldowns.Remove(key);
                    else unit.cooldowns[key] = remain;
                }
            }
        }

        public static void Execute(BattleBootstrap3D boot, CharacterRuntime actor, AbilityDefinition ability, CharacterRuntime target, bool bypassCosts = false)
        {
            if (boot == null || actor == null || ability == null) return;
            if (target == null) target = actor;

            if (!CanUse(actor, ability, boot.energy, bypassCosts)) return;
            if (!bypassCosts && ability.cost > 0 && !boot.energy.Spend(actor.Team, ability.cost)) return;

            ApplyEffect(actor, target, ability);

            if (!bypassCosts && ability.cooldown > 0)
            {
                actor.cooldowns[ability.id] = ability.cooldown;
            }

            boot.GetComponent<CleanRPG.Systems.ComboSystem>()?.RegisterAction(actor, ability, target);
            boot.GetComponent<CleanRPG.Replay.ReplaySystem>()?.OnAction(actor, ability, target);
        }

        static void ApplyEffect(CharacterRuntime actor, CharacterRuntime target, AbilityDefinition ability)
        {
            switch (ability.effect)
            {
                case EffectType.Damage:
                    {
                        int damage = Mathf.Max(1, Mathf.RoundToInt(actor.ATK * (ability.power / 100f)));
                        target.TakeDamage(damage);
                        break;
                    }
                case EffectType.Heal:
                    target.Heal(Mathf.Max(1, ability.power));
                    break;
                case EffectType.Shield:
                    target.AddShield(Mathf.Max(1, ability.power), Mathf.Max(1, ability.duration));
                    break;
                case EffectType.BuffSPD:
                case EffectType.DebuffSPD:
                case EffectType.Stun:
                case EffectType.Bleed:
                case EffectType.Taunt:
                case EffectType.Immunity:
                    target.AddStatus(new StatusInstance(ability.effect, Mathf.Max(1, ability.duration), ability.potency));
                    break;
                case EffectType.Cleanse:
                    target.statuses.Clear();
                    break;
            }
        }
    }

    /// <summary>
    /// Backwards compatibility wrapper preserving the previous API.
    /// </summary>
    public static class AbilityExecutor
    {
        public static bool CanUse(CharacterRuntime actor, AbilityDefinition ability, EnergySystem energy) => AbilityRuntime.CanUse(actor, ability, energy);
        public static void ApplyCooldowns(List<CharacterRuntime> party) => AbilityRuntime.ApplyCooldowns(party);
        public static void Execute(BattleBootstrap3D boot, CharacterRuntime actor, AbilityDefinition ability, CharacterRuntime target) => AbilityRuntime.Execute(boot, actor, ability, target);
        public static void Execute(BattleBootstrap3D boot, CharacterRuntime actor, AbilityDefinition ability, CharacterRuntime target, bool bypassCosts) => AbilityRuntime.Execute(boot, actor, ability, target, bypassCosts);
    }
}
