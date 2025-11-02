using System;
using System.Collections.Generic;
using UnityEngine;
using Player.Progress;
using Systems.Jobs;
using Systems.Perks;
using Systems.Statistics;
using Spells;

namespace Player.Perks
{
    /// <summary>
    /// Aggregates active perk effects for the player at runtime and exposes utility accessors
    /// for other gameplay systems (stats, spells, etc.). Interval-triggered perks are evaluated
    /// explicitly via <see cref="PrepareForCast"/> so callers can apply their rewards to the
    /// current spell before it resolves.
    /// </summary>
    public class PlayerPerkRuntime : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ProgressController progressController;

        private readonly Dictionary<JobInstance, Action<JobInstance, JobPerkNode>> jobSubscriptions = new();
        private readonly Dictionary<EEffectType, float> flatBonuses = new();
        private readonly Dictionary<EEffectType, float> percentBonuses = new();
        private readonly Dictionary<EStatistics, int> statBonuses = new();
        private readonly List<IntervalTracker> intervalEffects = new();
        private readonly List<(PerkIntervalTriggerEffect interval, PerkEffectBase reward)> triggerBuffer = new();
        private float castSpellPowerBonus;
        public event Action UpdateResources;
        public event Action<PerkIntervalTriggerEffect> IntervalEffectPrimed;
        public event Action<PerkIntervalTriggerEffect, PerkEffectBase> IntervalEffectTriggered;

        public float GetFlatBonus(EEffectType type) =>
            flatBonuses.TryGetValue(type, out var value) ? value : 0f;

        public float GetPercentBonus(EEffectType type) =>
            percentBonuses.TryGetValue(type, out var value) ? value : 0f;

        public int GetStatBonus(EStatistics stat) =>
            statBonuses.TryGetValue(stat, out var value) ? value : 0;

        public float SpellPowerPercentBonus => GetPercentBonus(EEffectType.SpellPower);
        public float BaseSpellPowerMultiplier => 1f + SpellPowerPercentBonus;
        public float CurrentCastSpellPowerBonus => castSpellPowerBonus;
        public float SpellPowerMultiplier => 1f + SpellPowerPercentBonus + castSpellPowerBonus;

        private void OnEnable()
        {
            if (progressController == null)
                progressController = GetComponentInParent<ProgressController>();

            if (progressController != null)
                progressController.JobExperienceChanged += OnJobExperienceChanged;

            RebuildAll();
        }

        private void OnDisable()
        {
            if (progressController != null)
                progressController.JobExperienceChanged -= OnJobExperienceChanged;

            foreach (var kvp in jobSubscriptions)
            {
                if (kvp.Key != null)
                    kvp.Key.PerkUnlocked -= kvp.Value;
            }

            jobSubscriptions.Clear();
            intervalEffects.Clear();
        }

        private void OnJobExperienceChanged(JobInstance job)
        {
            if (job == null)
                return;

            SubscribeToJob(job);
            RebuildAll();
        }

        private void SubscribeToJob(JobInstance job)
        {
            if (job == null || jobSubscriptions.ContainsKey(job))
                return;

            Action<JobInstance, JobPerkNode> handler = OnJobPerkUnlocked;
            job.PerkUnlocked += handler;
            jobSubscriptions[job] = handler;
        }

        private void OnJobPerkUnlocked(JobInstance job, JobPerkNode node)
        {
            if (node?.Perk == null)
                return;

            ProcessPerk(node.Perk);
            UpdateResources?.Invoke();
        }

        private void RebuildAll()
        {
            flatBonuses.Clear();
            percentBonuses.Clear();
            statBonuses.Clear();
            intervalEffects.Clear();
            castSpellPowerBonus = 0f;

            if (progressController != null)
            {
                foreach (var job in progressController.GetAllJobs())
                {
                    SubscribeToJob(job);
                    CollectJobPerks(job);
                }
            }

            UpdateResources?.Invoke();
        }

        private void CollectJobPerks(JobInstance job)
        {
            if (job == null)
                return;

            foreach (var node in job.GetUnlockedNodes())
                ProcessPerk(node?.Perk);
        }

        private void ProcessPerk(PerkData perk)
        {
            if (perk == null || perk.effects == null)
                return;

            foreach (var effect in perk.effects)
                ProcessEffect(effect);
        }

        private void ProcessEffect(PerkEffectBase effect)
        {
            switch (effect)
            {
                case PerkAddStatEffect addStat:
                    if (!statBonuses.ContainsKey(addStat.stat))
                        statBonuses.Add(addStat.stat, 0);
                    statBonuses[addStat.stat] += addStat.value;
                    break;

                case PerkPercentEffect percent:
                    var percentType = percent.effectType;
                    if (!percentBonuses.ContainsKey(percentType))
                        percentBonuses.Add(percentType, 0f);
                    percentBonuses[percentType] += percent.value * 0.01f;
                    break;

                case PerkFlatEffect flat:
                    var flatType = flat.effectType;
                    if (!flatBonuses.ContainsKey(flatType))
                        flatBonuses.Add(flatType, 0f);
                    flatBonuses[flatType] += flat.value;
                    break;

                case PerkIntervalTriggerEffect interval:
                    if (interval.RewardEffect != null)
                        intervalEffects.Add(new IntervalTracker(interval));
                    break;
            }
        }

        public IReadOnlyList<(PerkIntervalTriggerEffect interval, PerkEffectBase reward)> PrepareForCast(Spell spell)
        {
            triggerBuffer.Clear();
            castSpellPowerBonus = 0f;

            for (int i = 0; i < intervalEffects.Count; i++)
            {
                var tracker = intervalEffects[i].Advance();
                if (tracker.ShouldTrigger)
                {
                    intervalEffects[i] = tracker.Reset();
                    if (tracker.Effect.RewardEffect != null)
                        triggerBuffer.Add((tracker.Effect, tracker.Effect.RewardEffect));
                }
                else
                {
                    intervalEffects[i] = tracker;
                    if (tracker.WillTriggerNext)
                        IntervalEffectPrimed?.Invoke(tracker.Effect);
                }
            }

            foreach (var pair in triggerBuffer)
            {
                ApplyCastReward(pair.reward);
                IntervalEffectTriggered?.Invoke(pair.interval, pair.reward);
            }

            return triggerBuffer;
        }

        private void ApplyCastReward(PerkEffectBase reward)
        {
            switch (reward)
            {
                case PerkPercentEffect percent when percent.effectType == EEffectType.SpellPower:
                    castSpellPowerBonus += percent.value * 0.01f;
                    break;
            }
        }


        private readonly struct IntervalTracker
        {
            public PerkIntervalTriggerEffect Effect { get; }
            private readonly int triggerInterval;
            private readonly int counter;

            public IntervalTracker(PerkIntervalTriggerEffect effect)
            {
                Effect = effect;
                triggerInterval = effect != null ? effect.TriggerInterval : 1;
                counter = 0;
            }

            private IntervalTracker(PerkIntervalTriggerEffect effect, int interval, int currentCount)
            {
                Effect = effect;
                triggerInterval = interval <= 0 ? 1 : interval;
                counter = currentCount;
            }

            public IntervalTracker Advance()
            {
                return new IntervalTracker(Effect, triggerInterval, counter + 1);
            }

            public bool ShouldTrigger => counter >= triggerInterval && Effect != null;
            public bool WillTriggerNext => Effect != null && counter == triggerInterval - 1;

            public IntervalTracker Reset()
            {
                return new IntervalTracker(Effect, triggerInterval, 0);
            }
        }
    }
}
