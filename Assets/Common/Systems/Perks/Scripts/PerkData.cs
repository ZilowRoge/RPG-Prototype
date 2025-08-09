using System.Collections.Generic;
using UnityEngine;

namespace Systems.Perks {
[CreateAssetMenu(menuName = "Perk/Perk")]
public class PerkData : ScriptableObject
{
    [Header("Perk Metadata")]
    public string perkName;
    public string description;
    public int unlockLevel;

    [Header("Effects")]
    public List<PerkEffectBase> effects;
}
}