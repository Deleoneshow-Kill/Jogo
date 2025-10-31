using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CleanRPG.Core;

namespace CleanRPG.Battle
{
    /// <summary>
    /// Handles the cinematic sequencing of abilities: short dash, impact pulse and return.
    /// Keeps the logical execution (damage, shields, etc.) in sync via callback.
    /// </summary>
    public class BattleAbilityTimeline : MonoBehaviour
    {
        public struct Request
        {
            public CharacterRuntime actor;
            public CharacterRuntime target;
            public AbilityDefinition ability;
            public Action onResolve;
            public Action onComplete;
        }

        readonly Queue<Request> queue = new Queue<Request>();
        BattleBootstrap3D owner;
        bool processing;

        public bool IsProcessing => processing;

        public void Initialize(BattleBootstrap3D bootstrap)
        {
            owner = bootstrap;
        }

        public void Enqueue(Request request)
        {
            queue.Enqueue(request);
            if (!processing)
                StartCoroutine(Process());
        }

        IEnumerator Process()
        {
            processing = true;
            while (queue.Count > 0)
            {
                var req = queue.Dequeue();
                yield return Play(req);
            }
            processing = false;
        }

        IEnumerator Play(Request request)
        {
            var actor = request.actor;
            if (actor == null)
            {
                request.onResolve?.Invoke();
                request.onComplete?.Invoke();
                yield break;
            }

            var startPos = actor.transform.position;
            var startRot = actor.transform.rotation;

            var focus = FindApproachPoint(actor, request.target);
            var dashTime = 0.25f;
            yield return MoveOverTime(actor.transform, startPos, focus, dashTime);

            actor.transform.LookAt(request.target ? request.target.transform.position : focus);
            SpawnImpact(focus);

            yield return new WaitForSeconds(0.15f);
            request.onResolve?.Invoke();
            yield return new WaitForSeconds(0.2f);

            yield return MoveOverTime(actor.transform, actor.transform.position, startPos, dashTime);
            actor.transform.position = startPos;
            actor.transform.rotation = startRot;
            request.onComplete?.Invoke();
        }

        Vector3 FindApproachPoint(CharacterRuntime actor, CharacterRuntime target)
        {
            var start = actor.transform.position;
            if (target == null)
                return start + actor.transform.forward * 1.5f;

            var targetPos = target.transform.position;
            var direction = (targetPos - start).normalized;
            if (direction.sqrMagnitude < Mathf.Epsilon)
                direction = actor.transform.forward;

            var offset = direction * Mathf.Clamp(Vector3.Distance(start, targetPos) - 1.2f, 0.5f, 3f);
            var approach = targetPos - offset;
            approach.y = start.y;
            return approach;
        }

        IEnumerator MoveOverTime(Transform subject, Vector3 from, Vector3 to, float duration)
        {
            if (duration <= 0f)
            {
                subject.position = to;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                subject.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
        }

        void SpawnImpact(Vector3 position)
        {
            var fx = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            if (fx == null) return;

            Destroy(fx.GetComponent<Collider>());
            fx.transform.position = position + Vector3.up * 0.8f;
            fx.transform.localScale = Vector3.one * 0.4f;

            var renderer = fx.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                var mat = renderer.material;
                if (mat != null)
                {
                    mat.color = new Color(1f, 0.8f, 0.2f, 0.9f);
                    if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.8f);
                }
            }

            StartCoroutine(ScaleAndFade(fx));
        }

        IEnumerator ScaleAndFade(GameObject fx)
        {
            if (fx == null) yield break;

            var renderer = fx.GetComponent<MeshRenderer>();
            var startScale = fx.transform.localScale;
            float duration = 0.35f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                fx.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                if (renderer != null && renderer.material != null)
                {
                    var col = renderer.material.color;
                    col.a = Mathf.Lerp(0.9f, 0f, t);
                    renderer.material.color = col;
                }
                yield return null;
            }

            if (fx != null)
                Destroy(fx);
        }
    }
}
