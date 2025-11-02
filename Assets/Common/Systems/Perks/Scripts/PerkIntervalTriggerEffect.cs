using UnityEngine;

namespace Systems.Perks
{
    [CreateAssetMenu(menuName = "Perk/Interval Trigger Effect")]
    public class PerkIntervalTriggerEffect : PerkEffectBase
    {
        [Header("Trigger")]
        [Min(1)]
        [SerializeField] private int triggerInterval = 3;

        [Header("Reward")]
        [SerializeField] private PerkEffectBase rewardEffect;
        [SerializeField] private Sprite rewardIcon;
        [SerializeField] private bool consumeOnNextCast = true;

        public int TriggerInterval => Mathf.Max(1, triggerInterval);
        public PerkEffectBase RewardEffect => rewardEffect;
        public Sprite RewardIcon => rewardIcon;
        public bool ConsumeOnNextCast => consumeOnNextCast;

        public override string GetEffectString()
        {
            if (rewardEffect == null)
                return string.Empty;

            var rewardText = rewardEffect.GetEffectString();
            if (string.IsNullOrWhiteSpace(rewardText))
                rewardText = FormatKeyDisplay(rewardEffect.effectType);

            var intervalText = BuildIntervalText();
            if (string.IsNullOrWhiteSpace(intervalText))
                return rewardText;

            return $"{intervalText}: {rewardText}";
        }

        private string BuildIntervalText() => TriggerInterval <= 1
            ? "Each trigger"
            : $"Every {TriggerInterval} triggers";
    }
}
