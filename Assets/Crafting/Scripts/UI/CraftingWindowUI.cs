using System;
using System.Collections.Generic;
using Items;
using Player;
using UI.Player.Common;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;
using CraftingData = global::Crafting;
using InventorySystem = global::Inventory;

namespace UI.Player.Crafting
{
    public class CraftingWindowUI : MonoBehaviour, IPlayerReferenceReceiver
    {
        [Header("Root")]
        [FormerlySerializedAs("windowRootObject")]
        [SerializeField] private GameObject root;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private bool closeOnEscape = true;
        [SerializeField] private bool manageCursor = true;

        [Header("Debug")]
        [SerializeField] private bool enableDebugToggleKey = true;
        [SerializeField] private Key debugToggleKey = Key.P;

        [Header("Data")]
        [SerializeField] private CraftingData.CraftingRecipeDatabase recipeDatabase;
        [SerializeField] private ItemDefinitionDatabase itemDatabase;

        [Header("UI")]
        [SerializeField] private CraftingRecipeListUI recipeListUI;
        [SerializeField] private CraftingRecipeIconListUI materialsListUI;
        [SerializeField] private CraftingRecipeIconListUI productsListUI;
        [SerializeField] private Button craftButton;

        private readonly List<CraftingData.CraftingRecipe> stationRecipes = new();
        private readonly List<CraftingData.CraftingRecipe> standaloneRecipes = new();
        private readonly Dictionary<CraftingData.CraftingRecipe, CraftingData.CraftingRecipeAvailability> availabilityByRecipe = new();

        private CraftingData.CraftingRecipe selectedRecipe;
        private CraftingData.CraftingRecipeAvailability selectedAvailability;
        private CraftingData.CraftingStation activeStation;
        private InventorySystem.InventoryController inventoryController;
        private bool dependenciesInitialized;
        private bool isOpen;
        private bool cursorCaptured;
        private CursorLockMode previousCursorLock;
        private bool previousCursorVisible;

        public event Action<CraftingData.CraftingRecipe> CraftRequested;

        public bool IsOpen => isOpen;
        public CraftingData.CraftingRecipe SelectedRecipe => selectedRecipe;
        public InventorySystem.InventoryController InventoryController => inventoryController;
        public CraftingData.CraftingStation ActiveStation => activeStation;
        public CraftingData.CraftingRecipeAvailability SelectedAvailability => selectedAvailability;

        private void Awake()
        {
            EnsureRoot();
            EnsureDependencies();
            HideImmediate();
            UpdateCraftButtonState();
        }

        private void OnEnable()
        {
            EnsureRoot();
            EnsureDependencies();
            Subscribe();

            if (isOpen)
                Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();

            if (isOpen)
            {
                PlayerInputLockService.TryGetInstance()?.SetLock(this, false);
                isOpen = false;
            }

            if (manageCursor)
                ReleaseCursor();

            ClearStationContext();
        }

        private void Update()
        {
            if (Keyboard.current == null)
                return;

            if (enableDebugToggleKey && debugToggleKey != Key.None && Keyboard.current[debugToggleKey].wasPressedThisFrame)
            {
                if (isOpen)
                    Close();
                else
                    Open();

                return;
            }

            if (!isOpen || !closeOnEscape)
                return;

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
                Close();
        }

        public void Open()
        {
            Open((string)null);
        }

        public void Open(string recipeId)
        {
            EnsureRoot();
            EnsureDependencies();

            if (!isOpen)
            {
                isOpen = true;
                SetRootVisible(true);
                PlayerInputLockService.Instance?.SetLock(this, true);
                if (manageCursor)
                    CaptureCursor();
            }
            else
            {
                SetRootVisible(true);
            }

            Refresh();

            if (!string.IsNullOrWhiteSpace(recipeId))
                SelectRecipeById(recipeId);
        }

        public void Open(CraftingData.CraftingRecipe recipe)
        {
            Open();

            if (recipe != null)
                SelectRecipe(recipe);
        }

        public void OpenForStation(CraftingData.CraftingStation station, InventorySystem.InventoryController playerInventory, string recipeId = null)
        {
            activeStation = station;
            inventoryController = playerInventory != null ? playerInventory : inventoryController;
            SetStationRecipes(station != null ? station.GetAvailableRecipes() : null);
            Open(recipeId);
        }

        public void Close()
        {
            if (!isOpen)
            {
                ClearStationContext();
                return;
            }

            PlayerInputLockService.Instance?.SetLock(this, false);
            if (manageCursor)
                ReleaseCursor();

            isOpen = false;
            SetRootVisible(false);
            ClearStationContext();
        }

        public void Toggle()
        {
            if (isOpen)
                Close();
            else
                Open();
        }

        public void BindPlayerReferences(PlayerUIReferences refs)
        {
            inventoryController = refs.Inventory;

            if (isOpen)
                Refresh();
        }

        public void Refresh()
        {
            EnsureDependencies();

            var recipes = GetVisibleRecipes();
            RebuildAvailability(recipes);

            if (recipeListUI != null)
                selectedRecipe = recipeListUI.Refresh(recipes, selectedRecipe, availabilityByRecipe);
            else
                selectedRecipe = DetermineSelection(recipes, selectedRecipe);

            RefreshRecipeDetails();
        }

        public void SelectRecipe(CraftingData.CraftingRecipe recipe)
        {
            selectedRecipe = recipe;
            recipeListUI?.ApplySelection(selectedRecipe);
            RefreshRecipeDetails();
        }

        public void SelectRecipeById(string recipeId)
        {
            if (string.IsNullOrWhiteSpace(recipeId))
            {
                SelectRecipe(null);
                return;
            }

            var recipes = GetVisibleRecipes();
            for (int i = 0; i < recipes.Count; i++)
            {
                var recipe = recipes[i];
                if (recipe != null && string.Equals(recipe.Id, recipeId, StringComparison.Ordinal))
                {
                    SelectRecipe(recipe);
                    return;
                }
            }

            SelectRecipe(null);
        }

        private void HandleRecipeSelected(CraftingData.CraftingRecipe recipe)
        {
            SelectRecipe(recipe);
        }

        private void HandleCraftButtonClicked()
        {
            if (selectedRecipe == null)
                return;

            bool crafted;
            if (activeStation != null)
            {
                crafted = activeStation.TryCraft(selectedRecipe, inventoryController, out selectedAvailability);
            }
            else
            {
                crafted = CraftingData.CraftingService.TryCraft(
                    selectedRecipe,
                    inventoryController,
                    GetActiveItemDatabase(),
                    out selectedAvailability);
            }

            Refresh();

            if (crafted)
                CraftRequested?.Invoke(selectedRecipe);
        }

        private void RefreshRecipeDetails()
        {
            if (selectedRecipe != null && availabilityByRecipe.TryGetValue(selectedRecipe, out var availability))
                selectedAvailability = availability;
            else
                selectedAvailability = new CraftingData.CraftingRecipeAvailability(false, false, Array.Empty<bool>(), Array.Empty<bool>());

            var activeItemDatabase = GetActiveItemDatabase();

            if (selectedRecipe != null)
            {
                materialsListUI?.Bind(selectedRecipe.Materials, activeItemDatabase, selectedAvailability.MaterialAvailability);
                productsListUI?.Bind(selectedRecipe.Products, activeItemDatabase, selectedAvailability.ProductSpaceAvailability);
            }
            else
            {
                materialsListUI?.Clear();
                productsListUI?.Clear();
            }

            UpdateCraftButtonState();
        }

        private void UpdateCraftButtonState()
        {
            if (craftButton != null)
                craftButton.interactable = selectedRecipe != null && selectedAvailability.CanCraft;
        }

        private void EnsureRoot()
        {
            if (root == null || root == gameObject)
                root = FindDefaultRootChild();

            var canvasTarget = root != null ? root : gameObject;

            if (canvasGroup == null && canvasTarget != null)
                canvasGroup = canvasTarget.GetComponent<CanvasGroup>();

            if (canvasGroup == null && canvasTarget != null)
                canvasGroup = canvasTarget.AddComponent<CanvasGroup>();
        }

        private void EnsureDependencies()
        {
            if (dependenciesInitialized)
                return;

            if (recipeListUI == null)
                recipeListUI = GetComponentInChildren<CraftingRecipeListUI>(true);

            if (materialsListUI == null)
                materialsListUI = FindNamedChildComponent<CraftingRecipeIconListUI>("MaterialContainer");
            if (materialsListUI == null)
                materialsListUI = FindNamedChildComponent<CraftingRecipeIconListUI>("MaterialsContainer");

            if (productsListUI == null)
                productsListUI = FindNamedChildComponent<CraftingRecipeIconListUI>("ProductContainer");
            if (productsListUI == null)
                productsListUI = FindNamedChildComponent<CraftingRecipeIconListUI>("ProductsContainer");

            if (craftButton == null)
                craftButton = FindNamedChildComponent<Button>("Button");

            dependenciesInitialized = true;
        }

        private void Subscribe()
        {
            if (recipeListUI != null)
            {
                recipeListUI.RecipeSelected -= HandleRecipeSelected;
                recipeListUI.RecipeSelected += HandleRecipeSelected;
            }

            if (craftButton != null)
            {
                craftButton.onClick.RemoveListener(HandleCraftButtonClicked);
                craftButton.onClick.AddListener(HandleCraftButtonClicked);
            }
        }

        private void Unsubscribe()
        {
            if (recipeListUI != null)
                recipeListUI.RecipeSelected -= HandleRecipeSelected;

            if (craftButton != null)
                craftButton.onClick.RemoveListener(HandleCraftButtonClicked);
        }

        private IReadOnlyList<CraftingData.CraftingRecipe> GetVisibleRecipes()
        {
            if (activeStation != null)
                return stationRecipes;

            standaloneRecipes.Clear();
            if (recipeDatabase?.Recipes == null)
                return standaloneRecipes;

            for (int i = 0; i < recipeDatabase.Recipes.Count; i++)
            {
                var recipe = recipeDatabase.Recipes[i];
                if (!CraftingData.CraftingService.SupportsStation(recipe, CraftingData.CraftingStationType.Any))
                    continue;

                standaloneRecipes.Add(recipe);
            }

            return standaloneRecipes;
        }

        private void RebuildAvailability(IReadOnlyList<CraftingData.CraftingRecipe> recipes)
        {
            availabilityByRecipe.Clear();

            if (recipes == null)
                return;

            var activeItemDatabase = GetActiveItemDatabase();
            for (int i = 0; i < recipes.Count; i++)
            {
                var recipe = recipes[i];
                if (recipe == null)
                    continue;

                CraftingData.CraftingRecipeAvailability availability = activeStation != null
                    ? activeStation.EvaluateRecipe(recipe, inventoryController)
                    : CraftingData.CraftingService.Evaluate(recipe, inventoryController, activeItemDatabase);

                availabilityByRecipe[recipe] = availability;
            }
        }

        private ItemDefinitionDatabase GetActiveItemDatabase()
        {
            if (activeStation != null && activeStation.ItemDatabase != null)
                return activeStation.ItemDatabase;

            return itemDatabase;
        }

        private void SetStationRecipes(IReadOnlyList<CraftingData.CraftingRecipe> recipes)
        {
            stationRecipes.Clear();
            if (recipes == null)
                return;

            for (int i = 0; i < recipes.Count; i++)
            {
                if (recipes[i] != null)
                    stationRecipes.Add(recipes[i]);
            }
        }

        private void ClearStationContext()
        {
            activeStation = null;
            stationRecipes.Clear();
        }

        private void SetRootVisible(bool visible)
        {
            if (ShouldToggleRootActive())
                root.SetActive(visible);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.interactable = visible;
                canvasGroup.blocksRaycasts = visible;
            }

            if (!visible && manageCursor)
                ReleaseCursor();
        }

        private void HideImmediate()
        {
            if (ShouldToggleRootActive())
                root.SetActive(false);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            if (manageCursor)
                ReleaseCursor();
        }

        private void CaptureCursor()
        {
            if (cursorCaptured)
                return;

            cursorCaptured = true;
            previousCursorLock = Cursor.lockState;
            previousCursorVisible = Cursor.visible;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void ReleaseCursor()
        {
            if (!cursorCaptured)
                return;

            cursorCaptured = false;
            Cursor.lockState = previousCursorLock;
            Cursor.visible = previousCursorVisible;
        }

        private static CraftingData.CraftingRecipe DetermineSelection(
            IReadOnlyList<CraftingData.CraftingRecipe> recipes,
            CraftingData.CraftingRecipe current)
        {
            if (recipes != null)
            {
                for (int i = 0; i < recipes.Count; i++)
                {
                    var recipe = recipes[i];
                    if (recipe == null)
                        continue;

                    if (ReferenceEquals(recipe, current))
                        return current;
                }

                for (int i = 0; i < recipes.Count; i++)
                {
                    if (recipes[i] != null)
                        return recipes[i];
                }
            }

            return null;
        }

        private bool ShouldToggleRootActive()
        {
            return root != null && root != gameObject;
        }

        private GameObject FindDefaultRootChild()
        {
            var namedChild = FindChildRecursive(transform, "CraftingWindow");
            if (namedChild != null && namedChild != transform)
                return namedChild.gameObject;

            return transform.childCount > 0
                ? transform.GetChild(0).gameObject
                : null;
        }

        private T FindNamedChildComponent<T>(string childName) where T : Component
        {
            var child = FindChildRecursive(transform, childName);
            if (child == null)
                return GetComponentInChildren<T>(true);

            return child.GetComponent<T>() ?? child.GetComponentInChildren<T>(true);
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
            root = FindDefaultRootChild();
            canvasGroup = root != null ? root.GetComponent<CanvasGroup>() : GetComponent<CanvasGroup>();
            dependenciesInitialized = false;
            EnsureDependencies();
        }
    }
}
