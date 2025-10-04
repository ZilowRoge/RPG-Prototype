using UnityEngine;

namespace Player.FightSystem.Magic
{
    [CreateAssetMenu(fileName = "SymbolLesson", menuName = "Magic/Symbol Lesson", order = 0)]
    public class SymbolLesson : ScriptableObject
    {
        [SerializeField] [Min(0)] private int symbolId;
        [SerializeField] [Min(1)] private int requiredAttempts = 1;
        [SerializeField] [Range(0f, 1f)] private float recognitionThreshold = 0.8f;
        [SerializeField] private string completionFlagKey;
        [SerializeField] private string symbolName;
        [SerializeField] private Sprite referenceSprite;
        [TextArea] [SerializeField] private string description;

        public int SymbolId => symbolId;
        public int RequiredAttempts => requiredAttempts;
        public float RecognitionThreshold => recognitionThreshold;
        public string SymbolName => symbolName;
        public Sprite ReferenceSprite => referenceSprite;
        public string Description => description;
        public string CompletionFlagKey => string.IsNullOrWhiteSpace(completionFlagKey)
            ? $"lesson_completed_{symbolId}"
            : completionFlagKey;
    }
}
