using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Player.Crafting
{
    [DisallowMultipleComponent]
    public class CraftingItemIconUI : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text amountLabel;

        public void Bind(Sprite icon, int amount, Color tint)
        {
            SetIcon(icon);
            SetAmount(amount);
            SetTint(tint);
        }

        public void SetIcon(Sprite icon)
        {
            if (iconImage == null)
                return;

            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
            iconImage.preserveAspect = true;
        }

        public void SetAmount(int amount)
        {
            if (amountLabel == null)
                return;

            bool showAmount = amount > 0;
            amountLabel.gameObject.SetActive(showAmount);
            if (showAmount)
                amountLabel.text = amount.ToString();
        }

        public void SetTint(Color tint)
        {
            if (iconImage != null)
                iconImage.color = tint;

            if (amountLabel != null)
                amountLabel.color = tint;
        }
    }
}
