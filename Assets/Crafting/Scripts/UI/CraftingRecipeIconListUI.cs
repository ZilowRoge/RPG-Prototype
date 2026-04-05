using System;
using System.Collections.Generic;
using Items;
using UI.Player.Common;
using UnityEngine;
using CraftingData = global::Crafting;

namespace UI.Player.Crafting
{
    public class CraftingRecipeIconListUI : MonoBehaviour
    {
        [SerializeField] private Transform contentRoot;
        [SerializeField] private GameObject iconPrefab;
        [SerializeField] private Color availableColor = Color.white;
        [SerializeField] private Color unavailableColor = new Color(1f, 0.45f, 0.45f, 1f);

        private DynamicListPool<CraftingItemIconUI> iconPool;

        private void Awake()
        {
            EnsureContentRoot();
            EnsurePool();
        }

        public void Bind(
            IReadOnlyList<CraftingData.RecipeItemAmount> items,
            ItemDefinitionDatabase itemDatabase,
            IReadOnlyList<bool> availability = null)
        {
            EnsureContentRoot();
            EnsurePool();

            if (items == null || items.Count == 0)
            {
                iconPool.Clear();
                return;
            }

            iconPool.Render(items, (icon, itemAmount, index) =>
            {
                ItemDefinition definition = null;
                if (itemDatabase != null && !string.IsNullOrWhiteSpace(itemAmount.ItemId))
                    itemDatabase.TryGetById(itemAmount.ItemId, out definition);

                var sprite = definition != null ? definition.Icon : null;
                icon.Bind(sprite, itemAmount.Amount, ResolveColor(availability, index, sprite));
            });
        }

        public void Clear()
        {
            iconPool?.Clear();
        }

        private void EnsureContentRoot()
        {
            if (contentRoot != null)
                return;

            var content = FindChildRecursive(transform, "Content");
            contentRoot = content != null ? content : transform;
        }

        private void EnsurePool()
        {
            if (iconPool == null)
                iconPool = new DynamicListPool<CraftingItemIconUI>(iconPrefab, contentRoot);
        }

        private static Transform FindChildRecursive(Transform parent, string childName)
        {
            if (parent == null || string.IsNullOrWhiteSpace(childName))
                return null;

            if (string.Equals(parent.name, childName, StringComparison.Ordinal))
                return parent;

            for (int i = 0; i < parent.childCount; i++)
            {
                var result = FindChildRecursive(parent.GetChild(i), childName);
                if (result != null)
                    return result;
            }

            return null;
        }

        private void Reset()
        {
            EnsureContentRoot();
        }

        private Color ResolveColor(IReadOnlyList<bool> availability, int index, Sprite sprite)
        {
            if (sprite == null)
                return unavailableColor;

            if (availability == null)
                return availableColor;

            return index >= 0 && index < availability.Count && !availability[index]
                ? unavailableColor
                : availableColor;
        }
    }
}
