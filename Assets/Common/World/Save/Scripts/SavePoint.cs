using Common.World.Interaction;
using Systems.SaveSystem;
using UnityEngine;

namespace Common.World.Save
{
    [AddComponentMenu("Game/World/Save/Save Point")]
    [RequireComponent(typeof(Collider))]
    public class SavePoint : MonoBehaviour, IInteractable
    {
        [SerializeField] private InteractionMode supportedModes = InteractionMode.Both;
        [SerializeField] private InteractionTooltip tooltip;

        public InteractionMode SupportedModes => supportedModes;
        public InteractionTooltip Tooltip => tooltip = InteractionTooltipResolver.Resolve(this, tooltip);

        private void Awake()
        {
            var collider = GetComponent<Collider>();
            if (collider != null && !collider.isTrigger)
                collider.isTrigger = true;
        }

        public void Interact(GameObject player)
        {
            if (SaveManager.Instance == null)
            {
                Debug.LogWarning("[SavePoint] SaveManager instance not found. Cannot save progress.", this);
                return;
            }

            SaveManager.Instance.SaveGame();
            Debug.Log("[SavePoint] Game saved successfully.", this);
        }
    }
}
