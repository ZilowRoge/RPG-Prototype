    using UnityEngine;
    using System;
    using System.Linq;
    using Systems.Perks;
    using System.Collections.Generic;

    namespace Systems.Jobs
    {
        [System.Serializable]
        public class JobInstance
        {
            public JobData Data { get; private set; }
            public int CurrentLevel { get; private set; }
            public int Experience { get; private set; }
            public int PerkPoints { get; private set; }

            private List<PerkData> unlockedPerks = new();

            public JobInstance(JobData data)
            {
                Data = data;
                CurrentLevel = 1;
                Experience = 0;
                PerkPoints = 1; // startowy punkt perka
                unlockedPerks.Clear();
            }

            public float GetProgressToNextLevel()
            {
                int requiredExp = Data.GetRequiredExperience(CurrentLevel);
                return Mathf.Clamp01((float)Experience / requiredExp);
            }

            public bool IsPerkUnlocked(PerkData perk)
            {
                return unlockedPerks.Contains(perk);
            }

            public bool CanUnlock(PerkData perk)
            {
                return PerkPoints > 0 && CurrentLevel >= perk.unlockLevel && !IsPerkUnlocked(perk);
            }

            public void Unlock(PerkData perk)
            {
                if (IsPerkUnlocked(perk))
                    return;

                unlockedPerks.Add(perk);
                PerkPoints--;
            }

            public void AddExperience(int amount)
            {
                if (amount <= 0) return;

                Experience += amount;

                while (CurrentLevel < Data.maxLevel &&
                    Experience >= Data.GetRequiredExperience(CurrentLevel))
                {
                    Experience -= Data.GetRequiredExperience(CurrentLevel);
                    LevelUp();
                }
            }

            private void LevelUp()
            {
                CurrentLevel++;
                PerkPoints++;
                // Tu można dodać event np. OnLevelUp?.Invoke(this);
            }

            public void SetLevel(int level) => CurrentLevel = Mathf.Clamp(level, 1, Data.maxLevel);
            public void SetExperience(int exp) => Experience = Mathf.Max(0, exp);
            public void SetPerkPoints(int points) => PerkPoints = Mathf.Max(0, points);

            // Kluczowa wersja: przekazujesz same ID; JobInstance sam mapuje na PerkData z JobData
            public void SetUnlockedPerks(IEnumerable<string> perkIds)
            {
                unlockedPerks.Clear();
                if (perkIds == null) return;

                foreach (var id in perkIds.Where(s => !string.IsNullOrEmpty(s)).Distinct())
                {
                    var perk = FindPerkInJob(id);
                    if (perk != null) unlockedPerks.Add(perk);
                }
            }

             private PerkData FindPerkInJob(string perkId)
            {
                if (Data == null) return null;

                var list = Data.availablePerks;
                if (list == null) return null;

                return list.FirstOrDefault(p => p != null && p.perkName == perkId);
            }
        }
    }
