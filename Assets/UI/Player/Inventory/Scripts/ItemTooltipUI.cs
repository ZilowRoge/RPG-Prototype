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
        [SerializeField] private string emptyDescriptionFallback = "No description.";
        [SerializeField] private string unknownNameFallback = "Unknown item";

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

            ItemTooltipData tooltip = item.GetTooltip(unknownNameFallback, emptyDescriptionFallback);

            if (nameLabel != null)
            {
                nameLabel.text = tooltip.Name;
            }

            if (descriptionLabel != null)
            {
                descriptionLabel.text = tooltip.Description;
            }

            if (statsLabel != null)
            {
                bool hasText = !string.IsNullOrWhiteSpace(tooltip.StatsText);
                statsLabel.gameObject.SetActive(hasText);
                if (hasText)
                {
                    statsLabel.text = tooltip.StatsText;
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
