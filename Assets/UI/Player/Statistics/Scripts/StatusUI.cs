using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Player.Statistics;

namespace UI.Player.Statistics {
public class PlayerStatusUI : MonoBehaviour
{
    [SerializeField] private StatsController stats;

    [Header("Bars")]
    [SerializeField] private ResourceBarUI healthBar;
    [SerializeField] private ResourceBarUI manaBar;
    [SerializeField] private ResourceBarUI staminaBar;
    [SerializeField] private Image expBar;

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI expValueText;

    private void Update()
    {
        healthBar.UpdateBar(stats.CurrentHealth, stats.MaxHealth);
        manaBar.UpdateBar(stats.CurrentMana, stats.MaxMana);
        staminaBar.UpdateBar(stats.CurrentStamina, stats.MaxStamina);

        int currentExp = stats.CurrentExperience;
        int expForCurrent = stats.GetExperienceForLevel(stats.CurrentLevel);
        int expForNext = stats.GetExperienceForLevel(stats.CurrentLevel + 1);
        float progress = (float)(currentExp - expForCurrent) / (expForNext - expForCurrent);
        
        levelText.text = $"Lvl: {stats.CurrentLevel}";
        expValueText.text = $"{currentExp}/{expForNext}";
        expBar.fillAmount =  Mathf.Clamp01(progress);
    }
}
}