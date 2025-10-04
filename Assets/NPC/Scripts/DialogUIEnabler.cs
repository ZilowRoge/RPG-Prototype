using UnityEngine;

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
            if(!dialogUI.gameObject.activeSelf) {
                dialogUI.gameObject.SetActive(true);
                dialogUI.Begin(dialogAsset);
            } else {
                dialogUI.Close();
                dialogUI.gameObject.SetActive(false);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.name.Equals("Player") && dialogUI != null && dialogUI.gameObject.activeSelf)
            {
                dialogUI.Close();
                dialogUI.gameObject.SetActive(false);
            }
        }
    }
}