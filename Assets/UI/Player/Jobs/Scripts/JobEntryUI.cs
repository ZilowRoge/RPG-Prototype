using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Systems.Jobs;

namespace UI.Player.Jobs
{
    public class JobEntryUI : MonoBehaviour
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

            jobNameText.text = job.Data.displayName;
            levelText.text = $"Lv {job.CurrentLevel}";
            slider.fillAmount = job.GetProgressToNextLevel();

            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            controller.OnJobSelected(job);
        }
    }
}
