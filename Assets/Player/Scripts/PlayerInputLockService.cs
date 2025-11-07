using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    /// <summary>
    /// Centralized helper that temporarily disables player movement and camera look actions.
    /// </summary>
    public class PlayerInputLockService : MonoBehaviour
    {
        private static PlayerInputLockService instance;
        private static bool isApplicationQuitting;
        public static PlayerInputLockService Instance => ResolveInstance(autoCreate: true);
        public static PlayerInputLockService TryGetInstance() => ResolveInstance(autoCreate: false);

        [Header("Input References")]
        [SerializeField] private PlayerInput playerInput;
        [SerializeField] private string[] actionNamesToBlock = { "Move", "Look", "Sprint" };
        [SerializeField] private InputActionReference[] actionReferencesToBlock;

        [Header("Optional Behaviours")]
        [SerializeField] private ThirdPersonMovment movementController;
        [SerializeField] private MonoBehaviour[] componentsToDisable;

        private readonly HashSet<object> activeLocks = new();
        private readonly List<InputAction> disabledActions = new();
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

            if (transform.parent == null && gameObject.scene.IsValid())
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
        }

        public void ClearAllLocks()
        {
            activeLocks.Clear();
            ReleaseLock();
        }

        private void ApplyLock()
        {
            if (isLocked)
                return;

            isLocked = true;
            EnsureReferences();
            DisableActions();
            ToggleComponents(false);
            movementController?.SetInputBlocked(true);
        }

        private void ReleaseLock()
        {
            if (!isLocked)
                return;

            isLocked = false;
            movementController?.SetInputBlocked(false);
            ToggleComponents(true);
            EnableActions();
        }

        private static PlayerInputLockService ResolveInstance(bool autoCreate)
        {
            if (instance != null)
                return instance;

            if (isApplicationQuitting)
                return instance;

            instance = FindFirstObjectByType<PlayerInputLockService>();
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
            DontDestroyOnLoad(go);
            instance = go.AddComponent<PlayerInputLockService>();
            return instance;
        }

        private void EnsureReferences()
        {
            if (playerInput == null)
                playerInput = FindFirstObjectByType<PlayerInput>();

            if (movementController == null)
                movementController = FindFirstObjectByType<ThirdPersonMovment>();
        }

        private void DisableActions()
        {
            disabledActions.Clear();

            if (playerInput != null && playerInput.actions != null)
            {
                foreach (var actionName in actionNamesToBlock)
                {
                    if (string.IsNullOrWhiteSpace(actionName))
                        continue;

                    var action = playerInput.actions.FindAction(actionName, throwIfNotFound: false);
                    TryDisableAction(action);
                }
            }

            if (actionReferencesToBlock != null)
            {
                foreach (var reference in actionReferencesToBlock)
                {
                    TryDisableAction(reference?.action);
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

        private void ToggleComponents(bool enabled)
        {
            if (componentsToDisable == null || componentsToDisable.Length == 0)
                return;

            for (int i = 0; i < componentsToDisable.Length; i++)
            {
                if (componentsToDisable[i] != null)
                    componentsToDisable[i].enabled = enabled;
            }
        }

        private void TryDisableAction(InputAction action)
        {
            if (action == null || !action.enabled)
                return;

            action.Disable();
            disabledActions.Add(action);
        }
    }
}
