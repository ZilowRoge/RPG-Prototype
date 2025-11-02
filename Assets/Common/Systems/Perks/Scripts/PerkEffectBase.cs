using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.Serialization;

namespace Systems.Perks
{
    public abstract class PerkEffectBase : ScriptableObject
    {
        [FormerlySerializedAs("key")]
        public EEffectType effectType;

        public abstract string GetEffectString();

        protected static string FormatSignedInt(int value)
        {
            return value > 0 ? $"+{value}" : value.ToString(CultureInfo.InvariantCulture);
        }

        protected static string FormatSignedFloat(float value, string format = "0.##")
        {
            var formatted = value.ToString(format, CultureInfo.InvariantCulture);
            if (value > 0f)
                return $"+{formatted}";
            if (value < 0f)
                return formatted;
            return "0";
        }

        protected static string FormatKeyDisplay(EEffectType effectKey)
        {
            return SplitCamelCase(effectKey.ToString());
        }

        protected static string SplitCamelCase(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;

            var builder = new StringBuilder(raw.Length + 4);
            builder.Append(raw[0]);

            for (int i = 1; i < raw.Length; i++)
            {
                var current = raw[i];
                var previous = raw[i - 1];

                if (char.IsUpper(current) && !char.IsWhiteSpace(previous) && !char.IsUpper(previous))
                    builder.Append(' ');

                builder.Append(current);
            }

            return builder.ToString();
        }
    }
}
