using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Systems.Jobs;

namespace UI.Player.Perks
{
    public class PerkNodeUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
    {
        [Header("UI References")]
        [SerializeField] private Button unlockButton;
        [SerializeField] private TextMeshProUGUI perkNameText;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private string initialNodeLabel = "Start";

        [Header("Colors")]
        [SerializeField] private Color lockedColor = new(0.25f, 0.25f, 0.25f, 0.9f);
        [SerializeField] private Color availableColor = new(0.85f, 0.76f, 0.28f, 1f);
        [SerializeField] private Color unlockedColor = new(0.22f, 0.82f, 0.32f, 1f);
        [SerializeField] private Color queuedColor = new(1f, 0.82f, 0.4f, 1f);
        [SerializeField] private Color initialColor = new(0.35f, 0.38f, 0.45f, 0.9f);

        private JobPerkNode nodeData;
        private PerkControllerUI controller;
        private bool isQueued;
        private bool isInitial;
        private bool isPointerOver;

        public JobPerkNode NodeData => nodeData;
        public RectTransform RectTransform => (RectTransform)transform;

        public void Initialize(JobPerkNode node, PerkControllerUI owningController)
        {
            nodeData = node;
            controller = owningController;
            isQueued = false;
            isInitial = node?.IsInitial ?? false;
            isPointerOver = false;

            if (perkNameText != null)
            {
                string label = node?.Perk?.perkName;
                if (string.IsNullOrWhiteSpace(label))
                {
                    label = string.IsNullOrWhiteSpace(node?.name)
                        ? initialNodeLabel
                        : node.name;
                }

                perkNameText.text = label;
            }

            if (unlockButton != null)
            {
                unlockButton.onClick.RemoveAllListeners();
                unlockButton.onClick.AddListener(OnSelectPressed);
            }

            if (RectTransform != null)
            {
                RectTransform.anchorMin = RectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            }
            ApplyState(new NodeVisualState(isInitial, false, false, false, false));
        }

        public void ApplyState(NodeVisualState state)
        {
            isInitial = state.IsInitial;
            isQueued = state.IsQueued;

            if (unlockButton != null)
                unlockButton.interactable = state.IsInteractable;

            if (backgroundImage == null)
                return;

            var color = state.IsInitial
                ? initialColor
                : state.IsUnlocked
                    ? unlockedColor
                    : state.IsQueued
                        ? queuedColor
                        : state.IsAvailable
                            ? availableColor
                            : lockedColor;

            backgroundImage.color = color;
        }

        private void OnSelectPressed()
        {
            if (controller == null || isInitial) return;
            controller.ToggleNode(this);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (controller == null) return;
            isPointerOver = true;
            controller.ShowTooltip(nodeData, eventData.position);
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            if (!isPointerOver || controller == null) return;
            controller.ShowTooltip(nodeData, eventData.position);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (controller == null) return;
            isPointerOver = false;
            controller.HideTooltip(this);
        }

        private void OnDisable()
        {
            if (!isPointerOver || controller == null) return;
            isPointerOver = false;
            controller.HideTooltip(this);
        }
    }
}
