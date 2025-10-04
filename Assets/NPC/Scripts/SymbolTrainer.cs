using System;
using System.Collections.Generic;
using Player.FightSystem.Magic;
using Player.Progress;
using UnityEngine;

namespace NPC.Training
{
    [AddComponentMenu("NPC/Symbol Trainer")]
    public class SymbolTrainer : MonoBehaviour
    {
        [Serializable]
        private class LessonTrigger
        {
            public string startFlag;
            public SymbolLesson lesson;
            public bool resetFlagAfterStart = true;
        }

        [SerializeField] private ProgressController progressController;
        [SerializeField] private SymbolInputManager inputManager;
        [SerializeField] private LessonSymbolConsumer lessonConsumer;
        [SerializeField] private List<LessonTrigger> lessonTriggers = new();

        private bool subscriptionsInitialized;

        private void Awake()
        {
            EnsureDependencies();
        }

        private void OnEnable()
        {
            EnsureDependencies();
            if (progressController != null && !subscriptionsInitialized)
            {
                progressController.FlagChanged += OnFlagChanged;
                subscriptionsInitialized = true;
            }
        }

        private void OnDisable()
        {
            if (progressController != null && subscriptionsInitialized)
            {
                progressController.FlagChanged -= OnFlagChanged;
                subscriptionsInitialized = false;
            }
        }

        public bool StartLesson(SymbolLesson lesson)
        {
            if (!EnsureDependencies())
                return false;

            if (lesson == null)
                return false;

            if (progressController.KnowsSymbol(lesson.SymbolId))
                return false;

            progressController.SetFlag(lesson.CompletionFlagKey, false);
            return BeginLesson(lesson, null);
        }

        private void OnFlagChanged(string key, bool value)
        {
            if (!value || string.IsNullOrEmpty(key))
                return;

            if (!EnsureDependencies())
                return;

            var trigger = FindTriggerByFlag(key);
            if (trigger == null || trigger.lesson == null)
                return;

            if (progressController.KnowsSymbol(trigger.lesson.SymbolId))
            {
                if (trigger.resetFlagAfterStart)
                    ResetFlag(key);
                return;
            }

            progressController.SetFlag(trigger.lesson.CompletionFlagKey, false);
            if (BeginLesson(trigger.lesson, trigger) && trigger.resetFlagAfterStart)
            {
                ResetFlag(key);
            }
        }

        private bool BeginLesson(SymbolLesson lesson, LessonTrigger trigger)
        {
            if (lessonConsumer == null || inputManager == null)
            {
                Debug.LogWarning("[SymbolTrainer] Missing consumer or input manager.", this);
                return false;
            }

            if (lessonConsumer.IsLessonActive)
                return false;

            var previousConsumer = inputManager.SetActiveConsumer(lessonConsumer);

            bool started = lessonConsumer.BeginLesson(
                lesson,
                previousConsumer,
                (success, completedLesson, fallback) => OnLessonFinished(trigger, success, completedLesson, fallback, previousConsumer));

            if (!started)
            {
                inputManager.SetActiveConsumer(previousConsumer);
                return false;
            }

            Debug.Log($"[SymbolTrainer] Lesson started for symbol {lesson.SymbolId}.");
            return true;
        }

        private void OnLessonFinished(LessonTrigger trigger, bool success, SymbolLesson lesson, ISymbolConsumer fallback, ISymbolConsumer previousConsumer)
        {
            if (progressController != null && lesson != null)
            {
                progressController.SetFlag(lesson.CompletionFlagKey, success);
            }

            RestoreConsumer(fallback, previousConsumer);
        }

        private void RestoreConsumer(ISymbolConsumer fallback, ISymbolConsumer previousConsumer)
        {
            if (inputManager == null)
                return;

            var target = fallback ?? previousConsumer ?? inputManager.DefaultCombatConsumer;
            if (target != null)
                inputManager.SetActiveConsumer(target);
            else
                inputManager.ResetToDefaultConsumer();
        }

        private void ResetFlag(string flagKey)
        {
            if (progressController == null || string.IsNullOrEmpty(flagKey))
                return;

            progressController.SetFlag(flagKey, false);
        }

        private LessonTrigger FindTriggerByFlag(string flagKey)
        {
            for (int i = 0; i < lessonTriggers.Count; i++)
            {
                var trigger = lessonTriggers[i];
                if (trigger == null)
                    continue;
                if (!string.IsNullOrEmpty(trigger.startFlag) && trigger.startFlag == flagKey)
                    return trigger;
            }
            return null;
        }

        private bool EnsureDependencies()
        {
            if (progressController == null)
                progressController = FindFirstObjectByType<ProgressController>();
            if (inputManager == null)
                inputManager = FindFirstObjectByType<SymbolInputManager>();
            if (lessonConsumer == null)
                lessonConsumer = FindFirstObjectByType<LessonSymbolConsumer>();

            return progressController != null && inputManager != null && lessonConsumer != null;
        }
    }
}
