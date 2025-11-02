using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Player.Statistics;
using Player.Targeting;

namespace UI.Player.Target
{
    /// <summary>
    /// Displays the currently selected target's name and health using a fill image.
    /// </summary>
    public class TargetHealthUI : MonoBehaviour
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
        private StatsController currentStats;
        private float nextUpdateTime;
        private bool hasTarget;

        private void Awake()
        {
            if (targetSelector == null)
                targetSelector = FindFirstObjectByType<TargetSelector>();
        }

        private void OnEnable()
        {
            SynchroniseTarget(targetSelector != null ? targetSelector.CurrentTarget : null);
            nextUpdateTime = 0f;
            hasTarget = currentStats != null;
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
            currentStats = target != null ? target.GetComponentInParent<StatsController>() : null;
            hasTarget = currentStats != null;
            UpdateVisibility();
            RefreshUI();
        }

        private void UpdateVisibility()
        {
            bool visible = currentStats != null;

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
            if (currentStats == null)
            {
                if (fillImage != null) fillImage.fillAmount = 0f;
                if (label != null) label.text = string.Empty;
                hasTarget = false;
                UpdateVisibility();
                return;
            }

            float currentHealth = Mathf.Max(0f, currentStats.CurrentHealth);
            float maxHealth = Mathf.Max(1f, currentStats.maxHealth);
            string displayName = currentStats.gameObject.name;

            if (fillImage != null)
                fillImage.fillAmount = Mathf.Clamp01(currentHealth / maxHealth);

            if (label != null)
                label.text = $"{displayName}";// {Mathf.CeilToInt(currentHealth)}/{Mathf.CeilToInt(maxHealth)}";
        }
    }
}
