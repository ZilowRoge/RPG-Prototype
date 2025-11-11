using System;
using UnityEngine;

namespace Enemies.TrainingConstruct
{
    [RequireComponent(typeof(Collider))]
    public class ChargeHitbox : MonoBehaviour
    {
        [SerializeField, Tooltip("Collider used to detect charge hits. Must be a trigger.")]
        private Collider triggerCollider;
        [SerializeField, Tooltip("Layers that can be hit by the charge.")]
        private LayerMask targetMask = ~0;

        private bool isActive;
        private bool hasHit;

        public event Action<Collider> Hit;

        private void Awake()
        {
            if (triggerCollider == null)
                triggerCollider = GetComponent<Collider>();

            if (triggerCollider == null)
            {
                Debug.LogError("[ChargeHitbox] Charge hitbox requires a collider.", this);
                return;
            }

            triggerCollider.isTrigger = true;
            triggerCollider.enabled = false;
        }

        public void Activate()
        {
            if (triggerCollider == null)
                return;

            hasHit = false;
            isActive = true;
            triggerCollider.enabled = true;
        }

        public void Deactivate()
        {
            if (triggerCollider == null)
                return;

            isActive = false;
            triggerCollider.enabled = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isActive || hasHit || other == null)
                return;

            if (((1 << other.gameObject.layer) & targetMask.value) == 0)
                return;

            if (other.transform == transform.root)
                return;

            Debug.Log($"[ChargeHitbox] Hit {other.name}", this);
            hasHit = true;
            Hit?.Invoke(other);
        }
    }
}
