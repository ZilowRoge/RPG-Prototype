using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Common.Systems.SymbolTraining
{
    [CreateAssetMenu(fileName = "SymbolLesson", menuName = "Magic/Symbol Lesson", order = 0)]
    public class SymbolLesson : ScriptableObject
    {
        [SerializeField] private string symbolId = string.Empty;
        [SerializeField] [Min(1)] private int requiredSuccessfulAttempts = 1;
        [SerializeField] [Min(1)] private int maxAttempts = 1;
        [SerializeField] [Range(0f, 1f)] private float recognitionThreshold = 0.8f;
        [SerializeField] private string completionFlagKey;
        [SerializeField] private string symbolName;
        [SerializeField] private Sprite referenceSprite;
        [TextArea] [SerializeField] private string description;

        public string SymbolId => symbolId;
        public int RequiredSuccessfulAttempts => requiredSuccessfulAttempts;
        public int MaxAttempts => maxAttempts;
        public float RecognitionThreshold => recognitionThreshold;
        public string SymbolName => symbolName;
        public Sprite ReferenceSprite => referenceSprite;
        public string Description => description;
        public string CompletionFlagKey => string.IsNullOrWhiteSpace(completionFlagKey)
            ? $"lesson_completed_{GetSafeSymbolKey()}"
            : completionFlagKey;

        public bool TryGetNumericSymbolId(out int numericId)
        {
            if (string.IsNullOrWhiteSpace(symbolId))
            {
                numericId = -1;
                return false;
            }

            return int.TryParse(symbolId, out numericId);
        }

        private void OnValidate()
        {
            requiredSuccessfulAttempts = Mathf.Max(1, requiredSuccessfulAttempts);
            maxAttempts = Mathf.Max(requiredSuccessfulAttempts, maxAttempts);

            if (symbolId != null)
                symbolId = symbolId.Trim();
        }

        private string GetSafeSymbolKey()
        {
            if (string.IsNullOrWhiteSpace(symbolId))
                return "unknown";

            return symbolId.Replace(' ', '_');
        }
    }
}


