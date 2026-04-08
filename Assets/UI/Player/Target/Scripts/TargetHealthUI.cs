using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Player.Interfaces;
using Player.Targeting;
using UI.Player.Common;

namespace UI.Player.Target
{
    /// <summary>
    /// Displays the currently selected target's name and health using a fill image.
    /// </summary>
    public class TargetHealthUI : MonoBehaviour, IPlayerReferenceReceiver
    {
        [Header("References")]
        [SerializeField] private TargetSelector targetSelector;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Image fillImage;

        [Header("Behaviour")]
        [SerializeField, Min(0.1f)] private float updatesPerSecond = 10f;
        [SerializeField] private bool hideWhenNoTarget = true;

        private Transform cachedTarget;
        private IHealthProvider currentHealth;
        private Component currentHealthComponent;
        private float nextUpdateTime;
        private bool hasTarget;

        private void Awake()
        {
            if (targetSelector == null)
                targetSelector = FindAnyObjectByType<TargetSelector>();
        }

        private void OnEnable()
        {
            SynchroniseTarget(targetSelector != null ? targetSelector.CurrentTarget : null);
            nextUpdateTime = 0f;
            hasTarget = currentHealth != null;
        }

        private void Update()
        {
            var selected = targetSelector != null ? targetSelector.CurrentTarget : null;
            if (selected == null && hasTarget)
            {
                SynchroniseTarget(null);
                nextUpdateTime = 0f;
            }
            else if (selected != cachedTarget)
            {
                SynchroniseTarget(selected);
                nextUpdateTime = 0f;
            }

            if (Time.time >= nextUpdateTime)
            {
                RefreshUI();
                float interval = Mathf.Approximately(updatesPerSecond, 0f) ? 0.1f : 1f / updatesPerSecond;
                nextUpdateTime = Time.time + interval;
            }
        }

        private void SynchroniseTarget(Transform target)
        {
            cachedTarget = target;
            if (target != null)
            {
                currentHealth = target.GetComponentInParent<IHealthProvider>();
                currentHealthComponent = currentHealth as Component;
                if (currentHealth == null)
                {
                    Debug.LogWarning($"[TargetHealthUI] Target '{target.name}' is missing IHealthProvider in parent chain.", this);
                }
            }
            else
            {
                currentHealth = null;
                currentHealthComponent = null;
            }

            hasTarget = currentHealth != null;
            UpdateVisibility();
            RefreshUI();
        }

        private void UpdateVisibility()
        {
            bool visible = currentHealth != null;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.interactable = visible;
                canvasGroup.blocksRaycasts = visible;
                return;
            }

            if (hideWhenNoTarget)
            {
                if (label != null) label.gameObject.SetActive(visible);
                if (fillImage != null) fillImage.gameObject.SetActive(visible);
            }
        }

        private void RefreshUI()
        {
            if (currentHealth == null)
            {
                if (fillImage != null) fillImage.fillAmount = 0f;
                if (label != null) label.text = string.Empty;
                hasTarget = false;
                UpdateVisibility();
                return;
            }

            float currentValue = Mathf.Max(0f, currentHealth.CurrentHealth);
            float maxValue = Mathf.Max(1f, currentHealth.maxHealth);
            var displayTransform = currentHealthComponent != null ? currentHealthComponent.transform : cachedTarget;
            string displayName = displayTransform != null ? displayTransform.gameObject.name : string.Empty;

            if (fillImage != null)
                fillImage.fillAmount = Mathf.Clamp01(currentValue / maxValue);

            if (label != null)
                label.text = $"{displayName}";
        }

        public void BindPlayerReferences(PlayerUIReferences refs)
        {
            targetSelector = refs.TargetSelector;
            if (targetSelector == null)
                targetSelector = FindAnyObjectByType<TargetSelector>();

            SynchroniseTarget(targetSelector != null ? targetSelector.CurrentTarget : null);
            nextUpdateTime = 0f;
            hasTarget = currentHealth != null;
        }
    }
}

