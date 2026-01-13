using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Systems.Jobs;
using UI.Player.Perks;
using Player.Progress;
using Player.Events;
using UI.Player.Common;

namespace UI.Player.Jobs
{
    public class JobControllerUI : MonoBehaviour, IPlayerReferenceReceiver
    {
        [Header("Core References")]
        [SerializeField] private ProgressController progressController;
        [SerializeField] private Transform slotParent;
        [SerializeField] private GameObject slotPrefab;
        [SerializeField] private PerkControllerUI perkController;

        [Header("Selection Details")]
        [SerializeField] private TMP_Text availableExperienceText;
        [SerializeField] private JobExperienceDialogUI experienceDialog;

        private readonly Dictionary<JobInstance, JobEntryUI> entryLookup = new();
        private JobInstance selectedJob;
        private bool isSubscribed;
        private PlayerEventHub playerEvents;

        private void OnEnable()
        {
            Subscribe();
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
            experienceDialog?.Hide();
        }

        private void Subscribe()
        {
            if (isSubscribed || progressController == null) return;
            playerEvents = progressController.EventHub ?? playerEvents;
            if (playerEvents == null) return;

            playerEvents.AvailableExperienceChanged += OnAvailableExperienceChanged;
            playerEvents.JobExperienceChanged += OnJobExperienceChanged;
            isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!isSubscribed) return;

            if (playerEvents == null && progressController != null)
                playerEvents = progressController.EventHub;

            if (playerEvents != null)
            {
                playerEvents.AvailableExperienceChanged -= OnAvailableExperienceChanged;
                playerEvents.JobExperienceChanged -= OnJobExperienceChanged;
            }

            isSubscribed = false;
            playerEvents = null;
        }

        public void Refresh()
        {
            if (progressController == null)
            {
                Debug.LogError("JobControllerUI: missing progressController", this);
                return;
            }

            if (slotParent == null || slotPrefab == null)
            {
                Debug.LogWarning("JobControllerUI: slot references not assigned.", this);
                return;
            }

            entryLookup.Clear();

            for (int i = slotParent.childCount - 1; i >= 0; i--)
            {
                var child = slotParent.GetChild(i);
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    DestroyImmediate(child.gameObject);
                else
#endif
                    Destroy(child.gameObject);
            }

            foreach (var job in progressController.GetAllJobs())
            {
                var slot = Instantiate(slotPrefab, slotParent);
                var entry = slot.GetComponent<JobEntryUI>() ?? slot.GetComponentInChildren<JobEntryUI>(true);
                if (entry == null) continue;

                entry.Initialize(job, this);
            }

            if (selectedJob == null || !entryLookup.ContainsKey(selectedJob))
                selectedJob = entryLookup.Keys.FirstOrDefault();

            ApplySelection(selectedJob, notifyPerkPanel: true);
            UpdateAvailableExperienceUI(progressController.AvailableExperience);
        }

        public void RegisterEntry(JobInstance job, JobEntryUI entry)
        {
            if (job == null || entry == null) return;
            entryLookup[job] = entry;
        }

        public void OnJobSelected(JobInstance job)
        {
            if (job == null || !entryLookup.ContainsKey(job))
                return;

            ApplySelection(job, notifyPerkPanel: true);
        }

        private void ApplySelection(JobInstance job, bool notifyPerkPanel)
        {
            selectedJob = job;

            if (notifyPerkPanel && selectedJob != null && perkController != null)
                perkController.Show(selectedJob);
        }

        private void UpdateAvailableExperienceUI(int amount)
        {
            if (availableExperienceText != null)
                availableExperienceText.text = $"{Mathf.Max(0, amount)} XP";
        }

        private void OnAvailableExperienceChanged(int amount)
        {
            UpdateAvailableExperienceUI(amount);
        }

        private void OnJobExperienceChanged(JobInstance job)
        {
            if (job == null) return;

            if (entryLookup.TryGetValue(job, out var entry))
            {
                entry.Refresh();
            }
            else
            {
                Refresh();
                return;
            }

            if (job == selectedJob)
            {
                if (perkController != null)
                    perkController.Show(selectedJob);
            }
        }

        internal void RequestExperienceAllocation(JobInstance job)
        {
            if (job == null || progressController == null)
                return;

            if (experienceDialog == null)
            {
                Debug.LogWarning("JobControllerUI: Experience dialog reference not assigned.", this);
                return;
            }

            experienceDialog.Show(job, progressController);
        }

        public void BindPlayerReferences(PlayerUIReferences refs)
        {
            Unsubscribe();

            progressController = refs.Progress;
            playerEvents = refs.EventHub;

            if (progressController == null)
                progressController = FindFirstObjectByType<ProgressController>();

            if (playerEvents == null && progressController != null)
                playerEvents = progressController.EventHub;

            if (isActiveAndEnabled)
            {
                Subscribe();
                if (progressController != null)
                    Refresh();
            }
        }
    }
}
