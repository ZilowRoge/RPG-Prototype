using TMPro;
using UnityEngine;

namespace Common.World.Interaction
{
    [AddComponentMenu("Game/World/Interaction/Interaction Tooltip")]
    public class InteractionTooltip : MonoBehaviour
    {
        private const string TooltipTag = "Tooltip";

        [Header("Tooltip")]
        [SerializeField] private GameObject tooltipRoot;
        [SerializeField] private TMP_Text tooltipLabel;
        [SerializeField] private string message = "Nacisnij [E], aby wejsc w interakcje";
        [SerializeField] private Color normalTextColor = Color.white;
        [SerializeField] private Color blockedTextColor = new Color(1f, 0.55f, 0.55f, 1f);
        [SerializeField] private bool logDebug;
        [SerializeField] private bool logWarnings = true;

        private GameObject currentPlayer;
        private bool isVisible;
        private bool loggedMissingTooltipTag;
        private bool loggedMissingTooltipLabel;

        private void Reset()
        {
            AutoAssignTooltipReferences();
        }

        private void Awake()
        {
            AutoAssignTooltipReferences();

            HideTooltip();
        }

        private void OnEnable()
        {
            HideTooltip();
        }

        private void OnDisable()
        {
            currentPlayer = null;
            isVisible = false;
            HideTooltip();
        }

        private void Update()
        {
            if (!isVisible || !IsTooltipVisible())
                return;

            RefreshTooltipState();
        }

        public void ShowFor(GameObject player)
        {
            currentPlayer = player;
            isVisible = true;
            LogDebug($"ShowFor called. player={(player != null ? player.name : "null")}, hasRoot={tooltipRoot != null}, hasLabel={tooltipLabel != null}");
            ShowTooltip();
        }

        public void RefreshFor(GameObject player)
        {
            currentPlayer = player;
            if (!isVisible)
            {
                LogDebug("RefreshFor ignored because tooltip is not marked visible.");
                return;
            }

            LogDebug($"RefreshFor called. player={(player != null ? player.name : "null")}, rootActive={(tooltipRoot != null && tooltipRoot.activeSelf)}, labelEnabled={(tooltipLabel != null && tooltipLabel.enabled)}");
            RefreshTooltipState();
        }

        public void HideFor(GameObject player = null)
        {
            if (player != null && currentPlayer != null && !ReferenceEquals(player, currentPlayer))
            {
                LogDebug($"HideFor ignored due to player mismatch. requested={(player != null ? player.name : "null")}, current={(currentPlayer != null ? currentPlayer.name : "null")}");
                return;
            }

            currentPlayer = null;
            isVisible = false;
            LogDebug("HideFor accepted.");
            HideTooltip();
        }

        public void HideAfterInteraction()
        {
            HideFor();
        }

        private void ShowTooltip()
        {
            EnsureTooltipReferences();
            RefreshTooltipState();
            EnsureParentChainIsActive();

            var canvasGroup = tooltipRoot != null ? tooltipRoot.GetComponent<CanvasGroup>() : null;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            if (tooltipLabel != null)
                tooltipLabel.enabled = true;

            if (tooltipRoot != null)
            {
                tooltipRoot.SetActive(true);
                LogDebug($"ShowTooltip: tooltipRoot active. root={tooltipRoot.name}, activeSelf={tooltipRoot.activeSelf}, activeInHierarchy={tooltipRoot.activeInHierarchy}, label={(tooltipLabel != null ? tooltipLabel.name : "null")}");
                return;
            }

            if (tooltipLabel != null)
            {
                LogDebug($"ShowTooltip: tooltipLabel enabled. label={tooltipLabel.name}");
                return;
            }

            LogMissingTooltip("ShowTooltip");
        }

        private void HideTooltip()
        {
            EnsureTooltipReferences();

            var canvasGroup = tooltipRoot != null ? tooltipRoot.GetComponent<CanvasGroup>() : null;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            if (tooltipLabel != null)
                tooltipLabel.enabled = false;

            if (tooltipRoot != null)
            {
                tooltipRoot.SetActive(false);
                LogDebug($"HideTooltip: tooltipRoot inactive. root={tooltipRoot.name}, activeSelf={tooltipRoot.activeSelf}, activeInHierarchy={tooltipRoot.activeInHierarchy}");
                return;
            }

            if (tooltipLabel != null)
            {
                LogDebug($"HideTooltip: tooltipLabel disabled. label={tooltipLabel.name}");
                return;
            }

            LogMissingTooltip("HideTooltip");
        }

        private void RefreshTooltipState()
        {
            EnsureTooltipReferences();
            var state = ResolveTooltipState();
            var resolvedMessage = ResolveTooltipMessage(state);

            if (tooltipLabel != null)
            {
                tooltipLabel.text = resolvedMessage;
                tooltipLabel.color = state.IsBlocked ? blockedTextColor : normalTextColor;
                LogDebug($"RefreshTooltipState: message='{resolvedMessage}', blocked={state.IsBlocked}");
            }
            else
            {
                LogDebug("RefreshTooltipState: tooltipLabel is null.");
            }
        }

        private void AutoAssignTooltipReferences()
        {
            if (tooltipLabel != null && tooltipRoot == null)
                tooltipRoot = ResolveRootFromLabel(tooltipLabel);

            if (tooltipRoot != null && tooltipLabel == null)
            {
                tooltipLabel = tooltipRoot.GetComponent<TMP_Text>();
                if (tooltipLabel == null)
                    tooltipLabel = tooltipRoot.GetComponentInChildren<TMP_Text>(true);
            }

            if (tooltipRoot != null && tooltipLabel != null)
            {
                LogDebug($"AutoAssignTooltipReferences: root={(tooltipRoot != null ? tooltipRoot.name : "null")}, label={(tooltipLabel != null ? tooltipLabel.name : "null")}");
                return;
            }

            var tooltipObject = GameObject.FindGameObjectWithTag(TooltipTag);
            if (tooltipObject == null)
                tooltipObject = FindTooltipObjectByTagIncludingInactive();

            if (tooltipObject == null)
            {
                LogMissingTooltipTag();
                return;
            }

            if (tooltipLabel == null)
            {
                tooltipLabel = tooltipObject.GetComponent<TMP_Text>();
                if (tooltipLabel == null)
                    tooltipLabel = tooltipObject.GetComponentInChildren<TMP_Text>(true);
            }

            if (tooltipRoot == null && tooltipLabel != null && tooltipLabel.gameObject != tooltipObject)
                tooltipRoot = tooltipObject;

            if (tooltipRoot == null && tooltipLabel != null)
                tooltipRoot = ResolveRootFromLabel(tooltipLabel);

            LogDebug($"AutoAssignTooltipReferences: root={(tooltipRoot != null ? tooltipRoot.name : "null")}, label={(tooltipLabel != null ? tooltipLabel.name : "null")}");
        }

        private void EnsureTooltipReferences()
        {
            if (tooltipRoot == null || tooltipLabel == null)
                AutoAssignTooltipReferences();
        }

        private void EnsureParentChainIsActive()
        {
            if (tooltipRoot == null)
                return;

            var current = tooltipRoot.transform;
            while (current != null)
            {
                if (!current.gameObject.activeSelf)
                    current.gameObject.SetActive(true);

                if (current.GetComponent<Canvas>() != null)
                    break;

                current = current.parent;
            }
        }

        private static GameObject ResolveRootFromLabel(TMP_Text label)
        {
            if (label == null)
                return null;

            var canvasGroup = label.GetComponentInParent<CanvasGroup>();
            if (canvasGroup != null)
                return canvasGroup.gameObject;

            return label.gameObject;
        }

        private static GameObject FindTooltipObjectByTagIncludingInactive()
        {
            var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var obj in allObjects)
            {
                if (obj == null)
                    continue;

                if (!obj.CompareTag(TooltipTag))
                    continue;

                if (obj.hideFlags != HideFlags.None)
                    continue;

                return obj;
            }

            return null;
        }

        private bool IsTooltipVisible()
        {
            if (tooltipRoot != null)
                return tooltipRoot.activeSelf;

            return tooltipLabel != null && tooltipLabel.enabled;
        }

        private InteractionTooltipState ResolveTooltipState()
        {
            var provider = ResolveTooltipProvider();
            if (provider == null)
                return new InteractionTooltipState(message, false);

            return provider.GetTooltipState(currentPlayer);
        }

        private string ResolveTooltipMessage(InteractionTooltipState state)
        {
            if (!string.IsNullOrWhiteSpace(state.Message))
                return state.Message;

            return message;
        }

        private IInteractionTooltipProvider ResolveTooltipProvider()
        {
            return GetComponent<IInteractionTooltipProvider>()
                ?? GetComponentInParent<IInteractionTooltipProvider>()
                ?? GetComponentInChildren<IInteractionTooltipProvider>(true);
        }

        private void LogMissingTooltip(string context)
        {
            if (!logWarnings || loggedMissingTooltipLabel)
                return;

            Debug.LogWarning($"[InteractionTooltip] {context}: tooltipRoot and tooltipLabel are missing.", this);
            loggedMissingTooltipLabel = true;
        }

        private void LogMissingTooltipTag()
        {
            if (!logWarnings || loggedMissingTooltipTag)
                return;

            Debug.LogWarning($"[InteractionTooltip] Tooltip tag '{TooltipTag}' not found. Assign tooltipRoot/tooltipLabel manually.", this);
            loggedMissingTooltipTag = true;
        }

        private void LogDebug(string message)
        {
            if (!logDebug)
                return;

            Debug.Log($"[InteractionTooltip] {message}", this);
        }
    }
}
