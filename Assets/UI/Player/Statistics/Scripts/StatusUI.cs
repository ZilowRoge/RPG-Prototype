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

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI levelText;

    private void Update()
    {
        healthBar.UpdateBar(stats.CurrentHealth, stats.MaxHealth);
        manaBar.UpdateBar(stats.CurrentMana, stats.MaxMana);
        staminaBar.UpdateBar(stats.CurrentStamina, stats.MaxStamina);

        levelText.text = $"Lvl: {stats.CurrentLevel}";
    }
}
}