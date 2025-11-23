using System;

namespace Player.Statistics
{
    /// <summary>
    /// Fluent builder to compose buff callbacks as needed before creating a <see cref="BuffRequest"/>.
    /// </summary>
    public sealed class BuffRequestBuilder
    {
        private readonly BuffKey key;
        private readonly float duration;
        private string displayName;
        private Action<BuffContext> onStart;
        private Action<BuffContext, float> onTick;
        private Action<BuffContext> onComplete;
        private Action<BuffContext> onCancelled;

        private BuffRequestBuilder(BuffKey key, float duration)
        {
            this.key = key;
            this.duration = duration;
        }

        public static BuffRequestBuilder Create(BuffKey key, float duration) => new(key, duration);

        public BuffRequestBuilder WithDisplayName(string name)
        {
            displayName = name;
            return this;
        }

        public BuffRequestBuilder OnStart(Action<BuffContext> callback)
        {
            onStart = callback;
            return this;
        }

        public BuffRequestBuilder OnTick(Action<BuffContext, float> callback)
        {
            onTick = callback;
            return this;
        }

        public BuffRequestBuilder OnComplete(Action<BuffContext> callback)
        {
            onComplete = callback;
            return this;
        }

        public BuffRequestBuilder OnCancelled(Action<BuffContext> callback)
        {
            onCancelled = callback;
            return this;
        }

        public BuffRequest Build()
        {
            var callbacks = new BuffCallbacks(onStart, onTick, onComplete, onCancelled);
            return new BuffRequest(key, displayName, duration, callbacks);
        }
    }
}
