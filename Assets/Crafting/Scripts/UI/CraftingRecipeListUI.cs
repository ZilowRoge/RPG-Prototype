using System;
using System.Collections.Generic;
using UI.Player.Common;
using UnityEngine;
using CraftingData = global::Crafting;

namespace UI.Player.Crafting
{
    public class CraftingRecipeListUI : MonoBehaviour
    {
        [SerializeField] private Transform contentRoot;
        [SerializeField] private GameObject itemPrefab;
        [SerializeField] private bool autoSelectFirst = true;
        [SerializeField] private Color selectedTextColor = Color.white;
        [SerializeField] private Color normalTextColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        [SerializeField] private Color selectedUnavailableTextColor = new Color(1f, 0.7f, 0.7f, 1f);
        [SerializeField] private Color unavailableTextColor = new Color(0.75f, 0.45f, 0.45f, 1f);

        private readonly List<CraftingData.CraftingRecipe> recipeBuffer = new();
        private DynamicListPool<CraftingRecipeListItemUI> itemPool;
        private CraftingData.CraftingRecipe currentSelection;

        public event Action<CraftingData.CraftingRecipe> RecipeSelected;

        private void Awake()
        {
            EnsureContentRoot();
            EnsurePool();
        }

        public CraftingData.CraftingRecipe Refresh(
            IReadOnlyList<CraftingData.CraftingRecipe> recipes,
            CraftingData.CraftingRecipe selectedRecipe,
            IReadOnlyDictionary<CraftingData.CraftingRecipe, CraftingData.CraftingRecipeAvailability> availabilityByRecipe = null)
        {
            EnsureContentRoot();
            EnsurePool();

            recipeBuffer.Clear();
            if (recipes != null)
            {
                for (int i = 0; i < recipes.Count; i++)
                {
                    var recipe = recipes[i];
                    if (recipe != null)
                        recipeBuffer.Add(recipe);
                }
            }

            currentSelection = DetermineSelection(selectedRecipe);

            itemPool.Render(recipeBuffer, (item, recipe, _) =>
            {
                bool canCraft = availabilityByRecipe == null ||
                                !availabilityByRecipe.TryGetValue(recipe, out var availability) ||
                                availability.CanCraft;

                item.Bind(recipe, ResolveRecipeLabel(recipe), HandleItemClicked, canCraft);
                item.UpdateVisualState(
                    ReferenceEquals(recipe, currentSelection),
                    selectedTextColor,
                    normalTextColor,
                    selectedUnavailableTextColor,
                    unavailableTextColor);
            });

            return currentSelection;
        }

        public void ApplySelection(CraftingData.CraftingRecipe recipe)
        {
            currentSelection = recipe;

            if (itemPool == null)
                return;

            foreach (var item in itemPool.ActiveItems)
            {
                item.UpdateVisualState(
                    ReferenceEquals(item.Recipe, currentSelection),
                    selectedTextColor,
                    normalTextColor,
                    selectedUnavailableTextColor,
                    unavailableTextColor);
            }
        }

        public void Clear()
        {
            currentSelection = null;
            recipeBuffer.Clear();
            itemPool?.Clear();
        }

        private CraftingData.CraftingRecipe DetermineSelection(CraftingData.CraftingRecipe selectedRecipe)
        {
            if (selectedRecipe != null)
            {
                for (int i = 0; i < recipeBuffer.Count; i++)
                {
                    if (ReferenceEquals(recipeBuffer[i], selectedRecipe))
                        return selectedRecipe;
                }
            }

            if (autoSelectFirst && recipeBuffer.Count > 0)
                return recipeBuffer[0];

            return null;
        }

        private void HandleItemClicked(CraftingData.CraftingRecipe recipe)
        {
            ApplySelection(recipe);
            RecipeSelected?.Invoke(recipe);
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
            if (itemPool == null)
                itemPool = new DynamicListPool<CraftingRecipeListItemUI>(itemPrefab, contentRoot);
        }

        private static string ResolveRecipeLabel(CraftingData.CraftingRecipe recipe)
        {
            if (recipe == null)
                return string.Empty;

            if (!string.IsNullOrWhiteSpace(recipe.name) &&
                !string.Equals(recipe.name, "CraftingRecipe", StringComparison.OrdinalIgnoreCase))
            {
                return recipe.name;
            }

            if (!string.IsNullOrWhiteSpace(recipe.Id))
                return recipe.Id;

            return recipe.name;
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
    }
}
