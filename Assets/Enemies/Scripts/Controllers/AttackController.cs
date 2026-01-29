using Enemies.Combat;
using UnityEngine;

namespace Enemies.Controllers
{
    /// <summary>
    /// Generic runtime controller that manages attack execution and cooldown tracking.
    /// </summary>
    public class AttackController : MonoBehaviour
    {
        [SerializeField] private AttackRuntimeState runtimeState = new();

        public AttackRuntimeState RuntimeState => runtimeState;

        public bool TryUseAttack(AttackDefinition attack, Transform target)
        {
            if (attack == null)
            {
                Debug.LogWarning("[AttackController] TryUseAttack called with null attack.", this);
                return false;
            }

            float currentTime = Time.time;
            if (!runtimeState.IsReady(attack, currentTime))
            {
                Debug.Log($"[AttackController] Attack '{attack.name}' not ready at {currentTime}.", this);
                return false;
            }

            var context = new AttackContext(
                transform,
                target,
                attack,
                Time.deltaTime);

            Debug.Log($"[AttackController] Executing '{attack.name}' on {(target != null ? target.name : "null")}.", this);
            attack.Execute(in context);
            runtimeState.StartCooldown(attack, currentTime);
            return true;
        }
    }
}
