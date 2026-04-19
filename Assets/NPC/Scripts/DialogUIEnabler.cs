using UnityEngine;
using Common.World.Interaction;

namespace NPC {
    public class DialogUIEnabler : MonoBehaviour, IInteractable
    {
        [SerializeField] private MonoBehaviour dialogPresenterSource;
        [SerializeField] private Dialog.DialogAsset dialogAsset;
        [SerializeField] private InteractionMode supportedModes = InteractionMode.Both;
        [SerializeField] private InteractionTooltip tooltip;

        private Dialog.IDialogPresenter dialogUI;

        public InteractionMode SupportedModes => supportedModes;
        public InteractionTooltip Tooltip => tooltip = InteractionTooltipResolver.Resolve(this, tooltip);

        private void Awake()
        {
            CachePresenter();
            if (dialogUI != null)
                dialogUI.Close();
        }

        public void Interact(GameObject gameObject)
        {
            CachePresenter();
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
            CachePresenter();
            if (dialogUI == null) return;
            var isPlayer = other != null && other.GetComponentInParent<Transform>()?.CompareTag("Player") == true;
            if (!isPlayer) return;
            if (dialogUI.IsOpen)
                dialogUI.Close();
        }

        private void CachePresenter()
        {
            dialogUI = dialogPresenterSource as Dialog.IDialogPresenter;
        }
    }
}
