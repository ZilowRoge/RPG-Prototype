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
        [SerializeField] private InventoryPanelUI ownerPanel;
        private Action<InventorySlotUI> onClicked;
        private Action<InventorySlotUI> onDoubleClicked;
        private Action<InventorySlotUI, PointerEventData> onBeginDrag;
        private Action<InventorySlotUI, PointerEventData> onDrop;
        private Action onEndDrag;
        private Action<PointerEventData> onDrag;
        private Action<InventorySlotUI, PointerEventData> onPointerEnter;
        private Action<InventorySlotUI, PointerEventData> onPointerExit;

        public int SlotId => slotId;
        public InventoryPanelUI OwnerPanel => ownerPanel;

        public void SetOwner(InventoryPanelUI owner)
        {
            ownerPanel = owner;
        }

        public void Configure(
            int id,
            Action<InventorySlotUI> clickHandler,
            Action<InventorySlotUI> doubleClickHandler,
            Action<InventorySlotUI, PointerEventData> beginDragHandler,
            Action<InventorySlotUI, PointerEventData> dropHandler,
            Action endDragHandler,
            Action<PointerEventData> dragHandler,
            Action<InventorySlotUI, PointerEventData> pointerEnterHandler,
            Action<InventorySlotUI, PointerEventData> pointerExitHandler)
        {
            slotId = id;
            ownerPanel = ownerPanel == null ? GetComponentInParent<InventoryPanelUI>() : ownerPanel;
            onClicked = clickHandler;
            onDoubleClicked = doubleClickHandler;
            onBeginDrag = beginDragHandler;
            onDrop = dropHandler;
            onEndDrag = endDragHandler;
            onDrag = dragHandler;
            onPointerEnter = pointerEnterHandler;
            onPointerExit = pointerExitHandler;
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
            onPointerEnter?.Invoke(this, eventData);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            onPointerExit?.Invoke(this, eventData);
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
