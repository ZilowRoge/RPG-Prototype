using System;
using Player;
using Player.FightSystem.Magic;
using Player.Progress;
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

        private SymbolInputManager inputManager;
        private bool awaiting;
        private Interactor activeInteractor;
        private string pendingSymbolId;
        public string LastRecognizedSymbol { get; private set; }

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
                var progress = player.GetComponentInParent<ProgressController>() ??
                               FindFirstObjectByType<ProgressController>();
                if (progress == null || !progress.KnowsSymbol(requiredSymbolId))
                    return;
            }

            inputManager = player.GetComponentInChildren<SymbolInputManager>() ??
                           player.GetComponent<SymbolInputManager>() ??
                           FindFirstObjectByType<SymbolInputManager>();
            if (inputManager == null)
                return;

            activeInteractor = player.GetComponentInParent<Interactor>();
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

            if (activeInteractor != null && other.GetComponentInParent<Interactor>() == activeInteractor)
                Cancel();
        }

        private void Restore()
        {
            if (inputManager != null)
                inputManager.ResetToDefaultConsumer();
            inputManager = null;
            activeInteractor = null;
            pendingSymbolId = null;
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
