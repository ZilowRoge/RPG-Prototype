using System;
using System.Collections.Generic;
using Systems.Jobs;
using UnityEngine.Serialization;

namespace Systems.SaveSystem.SaveData {
    [Serializable]
    public class SerializedJobEntry
    {
        public string jobId;
        public int level;
        public int experience;
        public int perkPoints;
        [FormerlySerializedAs("perkNames")] public List<string> unlockedNodeIds;

        public SerializedJobEntry(string jobId, int level, int experience, int perkPoints, List<string> nodeIds)
        {
            this.jobId      = jobId;
            this.level      = level;
            this.experience = experience;
            this.perkPoints = perkPoints;
            this.unlockedNodeIds  = nodeIds ?? new List<string>();
        }
    }
}
