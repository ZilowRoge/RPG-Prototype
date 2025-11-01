using UnityEngine;
using Systems.Perks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Systems.Jobs
{
    [System.Serializable]
    public class JobInstance
    {
        public JobData Data { get; private set; }
        public int CurrentLevel { get; private set; }
        public int Experience { get; private set; }
        public int PerkPoints { get; private set; }

        private readonly List<JobPerkNode> unlockedNodes = new();
        private event Action<JobInstance> OnJobAdvanced;

        public JobInstance(JobData data, Action<JobInstance> onAdvanced)
        {
            Data = data;
            CurrentLevel = 0;
            Experience = 0;
            PerkPoints = 0;
            unlockedNodes.Clear();
            OnJobAdvanced += onAdvanced;
        }

        public float GetProgressToNextLevel()
        {
            int requiredExp = Data.GetRequiredExperience(CurrentLevel);
            return Mathf.Clamp01((float)Experience / requiredExp);
        }

        public bool IsNodeUnlocked(JobPerkNode node) => node != null && unlockedNodes.Contains(node);

        public bool IsPerkUnlocked(PerkData perk)
        {
            if (perk == null) return false;
            return unlockedNodes.Any(node => node?.Perk == perk);
        }

        public bool CanUnlock(JobPerkNode node)
        {
            if (Data == null || node == null) return false;
            if (!node.HasPerk) return false;
            if (PerkPoints <= 0) return false;
            if (IsNodeUnlocked(node)) return false;

            return ArePrerequisitesMet(node);
        }

        public bool CanUnlock(PerkData perk)
        {
            if (Data == null || perk == null) return false;

            foreach (var node in Data.PerkNodes)
            {
                if (node?.Perk == perk && CanUnlock(node))
                    return true;
            }

            return false;
        }

        public void Unlock(JobPerkNode node)
        {
            if (!CanUnlock(node)) return;

            unlockedNodes.Add(node);
            PerkPoints--;
        }

        public void Unlock(PerkData perk)
        {
            if (Data == null || perk == null) return;

            foreach (var node in Data.PerkNodes)
            {
                if (node?.Perk != perk) continue;
                if (!CanUnlock(node)) continue;

                Unlock(node);
                break;
            }
        }

        public int AddExperience(int amount)
        {
            if (amount <= 0 || Data == null) return 0;

            Experience += amount;

            while (CurrentLevel < Data.maxLevel &&
                   Experience >= Data.GetRequiredExperience(CurrentLevel))
            {
                Experience -= Data.GetRequiredExperience(CurrentLevel);
                LevelUp();
            }

            if (CurrentLevel >= Data.maxLevel && Experience > 0)
            {
                int overflow = Experience;
                Experience = 0;
                return overflow;
            }

            return 0;
        }

        public void SetLevel(int level)       => CurrentLevel = Mathf.Clamp(level, 0, Data.maxLevel);
        public void SetExperience(int exp)    => Experience   = Mathf.Max(0, exp);
        public void SetPerkPoints(int points) => PerkPoints   = Mathf.Max(0, points);

        public void SetUnlockedNodes(IEnumerable<string> nodeIds)
        {
            unlockedNodes.Clear();
            if (nodeIds == null) return;

            foreach (var id in nodeIds.Where(s => !string.IsNullOrEmpty(s)).Distinct())
            {
                var node = Data?.GetNodeById(id);
                if (node == null && Data?.PerkNodes != null)
                {
                    node = Data.PerkNodes
                        .FirstOrDefault(n =>
                            n != null &&
                            n.HasPerk &&
                            n.Perk != null &&
                            string.Equals(n.Perk.perkName, id, StringComparison.OrdinalIgnoreCase));
                }

                if (node != null && node.HasPerk && !IsNodeUnlocked(node))
                    unlockedNodes.Add(node);
            }
        }

        [Obsolete("Use SetUnlockedNodes instead.")]
        public void SetUnlockedPerks(IEnumerable<string> perkIds) => SetUnlockedNodes(perkIds);

        public List<string> GetUnlockedNodeIds()
        {
            return unlockedNodes
                .Where(node => node != null && !string.IsNullOrEmpty(node.NodeId))
                .Select(node => node.NodeId)
                .ToList();
        }

        [Obsolete("Use GetUnlockedNodeIds instead.")]
        public List<string> GetUnlockedPerkIds() => GetUnlockedNodeIds();

        private bool ArePrerequisitesMet(JobPerkNode node)
        {
            if (node == null) return false;

            var connections = node.ConnectedNodes;
            if (connections == null || connections.Count == 0)
                return true;

            foreach (var connection in connections)
            {
                if (connection == null) continue;

                if (!connection.HasPerk)
                    return true;

                if (IsNodeUnlocked(connection))
                    return true;
            }

            return false;
        }

        private void LevelUp()
        {
            CurrentLevel++;
            PerkPoints++;
            OnJobAdvanced?.Invoke(this);
        }
    }
}
