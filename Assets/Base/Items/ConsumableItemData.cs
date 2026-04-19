using UnityEngine;

namespace Items
{
    [CreateAssetMenu(fileName = "ConsumableData", menuName = "Items/Type Data/Consumable")]
    public class ConsumableItemData : ItemTypeData
    {
        [SerializeField] private ConsumableEffectType effectType = ConsumableEffectType.Health;

        [Min(0f)]
        [SerializeField] private float effectValue = 10f;

        [Min(0f)]
        [SerializeField] private float effectDuration = 0f;

        public ConsumableEffectType EffectType => effectType;
        public float EffectValue => effectValue;
        public float EffectDuration => effectDuration;

        public bool HasEffect => effectValue > 0f;
        public bool IsInstant => effectDuration <= 0.001f;

        public override string GetString()
        {
            if (!HasEffect)
                return $"{effectType} (no effect)";

            if (IsInstant)
                return $"{effectType}: +{effectValue:0.#}";

            return $"{effectType}: +{effectValue:0.#} over {effectDuration:0.#}s";
        }
    }
}
