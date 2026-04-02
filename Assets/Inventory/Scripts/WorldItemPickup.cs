using Items;
using UnityEngine;

namespace Inventory
{
    [AddComponentMenu("Game/Inventory/World Item Pickup")]
    [RequireComponent(typeof(Collider))]
    public class WorldItemPickup : MonoBehaviour
    {
        [Header("Item")]
        [SerializeField] private ItemDefinition itemDefinition;
        [SerializeField, Min(1)] private int amount = 1;

        [Header("Pickup")]
        [SerializeField] private bool destroyAfterPickup = true;

        public void Configure(int stackAmount)
        {
            amount = Mathf.Max(1, stackAmount);
        }

        private void Reset()
        {
            EnsureTriggerCollider();
        }

        private void Awake()
        {
            EnsureTriggerCollider();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (itemDefinition == null)
            {
                Debug.LogWarning("[WorldItemPickup] Missing ItemDefinition.", this);
                return;
            }

            var inventory = other.GetComponentInParent<InventoryController>();
            if (inventory == null)
                return;

            if (!inventory.TryAddItem(itemDefinition, amount))
                return;

            if (destroyAfterPickup)
                Destroy(gameObject);
            else
                gameObject.SetActive(false);
        }

        private void EnsureTriggerCollider()
        {
            var itemCollider = GetComponent<Collider>();
            if (itemCollider != null && !itemCollider.isTrigger)
                itemCollider.isTrigger = true;
        }
    }
}
