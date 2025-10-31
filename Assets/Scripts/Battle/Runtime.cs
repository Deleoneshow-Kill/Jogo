
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CleanRPG.Core;
using CleanRPG.Systems;
namespace CleanRPG.Battle
{
    public class StatusInstance { public EffectType type; public int duration; public int potency; public StatusInstance(EffectType t,int d,int p){type=t;duration=d;potency=p;} }
    public class CharacterRuntime : MonoBehaviour
    {
        public CharacterDefinition def;
        public Team Team;
        public int HP { get; private set; }
        public int MaxHP { get; private set; }
        public int ShieldHP { get; private set; }
        public int ATK { get; private set; }
        public int DEF { get; private set; }
        public int SPD { get; private set; }
        public int CritChance { get; private set; }
        public CharacterProgressionState Progression { get; private set; }
        public StatBundle SynergyBonus { get; private set; }
        public Dictionary<string,int> cooldowns = new Dictionary<string,int>();
        public List<StatusInstance> statuses = new List<StatusInstance>();
        public bool IsAlive => HP>0;
        public int Level => Progression?.level ?? 1;
        public int Rank => Progression?.rank ?? 1;
        public bool SeventhSenseAwakened => Progression?.seventhSense ?? false;
        public IReadOnlyList<string> EquippedCosmos => Progression != null ? Progression.cosmos : System.Array.Empty<string>();

        public void Init(CharacterDefinition d, Team team)
        {
            def = d;
            Team = team;
            cooldowns.Clear();
            statuses.Clear();
            ShieldHP = 0;
            SynergyBonus = new StatBundle();
            CritChance = 5;
            BaseAssignFromDefinition();
            ProgressionSystem.ApplyProgression(def, this);
        }

        void BaseAssignFromDefinition()
        {
            MaxHP = Mathf.Max(1, def.maxHP);
            HP = MaxHP;
            ATK = Mathf.Max(1, def.atk);
            DEF = Mathf.Max(0, def.defStat);
            SPD = Mathf.Max(1, def.speed);
        }

        public void OverrideStats(int hp, int atk, int defStat, int spd, int crit)
        {
            MaxHP = Mathf.Max(1, hp);
            HP = MaxHP;
            ATK = Mathf.Max(1, atk);
            DEF = Mathf.Max(0, defStat);
            SPD = Mathf.Max(1, spd);
            CritChance = Mathf.Clamp(crit, 0, 100);
        }

        public void SetProgression(CharacterProgressionState state)
        {
            Progression = state;
        }

        public void ApplySynergyBonus(StatBundle bonus)
        {
            SynergyBonus = SynergyBonus + bonus;
            MaxHP = Mathf.Max(1, MaxHP + bonus.hp);
            HP = Mathf.Min(MaxHP, HP + bonus.hp);
            ATK = Mathf.Max(1, ATK + bonus.atk);
            DEF = Mathf.Max(0, DEF + bonus.defStat);
            SPD = Mathf.Max(1, SPD + bonus.spd);
            CritChance = Mathf.Clamp(CritChance + bonus.crit, 0, 100);
        }

        public void ResetShields()
        {
            ShieldHP = 0;
            statuses.RemoveAll(s => s.type == EffectType.Shield);
        }

        public int EffectiveSPD(){ int s=SPD; foreach(var st in statuses){ if(st.type==EffectType.BuffSPD) s+=st.potency; if(st.type==EffectType.DebuffSPD) s-=st.potency; } return Mathf.Max(1,s); }
        public void AddStatus(StatusInstance s){ statuses.Add(s); }
        public void TickStatuses(){ for(int i=statuses.Count-1;i>=0;i--){ if(statuses[i].duration>0){ statuses[i].duration--; if(statuses[i].duration<=0) statuses.RemoveAt(i);} } }
        public void AddShield(int amount,int duration){ ShieldHP+=amount; statuses.Add(new StatusInstance(EffectType.Shield,duration,amount)); }
        public void RemoveShield(){ ResetShields(); }
        public void TakeDamage(int amount){ int rem=amount; if(ShieldHP>0){ int absorb=Mathf.Min(ShieldHP,rem); ShieldHP-=absorb; rem-=absorb; if(ShieldHP<=0) statuses.RemoveAll(s=>s.type==EffectType.Shield);} if(rem>0){ int mitigation=Mathf.Max(0, DEF/15); int mitigated=Mathf.Max(0, rem-mitigation); HP=Mathf.Max(0,HP-mitigated);} }
        public void Heal(int amount){ HP=Mathf.Min(MaxHP, HP+Mathf.Max(0,amount)); }
    }
    public class TurnController
    {
        private System.Collections.Generic.List<CharacterRuntime> all;
        public int Round {get; private set;}=1;
        public System.Collections.Generic.Queue<CharacterRuntime> Queue {get; private set;}=new System.Collections.Generic.Queue<CharacterRuntime>();
        public TurnController(System.Collections.Generic.List<CharacterRuntime> actors){ all=actors; Refill(); }
        void Refill(){ Queue.Clear(); var sorted=new System.Collections.Generic.List<CharacterRuntime>(all); sorted.Sort((a,b)=> b.EffectiveSPD().CompareTo(a.EffectiveSPD())); foreach(var c in sorted) if(c.IsAlive) Queue.Enqueue(c); }
        public CharacterRuntime Next(){ if(Queue.Count==0){ Round++; foreach(var c in all) c.TickStatuses(); Refill(); } return Queue.Count>0?Queue.Dequeue():null; }
    }
    public class EnergySystem
    {
        private readonly int startEnergy;
        private readonly int maxEnergy;
        private readonly Dictionary<Team, int> pools = new Dictionary<Team, int>();
        public int Round { get; private set; } = 1;

        public EnergySystem(int startEnergy = 3, int maxEnergy = 10)
        {
            this.startEnergy = startEnergy;
            this.maxEnergy = Mathf.Max(1, maxEnergy);
        }

        public int MaxEnergy => maxEnergy;
        public int StartEnergy => startEnergy;

        public void Reset(IEnumerable<Team> teams)
        {
            pools.Clear();
            foreach (var team in teams.Distinct())
                pools[team] = startEnergy;
            Round = 1;
        }

        public void RegisterTeam(Team team)
        {
            if (!pools.ContainsKey(team)) pools[team] = startEnergy;
        }

        public int GetCurrent(Team team)
        {
            if (!pools.TryGetValue(team, out var value))
            {
                value = startEnergy;
                pools[team] = value;
            }
            return Mathf.Clamp(value, 0, maxEnergy);
        }

        public bool CanAfford(Team team, int cost)
        {
            if (cost <= 0) return true;
            return GetCurrent(team) >= cost;
        }

        public bool Spend(Team team, int cost)
        {
            if (!CanAfford(team, cost)) return false;
            pools[team] = Mathf.Clamp(GetCurrent(team) - Mathf.Max(0, cost), 0, maxEnergy);
            return true;
        }

        public void SyncRound(int round)
        {
            Round = Mathf.Max(1, round);
            int baseline = GetBaselineForRound(Round);
            var teams = new List<Team>(pools.Keys);
            foreach (var team in teams)
            {
                pools[team] = Mathf.Clamp(Mathf.Max(pools[team], baseline), 0, maxEnergy);
            }
        }

        public int GetBaselineForRound(int round)
        {
            return Mathf.Min(maxEnergy, startEnergy + Mathf.Max(0, round - 1));
        }
    }
}
