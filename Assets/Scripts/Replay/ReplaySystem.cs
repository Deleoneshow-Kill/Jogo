
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CleanRPG.Battle;
using CleanRPG.Core;

namespace CleanRPG.Replay
{
    public class ReplaySystem : MonoBehaviour
    {
        [System.Serializable] public class ActionEvent
        {
            public string actorId;
            public string abilityId;
            public string targetId;
            public float time;
        }

        public bool IsRecording { get; private set; }
        public bool IsPlaying { get; private set; }
        public float playbackSpeed = 0.25f;

        private List<ActionEvent> timeline = new List<ActionEvent>();
        private float startTime;

        public void StartRecording(){ IsRecording = true; timeline.Clear(); startTime = Time.time; }
        public void StopRecording(){ IsRecording = false; }
        public void Clear(){ IsRecording=false; IsPlaying=false; timeline.Clear(); }

        public void OnAction(CharacterRuntime actor, AbilityDefinition ab, CharacterRuntime target)
        {
            if (!IsRecording) return;
            timeline.Add(new ActionEvent{
                actorId = actor.def.id, abilityId = ab.id, targetId = target? target.def.id : null,
                time = Time.time - startTime
            });
        }

        public void Play(CleanRPG.Battle.BattleBootstrap3D boot)
        {
            if (timeline.Count==0 || IsPlaying) return;
            StartCoroutine(PlayRoutine(boot));
        }

        IEnumerator PlayRoutine(CleanRPG.Battle.BattleBootstrap3D boot)
        {
            IsPlaying = true;
            float oldScale = Time.timeScale;
            Time.timeScale = playbackSpeed;

            // reset a fresh setup with same teams (using boot.SetupDefault for now)
            var players = new List<string>(); var enemies = new List<string>();
            foreach (var c in boot.all){ if (c.Team==Team.Player) players.Add(c.def.id); else enemies.Add(c.def.id); }
            boot.SetupWithTeam(players, enemies);

            float t0 = Time.time;
            foreach (var ev in timeline)
            {
                // wait until event time (scaled)
                float targetTime = t0 + ev.time / playbackSpeed;
                while (Time.time < targetTime) yield return null;

                // find actor/target and execute
                var actor = boot.all.Find(x=>x.def.id==ev.actorId && x.Team==Team.Player) ?? boot.all.Find(x=>x.def.id==ev.actorId);
                var target = boot.all.Find(x=>x.def.id==ev.targetId);
                if (boot.abDB.TryGetValue(ev.abilityId, out var ab))
                {
                    // bypass costs/cooldowns so recordings remain deterministic
                    CleanRPG.Battle.AbilityExecutor.Execute(boot, actor, ab, target, true);
                }
            }

            Time.timeScale = oldScale;
            IsPlaying = false;
        }
    }
}
