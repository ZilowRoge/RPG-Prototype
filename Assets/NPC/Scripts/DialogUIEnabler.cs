using UnityEngine;
using Common.World.Interaction;
using Player;

namespace NPC {
    public class DialogUIEnabler : MonoBehaviour, IInteractable
    {
        [SerializeField] private UI.NPC.Dialog.DialogControllerUI dialogUI;
        [SerializeField] private Dialog.DialogAsset dialogAsset;

        private void Awake()
        {
            if (dialogUI)
                dialogUI.Close();
        }

        public void Interact(GameObject gameObject)
        {
            if (dialogUI == null)
                return;
            // Toggle using DialogControllerUI state (parent-managed)
            if(!dialogUI.IsOpen) {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                dialogUI.Begin(dialogAsset);
            } else {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                dialogUI.Close();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (dialogUI == null) return;
            var isPlayer = other != null && other.GetComponentInParent<Interactor>() != null;
            if (!isPlayer) return;
            if (dialogUI.IsOpen)
                dialogUI.Close();
        }
    }
}
