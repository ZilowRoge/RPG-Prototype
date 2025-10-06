using System;
using Player.FightSystem.Magic;
using Player.Progress;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Common.Systems.SymbolTraining
{
    [MovedFrom("Player.FightSystem.Magic")]
    public class LessonSymbolConsumer : MonoBehaviour, ISymbolConsumer
    {
        private SymbolLesson currentLesson;
        private ISymbolConsumer previousConsumer;
        private int successfulAttempts;
        private int totalAttempts;
        private bool isActive;
        private Action<bool, SymbolLesson, ISymbolConsumer> completionCallback;

        public bool IsLessonActive => isActive;
        public SymbolLesson CurrentLesson => currentLesson;

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

            return true;
        }

        public void CancelLesson()
        {
            if (!isActive)
                return;

            FailLesson();
        }

        public void OnSymbolRecognized(string symbolId)
        {
            if (!isActive || currentLesson == null)
                return;

            totalAttempts++;

            if (!string.IsNullOrWhiteSpace(symbolId) &&
                string.Equals(symbolId, currentLesson.SymbolId, StringComparison.OrdinalIgnoreCase))
            {
                successfulAttempts++;
                if (successfulAttempts >= currentLesson.RequiredSuccessfulAttempts)
                {
                    CompleteLesson();
                    return;
                }
            }
            else
            {
                Debug.Log($"[LessonSymbolConsumer] Incorrect symbol '{symbolId}' for lesson '{currentLesson.SymbolId}'.", this);
            }

            TryFailLessonOnAttempts();
        }

        public void OnDrawingFinished()
        {
            // Lessons do not react to drawing stop events.
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
            completionCallback?.Invoke(true, currentLesson, previousConsumer);
            EndLessonInternal();
        }

        private void FailLesson()
        {
            completionCallback?.Invoke(false, currentLesson, previousConsumer);
            EndLessonInternal();
        }

        private void EndLessonInternal()
        {
            successfulAttempts = 0;
            totalAttempts = 0;
            isActive = false;
            currentLesson = null;
            previousConsumer = null;
            completionCallback = null;
        }
    }
}
