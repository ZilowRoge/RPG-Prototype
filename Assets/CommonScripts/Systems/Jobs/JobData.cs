using UnityEngine;

namespace Systems.Jobs {
[CreateAssetMenu(menuName = "Jobs/Job")]
public class JobData : ScriptableObject
{
    [Header("Identification")]
    public string id;
    public string displayName;
    public JobTier tier;
    public int maxLevel = 20;

    [Header("Descritption and apperance")]
    [TextArea] public string description;
    public Sprite icon;

    [Header("EXP Settings")]
    public int baseExpToLevel = 100;
    public float expGrowthRate = 1.25f;
}
}