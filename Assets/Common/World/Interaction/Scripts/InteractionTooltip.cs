using Player;
using TMPro;
using UnityEngine;

namespace Common.World.Interaction
{
    [AddComponentMenu("Game/World/Interaction/Interaction Tooltip")]
    [RequireComponent(typeof(Collider))]
    public class InteractionTooltip : MonoBehaviour
    {
        [Header("Tooltip")]
        [SerializeField] private GameObject tooltipRoot;
        [SerializeField] private TMP_Text tooltipLabel;
        [SerializeField] private string message = "Nacisnij [E], aby wejsc w interakcje";

        private int activeInteractors;

        private void Reset()
        {
            var col = GetComponent<Collider>();
            if (col != null)
                col.isTrigger = true;

            if (tooltipRoot == null && tooltipLabel != null)
                tooltipRoot = tooltipLabel.gameObject;
        }

        private void Awake()
        {
            var col = GetComponent<Collider>();
            if (col != null && !col.isTrigger)
                col.isTrigger = true;

            HideTooltip();
        }

        private void OnEnable()
        {
            HideTooltip();
        }

        private void OnDisable()
        {
            activeInteractors = 0;
            HideTooltip();
        }

        public void HideAfterInteraction()
        {
            HideTooltip();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsPlayer(other)) return;
            activeInteractors++;
            ShowTooltip();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsPlayer(other)) return;
            activeInteractors = Mathf.Max(0, activeInteractors - 1);
            if (activeInteractors == 0)
                HideTooltip();
        }

        private void ShowTooltip()
        {
            if (tooltipLabel != null)
                tooltipLabel.text = message;

            if (tooltipRoot != null)
            {
                tooltipRoot.SetActive(true);
                return;
            }

            if (tooltipLabel != null)
                tooltipLabel.enabled = true;
        }

        private void HideTooltip()
        {
            if (tooltipRoot != null)
            {
                tooltipRoot.SetActive(false);
                return;
            }

            if (tooltipLabel != null)
                tooltipLabel.enabled = false;
        }

        private static bool IsPlayer(Component c)
        {
            if (c == null) return false;
            if (c.GetComponentInParent<Interactor>() != null) return true;
            return string.Equals(c.gameObject.name, "Player");
        }
    }
}
