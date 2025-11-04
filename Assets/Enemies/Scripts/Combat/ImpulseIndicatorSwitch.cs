using UnityEngine;

namespace Enemies.Combat
{
    /// <summary>
    /// Włącza obiekt ładowania na starcie, a po upływie zadanego czasu przełącza na obiekt impulsu.
    /// </summary>
    public class ImpulseIndicatorSwitch : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject chargeObject;
        [SerializeField] private GameObject impactObject;

        [Header("Settings")]
        [SerializeField, Tooltip("Czas po którym uruchamiany jest obiekt impulsu.")]
        private float triggerDelay = 0.5f;

        private void Start()
        {
            if (chargeObject != null)
                chargeObject.SetActive(true);

            if (impactObject != null)
                impactObject.SetActive(false);

            if (triggerDelay > 0f && impactObject != null)
                Invoke(nameof(TriggerImpact), triggerDelay);
            else
                TriggerImpact();
        }

        /// <summary>
        /// Wyłącza obiekt ładowania i włącza obiekt impulsu.
        /// </summary>
        public void TriggerImpact()
        {
            if (chargeObject != null)
                chargeObject.SetActive(false);

            if (impactObject != null)
                impactObject.SetActive(true);
        }
    }
}
