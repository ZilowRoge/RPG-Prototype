using Player;
using TMPro;
using UnityEngine;

namespace Common.World.Interaction
{
    [AddComponentMenu("Game/World/Interaction/Interaction Tooltip")]
    [RequireComponent(typeof(Collider))]
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

        private int activeInteractors;
        private GameObject currentPlayer;
        private bool loggedMissingTooltipTag;
        private bool loggedMissingTooltipLabel;

        private void Reset()
        {
            var col = GetComponent<Collider>();
            if (col != null)
                col.isTrigger = true;

            AutoAssignTooltipReferences();
        }

        private void Awake()
        {
            AutoAssignTooltipReferences();

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
            currentPlayer = null;
            HideTooltip();
        }

        private void Update()
        {
            if (activeInteractors <= 0 || !IsTooltipVisible())
                return;

            RefreshTooltipState();
        }

        public void HideAfterInteraction()
        {
            HideTooltip();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsPlayer(other))
            {
                LogDebug($"OnTriggerEnter ignored: {other.name}");
                return;
            }
            activeInteractors++;
            currentPlayer = ResolvePlayer(other);
            LogDebug($"OnTriggerEnter player: {other.name}, activeInteractors={activeInteractors}");
            ShowTooltip();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsPlayer(other))
            {
                LogDebug($"OnTriggerExit ignored: {other.name}");
                return;
            }
            activeInteractors = Mathf.Max(0, activeInteractors - 1);
            LogDebug($"OnTriggerExit player: {other.name}, activeInteractors={activeInteractors}");
            if (activeInteractors == 0)
            {
                currentPlayer = null;
                HideTooltip();
            }
        }

        private void ShowTooltip()
        {
            EnsureTooltipReferences();
            RefreshTooltipState();

            if (tooltipLabel != null)
                tooltipLabel.enabled = true;

            if (tooltipRoot != null)
            {
                tooltipRoot.SetActive(true);
                LogDebug("ShowTooltip: tooltipRoot active.");
                return;
            }

            if (tooltipLabel != null)
            {
                LogDebug("ShowTooltip: tooltipLabel enabled.");
                return;
            }

            LogMissingTooltip("ShowTooltip");
        }

        private void HideTooltip()
        {
            EnsureTooltipReferences();
            if (tooltipLabel != null)
                tooltipLabel.enabled = false;

            if (tooltipRoot != null)
            {
                tooltipRoot.SetActive(false);
                LogDebug("HideTooltip: tooltipRoot inactive.");
                return;
            }

            if (tooltipLabel != null)
            {
                LogDebug("HideTooltip: tooltipLabel disabled.");
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

        private static bool IsPlayer(Component c)
        {
            if (c == null) return false;
            if (c.GetComponentInParent<Interactor>() != null) return true;
            return string.Equals(c.gameObject.name, "Player");
        }

        private GameObject ResolvePlayer(Component component)
        {
            if (component == null)
                return currentPlayer;

            var interactor = component.GetComponentInParent<Interactor>();
            if (interactor != null)
                return interactor.gameObject;

            return IsPlayer(component) ? component.gameObject : currentPlayer;
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
