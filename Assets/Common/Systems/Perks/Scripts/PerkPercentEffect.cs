using System;
using UnityEngine;

namespace Systems.Perks
{
    [CreateAssetMenu(menuName = "Perk/Percent Effect")]
    public class PerkPercentEffect : PerkEffectBase
    {
        public float value;

        public override string GetEffectString()
        {
            float scaled = Mathf.Abs(value) <= 1f ? value * 100f : value;
            var label = TrimPercentSuffix(FormatKeyDisplay(key));
            return $"{FormatSignedFloat(scaled)}% {label}";
        }

        private static string TrimPercentSuffix(string label)
        {
            const string suffix = " Percent";
            if (string.IsNullOrWhiteSpace(label))
                return string.Empty;

            return label.EndsWith(suffix, StringComparison.Ordinal)
                ? label.Substring(0, label.Length - suffix.Length)
                : label;
        }
    }
}
