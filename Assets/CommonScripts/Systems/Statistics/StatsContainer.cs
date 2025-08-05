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
        [SerializeField] private List<StatEntry> stats = new();

        public int Get(EStatistics stat)
        {
            var entry = stats.Find(e => e.stat == stat);
            return entry != null ? entry.value : 0;
        }

        public void Set(EStatistics stat, int value)
        {
            var entry = stats.Find(e => e.stat == stat);
            if (entry != null)
            {
                entry.value = value;
            }
            else
            {
                stats.Add(new StatEntry { stat = stat, value = value });
            }
        }

        public bool Contains(EStatistics stat)
        {
            return stats.Exists(e => e.stat == stat);
        }

        public void Remove(EStatistics stat)
        {
            stats.RemoveAll(e => e.stat == stat);
        }

        public void Clear()
        {
            stats.Clear();
        }

        public IEnumerable<(EStatistics stat, int value)> GetAll()
        {
            foreach (var entry in stats)
                yield return (entry.stat, entry.value);
        }

        public override string ToString()
        {
            return string.Join(", ", stats.ConvertAll(e => $"{e.stat}: {e.value}"));
        }
    }
}
