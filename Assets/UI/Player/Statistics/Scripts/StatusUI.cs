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

    private void Update()
    {
        healthBar.UpdateBar(stats.CurrentHealth, stats.maxHealth);
        manaBar.UpdateBar(stats.CurrentMana, stats.maxMana);
        staminaBar.UpdateBar(stats.CurrentStamina, stats.maxStamina);
    }
}
}