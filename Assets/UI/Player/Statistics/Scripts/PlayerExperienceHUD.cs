using TMPro;
using UnityEngine;
using Player.Progress;
using Player.Events;
using Systems.Jobs;
using UI.Player.Common;

namespace UI.Player.Statistics
{
    /// <summary>
    /// Displays the pool of spendable experience on the HUD
    /// and hides it until the player can afford at least one class level-up.
    /// </summary>
    public class PlayerExperienceHUD : MonoBehaviour, IPlayerReferenceReceiver
    {
        [SerializeField] private ProgressController progress;
        [SerializeField] private TextMeshProUGUI experienceText;
        [SerializeField] private PlayerEventHub playerEvents;
        [SerializeField] private string format = "{0} XP";

        private bool subscribed;

        private void OnEnable()
        {
            CacheEventHub();
            Subscribe();
            RefreshFromProgress();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (subscribed || playerEvents == null)
                return;

            playerEvents.AvailableExperienceChanged += HandleExperienceChanged;
            playerEvents.JobExperienceChanged += HandleJobChanged;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || playerEvents == null)
                return;

            playerEvents.AvailableExperienceChanged -= HandleExperienceChanged;
            playerEvents.JobExperienceChanged -= HandleJobChanged;
            subscribed = false;
        }

        private void HandleExperienceChanged(int amount)
        {
            Refresh(amount);
        }

        private void HandleJobChanged(JobInstance _)
        {
            RefreshFromProgress();
        }

        private void RefreshFromProgress()
        {
            int amount = progress != null ? progress.AvailableExperience : 0;
            Refresh(amount);
        }

        private void Refresh(int amount)
        {
            if (experienceText == null)
                return;

            if (ShouldDisplay(amount))
                experienceText.text = string.Format(format, Mathf.Max(0, amount));
            else
                experienceText.text = string.Empty;
        }

        private bool ShouldDisplay(int availableAmount)
        {
            if (progress == null)
                return false;

            int threshold = GetCheapestLevelRequirement();
            if (threshold == int.MaxValue || threshold <= 0)
                return false;

            return availableAmount >= threshold;
        }

        private int GetCheapestLevelRequirement()
        {
            if (progress == null)
                return int.MaxValue;

            int minRequirement = int.MaxValue;
            foreach (var job in progress.GetAllJobs())
            {
                int remaining = GetRemainingExperience(job);
                if (remaining < minRequirement)
                    minRequirement = remaining;
            }

            return minRequirement;
        }

        private static int GetRemainingExperience(JobInstance job)
        {
            if (job == null || job.Data == null)
                return int.MaxValue;

            if (job.CurrentLevel >= job.Data.maxLevel)
                return int.MaxValue;

            int requiredTotal = job.Data.GetRequiredExperience(job.CurrentLevel);
            return Mathf.Max(0, requiredTotal - job.Experience);
        }

        private void CacheEventHub()
        {
            if (playerEvents == null && progress != null)
                playerEvents = progress.EventHub;
        }

        public void BindPlayerReferences(PlayerUIReferences refs)
        {
            Unsubscribe();

            progress = refs.Progress;
            playerEvents = refs.EventHub;

            if (progress == null)
                progress = FindAnyObjectByType<ProgressController>();

            CacheEventHub();

            if (isActiveAndEnabled)
            {
                Subscribe();
                RefreshFromProgress();
            }
        }
    }
}

