using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Player.Statistics
{
    /// <summary>
    /// Manages timed buffs applied to the player.
    /// </summary>
    public class BuffController : MonoBehaviour
    {
        [SerializeField] private StatsController statsController;

        private readonly List<ActiveBuff> activeBuffs = new();
        private readonly List<BuffInfo> infoBuffer = new();
        private int nextBuffId = 1;

        private void Reset()
        {
            AssignDependencies();
        }

        private void OnValidate()
        {
            AssignDependencies();
        }

        private void Awake()
        {
            AssignDependencies();
        }

        private void OnDisable()
        {
            for (int i = activeBuffs.Count - 1; i >= 0; i--)
            {
                var buff = activeBuffs[i];
                if (buff.Routine != null)
                    StopCoroutine(buff.Routine);

                buff.Elapsed = buff.Duration;
                buff.Request.Callbacks.OnCancelled?.Invoke(buff.Context);
            }

            activeBuffs.Clear();
        }

        public IReadOnlyList<BuffInfo> ActiveBuffs
        {
            get
            {
                infoBuffer.Clear();
                foreach (var buff in activeBuffs)
                    infoBuffer.Add(buff.ToInfo());
                return infoBuffer;
            }
        }

        public bool ApplyBuff(BuffRequest request)
        {
            if (!request.IsValid)
            {
                Debug.LogWarning("[BuffController] Invalid buff request supplied.", this);
                return false;
            }

            AssignDependencies();

            CancelExistingBuffs(request.Key);

            var context = new BuffContext(this, statsController);
            var buff = new ActiveBuff(nextBuffId++, request, context);
            buff.Routine = StartCoroutine(RunBuff(buff));
            activeBuffs.Add(buff);
            return true;
        }

        private IEnumerator RunBuff(ActiveBuff buff)
        {
            buff.Request.Callbacks.OnStart?.Invoke(buff.Context);

            if (buff.Duration <= Mathf.Epsilon)
            {
                buff.Request.Callbacks.OnTick?.Invoke(buff.Context, buff.Duration);
                buff.Request.Callbacks.OnComplete?.Invoke(buff.Context);
                FinalizeBuff(buff);
                yield break;
            }

            while (buff.Elapsed < buff.Duration)
            {
                float delta = Mathf.Min(Time.deltaTime, buff.Duration - buff.Elapsed);
                if (delta > 0f)
                    buff.Request.Callbacks.OnTick?.Invoke(buff.Context, delta);

                buff.Elapsed += delta;
                yield return null;
            }

            buff.Request.Callbacks.OnComplete?.Invoke(buff.Context);
            FinalizeBuff(buff);
        }

        private void FinalizeBuff(ActiveBuff buff)
        {
            activeBuffs.Remove(buff);
            buff.Elapsed = buff.Duration;
        }

        private void CancelExistingBuffs(BuffKey key)
        {
            if (key == BuffKey.None)
                return;

            for (int i = activeBuffs.Count - 1; i >= 0; i--)
            {
                var existing = activeBuffs[i];
                if (existing.Request.Key != key)
                    continue;

                if (existing.Routine != null)
                    StopCoroutine(existing.Routine);

                existing.Request.Callbacks.OnCancelled?.Invoke(existing.Context);
                activeBuffs.RemoveAt(i);
            }
        }

        private void AssignDependencies()
        {
            if (statsController == null)
                statsController = GetComponent<StatsController>() ?? GetComponentInParent<StatsController>();
        }

        [Serializable]
        public readonly struct BuffInfo
        {
            public BuffInfo(int id, BuffKey key, string displayName, float duration, float elapsed)
            {
                Id = id;
                Key = key;
                DisplayName = displayName;
                Duration = duration;
                Elapsed = elapsed;
            }

            public int Id { get; }
            public BuffKey Key { get; }
            public string DisplayName { get; }
            public float Duration { get; }
            public float Elapsed { get; }
        }

        private class ActiveBuff
        {
            public ActiveBuff(int id, BuffRequest request, BuffContext context)
            {
                Id = id;
                Request = request;
                Context = context;
                Duration = request.Duration;
            }

            public int Id { get; }
            public BuffRequest Request { get; }
            public BuffContext Context { get; }
            public float Duration { get; }
            public float Elapsed { get; set; }
            public Coroutine Routine { get; set; }

            public BuffInfo ToInfo()
            {
                float clamped = Mathf.Clamp(Elapsed, 0f, Duration);
                return new BuffInfo(Id, Request.Key, Request.DisplayName, Duration, clamped);
            }
        }
    }
}
