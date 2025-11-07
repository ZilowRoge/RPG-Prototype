using System;
using Player.FightSystem.Magic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Common.Systems.SymbolTraining
{
    [MovedFrom("Player.FightSystem.Magic")]
    public class LessonSymbolConsumer : MonoBehaviour, ISymbolConsumer, Player.FightSystem.Magic.ICancelableSymbolFlow
    {
        [SerializeField] private SymbolLessonUI lessonUI;

        private SymbolLesson currentLesson;
        private ISymbolConsumer previousConsumer;
        private int successfulAttempts;
        private int totalAttempts;
        private bool isActive;
        private string pendingSymbolId;
        private Action<bool, SymbolLesson, ISymbolConsumer> completionCallback;

        public bool IsLessonActive => isActive;
        public SymbolLesson CurrentLesson => currentLesson;

        private void Awake()
        {
            if (lessonUI == null)
                lessonUI = FindFirstObjectByType<SymbolLessonUI>(FindObjectsInactive.Include);
        }

        public bool BeginLesson(
            SymbolLesson lesson,
            ISymbolConsumer fallbackConsumer,
            Action<bool, SymbolLesson, ISymbolConsumer> onCompletion)
        {
            if (lesson == null)
            {
                Debug.LogWarning("[LessonSymbolConsumer] Cannot start lesson: lesson is null.", this);
                return false;
            }

            if (isActive)
            {
                Debug.LogWarning("[LessonSymbolConsumer] Lesson already active.", this);
                return false;
            }

            currentLesson = lesson;
            previousConsumer = fallbackConsumer;
            successfulAttempts = 0;
            totalAttempts = 0;
            isActive = true;
            completionCallback = onCompletion;

            ShowLessonUI();

            return true;
        }

        public void CancelLesson()
        {
            if (!isActive)
                return;

            FailLesson();
        }

        public void CancelSymbolFlow()
        {
            CancelLesson();
        }

        public void OnSymbolRecognized(string symbolId)
        {
            if (!isActive)
                return;

            pendingSymbolId = symbolId;
        }

        public void OnSymbolSequenceCommitted()
        {
            if (!isActive || currentLesson == null)
                return;

            if (string.IsNullOrWhiteSpace(pendingSymbolId))
                return;

            totalAttempts++;

            string recognizedId = pendingSymbolId?.Trim();
            pendingSymbolId = null;
            string expectedId = currentLesson.SymbolId;

            bool matched = !string.IsNullOrWhiteSpace(recognizedId) &&
                           !string.IsNullOrWhiteSpace(expectedId) &&
                           string.Equals(recognizedId, expectedId, StringComparison.OrdinalIgnoreCase);

            if (!matched &&
                TryExtractNumericId(recognizedId, out int recognizedNumeric) &&
                TryExtractNumericId(expectedId, out int expectedNumeric))
            {
                matched = recognizedNumeric == expectedNumeric;
            }

            if (matched)
            {
                successfulAttempts++;
            }
            else
            {
                Debug.Log("[LessonSymbolConsumer] Incorrect symbol '" + (recognizedId ?? "<null>") + "' for lesson '" + expectedId + "'.", this);
            }

            UpdateLessonUIProgress();

            if (matched && successfulAttempts >= currentLesson.RequiredSuccessfulAttempts)
            {
                CompleteLesson();
                return;
            }

            TryFailLessonOnAttempts();
        }

        private void TryFailLessonOnAttempts()
        {
            if (!isActive || currentLesson == null)
                return;

            if (totalAttempts >= currentLesson.MaxAttempts)
            {
                FailLesson();
            }
        }

        private void CompleteLesson()
        {
            var lesson = currentLesson;
            completionCallback?.Invoke(true, lesson, previousConsumer);
            EndLessonUI();
            EndLessonInternal();
        }

        private void FailLesson()
        {
            var lesson = currentLesson;
            completionCallback?.Invoke(false, lesson, previousConsumer);
            EndLessonUI();
            EndLessonInternal();
        }

        private void EndLessonInternal()
        {
            successfulAttempts = 0;
            totalAttempts = 0;
            isActive = false;
            currentLesson = null;
            previousConsumer = null;
            pendingSymbolId = null;
            completionCallback = null;
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

        private void ShowLessonUI()
        {
            if (lessonUI == null || currentLesson == null)
                return;

            lessonUI.ShowLesson(currentLesson, successfulAttempts);
        }

        private void UpdateLessonUIProgress()
        {
            if (lessonUI == null || currentLesson == null)
                return;

            lessonUI.UpdateProgress(successfulAttempts);
        }

        private void EndLessonUI()
        {
            lessonUI?.EndLesson();
        }
    }
}
