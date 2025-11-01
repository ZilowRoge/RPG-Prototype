using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Systems.Jobs;
using Systems.Perks;

namespace UI.Player.Perks
{
    /// <summary>
    /// Simple tooltip presenter for perk nodes. Expects to be wired to a panel with title/description labels.
    /// </summary>
    public class PerkTooltipUI : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private RectTransform container;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text descriptionText;

        [Header("Layout")]
        [SerializeField] private Vector2 pointerOffset = new(20f, -20f);

        [Header("Content")]
        [SerializeField] private bool includeEffectList = true;
        [SerializeField] private string effectsLabel = "Effects: ";
        [SerializeField] private string emptyDescriptionFallback = "No description provided.";

        private Canvas rootCanvas;

        private void Awake()
        {
            rootCanvas = GetComponentInParent<Canvas>();
            Hide();
        }

        public void Show(JobPerkNode node, Vector2 screenPosition)
        {
            if (container == null || node?.Perk == null)
            {
                Hide();
                return;
            }

            var perk = node.Perk;
            if (titleText != null)
            {
                var label = string.IsNullOrWhiteSpace(perk.perkName)
                    ? node.name
                    : perk.perkName;
                titleText.text = label;
            }

            if (descriptionText != null)
            {
                descriptionText.text = BuildDescription(perk);
            }

            UpdatePosition(screenPosition);

            if (!container.gameObject.activeSelf)
                container.gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (container != null && container.gameObject.activeSelf)
                container.gameObject.SetActive(false);
        }

        private string BuildDescription(PerkData perk)
        {
            if (perk == null)
                return string.Empty;

            var description = string.IsNullOrWhiteSpace(perk.description)
                ? emptyDescriptionFallback
                : perk.description;

            if (!includeEffectList || perk.effects == null || perk.effects.Count == 0)
                return description;

            var effectDescriptions = new List<string>();
            foreach (var effect in perk.effects)
            {
                if (effect == null)
                    continue;

                var text = effect.GetEffectString();
                if (!string.IsNullOrWhiteSpace(text))
                    effectDescriptions.Add(text);
            }

            if (effectDescriptions.Count == 0)
                return description;

            var joined = string.Join("\n", effectDescriptions);
            if (!string.IsNullOrWhiteSpace(effectsLabel))
                joined = $"{effectsLabel.Trim()}\n{joined}";

            return string.IsNullOrWhiteSpace(description)
                ? joined
                : $"{description}\n\n{joined}";
        }

        private void UpdatePosition(Vector2 screenPosition)
        {
            if (container == null)
                return;

            var targetScreenPoint = screenPosition + pointerOffset;

            var parentRect = container.parent as RectTransform;
            if (parentRect == null)
            {
                var cameraForWorld = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                    ? rootCanvas.worldCamera
                    : null;

                RectTransformUtility.ScreenPointToWorldPointInRectangle(container, targetScreenPoint, cameraForWorld, out var worldPoint);
                container.position = worldPoint;
                return;
            }

            var camera = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? rootCanvas.worldCamera
                : null;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, targetScreenPoint, camera, out var localPoint);
            localPoint.y -= parentRect.rect.height * 0.5f;
            container.anchoredPosition = localPoint;
        }
    }
}
