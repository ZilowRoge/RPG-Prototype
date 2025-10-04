using System;
using Player.Progress;
using UnityEngine;

namespace Player.FightSystem.Magic
{
    public class LessonSymbolConsumer : MonoBehaviour, ISymbolConsumer
    {
        private SymbolLesson currentLesson;
        private ISymbolConsumer previousConsumer;
        private int successfulAttempts;
        private bool isActive;
        private System.Action<bool, SymbolLesson, ISymbolConsumer> completionCallback;

        public bool IsLessonActive => isActive;
        public SymbolLesson CurrentLesson => currentLesson;

        public bool BeginLesson(
            SymbolLesson lesson,
            ISymbolConsumer fallbackConsumer,
            System.Action<bool, SymbolLesson, ISymbolConsumer> onCompletion)
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

            if (!int.TryParse(symbolId, out var parsedId) || parsedId != currentLesson.SymbolId)
            {
                FailLesson();
                return;
            }

            successfulAttempts++;
            if (successfulAttempts >= currentLesson.RequiredAttempts)
            {
                CompleteLesson();
            }
        }

        public void OnDrawingFinished()
        {
            // No action required when drawing stops for lessons.
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
            isActive = false;
            currentLesson = null;
            previousConsumer = null;
            completionCallback = null;
        }
    }
}
