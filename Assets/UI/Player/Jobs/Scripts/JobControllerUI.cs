using System.Collections.Generic;
using UnityEngine;
using Systems.Jobs;
using UI.Player.Perks;
using Player.Progress;

namespace UI.Player.Jobs
{
    public class JobControllerUI : MonoBehaviour
    {
        [SerializeField] private ProgressController progressController;
        [SerializeField] private Transform slotParent;
        [SerializeField] private GameObject slotPrefab;
        [SerializeField] private PerkControllerUI perkController;

        public void Start()
        {
             if (progressController == null)
            {
                Debug.LogError("JobControllerUI: missing progressController");
                return;
            }

            foreach (Transform child in slotParent)
                Destroy(child);

            foreach (var job in progressController.GetAllJobs())
            {
                var slot = Instantiate(slotPrefab, slotParent);
                slot.GetComponent<JobEntryUI>().Initialize(job, this);
            }
        }

        public void OnJobSelected(JobInstance selectedJob)
        {
            perkController.DisplayPerks(selectedJob);
        }
    }
}