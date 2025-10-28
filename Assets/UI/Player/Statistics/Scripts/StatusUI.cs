using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Player.Statistics;
using Player.Progress;

namespace UI.Player.Statistics {
public class PlayerStatusUI : MonoBehaviour
{
    [SerializeField] private StatsController stats;
    [SerializeField] private ProgressController progress;

    [Header("Bars")]
    [SerializeField] private ResourceBarUI healthBar;
    [SerializeField] private ResourceBarUI manaBar;
    [SerializeField] private ResourceBarUI staminaBar;

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI experienceText;

    private void OnEnable()
    {
        if (progress != null)
            progress.AvailableExperienceChanged += HandleExperienceChanged;

        RefreshExperience();
    }

    private void OnDisable()
    {
        if (progress != null)
            progress.AvailableExperienceChanged -= HandleExperienceChanged;
    }

    private void Update()
    {
        healthBar.UpdateBar(stats.CurrentHealth, stats.maxHealth);
        manaBar.UpdateBar(stats.CurrentMana, stats.maxMana);
        staminaBar.UpdateBar(stats.CurrentStamina, stats.maxStamina);
    }

    private void HandleExperienceChanged(int amount) => RefreshExperience(amount);

    private void RefreshExperience()
    {
        if (progress != null)
            RefreshExperience(progress.AvailableExperience);
        else
            RefreshExperience(0);
    }

    private void RefreshExperience(int amount)
    {
        if (experienceText != null)
            experienceText.text = $"{amount} XP";
    }
}
}
