using System;
using System.Collections.Generic;
using UnityEngine;

namespace UI.Player.Common
{
    /// <summary>
    /// Lightweight object pool that instantiates UI items on demand and reuses them between refreshes.
    /// Prevents needless destroy/instantiate cycles when rebuilding dynamic lists every frame.
    /// </summary>
    /// <typeparam name="TItem">Component type expected on the pooled prefab.</typeparam>
    public sealed class DynamicListPool<TItem> where TItem : Component
    {
        private readonly Transform parent;
        private readonly GameObject prefab;
        private readonly List<TItem> items = new();
        private int activeCount;
        private bool warnedInvalid;

        public DynamicListPool(GameObject prefab, Transform parent)
        {
            this.prefab = prefab;
            this.parent = parent;
        }

        /// <summary>
        /// Number of items currently marked as active (visible).
        /// </summary>
        public int ActiveCount => activeCount;

        /// <summary>
        /// Enumerates the active items produced by the last render call.
        /// </summary>
        public IEnumerable<TItem> ActiveItems
        {
            get
            {
                for (int i = 0; i < activeCount; i++)
                    yield return items[i];
            }
        }

        /// <summary>
        /// Renders the supplied data set using pooled UI items.
        /// </summary>
        /// <typeparam name="TData">Data type supplied to the binder.</typeparam>
        /// <param name="data">Ordered list of view models to display.</param>
        /// <param name="binder">Callback invoked for each active item.</param>
        public void Render<TData>(IReadOnlyList<TData> data, Action<TItem, TData, int> binder)
        {
            int targetCount = data?.Count ?? 0;
            if (!EnsureReady())
            {
                activeCount = 0;
                return;
            }

            EnsureCapacity(targetCount);

            for (int i = 0; i < targetCount; i++)
            {
                var item = items[i];
                SetActive(item, true);
                binder?.Invoke(item, data![i], i);
            }

            for (int i = targetCount; i < items.Count; i++)
                SetActive(items[i], false);

            activeCount = targetCount;
        }

        /// <summary>
        /// Deactivates every pooled item without destroying instances.
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < items.Count; i++)
                SetActive(items[i], false);
            activeCount = 0;
        }

        private bool EnsureReady()
        {
            if (prefab != null && parent != null)
                return true;

            if (!warnedInvalid)
            {
                Debug.LogWarning($"[DynamicListPool<{typeof(TItem).Name}>] Missing prefab or parent transform. Pool cannot operate.");
                warnedInvalid = true;
            }

            return false;
        }

        private void EnsureCapacity(int targetCount)
        {
            while (items.Count < targetCount)
            {
                var instance = UnityEngine.Object.Instantiate(prefab, parent);
                var component = instance.GetComponent<TItem>() ?? instance.GetComponentInChildren<TItem>(true);
                if (component == null)
                    component = instance.AddComponent<TItem>();
                items.Add(component);
            }
        }

        private static void SetActive(Component item, bool value)
        {
            if (item == null)
                return;

            var go = item.gameObject;
            if (go.activeSelf != value)
                go.SetActive(value);
        }
    }
}

