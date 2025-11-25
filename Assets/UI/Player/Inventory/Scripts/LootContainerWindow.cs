using Inventory;
using Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UI.Player.Inventory
{
    /// <summary>
    /// Displays a container inventory alongside the player's inventory and supports transferring items between them.
    /// </summary>
    public class LootContainerWindow : MonoBehaviour
    {
        private const string LogPrefix = "[LootContainerWindow]";

        [Header("Root")]
        [SerializeField] private GameObject root;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private bool closeOnEscape = true;
        [SerializeField] private bool autoCloseWhenEmpty;

        [Header("Panels")]
        [SerializeField] private InventoryPanelUI playerPanel;
        [SerializeField] private InventoryPanelUI containerPanel;
        [SerializeField] private bool manageCursor = true;

        private InventoryController playerController;
        private InventoryController containerController;
        private bool isOpen;
        private bool cursorCaptured;
        private CursorLockMode previousCursorLock;
        private bool previousCursorVisible;

        public bool IsOpen => isOpen;

        private void Awake()
        {
            if (root == null)
                root = gameObject;

            HideImmediate();
        }

        private void OnDisable()
        {
            if (isOpen)
            {
                PlayerInputLockService.Instance?.SetLock(this, false);
                isOpen = false;
            }
        }

        private void Update()
        {
            if (!isOpen || !closeOnEscape || Keyboard.current == null)
                return;

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
                Close();
        }

        public void Open(InventoryController playerInventory, InventoryController containerInventory, string containerName = null)
        {
            if (isOpen)
                Close();

            if (playerInventory == null || containerInventory == null)
            {
                Debug.LogWarning($"{LogPrefix} Missing inventory controller reference.");
                return;
            }

            playerController = playerInventory;
            containerController = containerInventory;

            SetupPanel(playerPanel, playerController, HandlePlayerSlotDoubleClick);
            SetupPanel(containerPanel, containerController, HandleContainerSlotDoubleClick);

            isOpen = true;
            SetRootVisible(true);
            Refresh();
            PlayerInputLockService.Instance?.SetLock(this, true);
            if (manageCursor)
                CaptureCursor();
        }

        public void Close()
        {
            if (!isOpen)
                return;

            PlayerInputLockService.Instance?.SetLock(this, false);
            if (manageCursor)
                ReleaseCursor();
            isOpen = false;
            SetRootVisible(false);
            ClearPanelHandlers(playerPanel);
            ClearPanelHandlers(containerPanel);
            playerController = null;
            containerController = null;
        }

        private void Refresh()
        {
            playerPanel?.Refresh();
            containerPanel?.Refresh();
            TryCloseIfContainerEmpty();
        }

        private void SetupPanel(InventoryPanelUI panel, InventoryController controller, System.Func<int, bool> doubleClickHandler)
        {
            if (panel == null)
            {
                Debug.LogWarning($"{LogPrefix} Missing panel reference.", this);
                return;
            }

            panel.SetInventoryController(controller);
            panel.SetDoubleClickHandler(doubleClickHandler);
            panel.SetExternalDropHandler(HandleExternalDrop);
            panel.SetRefreshCallback(Refresh);
        }

        private void ClearPanelHandlers(InventoryPanelUI panel)
        {
            if (panel == null)
                return;

            panel.SetInventoryController(null);
            panel.SetDoubleClickHandler(null);
            panel.SetExternalDropHandler(null);
            panel.SetRefreshCallback(null);
        }

        private bool HandleExternalDrop(InventorySlotUI targetSlot, InventorySlotUI sourceSlot)
        {
            if (targetSlot == null || sourceSlot == null)
                return false;

            var sourcePanel = sourceSlot != null ? sourceSlot.OwnerPanel : null;
            var targetPanel = targetSlot != null ? targetSlot.OwnerPanel : null;

            var source = ResolveController(sourcePanel);
            var target = ResolveController(targetPanel);

            if (source == null || target == null)
                return false;

            bool moved = source.TryTransferItemTo(target, sourceSlot.SlotId, targetSlot.SlotId);

            if (moved)
                Refresh();

            return moved;
        }

        private bool HandleContainerSlotDoubleClick(int slotIndex)
        {
            bool moved = Transfer(containerController, playerController, slotIndex);
            if (moved)
                Refresh();

            return moved;
        }

        private bool HandlePlayerSlotDoubleClick(int slotIndex)
        {
            bool moved = Transfer(playerController, containerController, slotIndex);
            if (moved)
                Refresh();

            return moved;
        }

        private bool Transfer(InventoryController source, InventoryController target, int sourceIndex, int targetIndex = -1)
        {
            if (source == null || target == null)
                return false;

            return source.TryTransferItemTo(target, sourceIndex, targetIndex);
        }

        private InventoryController ResolveController(InventoryPanelUI panel)
        {
            if (panel == null)
                return null;

            if (panel == playerPanel)
                return playerController;

            if (panel == containerPanel)
                return containerController;

            return panel.GetInventoryController();
        }

        public void TakeAll()
        {
            if (playerController?.Inventory == null || containerController?.Inventory == null)
                return;

            var containerInventory = containerController.Inventory;
            bool movedAny = false;

            for (int i = 0; i < containerInventory.SlotCount; i++)
            {
                var slot = containerInventory.Slots[i];
                if (slot == null || slot.IsEmpty)
                    continue;

                while (Transfer(containerController, playerController, i))
                {
                    movedAny = true;
                    if (slot.IsEmpty)
                        break;
                }
            }

            if (movedAny)
                Refresh();
        }

        private void TryCloseIfContainerEmpty()
        {
            if (!autoCloseWhenEmpty || !isOpen || containerController?.Inventory == null)
                return;

            foreach (var slot in containerController.Inventory.Slots)
            {
                if (slot != null && !slot.IsEmpty)
                    return;
            }

            Close();
        }

        private void SetRootVisible(bool visible)
        {
            if (root != null)
                root.SetActive(visible);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.interactable = visible;
                canvasGroup.blocksRaycasts = visible;
            }

            if (!visible && manageCursor)
                ReleaseCursor();
        }

        private void HideImmediate()
        {
            if (root != null)
                root.SetActive(false);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            if (manageCursor)
                ReleaseCursor();
        }

        private void CaptureCursor()
        {
            if (cursorCaptured)
                return;

            cursorCaptured = true;
            previousCursorLock = Cursor.lockState;
            previousCursorVisible = Cursor.visible;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void ReleaseCursor()
        {
            if (!cursorCaptured)
                return;

            cursorCaptured = false;
            Cursor.lockState = previousCursorLock;
            Cursor.visible = previousCursorVisible;
        }
    }
}
