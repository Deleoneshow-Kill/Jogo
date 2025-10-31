
using System;
namespace CleanRPG.Core
{
    [Serializable] public class CharacterDefinition
    {
        [Serializable] public class AbilitySlot
        {
            public string id;
            public string name;
            public int cost;
            public int cooldown;
            public string description;
            public string iconSpritePath;
            public string effect;
            public int power;
            public int potency;
            public int duration;
        }

        public string id;
        public string displayName;
        public string faction;
        public Role role;
        public string rarity;
        public int maxHP;
        public int atk;
        public int defStat;
        public int speed;
        public string[] tags;
        public string basic;
        public string special;
        public string ultimate;
        public string passive;
        public string portraitSpritePath;
        public string iconSpritePath;

        // Alternate payload fields for legacy/partner data imports.
        public string name;
        public string @class;
        public int hp;
        public int def;
        public int spd;
        public int attack;
        public int defense;
        public int speedStat;
        public int rarityNumeric;
        public AbilitySlot[] abilities;
    }
    [Serializable] public class AbilityDefinition
    {
        public string id, displayName;
        public AbilityKind kind;
        public CleanRPG.Core.EffectType effect;
        public int power, potency, duration, cost, cooldown;
        public string target, description, iconSpritePath;
        public string comboTag;
    }
}
