using System.Collections.Generic;
using UnityEngine;

namespace Crafting
{
    [CreateAssetMenu(fileName = "CraftingRecipeDatabase", menuName = "Crafting/Recipe Database")]
    public class CraftingRecipeDatabase : ScriptableObject
    {
        [SerializeField] private List<CraftingRecipe> recipes = new();

        private readonly Dictionary<string, CraftingRecipe> recipesById = new();
        private int cachedSignature = -1;

        public IReadOnlyList<CraftingRecipe> Recipes => recipes;

        public IReadOnlyDictionary<string, CraftingRecipe> RecipesById
        {
            get
            {
                EnsureLookup();
                return recipesById;
            }
        }

        private void OnEnable() => RebuildLookup();
        private void OnValidate() => RebuildLookup();

        public CraftingRecipe GetById(string recipeId)
        {
            TryGetById(recipeId, out var recipe);
            return recipe;
        }

        public bool TryGetById(string recipeId, out CraftingRecipe recipe)
        {
            EnsureLookup();

            if (string.IsNullOrWhiteSpace(recipeId))
            {
                recipe = null;
                return false;
            }

            return recipesById.TryGetValue(recipeId, out recipe);
        }

        public void SetRecipes(List<CraftingRecipe> list)
        {
            recipes = list ?? new List<CraftingRecipe>();
            RebuildLookup();
        }

        private void EnsureLookup()
        {
            if (cachedSignature != CalculateSignature())
                RebuildLookup();
        }

        private void RebuildLookup()
        {
            recipesById.Clear();

            if (recipes == null)
            {
                cachedSignature = 0;
                return;
            }

            for (int i = 0; i < recipes.Count; i++)
            {
                var recipe = recipes[i];
                if (recipe == null || string.IsNullOrWhiteSpace(recipe.Id))
                    continue;

                if (recipesById.ContainsKey(recipe.Id))
                {
                    Debug.LogWarning($"[CraftingRecipeDatabase] Duplicate recipe id '{recipe.Id}' found in database '{name}'. Last entry will be used.", this);
                }

                recipesById[recipe.Id] = recipe;
            }

            cachedSignature = CalculateSignature();
        }

        private int CalculateSignature()
        {
            if (recipes == null)
                return 0;

            unchecked
            {
                int hash = recipes.Count;
                for (int i = 0; i < recipes.Count; i++)
                {
                    var recipe = recipes[i];
                    hash = (hash * 397) ^ (recipe != null ? recipe.GetEntityId().GetHashCode() : 0);
                    hash = (hash * 397) ^ (recipe != null && !string.IsNullOrWhiteSpace(recipe.Id) ? recipe.Id.GetHashCode() : 0);
                }

                return hash;
            }
        }
    }
}
