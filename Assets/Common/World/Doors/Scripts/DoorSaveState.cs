using System.Collections;
using Systems.SaveSystem;
using Systems.SaveSystem.SaveData;
using UnityEngine;

namespace Common.Systems.SymbolTraining
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(DoorController))]
    public class DoorSaveState : MonoBehaviour, ISaveable
    {
        [SerializeField, Tooltip("Unique identifier used to persist this door's state across saves.")]
        private string doorId = "door_unique_id";

        [SerializeField] private DoorController door;

        private Coroutine registrationRoutine;

        private void Awake()
        {
            CacheDoorReference();
        }

        private void OnEnable()
        {
            CacheDoorReference();
            if (!TryRegister())
            {
                registrationRoutine = StartCoroutine(WaitForSaveManager());
            }
        }

        private void OnDisable()
        {
            if (registrationRoutine != null)
            {
                StopCoroutine(registrationRoutine);
                registrationRoutine = null;
            }

            if (SaveManager.Instance != null)
                SaveManager.Instance.Unregister(this);
        }

        public void OnSave(GameData data)
        {
            if (!Validate(data))
                return;

            var states = data.doorStates;
            int index = FindEntryIndex(states, doorId);
            if (index < 0)
                states.Add(new SerializedDoorState { doorId = doorId, isOpen = door.IsOpen });
            else
                states[index].isOpen = door.IsOpen;
        }

        public void OnLoad(GameData data)
        {
            if (!Validate(data))
                return;

            var states = data.doorStates;
            int index = FindEntryIndex(states, doorId);
            if (index < 0)
                return;

            bool desiredState = states[index]?.isOpen ?? false;
            door.RestoreState(desiredState);
        }

        private bool Validate(GameData data)
        {
            CacheDoorReference();

            if (data == null)
                return false;

            if (door == null)
                return false;

            if (string.IsNullOrEmpty(doorId))
                return false;

            if (data.doorStates == null)
                data.doorStates = new System.Collections.Generic.List<SerializedDoorState>();

            return true;
        }

        private void CacheDoorReference()
        {
            if (door == null)
                door = GetComponent<DoorController>();
        }

        private bool TryRegister()
        {
            if (SaveManager.Instance == null)
                return false;

            SaveManager.Instance.Register(this);
            return true;
        }

        private IEnumerator WaitForSaveManager()
        {
            while (SaveManager.Instance == null)
                yield return null;

            SaveManager.Instance.Register(this);
            registrationRoutine = null;
        }

        private static int FindEntryIndex(System.Collections.Generic.List<SerializedDoorState> states, string id)
        {
            if (states == null || states.Count == 0 || string.IsNullOrEmpty(id))
                return -1;

            for (int i = 0; i < states.Count; i++)
            {
                if (states[i] != null && string.Equals(states[i].doorId, id, System.StringComparison.Ordinal))
                    return i;
            }

            return -1;
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(doorId))
            {
                var sceneName = gameObject.scene.IsValid() ? gameObject.scene.name : "Scene";
                doorId = $"{sceneName}_{gameObject.name}";
            }
        }
    }
}
