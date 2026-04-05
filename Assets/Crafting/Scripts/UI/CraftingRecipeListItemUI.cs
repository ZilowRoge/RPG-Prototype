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
            Color selectedUnavailableColor,
            Color unavailableColor)
        {
            if (titleLabel == null)
                return;

            titleLabel.fontStyle = selected ? FontStyles.Bold : FontStyles.Normal;
            if (selected)
                titleLabel.color = canCraft ? selectedColor : selectedUnavailableColor;
            else
                titleLabel.color = canCraft ? normalColor : unavailableColor;
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

            if (button == null)
                button = GetComponent<Button>() ?? GetComponentInChildren<Button>(true);
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
