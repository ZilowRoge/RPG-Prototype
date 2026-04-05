using System.Collections.Generic;
using Common.World.Interaction;
using Inventory;
using UI.Player.Crafting;
using UnityEngine;

namespace Crafting
{
    [AddComponentMenu("Game/World/Crafting/CraftingStation")]
    [RequireComponent(typeof(Collider))]
    public class CraftingStation : MonoBehaviour, IInteractable
    {
        [Header("Station")]
        [SerializeField] private string stationId = "crafting_station_001";
        [SerializeField] private CraftingStationType stationType = CraftingStationType.Any;

        [Header("Data")]
        [SerializeField] private CraftingRecipeDatabase recipeDatabase;
        [SerializeField] private Items.ItemDefinitionDatabase itemDatabase;

        [Header("UI")]
        [SerializeField] private CraftingWindowUI craftingWindowOverride;

        private readonly List<CraftingRecipe> filteredRecipes = new();

        public string StationId => stationId;
        public CraftingStationType StationType => stationType;
        public Items.ItemDefinitionDatabase ItemDatabase => itemDatabase;

        private void Reset()
        {
            EnsureTriggerCollider();
        }

        private void Awake()
        {
            EnsureTriggerCollider();
        }

        public void Interact(GameObject player)
        {
            var playerInventory = ResolvePlayerInventory(player);
            if (playerInventory == null)
            {
                Debug.LogWarning("[CraftingStation] Player InventoryController not found.", this);
                return;
            }

            var window = ResolveWindow();
            if (window == null)
            {
                Debug.LogWarning("[CraftingStation] CraftingWindowUI not found on PlayerUI.", this);
                return;
            }

            window.OpenForStation(this, playerInventory);
        }

        public IReadOnlyList<CraftingRecipe> GetAvailableRecipes()
        {
            RebuildFilteredRecipes();
            return filteredRecipes;
        }

        public CraftingRecipeAvailability EvaluateRecipe(CraftingRecipe recipe, InventoryController playerInventory)
        {
            if (!CanUseRecipe(recipe))
            {
                return new CraftingRecipeAvailability(
                    hasRequiredMaterials: false,
                    hasOutputSpace: false,
                    materialAvailability: CreateFalseFlags(recipe != null ? recipe.Materials.Count : 0),
                    productSpaceAvailability: CreateFalseFlags(recipe != null ? recipe.Products.Count : 0));
            }

            return CraftingService.Evaluate(recipe, playerInventory, itemDatabase);
        }

        public bool TryCraft(CraftingRecipe recipe, InventoryController playerInventory, out CraftingRecipeAvailability availability)
        {
            if (!CanUseRecipe(recipe))
            {
                availability = EvaluateRecipe(recipe, playerInventory);
                return false;
            }

            return CraftingService.TryCraft(recipe, playerInventory, itemDatabase, out availability);
        }

        private bool CanUseRecipe(CraftingRecipe recipe)
        {
            return recipe != null &&
                   recipeDatabase != null &&
                   itemDatabase != null &&
                   CraftingService.SupportsStation(recipe, stationType);
        }

        private void RebuildFilteredRecipes()
        {
            filteredRecipes.Clear();

            if (recipeDatabase?.Recipes == null)
                return;

            for (int i = 0; i < recipeDatabase.Recipes.Count; i++)
            {
                var recipe = recipeDatabase.Recipes[i];
                if (!CanUseRecipe(recipe))
                    continue;

                filteredRecipes.Add(recipe);
            }
        }

        private InventoryController ResolvePlayerInventory(GameObject player)
        {
            if (player == null)
                return null;

            return player.GetComponentInParent<InventoryController>()
                ?? player.GetComponentInChildren<InventoryController>(true);
        }

        private CraftingWindowUI ResolveWindow()
        {
            if (craftingWindowOverride != null)
                return craftingWindowOverride;

            var playerUi = GameObject.FindWithTag("PlayerUI");
            return playerUi != null
                ? playerUi.GetComponentInChildren<CraftingWindowUI>(true)
                : null;
        }

        private static bool[] CreateFalseFlags(int count)
        {
            if (count <= 0)
                return System.Array.Empty<bool>();

            return new bool[count];
        }

        private void EnsureTriggerCollider()
        {
            var stationCollider = GetComponent<Collider>();
            if (stationCollider != null && !stationCollider.isTrigger)
                stationCollider.isTrigger = true;
        }
    }
}
