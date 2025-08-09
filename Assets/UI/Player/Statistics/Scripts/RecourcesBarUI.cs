using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Player.Statistics {
public class ResourceBarUI : MonoBehaviour
{
    [SerializeField] private Image barFillImage;
    [SerializeField] private TextMeshProUGUI valueText;

    public void UpdateBar(float current, float max)
    {
        float fill = Mathf.Clamp01(current / max);
        barFillImage.fillAmount = fill;

        valueText.text = $"{Mathf.FloorToInt(current)} / {Mathf.FloorToInt(max)}";
    }
}
}