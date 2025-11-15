using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.Player.Inventory
{
    public class InventorySlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private CanvasGroup iconCanvasGroup;
        [SerializeField] private int slotId;
        [SerializeField] private TMP_Text stackLabel;

        public int SlotId => slotId;

        public void Configure(int id)
        {
            slotId = id;
        }

        public void SetIcon(Sprite icon)
        {
            if (iconImage == null)
                return;

            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
            if (iconCanvasGroup != null)
                iconCanvasGroup.alpha = icon != null ? 1f : 0f;
        }

        public void SetStackCount(int count)
        {
            if (stackLabel == null)
                return;

            bool showCount = count > 1;
            stackLabel.gameObject.SetActive(showCount);
            if (showCount)
                stackLabel.text = count.ToString();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            // Placeholder for hover functionality (tooltip, highlight, etc.).
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // Placeholder for hover exit handling.
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // Placeholder for future click handling (select, use, drag).
        }
    }
}
