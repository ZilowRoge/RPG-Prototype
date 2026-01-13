using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Player.Progress;
using Player.Events;
using Systems.Jobs;
using UI.Player.Common;

namespace UI.Player.Jobs
{
    public class JobExperienceDialogUI : MonoBehaviour, IPlayerReferenceReceiver
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text jobNameText;
        [SerializeField] private TMP_Text jobLevelText;
        [SerializeField] private TMP_Text availablePoolText;
        [SerializeField] private TMP_Text requiredExperienceText;
        [SerializeField] private TMP_Text transferAmountText;
        [SerializeField] private Slider amountSlider;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

        private ProgressController progressController;
        private JobInstance jobInstance;
        private bool isVisible;
        private PlayerEventHub playerEvents;

        private void Awake()
        {
            if (root == null)
                root = gameObject;

            Hide();
        }

        public void Show(JobInstance job, ProgressController controller)
        {
            if (job == null || controller == null)
                return;

            if (amountSlider != null)
                amountSlider.onValueChanged.RemoveListener(OnSliderChanged);

            if (playerEvents != null)
                playerEvents.AvailableExperienceChanged -= OnAvailableExperienceChanged;

            jobInstance = job;
            progressController = controller;
            playerEvents = progressController != null ? progressController.EventHub : null;

            if (root != null)
                root.SetActive(true);

            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveListener(OnConfirm);
                confirmButton.onClick.AddListener(OnConfirm);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveListener(Hide);
                cancelButton.onClick.AddListener(Hide);
            }

            UpdateDisplay();
            UpdateFromPool(progressController.AvailableExperience);

            if (amountSlider != null)
            {
                amountSlider.onValueChanged.AddListener(OnSliderChanged);
                amountSlider.wholeNumbers = true;
            }

            if (playerEvents != null)
                playerEvents.AvailableExperienceChanged += OnAvailableExperienceChanged;
            isVisible = true;
        }

        public void Hide()
        {
            if (!isVisible && root != null)
                root.SetActive(false);

            if (playerEvents != null)
                playerEvents.AvailableExperienceChanged -= OnAvailableExperienceChanged;

            if (amountSlider != null)
                amountSlider.onValueChanged.RemoveListener(OnSliderChanged);

            isVisible = false;
            if (root != null)
                root.SetActive(false);

            jobInstance = null;
            progressController = null;
            playerEvents = null;
        }

        private void OnConfirm()
        {
            if (progressController == null || jobInstance == null)
            {
                Hide();
                return;
            }

            int requested = amountSlider != null ? Mathf.RoundToInt(amountSlider.value) : 0;
            if (requested <= 0)
            {
                Hide();
                return;
            }

            if (jobInstance.Data == null || string.IsNullOrEmpty(jobInstance.Data.id))
            {
                Hide();
                return;
            }

            progressController.AllocateExperienceToJob(jobInstance.Data.id, requested);
            Hide();
        }

        private void UpdateDisplay()
        {
            if (jobInstance == null) return;

            if (jobNameText != null)
            {
                string display = jobInstance.Data != null && !string.IsNullOrEmpty(jobInstance.Data.displayName)
                    ? jobInstance.Data.displayName
                    : "Job";
                jobNameText.text = $"Job: {display}";
            }

            if (jobLevelText != null)
                jobLevelText.text = $"Lvl: {jobInstance.CurrentLevel}";
        }

        private void UpdateFromPool(int poolAmount)
        {
            if (availablePoolText != null)
                availablePoolText.text = $"Available exp: {Mathf.Max(0, poolAmount)}";

            int maxTransfer = CalculateMaxTransfer(poolAmount);
            int sliderValue = maxTransfer;

            if (amountSlider != null)
            {
                amountSlider.minValue = 0;
                amountSlider.maxValue = maxTransfer;
                amountSlider.value = sliderValue;
                amountSlider.interactable = maxTransfer > 0;
            }

            UpdateRequiredExpText(poolAmount);
            UpdateTransferAmountDisplay(sliderValue);
            UpdateConfirmState(sliderValue);
        }

        private void OnAvailableExperienceChanged(int amount)
        {
            if (!isVisible) return;

            UpdateFromPool(amount);
        }

        private void OnSliderChanged(float value)
        {
            int cast = Mathf.RoundToInt(value);
            UpdateTransferAmountDisplay(cast);
            UpdateConfirmState(cast);
        }

        private void UpdateConfirmState(int requested)
        {
            if (confirmButton != null)
                confirmButton.interactable = requested > 0;
        }

        private void UpdateTransferAmountDisplay(int value)
        {
            if (transferAmountText != null)
                transferAmountText.text = $"Transfer: {value} XP";
        }

        private void UpdateRequiredExpText(int pool)
        {
            if (requiredExperienceText == null) return;

            if (jobInstance == null || jobInstance.Data == null || jobInstance.CurrentLevel >= jobInstance.Data.maxLevel)
            {
                requiredExperienceText.text = "Exp to next level: --";
                return;
            }

            int requiredTotal = jobInstance.Data.GetRequiredExperience(jobInstance.CurrentLevel);
            int remaining = Mathf.Max(0, requiredTotal - jobInstance.Experience);
            requiredExperienceText.text = $"Exp to next level: {remaining}";

            if (transferAmountText != null && amountSlider != null && amountSlider.maxValue == 0)
                UpdateTransferAmountDisplay(0);
        }

        private int CalculateMaxTransfer(int poolAmount)
        {
            if (jobInstance == null || jobInstance.Data == null)
                return Mathf.Max(0, poolAmount);

            if (jobInstance.CurrentLevel >= jobInstance.Data.maxLevel)
                return 0;

            int requiredTotal = jobInstance.Data.GetRequiredExperience(jobInstance.CurrentLevel);
            int remaining = Mathf.Max(0, requiredTotal - jobInstance.Experience);
            return Mathf.Max(0, Mathf.Min(poolAmount, remaining));
        }

        public void BindPlayerReferences(PlayerUIReferences refs)
        {
            if (isVisible)
                Hide();

            progressController = refs.Progress;
            playerEvents = refs.EventHub;
        }
    }
}
