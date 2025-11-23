using System.Collections.Generic;
using Items;
using TMPro;
using UnityEngine;

namespace UI.Player.Inventory
{
    /// <summary>
    /// Presents item data in a simple tooltip panel.
    /// </summary>
    public class ItemTooltipUI : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private RectTransform container;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text descriptionLabel;
        [SerializeField] private TMP_Text statsLabel;

        [Header("Layout")]
        [SerializeField] private Vector2 pointerOffset = new(18f, -18f);

        [Header("Content")]
        [SerializeField] private string emptyDescriptionFallback = "Brak opisu.";
        [SerializeField] private string unknownNameFallback = "Nieznany przedmiot";

        private Canvas rootCanvas;

        private void Awake()
        {
            if (container == null)
            {
                container = GetComponent<RectTransform>();
            }

            rootCanvas = GetComponentInParent<Canvas>();
            Hide();
        }

        public void Show(ItemInstance item, Vector2 screenPosition)
        {
            if (item == null || item.IsEmpty)
            {
                Hide();
                return;
            }

            var definition = item.Definition;
            if (nameLabel != null)
            {
                var label = definition != null && !string.IsNullOrWhiteSpace(definition.Name)
                    ? definition.Name
                    : unknownNameFallback;
                nameLabel.text = label;
            }

            if (descriptionLabel != null)
            {
                var description = definition != null ? definition.Description : string.Empty;
                descriptionLabel.text = string.IsNullOrWhiteSpace(description) ? emptyDescriptionFallback : description;
            }

            if (statsLabel != null)
            {
                var text = BuildStatsText(definition);
                bool hasText = !string.IsNullOrWhiteSpace(text);
                statsLabel.gameObject.SetActive(hasText);
                if (hasText)
                {
                    statsLabel.text = text;
                }
            }

            UpdatePosition(screenPosition);

            if (container != null && !container.gameObject.activeSelf)
            {
                container.gameObject.SetActive(true);
            }
        }

        public void Hide()
        {
            if (container != null && container.gameObject.activeSelf)
            {
                container.gameObject.SetActive(false);
            }
        }

        private string BuildStatsText(ItemDefinition definition)
        {
            if (definition == null)
                return string.Empty;

            var lines = new List<string>();

            foreach (var block in definition.GetAllStatBlocks())
            {
                if (block == null)
                    continue;

                var text = block.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    lines.Add(text);
                }
            }

            return lines.Count == 0 ? string.Empty : string.Join("\n", lines);
        }

        private void UpdatePosition(Vector2 screenPosition)
        {
            if (container == null)
                return;

            var targetScreenPoint = screenPosition + pointerOffset;

            var parentRect = container.parent as RectTransform;
            var camera = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? rootCanvas.worldCamera
                : null;

            if (parentRect == null)
            {
                RectTransformUtility.ScreenPointToWorldPointInRectangle(container, targetScreenPoint, camera, out var worldPoint);
                container.position = worldPoint;
                return;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, targetScreenPoint, camera, out var localPoint);
            container.anchoredPosition = localPoint;
        }
    }
}
