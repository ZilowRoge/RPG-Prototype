using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Systems.Perks;
using Systems.Jobs;

namespace UI.Player.Perks
{
    public class PerkEntryUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI perkNameText;
        [SerializeField] private TextMeshProUGUI perkDescriptionText;
        [SerializeField] private Button unlockButton;

        private PerkData perkData;
        private PerkControllerUI controller;

        public void Setup(PerkData perk, PerkControllerUI controllerRef)
        {
            perkData = perk;
            controller = controllerRef;

            perkNameText.text = perkData.perkName;
            perkDescriptionText.text = perkData.description;

            //  unlockButton.interactable = !perkData.IsUnlocked;
            unlockButton.onClick.RemoveAllListeners();
            unlockButton.onClick.AddListener(OnUnlockPressed);
        }

        private void OnUnlockPressed()
        {
            controller.TryUnlockPerk(perkData);
        }

        public void Refresh(JobInstance job)
        {
            unlockButton.interactable = job.CanUnlock(perkData);

            if (job.IsPerkUnlocked(perkData))
                perkDescriptionText.text = "<color=green>Unlocked</color>";
            else if (perkData.unlockLevel <= job.CurrentLevel)
                perkDescriptionText.text = "<color=yellow>Available</color>";
            else
                perkDescriptionText.text = "<color=red>Locked</color>";
        }
    }
}

