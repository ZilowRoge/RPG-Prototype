using System.Collections.Generic;
using UnityEngine;
using Systems.Jobs;
using UI.Player.Perks;
using Player.Statistics;

namespace UI.Player.Jobs
{
    public class JobControllerUI : MonoBehaviour
    {
        [SerializeField] private StatsController stats;
        [SerializeField] private Transform slotParent;
        [SerializeField] private GameObject slotPrefab;
        [SerializeField] private PerkControllerUI perkController;

        public void Start()
        {
             if (stats == null || stats.Jobs == null)
            {
                Debug.LogError("JobControllerUI: stats or stats.Jobs is null.");
                return;
            }

            foreach (Transform child in slotParent)
                Destroy(child);

            foreach (var job in stats.Jobs.GetAllJobs())
            {
                var slot = Instantiate(slotPrefab, slotParent);
                slot.GetComponent<JobEntryUI>().Initialize(job, this);
            }
        }

        public void OnJobSelected(JobInstance selectedJob)
        {
            perkController.DisplayPerks(selectedJob);
            // Debug.Log($"Selected job: {selectedJob.data.name}");
            // Tu możesz np. uaktualnić UI Perków albo szczegóły joba
        }
    }
}