
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CleanRPG.Battle
{
    public class CombatWatcher : MonoBehaviour
    {
        public class DeltaEvent{ public string type; public CharacterRuntime target; public int amount; public string statusName; }
        private readonly Dictionary<CharacterRuntime, int> hp = new();
        private readonly Dictionary<CharacterRuntime, int> statusCounts = new();
        private readonly System.Collections.Generic.Queue<DeltaEvent> events = new();
        private System.Collections.Generic.List<CharacterRuntime> all = new();

        public void Initialize(System.Collections.Generic.List<CharacterRuntime> actors)
        {
            all = actors;
            foreach (var c in actors)
            { hp[c] = c.HP; statusCounts[c] = c.statuses.Count; }
        }

        void LateUpdate()
        {
            foreach (var c in all.ToArray())
            {
                if (c == null) continue;
                if (!hp.ContainsKey(c)) { hp[c] = c.HP; statusCounts[c] = c.statuses.Count; }
                int prev = hp[c];
                if (c.HP < prev) events.Enqueue(new DeltaEvent{ type="damage", target=c, amount=prev-c.HP });
                else if (c.HP > prev) events.Enqueue(new DeltaEvent{ type="heal", target=c, amount=c.HP-prev });
                hp[c] = c.HP;

                int sc = c.statuses.Count;
                if (sc > statusCounts[c]) events.Enqueue(new DeltaEvent{ type="status", target=c, statusName=c.statuses.Last().type.ToString() });
                statusCounts[c] = sc;

                if (c.HP == 0 && prev > 0) events.Enqueue(new DeltaEvent{ type="death", target=c });
            }
        }

        public DeltaEvent TryDequeue(){ return events.Count==0 ? null : events.Dequeue(); }
    }
}
