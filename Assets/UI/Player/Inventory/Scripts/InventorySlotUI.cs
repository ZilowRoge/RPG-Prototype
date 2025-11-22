using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.Player.Inventory
{
    public class InventorySlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private CanvasGroup iconCanvasGroup;
        [SerializeField] private int slotId;
        [SerializeField] private TMP_Text stackLabel;
        private Action<InventorySlotUI> onClicked;
        private Action<InventorySlotUI> onDoubleClicked;
        private Action<InventorySlotUI, PointerEventData> onBeginDrag;
        private Action<InventorySlotUI, PointerEventData> onDrop;
        private Action onEndDrag;
        private Action<PointerEventData> onDrag;

        public int SlotId => slotId;

        public void Configure(
            int id,
            Action<InventorySlotUI> clickHandler,
            Action<InventorySlotUI> doubleClickHandler,
            Action<InventorySlotUI, PointerEventData> beginDragHandler,
            Action<InventorySlotUI, PointerEventData> dropHandler,
            Action endDragHandler,
            Action<PointerEventData> dragHandler)
        {
            slotId = id;
            onClicked = clickHandler;
            onDoubleClicked = doubleClickHandler;
            onBeginDrag = beginDragHandler;
            onDrop = dropHandler;
            onEndDrag = endDragHandler;
            onDrag = dragHandler;
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
            onClicked?.Invoke(this);
            if (eventData != null && eventData.clickCount >= 2)
            {
                onDoubleClicked?.Invoke(this);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            onBeginDrag?.Invoke(this, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            onDrag?.Invoke(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            onEndDrag?.Invoke();
        }

        public void OnDrop(PointerEventData eventData)
        {
            onDrop?.Invoke(this, eventData);
        }
    }
}
