using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CraftingData = global::Crafting;

namespace UI.Player.Crafting
{
    public class CraftingRecipeListItemUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private Image stateImage;
        [SerializeField] private Button button;

        private CraftingData.CraftingRecipe recipe;
        private Action<CraftingData.CraftingRecipe> clickCallback;
        private bool canCraft = true;

        public CraftingData.CraftingRecipe Recipe => recipe;
        public bool CanCraft => canCraft;

        private void Awake()
        {
            EnsureComponents();
        }

        private void OnEnable()
        {
            RegisterListener();
        }

        private void OnDisable()
        {
            UnregisterListener();
        }

        public void Bind(
            CraftingData.CraftingRecipe recipeAsset,
            string title,
            Action<CraftingData.CraftingRecipe> onClicked,
            bool isCraftable)
        {
            EnsureComponents();

            recipe = recipeAsset;
            clickCallback = onClicked;
            canCraft = isCraftable;

            if (titleLabel != null)
                titleLabel.text = title ?? string.Empty;

            RegisterListener();
        }

        public void UpdateVisualState(
            bool selected,
            Color selectedColor,
            Color normalColor,
            Color selectedAvailableImageColor,
            Color availableImageColor,
            Color selectedUnavailableImageColor,
            Color unavailableImageColor)
        {
            if (titleLabel != null)
            {
                titleLabel.fontStyle = selected ? FontStyles.Bold : FontStyles.Normal;
                titleLabel.color = selected ? selectedColor : normalColor;
            }

            if (stateImage != null)
            {
                if (selected)
                    stateImage.color = canCraft ? selectedAvailableImageColor : selectedUnavailableImageColor;
                else
                    stateImage.color = canCraft ? availableImageColor : unavailableImageColor;
            }
        }

        private void OnClicked()
        {
            if (recipe != null)
                clickCallback?.Invoke(recipe);
        }

        private void EnsureComponents()
        {
            if (titleLabel == null)
                titleLabel = GetComponentInChildren<TMP_Text>(true);

            if (stateImage == null)
                stateImage = ResolveStateImage();

            if (button == null)
                button = GetComponent<Button>() ?? GetComponentInChildren<Button>(true);
        }

        private Image ResolveStateImage()
        {
            var background = FindChildRecursive(transform, "Background");
            if (background != null)
            {
                var backgroundImage = background.GetComponent<Image>() ?? background.GetComponentInChildren<Image>(true);
                if (backgroundImage != null)
                    return backgroundImage;
            }

            return GetComponent<Image>() ?? GetComponentInChildren<Image>(true);
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

        private void RegisterListener()
        {
            if (button == null)
                return;

            button.onClick.RemoveListener(OnClicked);
            button.onClick.AddListener(OnClicked);
        }

        private void UnregisterListener()
        {
            if (button == null)
                return;

            button.onClick.RemoveListener(OnClicked);
        }

        private void Reset()
        {
            EnsureComponents();
        }
    }
}
