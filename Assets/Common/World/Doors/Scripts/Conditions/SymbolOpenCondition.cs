using UnityEngine;
using Player.FightSystem.Magic;

namespace Common.Systems.SymbolTraining
{
    [AddComponentMenu("Game/World/Doors/Conditions/Symbol Required")] 
    public class SymbolOpenCondition : MonoBehaviour, IDoorOpenCondition, ISymbolConsumer
    {
        [SerializeField] private string requiredSymbolId = string.Empty;
        private SymbolInputManager inputManager;
        private DoorController door;
        private bool awaiting;

        public string Id => requiredSymbolId?.Trim();

        public bool CanOpen(GameObject player)
        {
            // Start symbol drawing on first interaction; InteractiveDoor will not open immediately.
            if (awaiting)
                return false;

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

            if (matched)
            {
                if (door == null)
                    door = GetComponent<DoorController>();
                door?.TryOpen();
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
