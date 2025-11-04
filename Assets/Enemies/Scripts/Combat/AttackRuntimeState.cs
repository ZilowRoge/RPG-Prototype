using System.Collections.Generic;
using UnityEngine;

namespace Enemies.Combat
{
    /// <summary>
    /// Tracks cooldowns for attacks on a per-character basis.
    /// </summary>
    [System.Serializable]
    public class AttackRuntimeState
    {
        private readonly Dictionary<AttackDefinition, float> nextReadyTime = new();

        public bool IsReady(AttackDefinition attack, float currentTime)
        {
            if (attack == null)
                return false;

            return currentTime >= GetReadyTime(attack);
        }

        public float GetCooldownRemaining(AttackDefinition attack, float currentTime)
        {
            if (attack == null)
                return 0f;

            float remaining = GetReadyTime(attack) - currentTime;
            return Mathf.Max(0f, remaining);
        }

        public void StartCooldown(AttackDefinition attack, float currentTime, float cooldownModifier = 1f)
        {
            if (attack == null)
                return;

            float cooldown = attack.GetCooldown(cooldownModifier);
            nextReadyTime[attack] = cooldown > 0f ? currentTime + cooldown : currentTime;
        }

        private float GetReadyTime(AttackDefinition attack)
        {
            if (attack == null)
                return 0f;

            if (!nextReadyTime.TryGetValue(attack, out float readyTime))
                return 0f;

            return readyTime;
        }
    }
}
