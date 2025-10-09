using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Player.Statistics;
using Player.Targeting;

namespace UI.Player.Target
{
    public class TargetHpUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TargetSelector targetSelector;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Slider healthBar;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text hpLabel;

        [Header("Behavior")]
        [SerializeField] private float updatesPerSecond = 10f; // 10 Hz
        [SerializeField] private bool hideWhenNoTarget = true;

        private StatsController currentStats;
        private Transform lastTarget;
        private float nextHpUpdateTime;

        private void Awake()
        {
            if (targetSelector == null)
                targetSelector = FindObjectOfType<TargetSelector>();
        }

        private void OnEnable()
        {
            lastTarget = null;
            nextHpUpdateTime = 0f;
            HandleTargetChanged(targetSelector != null ? targetSelector.CurrentTarget : null);
        }

        private void OnDisable() { }

        private void HandleTargetChanged(Transform t)
        {
            currentStats = t != null ? t.GetComponent<StatsController>() : null;
            UpdateVisibility();
            // Immediate UI refresh on target change
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
                // Hide only visual children, keep this component active to detect target changes
                if (healthBar != null) healthBar.gameObject.SetActive(visible);
                if (nameLabel != null) nameLabel.gameObject.SetActive(visible);
                if (hpLabel != null) hpLabel.gameObject.SetActive(visible);
            }
        }

        private void Update()
        {
            var current = targetSelector != null ? targetSelector.CurrentTarget : null;
            if (current != lastTarget)
            {
                HandleTargetChanged(current);
                lastTarget = current;
                // Force immediate refresh on change
                nextHpUpdateTime = 0f;
            }

            if (Time.time >= nextHpUpdateTime)
            {
                RefreshUI();
                float interval = updatesPerSecond > 0f ? 1f / updatesPerSecond : 0.1f;
                nextHpUpdateTime = Time.time + interval;
            }
        }

        private void RefreshUI()
        {
            if (currentStats == null)
            {
                if (healthBar != null)
                {
                    healthBar.value = 0f;
                    healthBar.maxValue = 1f;
                }
                if (hpLabel != null) hpLabel.text = string.Empty;
                if (nameLabel != null) nameLabel.text = string.Empty;
                return;
            }

            float cur = currentStats.CurrentHealth;
            float max = currentStats.maxHealth;

            if (healthBar != null)
            {
                healthBar.maxValue = Mathf.Max(1f, max);
                healthBar.value = Mathf.Clamp(cur, 0f, healthBar.maxValue);
            }

            if (hpLabel != null)
                hpLabel.text = $"{Mathf.CeilToInt(cur)}/{Mathf.CeilToInt(max)}";

            if (nameLabel != null)
                nameLabel.text = currentStats.gameObject.name;
        }
    }
}
