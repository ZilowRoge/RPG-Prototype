using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Player.Statistics {
public class ResourceBarUI : MonoBehaviour
{
    [SerializeField] private Image barFillImage; // Typ Image (Fill)
    [SerializeField] private TextMeshProUGUI valueText;

    public void UpdateBar(float current, float max)
    {
        float fill = Mathf.Clamp01(current / max); // 0.0 - 1.0
        barFillImage.fillAmount = fill;

        valueText.text = $"{Mathf.FloorToInt(current)} / {Mathf.FloorToInt(max)}";
    }
}
}