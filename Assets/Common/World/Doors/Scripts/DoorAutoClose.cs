using System.Collections;
using Common.Runtime;
using UnityEngine;

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
        [Tooltip("Ignore player triggers for a short time right after a save load to avoid instant closing.")]
        [SerializeField] private float ignoreSecondsAfterLoad = 0.4f;

        private bool triggered;
        private float allowTriggerTime;

        private void Awake()
        {
            var col = GetComponent<Collider>();
            if (col != null && !col.isTrigger)
                col.isTrigger = true;

            allowTriggerTime = Time.time;
        }

        private void OnEnable()
        {
            SaveRuntimeState.PlayerLoadedFromSave += OnPlayerLoadedFromSave;
        }

        private void OnDisable()
        {
            SaveRuntimeState.PlayerLoadedFromSave -= OnPlayerLoadedFromSave;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (Time.time < allowTriggerTime)
                return;

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
            Debug.Log("[DoorAutoClose] Trying to close the door");
            door.TryClose();
        }

        private static bool IsPlayer(Component c)
        {
            if (c == null) return false;
            if (c.CompareTag("Player")) return true;
            var root = c.GetComponentInParent<Transform>();
            return root != null && root.CompareTag("Player");
        }

        private void OnPlayerLoadedFromSave()
        {
            allowTriggerTime = Time.time + Mathf.Max(0f, ignoreSecondsAfterLoad);
            triggered = false;
            StopAllCoroutines();
        }
    }
}
