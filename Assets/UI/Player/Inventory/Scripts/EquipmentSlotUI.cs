using UnityEngine;
using UnityEngine.UI;
using Items;

namespace UI.Player.Inventory
{
    /// <summary>
    /// Visual representation of a single equipment slot.
    /// </summary>
    public class EquipmentSlotUI : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private CanvasGroup iconCanvasGroup;
        [SerializeField] private EquipmentSlot slot;

        public EquipmentSlot Slot => slot;

        public void Configure(EquipmentSlot slotId)
        {
            slot = slotId;
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

    }
}
