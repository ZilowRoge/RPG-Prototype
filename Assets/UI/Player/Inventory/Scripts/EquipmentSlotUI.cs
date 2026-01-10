using System;
using Items;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.Player.Inventory
{
    /// <summary>
    /// Visual representation of a single equipment slot.
    /// </summary>
    public class EquipmentSlotUI : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private CanvasGroup iconCanvasGroup;
        [SerializeField] private EquipmentSlot slot;
        [SerializeField] private TMP_Text stackLabel;
        private Action<EquipmentSlotUI> onClicked;
        private Action<EquipmentSlotUI> onDoubleClicked;
        private Action<EquipmentSlotUI, PointerEventData> onBeginDrag;
        private Action<PointerEventData> onDrag;
        private Action onEndDrag;
        private Action<EquipmentSlotUI, PointerEventData> onDrop;
        private Action<EquipmentSlotUI, PointerEventData> onPointerEnter;
        private Action<EquipmentSlotUI, PointerEventData> onPointerExit;

        public EquipmentSlot Slot => slot;

        public void Configure(
            EquipmentSlot slotId,
            Action<EquipmentSlotUI> clickHandler,
            Action<EquipmentSlotUI> doubleClickHandler,
            Action<EquipmentSlotUI, PointerEventData> beginDragHandler,
            Action<EquipmentSlotUI, PointerEventData> dropHandler,
            Action endDragHandler,
            Action<PointerEventData> dragHandler,
            Action<EquipmentSlotUI, PointerEventData> pointerEnterHandler,
            Action<EquipmentSlotUI, PointerEventData> pointerExitHandler)
        {
            slot = slotId;
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
            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
            }

            if (iconCanvasGroup != null)
            {
                iconCanvasGroup.alpha = icon != null ? 1f : 0f;
            }
        }

        public void SetStackCount(int count)
        {
            if (stackLabel == null)
                return;

            bool showCount = count > 1;
            stackLabel.gameObject.SetActive(showCount);
            if (showCount)
            {
                stackLabel.text = count.ToString();
            }
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

        public void OnPointerEnter(PointerEventData eventData)
        {
            onPointerEnter?.Invoke(this, eventData);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            onPointerExit?.Invoke(this, eventData);
        }
    }
}
