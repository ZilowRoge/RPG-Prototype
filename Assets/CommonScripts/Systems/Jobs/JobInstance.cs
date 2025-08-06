using System;

namespace Systems.Jobs {
[System.Serializable]
public class JobInstance
{
    public JobData data;
    public int currentLevel = 1;
    public int experience = 0;
}
}