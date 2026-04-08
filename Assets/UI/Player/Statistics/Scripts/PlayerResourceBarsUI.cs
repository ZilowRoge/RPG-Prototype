using TMPro;
using UnityEngine;
using Player.Statistics;
using UnityEngine.UI;
using UI.Player.Common;

namespace UI.Player.Statistics
{
    /// <summary>
    /// Handles the player HUD resource bars (health / mana / stamina).
    /// </summary>
    public class PlayerResourceBarsUI : MonoBehaviour, IPlayerReferenceReceiver
    {
        [SerializeField] private StatsController stats;

        [Header("Health")]
        [SerializeField] private Image healthFill;
        [SerializeField] private TextMeshProUGUI healthLabel;

        [Header("Mana")]
        [SerializeField] private Image manaFill;
        [SerializeField] private TextMeshProUGUI manaLabel;

        [Header("Stamina")]
        [SerializeField] private Image staminaFill;
        [SerializeField] private TextMeshProUGUI staminaLabel;

        private void Awake()
        {
            if (stats == null)
                stats = FindAnyObjectByType<StatsController>();

            RefreshAll();
        }

        private void Update()
        {
            RefreshAll();
        }

        private void RefreshAll()
        {
            if (stats == null)
                return;

            UpdateBar(healthFill, healthLabel, stats.CurrentHealth, stats.maxHealth);
            UpdateBar(manaFill, manaLabel, stats.CurrentMana, stats.maxMana);
            UpdateBar(staminaFill, staminaLabel, stats.CurrentStamina, stats.maxStamina);
        }

        private static void UpdateBar(Image fillImage, TextMeshProUGUI label, float current, float max)
        {
            float clamped = Mathf.Clamp(current, 0f, max);

            if (fillImage != null)
                fillImage.fillAmount = max > 0f ? clamped / max : 0f;

            if (label != null)
                label.text = $"{Mathf.FloorToInt(clamped)} / {Mathf.FloorToInt(max)}";
        }

        public void BindPlayerReferences(PlayerUIReferences refs)
        {
            stats = refs.Stats;
            if (stats == null)
                stats = FindAnyObjectByType<StatsController>();

            if (isActiveAndEnabled)
                RefreshAll();
        }
    }
}

