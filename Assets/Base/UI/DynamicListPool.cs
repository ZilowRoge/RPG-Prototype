using System;
using System.Collections.Generic;
using UnityEngine;

namespace Common.UI
{
    /// <summary>
    /// Lightweight object pool that instantiates UI items on demand and reuses them between refreshes.
    /// Prevents needless destroy/instantiate cycles when rebuilding dynamic lists.
    /// </summary>
    public class DynamicListPool<TItem> where TItem : Component
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

        public int ActiveCount => activeCount;

        public IEnumerable<TItem> ActiveItems
        {
            get
            {
                for (int i = 0; i < activeCount; i++)
                    yield return items[i];
            }
        }

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
