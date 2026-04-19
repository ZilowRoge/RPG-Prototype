using System.Collections.Generic;
using Common.UI;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    /// <summary>
    /// Centralized helper that temporarily disables player movement and camera look actions.
    /// </summary>
    public class PlayerInputLockService : MonoBehaviour, IPlayerInputLockService
    {
        public enum InputActionToBlock
        {
            Move,
            Look,
            Sprint
        }

        private static PlayerInputLockService instance;
        private static bool isApplicationQuitting;
        public static PlayerInputLockService Instance => ResolveInstance(autoCreate: true);
        public static PlayerInputLockService TryGetInstance() => ResolveInstance(autoCreate: false);

        [Header("Input References")]
        [SerializeField] private PlayerInput playerInput;
        [SerializeField] private InputActionToBlock[] actionsToBlock =
        {
            InputActionToBlock.Move,
            InputActionToBlock.Look,
            InputActionToBlock.Sprint
        };
        [SerializeField] private InputActionReference[] actionReferencesToBlock;

        [Header("Optional Behaviours")]
        [SerializeField] private ThirdPersonMovment movementController;
        [SerializeField] private CinemachineBrain cinemachineBrain;
        [SerializeField] private bool persistAcrossScenes;

        [Header("Debug")]
        [SerializeField] private bool logDebug;

        private readonly HashSet<object> activeLocks = new();
        private readonly List<InputAction> disabledActions = new();
        private readonly List<object> staleLocks = new();
        private bool isLocked;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Debug.LogWarning($"[{nameof(PlayerInputLockService)}] Duplicate instance detected on {name}. Destroying the newest one.");
                Destroy(this);
                return;
            }

            instance = this;

            if (playerInput == null)
                playerInput = GetComponent<PlayerInput>();

            if (movementController == null)
                movementController = GetComponent<ThirdPersonMovment>();

            if (persistAcrossScenes && transform.parent == null && gameObject.scene.IsValid())
                DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        private void OnApplicationQuit()
        {
            isApplicationQuitting = true;
        }

        public void SetLock(object source, bool locked)
        {
            if (source == null)
                source = this;

            PruneStaleLocks();

            bool changed;
            if (locked)
                changed = activeLocks.Add(source);
            else
                changed = activeLocks.Remove(source);

            if (!changed && locked == isLocked)
                return;

            if (activeLocks.Count > 0)
                ApplyLock();
            else
                ReleaseLock();

            LogDebug($"SetLock source={DescribeSource(source)} locked={locked} activeLocks={activeLocks.Count} isLocked={isLocked}");
        }

        public void ClearAllLocks()
        {
            activeLocks.Clear();
            ReleaseLock();
            LogDebug("ClearAllLocks invoked");
        }

        private void ApplyLock()
        {
            if (isLocked)
                return;

            isLocked = true;
            EnsureReferences();
            DisableActions();
            ToggleCinemachineBrain(false);
            movementController?.SetInputBlocked(true);
            LogDebug(BuildStateLog("ApplyLock"));
        }

        private void ReleaseLock()
        {
            if (!isLocked)
                return;

            isLocked = false;
            EnsureReferences();
            EnableActions();
            EnsureBlockedActionsEnabled();
            if (playerInput?.currentActionMap != null && !playerInput.currentActionMap.enabled)
                playerInput.currentActionMap.Enable();
            ToggleCinemachineBrain(true);
            movementController?.SetInputBlocked(false);
            LogDebug(BuildStateLog("ReleaseLock"));
        }

        private static PlayerInputLockService ResolveInstance(bool autoCreate)
        {
            if (instance != null)
                return instance;

            if (isApplicationQuitting)
                return instance;

            instance = FindAnyObjectByType<PlayerInputLockService>();
            if (instance != null)
                return instance;

            if (!autoCreate || !Application.isPlaying)
                return null;

            return CreateInstance();
        }

        private static PlayerInputLockService CreateInstance()
        {
            if (instance != null || isApplicationQuitting || !Application.isPlaying)
                return instance;

            var go = new GameObject(nameof(PlayerInputLockService));
            instance = go.AddComponent<PlayerInputLockService>();
            instance.persistAcrossScenes = true;
            DontDestroyOnLoad(go);
            return instance;
        }

        private void EnsureReferences()
        {
            if (playerInput == null)
                playerInput = FindAnyObjectByType<PlayerInput>();

            if (movementController == null)
                movementController = FindAnyObjectByType<ThirdPersonMovment>();

            if (cinemachineBrain == null)
                cinemachineBrain = FindAnyObjectByType<CinemachineBrain>();
        }

        private void DisableActions()
        {
            disabledActions.Clear();

            if (playerInput != null && playerInput.actions != null)
            {
                foreach (var actionType in actionsToBlock)
                {
                    var actionName = GetActionName(actionType);
                    var action = playerInput.actions.FindAction(actionName, throwIfNotFound: false);
                    TryDisableAction(action);
                }
            }

            if (actionReferencesToBlock != null)
            {
                foreach (var reference in actionReferencesToBlock)
                {
                    var action = reference?.action;
                    TryDisableAction(action);
                }
            }
        }

        private void EnableActions()
        {
            foreach (var action in disabledActions)
            {
                if (action != null && !action.enabled)
                    action.Enable();
            }

            disabledActions.Clear();
        }

        private void ToggleCinemachineBrain(bool enabled)
        {
            if (cinemachineBrain == null)
                return;

            cinemachineBrain.enabled = enabled;
        }

        private void TryDisableAction(InputAction action)
        {
            if (action == null || !action.enabled)
                return;

            action.Disable();
            disabledActions.Add(action);
        }

        private void PruneStaleLocks()
        {
            if (activeLocks.Count == 0)
                return;

            staleLocks.Clear();
            foreach (var source in activeLocks)
            {
                if (source == null)
                    staleLocks.Add(source);
            }

            if (staleLocks.Count == 0)
                return;

            foreach (var source in staleLocks)
                activeLocks.Remove(source);

            staleLocks.Clear();
        }

        private void EnsureBlockedActionsEnabled()
        {
            if (playerInput?.actions == null)
                return;

            foreach (var actionType in actionsToBlock)
            {
                var actionName = GetActionName(actionType);
                var action = playerInput.actions.FindAction(actionName, throwIfNotFound: false);
                if (action != null && !action.enabled)
                    action.Enable();
            }

            if (actionReferencesToBlock == null)
                return;

            foreach (var reference in actionReferencesToBlock)
            {
                var action = reference?.action;
                if (action != null && !action.enabled)
                    action.Enable();
            }
        }

        private void LogDebug(string message)
        {
            if (!logDebug)
                return;

            Debug.Log($"[{nameof(PlayerInputLockService)}] {message}", this);
        }

        private string BuildStateLog(string context)
        {
            var map = playerInput?.currentActionMap;
            var mapName = map != null ? map.name : "null";
            var mapEnabled = map != null && map.enabled;
            return $"{context} activeLocks={activeLocks.Count} isLocked={isLocked} map={mapName} mapEnabled={mapEnabled} actions={DescribeActionStates()}";
        }

        private string DescribeActionStates()
        {
            if (playerInput?.actions == null || actionsToBlock == null || actionsToBlock.Length == 0)
                return "none";

            var parts = new List<string>(actionsToBlock.Length);
            foreach (var actionType in actionsToBlock)
            {
                var actionName = GetActionName(actionType);
                var action = playerInput.actions.FindAction(actionName, throwIfNotFound: false);
                var state = action == null ? "missing" : (action.enabled ? "enabled" : "disabled");
                parts.Add($"{actionName}:{state}");
            }

            return parts.Count > 0 ? string.Join(",", parts) : "none";
        }

        private static string DescribeSource(object source)
        {
            if (source == null)
                return "null";

            if (source is Object unityObject)
                return $"{unityObject.GetType().Name}({unityObject.name})";

            return source.GetType().Name;
        }

        private static string GetActionName(InputActionToBlock actionType) => actionType.ToString();
    }
}

