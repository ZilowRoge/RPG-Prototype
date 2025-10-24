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

        private void Start()
        {
            Refresh();
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
                if (entry != null)
                    entry.Initialize(job, this);
            }
        }

        public void OnJobSelected(JobInstance selectedJob)
        {
            if (perkController != null)
                perkController.DisplayPerks(selectedJob);
        }
    }
}
