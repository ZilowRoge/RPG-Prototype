using System;
using UnityEngine;
using Systems.Jobs;
using Systems.Perks;

namespace Player.Events
{
    /// <summary>
    /// Central hub for player-wide progress and perk notifications.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerEventHub : MonoBehaviour
    {
        public event Action<int> AvailableExperienceChanged;
        public event Action<JobInstance> JobExperienceChanged;
        public event Action<string, bool> FlagChanged;

        public event Action PerkResourcesUpdated;
        public event Action PlayerDied;
        public event Action<PerkIntervalTriggerEffect> PerkIntervalPrimed;
        public event Action<PerkIntervalTriggerEffect, PerkEffectBase> PerkIntervalTriggered;

        public void NotifyAvailableExperienceChanged(int value) =>
            AvailableExperienceChanged?.Invoke(value);

        public void NotifyJobExperienceChanged(JobInstance job) =>
            JobExperienceChanged?.Invoke(job);

        public void NotifyFlagChanged(string key, bool value) =>
            FlagChanged?.Invoke(key, value);

        public void NotifyPerkResourcesUpdated() =>
            PerkResourcesUpdated?.Invoke();

        public void NotifyPerkIntervalPrimed(PerkIntervalTriggerEffect effect) =>
            PerkIntervalPrimed?.Invoke(effect);

        public void NotifyPerkIntervalTriggered(PerkIntervalTriggerEffect effect, PerkEffectBase reward) =>
            PerkIntervalTriggered?.Invoke(effect, reward);

        public void NotifyPlayerDied() =>
            PlayerDied?.Invoke();
    }
}
