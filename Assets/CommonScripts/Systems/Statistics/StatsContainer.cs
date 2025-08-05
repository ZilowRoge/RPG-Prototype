using System;
using System.Collections.Generic;
using UnityEngine;

namespace Systems.Statistics
{
    [Serializable]
    public class StatEntry
    {
        public EStatistics stat;
        public int value;
    }

    [Serializable]
    public class StatsContainer
    {
        [SerializeField]
        private List<StatEntry> stats = new();

        [SerializeField]
        private int pendingAllocations = 0;

        public void AllocatePoint(EStatistics stat) {
            if (pendingAllocations > 0) {
                var targetStat = stats.Find(s => s.stat == stat);
                if (targetStat != null) {
                    targetStat.value += 1;
                    pendingAllocations--;
                }
            }
        }

        public void AddPendingPoint(int amount = 1) {
            pendingAllocations += amount;
        }

        public int GetPendingPoints() => pendingAllocations;

        public List<(EStatistics stat, int value)> GetAll() {
            List<(EStatistics, int)> result = new();
            foreach (var s in stats) {
                result.Add((s.stat, s.value));
            }
            return result;
        }

        public void SetStats(List<(EStatistics stat, int value)> newStats, int pending) {
            stats.Clear();
            foreach (var s in newStats) {
                stats.Add(new StatEntry { stat = s.stat, value = s.value });
            }
            pendingAllocations = pending;
        }

        public override string ToString()
        {
            return string.Join(", ", stats.ConvertAll(e => $"{e.stat}: {e.value}"));
        }
    }
}
