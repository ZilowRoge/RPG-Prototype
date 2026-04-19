using System;
using Common.Symbols;
using Player.Interfaces;
using UnityEngine;
using UnityEngine.Events;
using Common.World.Interaction;

namespace Common.World.Orb
{
    /// <summary>
    /// Handles symbol input for an orb and emits events when the correct symbol is drawn.
    /// </summary>
    [AddComponentMenu("Game/World/Orb/Symbol Orb Trigger")]
    public class SymbolOrbTrigger : MonoBehaviour, IInteractable, ISymbolConsumer, ICancelableSymbolFlow
    {
        [SerializeField] private string requiredSymbolId = string.Empty;
        [SerializeField] private bool requireKnownSymbol = true;
        [SerializeField] private bool autoCancelOnPlayerExit = true;
        [SerializeField] private UnityEvent onSymbolMatched;
        [SerializeField] private InteractionMode supportedModes = InteractionMode.Both;
        [SerializeField] private InteractionTooltip tooltip;

        [SerializeField] private MonoBehaviour inputManagerSource;
        [SerializeField] private MonoBehaviour progressSource;
        private ISymbolInputRouter inputManager;
        private IDialogueProgressContext progress;
        private bool awaiting;
        private GameObject activePlayer;
        private string pendingSymbolId;
        public string LastRecognizedSymbol { get; private set; }
        public InteractionMode SupportedModes => supportedModes;
        public InteractionTooltip Tooltip => tooltip = InteractionTooltipResolver.Resolve(this, tooltip);

        public void Interact(GameObject player)
        {
            BeginAwait(player);
        }

        /// <summary>
        /// Begins awaiting the player to draw the required symbol.
        /// </summary>
        public void BeginAwait(GameObject player)
        {
            if (awaiting)
                return;

            if (player == null)
                return;

            if (requireKnownSymbol && !string.IsNullOrEmpty(requiredSymbolId))
            {
                progress = ResolveProgress(player);
                if (progress == null || !progress.KnowsSymbol(requiredSymbolId))
                    return;
            }

            inputManager = ResolveInputManager(player);
            if (inputManager == null)
                return;

            activePlayer = player;
            awaiting = true;
            inputManager.SetActiveConsumer(this);
        }

        public void Cancel()
        {
            if (!awaiting)
                return;

            awaiting = false;
            Restore();
        }

        public void OnSymbolRecognized(string recognizedId)
        {
            if (!awaiting)
                return;

            pendingSymbolId = recognizedId;
        }

        public void OnSymbolSequenceCommitted()
        {
            if (!awaiting)
                return;

            var expected = requiredSymbolId?.Trim();
            var recognized = pendingSymbolId?.Trim();
            pendingSymbolId = null;

            if (string.IsNullOrWhiteSpace(recognized))
                return;

            bool matched = !string.IsNullOrWhiteSpace(expected) &&
                           string.Equals(expected, recognized, StringComparison.OrdinalIgnoreCase);

            if (!matched &&
                TryExtractNumericId(recognized, out int recNum) &&
                TryExtractNumericId(expected, out int expNum))
            {
                matched = recNum == expNum;
            }

            if (!matched)
                return;

            awaiting = false;
            Restore();
            LastRecognizedSymbol = recognized;
            onSymbolMatched?.Invoke();
        }

        public void CancelSymbolFlow()
        {
            Cancel();
        }

        private void OnDisable()
        {
            Cancel();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!autoCancelOnPlayerExit || !awaiting)
                return;

            if (other == null)
                return;

            if (activePlayer != null && other.GetComponentInParent<Transform>()?.gameObject == activePlayer)
                Cancel();
        }

        private void Restore()
        {
            if (inputManager != null)
                inputManager.ResetToDefaultConsumer();
            inputManager = null;
            progress = null;
            activePlayer = null;
            pendingSymbolId = null;
        }

        private ISymbolInputRouter ResolveInputManager(GameObject player)
        {
            if (inputManagerSource is ISymbolInputRouter router)
                return router;

            if (player != null)
            {
                var playerComponents = player.GetComponentsInChildren<MonoBehaviour>(true);
                for (int i = 0; i < playerComponents.Length; i++)
                {
                    if (playerComponents[i] is ISymbolInputRouter playerRouter)
                    {
                        inputManagerSource = playerComponents[i];
                        return playerRouter;
                    }
                }
            }

            var candidates = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include);
            for (int i = 0; i < candidates.Length; i++)
            {
                if (candidates[i] is ISymbolInputRouter candidateRouter)
                {
                    inputManagerSource = candidates[i];
                    return candidateRouter;
                }
            }

            return null;
        }

        private IDialogueProgressContext ResolveProgress(GameObject player)
        {
            if (progressSource is IDialogueProgressContext progressContext)
                return progressContext;

            if (player != null)
            {
                var playerComponents = player.GetComponentsInParent<MonoBehaviour>(true);
                for (int i = 0; i < playerComponents.Length; i++)
                {
                    if (playerComponents[i] is IDialogueProgressContext playerProgress)
                    {
                        progressSource = playerComponents[i];
                        return playerProgress;
                    }
                }
            }

            var candidates = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include);
            for (int i = 0; i < candidates.Length; i++)
            {
                if (candidates[i] is IDialogueProgressContext candidateProgress)
                {
                    progressSource = candidates[i];
                    return candidateProgress;
                }
            }

            return null;
        }

        private static bool TryExtractNumericId(string value, out int numericId)
        {
            numericId = -1;
            if (string.IsNullOrWhiteSpace(value))
                return false;

            value = value.Trim();
            if (int.TryParse(value, out numericId))
                return true;

            int length = value.Length;
            var buffer = new char[length];
            int bufferIndex = 0;
            for (int i = 0; i < length; i++)
            {
                char c = value[i];
                if (char.IsDigit(c))
                    buffer[bufferIndex++] = c;
            }

            if (bufferIndex == 0)
                return false;

            return int.TryParse(new string(buffer, 0, bufferIndex), out numericId);
        }
    }
}

