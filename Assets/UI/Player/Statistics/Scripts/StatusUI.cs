using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Player.Statistics;
using Player.Progress;
using Player.Events;
using UI.Player.Common;

namespace UI.Player.Statistics {
public class PlayerStatusUI : MonoBehaviour, IPlayerReferenceReceiver
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
    [SerializeField] private PlayerEventHub playerEvents;
    private bool subscribed;

    private void OnEnable()
    {
        CacheEventHub();
        Subscribe();

        RefreshExperience();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Update()
    {
        if (stats == null)
            return;

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

    private void CacheEventHub()
    {
        if (playerEvents == null && progress != null)
            playerEvents = progress.EventHub;
    }

    public void BindPlayerReferences(PlayerUIReferences refs)
    {
        Unsubscribe();

        stats = refs.Stats;
        progress = refs.Progress;
        playerEvents = refs.EventHub;

        if (stats == null)
            stats = FindFirstObjectByType<StatsController>();

        if (progress == null)
            progress = FindFirstObjectByType<ProgressController>();

        CacheEventHub();

        if (isActiveAndEnabled)
        {
            Subscribe();
            RefreshExperience();
        }
    }

    private void Subscribe()
    {
        if (subscribed || playerEvents == null)
            return;

        playerEvents.AvailableExperienceChanged += HandleExperienceChanged;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed || playerEvents == null)
            return;

        playerEvents.AvailableExperienceChanged -= HandleExperienceChanged;
        subscribed = false;
    }

}
}
