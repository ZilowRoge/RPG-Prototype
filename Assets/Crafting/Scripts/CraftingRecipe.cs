using System.Collections.Generic;
using UnityEngine;

namespace Crafting
{
    public enum CraftingStationType
    {
        Any = 0
    }

    [CreateAssetMenu(fileName = "CraftingRecipe", menuName = "Crafting/Recipe")]
    public class CraftingRecipe : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string recipeId = "recipe_000";

        [Header("Recipe")]
        [SerializeField] private CraftingStationType stationType = CraftingStationType.Any;
        [SerializeField] private List<RecipeItemAmount> materials = new();
        [SerializeField] private List<RecipeItemAmount> products = new();

        public string Id => recipeId;
        public CraftingStationType StationType => stationType;
        public IReadOnlyList<RecipeItemAmount> Materials => materials;
        public IReadOnlyList<RecipeItemAmount> Products => products;

        private void OnValidate()
        {
            if (materials == null)
                materials = new List<RecipeItemAmount>();

            if (products == null)
                products = new List<RecipeItemAmount>();
        }
    }
}
