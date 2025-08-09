using System;
using System.Collections.Generic;
using Systems.Jobs;

namespace Systems.SaveSystem.SaveData {
    [Serializable]
    public class SerializedJobEntry
    {
        public string jobId;
        public int level;
        public int experience;
        public int perkPoints;
        public List<string> perkNames;

        public SerializedJobEntry(string jobId, int level, int experience, int perkPoints, List<string> perkNames)
        {
            this.jobId      = jobId;
            this.level      = level;
            this.experience = experience;
            this.perkPoints = perkPoints;
            this.perkNames  = perkNames ?? new List<string>();
        }
    }
}