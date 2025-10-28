using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Systems.Jobs;

namespace UI.Player.Jobs
{
    public class JobEntryUI : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private TextMeshProUGUI jobNameText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private Image slider;
        [SerializeField] private Button selectButton;

        private JobInstance job;
        private JobControllerUI controller;

        public void Initialize(JobInstance job, JobControllerUI parentController)
        {
            this.job = job;
            controller = parentController;

            EnsureButtonBinding();

            Refresh();
            controller?.RegisterEntry(job, this);
        }

        public void Refresh()
        {
            if (job == null) return;

            if (jobNameText != null)
                jobNameText.text = job.Data != null ? job.Data.displayName : "Job";

            if (levelText != null)
                levelText.text = $"Lv {job.CurrentLevel}";

            if (slider != null)
            {
                if (job.Data != null && job.CurrentLevel < job.Data.maxLevel)
                    slider.fillAmount = job.GetProgressToNextLevel();
                else
                    slider.fillAmount = 1f;
            }
        }

        private void OnClick()
        {
            controller?.OnJobSelected(job);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            controller?.OnJobSelected(job);
        }

        private void EnsureButtonBinding()
        {
            if (selectButton == null)
                selectButton = GetComponent<Button>();

            if (selectButton == null)
                return;

            selectButton.onClick.RemoveListener(OnClick);
            selectButton.onClick.AddListener(OnClick);
        }
    }
}
