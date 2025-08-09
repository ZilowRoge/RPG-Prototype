using System.Collections.Generic;
using UnityEngine;
using Systems.Jobs;
using Systems.Perks;

namespace UI.Player.Perks
{
    public class PerkControllerUI : MonoBehaviour
    {
        [SerializeField] private Transform perkListContainer;
        [SerializeField] private GameObject perkEntryPrefab;
        [SerializeField] private TMPro.TMP_Text perkPointsText;

        private List<PerkEntryUI> spawnedEntries = new();
        private JobInstance currentJob;

        public void DisplayPerks(JobInstance job)
        {
            currentJob = job;

            foreach (var entry in spawnedEntries)
                Destroy(entry.gameObject);
            spawnedEntries.Clear();

            foreach (var perk in currentJob.Data.availablePerks)
            {
                GameObject obj = Instantiate(perkEntryPrefab, perkListContainer);
                PerkEntryUI entryUI = obj.GetComponent<PerkEntryUI>();
                entryUI.Setup(perk, this);
                spawnedEntries.Add(entryUI);
            }
            RefreshAll();
        }

        public void TryUnlockPerk(PerkData perk)
        {
            if (currentJob.CanUnlock(perk))
            {
                currentJob.Unlock(perk);
                RefreshAll();
            }
        }

        private void RefreshAll()
        {
            UpdatePerkPointsUI();
            foreach (var entry in spawnedEntries)
                entry.Refresh(currentJob);
        }

        private void UpdatePerkPointsUI()
        {
            if (perkPointsText != null && currentJob != null)
                perkPointsText.text = $"Perk Points: {currentJob.PerkPoints}";
        }
    }
}
