using Common.World.Interaction;
using Inventory;
using UnityEngine;

namespace Common.World.Harvesting
{
    [AddComponentMenu("Game/World/Harvesting/Resource Node")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class ResourceNode : MonoBehaviour, IInteractable
    {
        [Header("Drop")]
        [SerializeField] private GameObject pickupPrefab;
        [SerializeField, Min(1)] private int dropAmount = 1;
        [SerializeField] private Transform dropPoint;
        [SerializeField] private Vector3 dropOffset = new Vector3(0f, 0.5f, 0f);

        [Header("Audio")]
        [SerializeField] private AudioClip interactionSound;
        [SerializeField, Range(0f, 1f)] private float interactionSoundVolume = 1f;

        private bool isDepleted;

        private void Reset()
        {
            EnsureTriggerCollider();
        }

        private void Awake()
        {
            EnsureTriggerCollider();
        }

        public void Interact(GameObject player)
        {
            if (isDepleted)
                return;

            if (pickupPrefab == null)
            {
                Debug.LogWarning("[ResourceNode] Missing pickup prefab.", this);
                return;
            }

            var pickupTemplate = pickupPrefab.GetComponent<WorldItemPickup>();
            if (pickupTemplate == null)
            {
                Debug.LogWarning("[ResourceNode] Pickup prefab must include WorldItemPickup.", this);
                return;
            }

            var spawnPosition = dropPoint != null ? dropPoint.position : transform.position + dropOffset;
            var spawnRotation = dropPoint != null ? dropPoint.rotation : Quaternion.identity;

            PlayInteractionSound();

            var spawnedPickupObject = Instantiate(pickupPrefab, spawnPosition, spawnRotation);
            var spawnedPickup = spawnedPickupObject.GetComponent<WorldItemPickup>();
            spawnedPickup.Configure(dropAmount);

            isDepleted = true;
            Destroy(gameObject);
        }

        private void EnsureTriggerCollider()
        {
            var nodeCollider = GetComponent<Collider>();
            if (nodeCollider != null && !nodeCollider.isTrigger)
                nodeCollider.isTrigger = true;
        }

        private void PlayInteractionSound()
        {
            if (interactionSound == null)
                return;

            var soundPosition = dropPoint != null ? dropPoint.position : transform.position;
            AudioSource.PlayClipAtPoint(interactionSound, soundPosition, interactionSoundVolume);
        }
    }
}
