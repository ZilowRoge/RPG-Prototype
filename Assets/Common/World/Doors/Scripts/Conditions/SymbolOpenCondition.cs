using UnityEngine;
using Player.FightSystem.Magic;
using Player.Progress;

namespace Common.Systems.SymbolTraining
{
    [AddComponentMenu("Game/World/Doors/Conditions/Symbol Required")] 
    public class SymbolOpenCondition : MonoBehaviour, IDoorOpenCondition, ISymbolConsumer
    {
        [SerializeField] private string requiredSymbolId = string.Empty;
        [Header("Requirements")]
        [Tooltip("If enabled, player must already know this symbol to attempt opening.")]
        [SerializeField] private bool requireKnownSymbol = true;
        [SerializeField] private DoorController door;
        private SymbolInputManager inputManager;
        private bool awaiting;

        public string Id => requiredSymbolId?.Trim();

        public bool CanOpen(GameObject player)
        {
            // Start symbol drawing on first interaction; InteractiveDoor will not open immediately.
            Debug.Log("[SymbolOpenCondition] Checking condition for doors");
            if (awaiting)
            {
                Debug.Log("[SymbolOpenCondition] Already awaiting symbol input.", this);
                return false;
            }

            if (requireKnownSymbol && !string.IsNullOrEmpty(requiredSymbolId))
            {
                ProgressController progress = null;
                if (player != null)
                    progress = player.GetComponentInParent<ProgressController>();
                if (progress == null)
                    progress = FindFirstObjectByType<ProgressController>();

                if (progress == null)
                {
                    Debug.LogWarning("[SymbolOpenCondition] ProgressController not found to verify known symbol.", this);
                    return false;
                }

                if (!progress.KnowsSymbol(requiredSymbolId))
                {
                    Debug.Log("[SymbolOpenCondition] Player does not know required symbol '" + requiredSymbolId + "'.", this);
                    return false;
                }
            }

            inputManager = player != null
                ? (player.GetComponentInChildren<SymbolInputManager>() ?? player.GetComponent<SymbolInputManager>())
                : null;
            if (inputManager == null)
                inputManager = FindFirstObjectByType<SymbolInputManager>();

            if (inputManager == null)
            {
                Debug.LogWarning("[SymbolOpenCondition] No SymbolInputManager found for player.", this);
                return false;
            }

            if (door == null)
                door = GetComponent<DoorController>();

            inputManager.SetActiveConsumer(this);
            awaiting = true;
            Debug.Log($"[SymbolOpenCondition] Awaiting symbol started. Required='{requiredSymbolId}'.", this);
            return false;
        }

        public void OnSymbolRecognized(string recognizedId)
        {
            if (!awaiting)
                return;

            var expected = requiredSymbolId?.Trim();
            var recognized = recognizedId?.Trim();

            bool matched = !string.IsNullOrWhiteSpace(expected) &&
                           !string.IsNullOrWhiteSpace(recognized) &&
                           string.Equals(expected, recognized, System.StringComparison.OrdinalIgnoreCase);

            if (!matched &&
                TryExtractNumericId(recognized, out int recNum) &&
                TryExtractNumericId(expected, out int expNum))
            {
                matched = recNum == expNum;
            }

            Debug.Log($"[SymbolOpenCondition] Symbol recognized. Expected='{expected}', Got='{recognized}'. Matched={matched}.", this);

            if (matched)
            {
                if (door == null)
                    door = GetComponent<DoorController>();
                bool opened = door != null && door.TryOpen();
                Debug.Log($"[SymbolOpenCondition] Door open trigger sent. Success={opened}.", this);
                awaiting = false;
                Restore();
            }
            else
            {
                Debug.Log("[SymbolOpenCondition] Incorrect symbol: '" + (recognized ?? "<null>") + "'.", this);
            }
        }

        public void OnDrawingFinished() { }

        private void Restore()
        {
            if (inputManager == null)
                return;
            Debug.Log("[SymbolOpenCondition] Restoring default symbol consumer.", this);
            inputManager.ResetToDefaultConsumer();
            inputManager = null;
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
