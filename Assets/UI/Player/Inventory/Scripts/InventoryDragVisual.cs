using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.Player.Inventory
{
    /// <summary>
    /// Handles showing and positioning the icon that follows the cursor while dragging inventory items.
    /// </summary>
    public class InventoryDragVisual : MonoBehaviour
    {
        [SerializeField] private RectTransform visualRoot;
        [SerializeField] private Image iconImage;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Vector2 pointerOffset = new Vector2(10f, -10f);

        private void Awake()
        {
            if (visualRoot == null && iconImage != null)
                visualRoot = iconImage.rectTransform;

            Hide();
        }

        public void Show(Sprite icon)
        {
            if (icon == null)
            {
                Hide();
                return;
            }

            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = true;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = false;
            }

            if (visualRoot != null)
            {
                visualRoot.gameObject.SetActive(true);
                visualRoot.SetAsLastSibling();
            }
        }

        public void Hide()
        {
            if (iconImage != null)
                iconImage.enabled = false;

            if (canvasGroup != null)
                canvasGroup.alpha = 0f;

            if (visualRoot != null)
                visualRoot.gameObject.SetActive(false);
        }

        public void UpdatePosition(PointerEventData eventData)
        {
            if (visualRoot == null || eventData == null)
                return;

            var canvas = visualRoot.GetComponentInParent<Canvas>();
            if (canvas == null)
                return;

            var canvasRect = canvas.transform as RectTransform;
            if (canvasRect == null)
                return;

            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, camera, out var localPoint))
            {
                visualRoot.anchoredPosition = localPoint + pointerOffset;
                if (!visualRoot.gameObject.activeSelf && iconImage != null && iconImage.sprite != null)
                    visualRoot.gameObject.SetActive(true);
            }
        }
    }
}
