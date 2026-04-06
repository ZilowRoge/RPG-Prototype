using Common.World.Interaction;
using Inventory;
using Items;
using UnityEngine;

namespace Common.World.Harvesting
{
    [AddComponentMenu("Game/World/Harvesting/Resource Node")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class ResourceNode : MonoBehaviour, IInteractable, IInteractionTooltipProvider
    {
        [Header("Resource")]
        [SerializeField, Min(1)] private int totalResourceAmount = 6;
        [SerializeField, Min(1)] private int dropAmountPerExtraction = 1;

        [Header("Tool Requirement")]
        [SerializeField] private GatheringToolType requiredToolType = GatheringToolType.Pickaxe;

        [Header("Mining")]
        [SerializeField, Min(1)] private int minHitsPerExtraction = 2;
        [SerializeField, Min(1)] private int maxHitsPerExtraction = 4;
        [SerializeField, Min(0f)] private float hitCooldownSeconds = 0.35f;

        [Header("Drop")]
        [SerializeField] private GameObject pickupPrefab;
        [SerializeField] private Transform dropPoint;
        [SerializeField] private Vector3 dropOffset = new Vector3(0f, 0.5f, 0f);
        [SerializeField, Min(0f)] private float spawnHorizontalOffset = 0.25f;
        [SerializeField, Min(0f)] private float horizontalLaunchImpulse = 0.9f;
        [SerializeField, Min(0f)] private float upwardLaunchImpulse = 0.35f;

        [Header("Audio")]
        [SerializeField] private AudioClip interactionSound;
        [SerializeField, Range(0f, 1f)] private float interactionSoundVolume = 1f;

        private int remainingResourceAmount;
        private int currentHits;
        private int hitsRequiredForNextExtraction;
        private float nextAllowedHitTime;
        private bool isDepleted;

        private void Reset()
        {
            EnsureTriggerCollider();
        }

        private void Awake()
        {
            EnsureTriggerCollider();
            InitializeRuntimeState();
        }

        public void Interact(GameObject player)
        {
            if (isDepleted)
                return;

            if (Time.time < nextAllowedHitTime)
                return;

            if (!HasRequiredToolEquipped(player))
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

            nextAllowedHitTime = Time.time + hitCooldownSeconds;
            currentHits++;

            PlayInteractionSound();
            ApplyToolDurabilityLoss(player);

            if (currentHits < hitsRequiredForNextExtraction)
                return;

            var spawnPosition = dropPoint != null ? dropPoint.position : transform.position + dropOffset;
            var spawnRotation = dropPoint != null ? dropPoint.rotation : Quaternion.identity;

            currentHits = 0;
            int extractionAmount = Mathf.Min(dropAmountPerExtraction, remainingResourceAmount);
            if (extractionAmount <= 0)
            {
                DepleteNode();
                return;
            }

            var spawnedPickupObject = Instantiate(pickupPrefab, spawnPosition, spawnRotation);
            var spawnedPickup = spawnedPickupObject.GetComponent<WorldItemPickup>();
            spawnedPickup.Configure(extractionAmount);
            ApplyDropScatter(spawnedPickupObject);

            remainingResourceAmount -= extractionAmount;
            if (remainingResourceAmount <= 0)
            {
                DepleteNode();
                return;
            }

            hitsRequiredForNextExtraction = GetRandomHitsRequired();
        }

        public InteractionTooltipState GetTooltipState(GameObject player)
        {
            if (!HasRequiredToolEquipped(player))
                return new InteractionTooltipState(GetMissingToolMessage(), true);

            return new InteractionTooltipState(string.Empty, false);
        }

        private void EnsureTriggerCollider()
        {
            var nodeCollider = GetComponent<Collider>();
            if (nodeCollider != null && !nodeCollider.isTrigger)
                nodeCollider.isTrigger = true;
        }

        private void InitializeRuntimeState()
        {
            remainingResourceAmount = Mathf.Max(1, totalResourceAmount);
            currentHits = 0;
            hitsRequiredForNextExtraction = GetRandomHitsRequired();
            nextAllowedHitTime = 0f;
            isDepleted = remainingResourceAmount <= 0;
        }

        private int GetRandomHitsRequired()
        {
            return Random.Range(minHitsPerExtraction, maxHitsPerExtraction + 1);
        }

        private bool HasRequiredToolEquipped(GameObject player)
        {
            var equipmentController = ResolvePlayerEquipment(player);
            if (equipmentController == null)
                return false;

            var equippedItem = equipmentController.GetItem(EquipmentSlot.Weapon);
            var definition = equippedItem?.Definition;
            if (definition == null)
                return false;

            var toolData = definition.GetStatBlock<GatheringToolItemData>();
            return toolData != null && toolData.ToolType == requiredToolType;
        }

        private static EquipmentController ResolvePlayerEquipment(GameObject player)
        {
            if (player == null)
                return null;

            return player.GetComponentInParent<EquipmentController>()
                ?? player.GetComponentInChildren<EquipmentController>(true);
        }

        private void ApplyToolDurabilityLoss(GameObject player)
        {
            var equipmentController = ResolvePlayerEquipment(player);
            if (equipmentController == null)
                return;

            var equippedItem = equipmentController.GetItem(EquipmentSlot.Weapon);
            if (equippedItem == null || !equippedItem.HasDurability)
                return;

            bool stillUsable = equippedItem.ConsumeDurability();
            if (!stillUsable)
                equipmentController.ClearSlot(EquipmentSlot.Weapon);
        }

        private string GetMissingToolMessage()
        {
            return requiredToolType switch
            {
                GatheringToolType.Axe => "Equip axe",
                GatheringToolType.Pickaxe => "Equip pickaxe",
                _ => $"Equip {requiredToolType}"
            };
        }

        private void DepleteNode()
        {
            isDepleted = true;
            Destroy(gameObject);
        }

        private void PlayInteractionSound()
        {
            if (interactionSound == null)
                return;

            var soundPosition = dropPoint != null ? dropPoint.position : transform.position;
            AudioSource.PlayClipAtPoint(interactionSound, soundPosition, interactionSoundVolume);
        }

        private void ApplyDropScatter(GameObject spawnedPickupObject)
        {
            if (spawnedPickupObject == null)
                return;

            var rigidbody = spawnedPickupObject.GetComponent<Rigidbody>();
            if (rigidbody == null)
                return;

            var direction = GetRandomHorizontalDirection();
            if (spawnHorizontalOffset > 0f)
            {
                spawnedPickupObject.transform.position += direction * spawnHorizontalOffset;
            }

            var impulse = direction * horizontalLaunchImpulse + Vector3.up * upwardLaunchImpulse;
            rigidbody.AddForce(impulse, ForceMode.Impulse);
        }

        private Vector3 GetRandomHorizontalDirection()
        {
            var basis = dropPoint != null ? dropPoint : transform;

            float forwardFactor = Random.Range(-1f, 1f);
            float rightFactor = Random.Range(-1f, 1f);

            var direction = basis.forward * forwardFactor + basis.right * rightFactor;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = Random.value < 0.5f ? basis.forward : basis.right;
                direction.y = 0f;
                direction *= Random.value < 0.5f ? -1f : 1f;
            }

            return direction.normalized;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (maxHitsPerExtraction < minHitsPerExtraction)
                maxHitsPerExtraction = minHitsPerExtraction;
        }
#endif
    }
}
