using UnityEngine;
using Spells.Shields;

namespace Spells {

[CreateAssetMenu(fileName = "MagicShield", menuName = "Spells/Magic Shield")]
public class MagicShield : Spell
{
    [Header("Shield")]
    [SerializeField] private GameObject shieldPrefab;
    [SerializeField] private float shieldAmount = 25f;
    [SerializeField] private float duration = 5f;

    public override void OnActivation(CasterData caster)
    {}

    public override void OnCast(CasterData caster)
    {
        if (caster == null)
        {
            Debug.LogWarning("[MagicShield] Missing caster data.");
            return;
        }

        var stats = caster.stats;
        if (stats == null)
        {
            Debug.LogWarning("[MagicShield] Missing StatsController.");
            return;
        }

        if (shieldPrefab == null)
        {
            Debug.LogWarning("[MagicShield] Shield prefab is not assigned.");
            return;
        }

        float finalAmount = shieldAmount * Mathf.Max(0f, caster.SpellPowerMultiplier);
        if (finalAmount <= 0f)
        {
            Debug.LogWarning("[MagicShield] Shield amount must be greater than zero.");
            return;
        }

        float finalDuration = Mathf.Max(0f, duration);
        if (finalDuration <= 0f)
        {
            Debug.LogWarning("[MagicShield] Shield duration must be greater than zero.");
            return;
        }

        Transform anchor = stats.transform;
        Vector3 position = anchor.position;
        Quaternion rotation = anchor.rotation;

        var instance = Object.Instantiate(shieldPrefab, position, rotation);
        ShieldController.TryInit(instance, stats, finalAmount, finalDuration, anchor);
    }

    public override void OnDeactivation(CasterData caster)
    {}
}

}
