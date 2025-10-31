using System;
using System.Collections.Generic;

namespace CleanRPG.Core
{
    [Serializable]
    public struct StatBundle
    {
        public int hp;
        public int atk;
        public int defStat;
        public int spd;
        public int crit;
        public static StatBundle operator +(StatBundle a, StatBundle b)
        {
            return new StatBundle
            {
                hp = a.hp + b.hp,
                atk = a.atk + b.atk,
                defStat = a.defStat + b.defStat,
                spd = a.spd + b.spd,
                crit = a.crit + b.crit
            };
        }
    }

    [Serializable]
    public class CosmoDefinition
    {
        public string id;
        public string slot;
        public StatBundle stats;
    }

    [Serializable]
    public class CharacterSkillLevelRecord
    {
        public string abilityId;
        public int level;
    }

    [Serializable]
    public class AbilityNodeRecord
    {
        public string nodeId;
        public int level;
    }

    [Serializable]
    public class CharacterProgressionRecord
    {
        public string id;
        public int level;
        public int rank;
        public bool seventhSense;
        public string[] cosmos;
        public CharacterSkillLevelRecord[] skills;
        public AbilityNodeRecord[] tree;
        public string[] unlockedConnections;
    }

    [Serializable]
    public class CharacterConnectionDefinition
    {
        public string id;
        public string name;
        public string description;
        public string[] members;
        public StatBundle bonus;
    }

    [Serializable]
    public class ProgressionPayload
    {
        public CosmoDefinition[] cosmos;
        public CharacterProgressionRecord[] characters;
        public CharacterConnectionDefinition[] connections;
    }
}

namespace CleanRPG.Systems
{
    using CleanRPG.Core;

    public class CharacterProgressionState
    {
        public string id;
        public int level = 1;
        public int rank = 1;
        public bool seventhSense;
        public List<string> cosmos = new List<string>();
        public Dictionary<string, int> skillLevels = new Dictionary<string, int>();
        public Dictionary<string, int> treeNodes = new Dictionary<string, int>();
        public HashSet<string> unlockedConnections = new HashSet<string>();

        public CharacterProgressionState Clone()
        {
            return new CharacterProgressionState
            {
                id = id,
                level = level,
                rank = rank,
                seventhSense = seventhSense,
                cosmos = new List<string>(cosmos),
                skillLevels = new Dictionary<string, int>(skillLevels),
                treeNodes = new Dictionary<string, int>(treeNodes),
                unlockedConnections = new HashSet<string>(unlockedConnections)
            };
        }
    }

    public struct SynergyReport
    {
        public string id;
        public string name;
        public string description;
        public bool active;
        public StatBundle bonus;
        public string[] members;
    }
}
