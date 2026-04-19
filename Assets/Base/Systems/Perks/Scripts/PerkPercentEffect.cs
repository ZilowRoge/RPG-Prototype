using System;
using System.Globalization;
using UnityEngine;

namespace Systems.Perks
{
    [CreateAssetMenu(menuName = "Perk/Percent Effect")]
    public class PerkPercentEffect : PerkEffectBase
    {
        public float value;

        public override string GetEffectString()
        {
            var label = TrimPercentSuffix(FormatKeyDisplay(effectType));
            return $"{FormatPercentValue(value)}% {label}";
        }

        private static string TrimPercentSuffix(string label)
        {
            if (string.IsNullOrWhiteSpace(label))
                return string.Empty;

            const string wordSuffix = " Percent";
            const string symbolSuffix = " %";

            if (label.EndsWith(symbolSuffix, StringComparison.Ordinal))
                return label.Substring(0, label.Length - symbolSuffix.Length);

            return label.EndsWith(wordSuffix, StringComparison.Ordinal)
                ? label.Substring(0, label.Length - wordSuffix.Length)
                : label;
        }

        private static string FormatPercentValue(float raw)
        {
            float percent = raw * 100f;
            float abs = Mathf.Abs(percent);
            string formatted = abs.ToString("0.##", CultureInfo.InvariantCulture);
            return raw < 0f ? $"-{formatted}" : formatted;
        }

    }
}
