using System;
using UnityEngine;

namespace Systems.Perks
{
    [CreateAssetMenu(menuName = "Perk/Flat Effect")]
    public class PerkFlatEffect : PerkEffectBase
    {
        public int value;

        public override string GetEffectString()
        {
            var label = effectType switch
            {
                EEffectType.AddMaxMana => "Max Mana",
                _ => FormatKeyDisplay(effectType)
            };

            if (label.StartsWith("Add ", StringComparison.OrdinalIgnoreCase))
                label = label.Substring(4);

            return $"{FormatSignedInt(value)} {label}";
        }
    }
}
