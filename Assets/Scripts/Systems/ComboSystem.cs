
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CleanRPG.Battle;
using CleanRPG.Core;
namespace CleanRPG.Systems
{
    public enum ComboTag { None, Starter, Bridge, Finisher, Launcher, Breaker }
    public class ComboWindow { public CharacterRuntime opener; public float expiresAt; public ComboTag tag; public CharacterRuntime lastTarget; }
    public class ComboSystem : MonoBehaviour
    {
        private ComboWindow window;
        private float now => Time.time;
        private System.Collections.Generic.Dictionary<ComboTag, System.Collections.Generic.Dictionary<ComboTag, Action<CharacterRuntime, CharacterRuntime>>> effects = new();
        private void Awake()
        {
            effects[ComboTag.Starter] = new System.Collections.Generic.Dictionary<ComboTag, Action<CharacterRuntime, CharacterRuntime>>{
                { ComboTag.Finisher, (actor,target) => { int bonus = Mathf.RoundToInt(actor.ATK * 0.5f); target.TakeDamage(bonus); target.AddStatus(new StatusInstance(EffectType.Stun, 1, 0)); }}
            };
            effects[ComboTag.Bridge] = new System.Collections.Generic.Dictionary<ComboTag, Action<CharacterRuntime, CharacterRuntime>>{
                { ComboTag.Finisher, (actor,target) => { foreach (var ally in FindObjectsByType<CharacterRuntime>(FindObjectsSortMode.None).Where(c=>c.Team==actor.Team && c.IsAlive)) ally.AddShield(80, 2); target.AddStatus(new StatusInstance(EffectType.Bleed, 2, 5)); }}
            };
            effects[ComboTag.Launcher] = new System.Collections.Generic.Dictionary<ComboTag, Action<CharacterRuntime, CharacterRuntime>>{
                { ComboTag.Breaker, (actor,target) => { target.RemoveShield(); target.AddStatus(new StatusInstance(EffectType.DebuffSPD, 2, 5)); }}
            };
        }
        public void RegisterAction(CharacterRuntime actor, AbilityDefinition ab, CharacterRuntime target)
        {
            var tag = ParseComboTag(ab);
            if (tag == ComboTag.None) { TryExpire(); return; }
            if (window == null || now > window.expiresAt || window.lastTarget != target)
            { window = new ComboWindow{ opener = actor, expiresAt = now + 3.0f, tag = tag, lastTarget = target }; return; }
            if (effects.TryGetValue(window.tag, out var next) && next.TryGetValue(tag, out var effect))
            { effect?.Invoke(actor, target); window = null; }
        }
        public void ForceExpire() { window = null; }
        private void TryExpire(){ if (window != null && now > window.expiresAt) window = null; }
        private ComboTag ParseComboTag(AbilityDefinition ab)
        {
            if (!string.IsNullOrEmpty(ab.comboTag) && Enum.TryParse<ComboTag>(ab.comboTag, out var t)) return t;
            if (ab.kind == AbilityKind.Basic) return ComboTag.Starter;
            if (ab.kind == AbilityKind.Special) return ComboTag.Bridge;
            if (ab.kind == AbilityKind.Ultimate) return ComboTag.Finisher;
            return ComboTag.None;
        }
    }
}
