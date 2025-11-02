using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Player.Events;
using Systems.Perks;

namespace UI.Player.Perks
{
    /// <summary>
    /// Manages a layout-based list of active effect icons.
    /// Adds an entry when an interval perk triggers and removes it after its lifetime.
    /// </summary>
    public class EffectIndicatorUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerEventHub playerEvents;
        [SerializeField] private GameObject effectEntryPrefab;
        [SerializeField] private Transform entryParent;

        [Header("Lifetime")]
        [SerializeField] private float defaultDuration = 5f;

        private readonly List<Entry> entries = new();

        private void OnEnable()
        {
            if (playerEvents == null)
                playerEvents = FindFirstObjectByType<PlayerEventHub>();

            if (playerEvents != null)
            {
                playerEvents.PerkIntervalPrimed += OnIntervalEffectPrimed;
                playerEvents.PerkIntervalTriggered += OnIntervalEffectTriggered;
            }
        }

        private void OnDisable()
        {
            if (playerEvents != null)
            {
                playerEvents.PerkIntervalPrimed -= OnIntervalEffectPrimed;
                playerEvents.PerkIntervalTriggered -= OnIntervalEffectTriggered;
                playerEvents = null;
            }

            ClearEntries();
        }

        private void Update()
        {
            if (entries.Count == 0)
                return;

            float now = Time.time;

            for (int i = entries.Count - 1; i >= 0; i--)
            {
                if (!entries[i].WaitForCast && entries[i].Expiry <= now)
                    RemoveEntryAt(i);
            }
        }

        private void OnIntervalEffectPrimed(PerkIntervalTriggerEffect intervalEffect)
        {
            if (effectEntryPrefab == null || intervalEffect == null)
                return;

            var icon = intervalEffect.RewardIcon;
            if (icon == null)
                return;

            AddEntry(intervalEffect, icon, -1f, waitForCast: true);
        }

        private void OnIntervalEffectTriggered(PerkIntervalTriggerEffect intervalEffect, PerkEffectBase reward)
        {
            if (effectEntryPrefab == null || intervalEffect == null)
                return;

            var icon = intervalEffect.RewardIcon;
            if (icon == null)
                return;

            if (intervalEffect.ConsumeOnNextCast)
            {
                RemoveEntryForEffect(intervalEffect);
            }
            else
            {
                float duration = GetDuration(intervalEffect);
                AddEntry(intervalEffect, icon, duration, waitForCast: false);
            }
        }

        private void AddEntry(PerkIntervalTriggerEffect effect, Sprite icon, float duration, bool waitForCast)
        {
            var parent = entryParent != null ? entryParent : transform;

            // Try reuse inactive entries before instantiating new ones
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].IsActive)
                    continue;

                var reused = entries[i];
                ActivateEntry(ref reused, effect, icon, duration, waitForCast);
                entries[i] = reused;
                return;
            }

            var instance = Instantiate(effectEntryPrefab, parent);
            var image = instance.GetComponentInChildren<Image>(true);
            var newEntry = new Entry(instance, image);
            ActivateEntry(ref newEntry, effect, icon, duration, waitForCast);
            entries.Add(newEntry);
        }

        private void ActivateEntry(ref Entry entry, PerkIntervalTriggerEffect effect, Sprite icon, float duration, bool waitForCast)
        {
            if (entry.Instance == null)
                return;

            entry.Instance.SetActive(true);

            if (entry.Image != null)
            {
                entry.Image.sprite = icon;
                entry.Image.enabled = true;
            }

            entry.Source = effect;
            entry.WaitForCast = waitForCast;
            if (waitForCast)
            {
                entry.Expiry = float.PositiveInfinity;
            }
            else
            {
                float lifetime = duration > 0f ? duration : defaultDuration;
                entry.Expiry = Time.time + lifetime;
            }
            entry.IsActive = true;
        }

        private float GetDuration(PerkIntervalTriggerEffect effect)
        {
            if (effect == null || effect.RewardEffect == null)
                return defaultDuration;

            if (effect.RewardEffect is ITimedPerkEffect timedEffect)
                return Mathf.Max(timedEffect.DurationSeconds, 0f);

            return defaultDuration;
        }

        private void RemoveEntryAt(int index)
        {
            if (index < 0 || index >= entries.Count)
                return;

            var entry = entries[index];
            if (entry.Image != null)
                entry.Image.enabled = false;
            if (entry.Instance != null)
                entry.Instance.SetActive(false);

            entry.IsActive = false;
            entry.Source = null;
            entries[index] = entry;
        }

        private void RemoveEntryForEffect(PerkIntervalTriggerEffect effect)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].IsActive && entries[i].Source == effect)
                {
                    RemoveEntryAt(i);
                    break;
                }
            }
        }

        private void ClearEntries()
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Instance != null)
                    Destroy(entries[i].Instance);
            }
            entries.Clear();
        }

        private struct Entry
        {
            public GameObject Instance { get; }
            public Image Image { get; }
            public float Expiry { get; set; }
            public bool IsActive { get; set; }
            public bool WaitForCast { get; set; }
            public PerkIntervalTriggerEffect Source { get; set; }

            public Entry(GameObject instance, Image image)
            {
                Instance = instance;
                Image = image;
                Expiry = 0f;
                IsActive = false;
                WaitForCast = false;
                Source = null;
            }
        }
    }

    public interface ITimedPerkEffect
    {
        float DurationSeconds { get; }
    }
}

