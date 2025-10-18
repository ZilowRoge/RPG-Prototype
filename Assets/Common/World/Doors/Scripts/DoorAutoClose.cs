using System.Collections;
using UnityEngine;
using Player; // For Interactor detection

namespace Common.Systems.SymbolTraining
{
    [RequireComponent(typeof(Collider))]
    [AddComponentMenu("Game/World/Doors/Auto Close Zone")]
    public class DoorAutoClose : MonoBehaviour
    {
        [SerializeField] private DoorController door;
        [Tooltip("Delay before closing after player enters the zone.")]
        [SerializeField] private float delaySeconds = 0.5f;
        [Tooltip("Close only once. If false, closes every time player enters.")]
        [SerializeField] private bool oneShot = true;

        private bool triggered;

        private void Awake()
        {
            var col = GetComponent<Collider>();
            if (col != null && !col.isTrigger)
                col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsPlayer(other)) return;
            if (oneShot && triggered) return;
            triggered = true;

            if (delaySeconds > 0f)
                StartCoroutine(CloseAfterDelay(delaySeconds));
            else
                CloseNow();
        }

        private IEnumerator CloseAfterDelay(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            CloseNow();
        }

        private void CloseNow()
        {
            if (door == null) door = GetComponentInParent<DoorController>() ?? GetComponent<DoorController>();
            if (door == null)
            {
                Debug.LogWarning("[DoorAutoClose] Missing DoorController.", this);
                return;
            }
            door.TryClose();
        }

        private static bool IsPlayer(Component c)
        {
            if (c == null) return false;
            // Prefer Interactor component, fallback to name check used elsewhere in repo
            if (c.GetComponentInParent<Interactor>() != null) return true;
            return string.Equals(c.gameObject.name, "Player");
        }
    }
}

