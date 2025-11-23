using System;
using UnityEngine;

namespace Player.Statistics
{
    /// <summary>
    /// Definition for a timed buff handled by <see cref="BuffController"/>.
    /// </summary>
    public readonly struct BuffRequest
    {
        public BuffRequest(BuffKey key, string displayName, float duration, BuffCallbacks callbacks)
        {
            Key = key;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? key.ToString() : displayName;
            Duration = Mathf.Max(0f, duration);
            Callbacks = callbacks;
        }

        public BuffKey Key { get; }
        public string DisplayName { get; }
        public float Duration { get; }
        public BuffCallbacks Callbacks { get; }

        public bool IsValid => Key != BuffKey.None && Duration >= 0f && Callbacks.HasCallbacks;
    }

    public readonly struct BuffCallbacks
    {
        public BuffCallbacks(
            Action<BuffContext> onStart,
            Action<BuffContext, float> onTick,
            Action<BuffContext> onComplete,
            Action<BuffContext> onCancelled = null)
        {
            OnStart = onStart;
            OnTick = onTick;
            OnComplete = onComplete;
            OnCancelled = onCancelled;
        }

        public Action<BuffContext> OnStart { get; }
        public Action<BuffContext, float> OnTick { get; }
        public Action<BuffContext> OnComplete { get; }
        public Action<BuffContext> OnCancelled { get; }

        public bool HasCallbacks => OnStart != null || OnTick != null || OnComplete != null || OnCancelled != null;
    }

    public readonly struct BuffContext
    {
        internal BuffContext(BuffController controller, StatsController stats)
        {
            Controller = controller;
            Stats = stats;
        }

        public BuffController Controller { get; }
        public GameObject Owner => Controller != null ? Controller.gameObject : null;
        public StatsController Stats { get; }
    }
}
