using System;
using System.Collections.Generic;
using Common.Runtime;
using Common.Symbols;
using Player.Interfaces;
using UnityEngine;

namespace Common.Systems.SymbolTraining
{
    [AddComponentMenu("Game/Managers/Symbol Trainer")]
    public class SymbolTrainer : MonoBehaviour
    {
        [Serializable]
        private class LessonTrigger
        {
            public string startFlag;
            public SymbolLesson lesson;
            public bool resetFlagAfterStart = true;
        }

        [SerializeField] private MonoBehaviour progressSource;
        [SerializeField] private MonoBehaviour progressEventsSource;
        [SerializeField] private MonoBehaviour inputManagerSource;
        [SerializeField] private LessonSymbolConsumer lessonConsumer;
        [SerializeField] private List<LessonTrigger> lessonTriggers = new();

        private bool subscriptionsInitialized;
        private IDialogueProgressContext progressController;
        private IFlagChangeSource progressEvents;
        private ISymbolInputRouter inputManager;

        private void Awake()
        {
            EnsureDependencies();
        }

        private void OnEnable()
        {
            EnsureDependencies();
            if (progressController != null && !subscriptionsInitialized)
            {
                if (progressEvents == null)
                    CacheProgressEvents();
                if (progressEvents != null)
                    progressEvents.FlagChanged += OnFlagChanged;
                subscriptionsInitialized = true;
            }
        }

        private void OnDisable()
        {
            if (subscriptionsInitialized)
            {
                if (progressEvents == null)
                    CacheProgressEvents();
                if (progressEvents != null)
                    progressEvents.FlagChanged -= OnFlagChanged;
                subscriptionsInitialized = false;
            }
        }

        private void OnFlagChanged(string key, bool value)
        {
            if (SaveRuntimeState.IsRestoring)
                return;

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
                CacheProgressController();
            if (progressEvents == null)
                CacheProgressEvents();
            if (inputManager == null)
                CacheInputManager();
            if (lessonConsumer == null)
                lessonConsumer = FindAnyObjectByType<LessonSymbolConsumer>();

            return progressController != null && progressEvents != null && inputManager != null && lessonConsumer != null;
        }

        private void CacheProgressController()
        {
            progressController = progressSource as IDialogueProgressContext;
            if (progressController != null)
                return;

            var candidates = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include);
            for (int i = 0; i < candidates.Length; i++)
            {
                if (candidates[i] is IDialogueProgressContext context)
                {
                    progressSource = candidates[i];
                    progressController = context;
                    return;
                }
            }
        }

        private void CacheProgressEvents()
        {
            progressEvents = progressEventsSource as IFlagChangeSource;
            if (progressEvents != null)
                return;

            if (progressSource != null)
            {
                var sourceComponents = progressSource.GetComponentsInParent<MonoBehaviour>(true);
                for (int i = 0; i < sourceComponents.Length; i++)
                {
                    if (sourceComponents[i] is IFlagChangeSource sourceEvents)
                    {
                        progressEventsSource = sourceComponents[i];
                        progressEvents = sourceEvents;
                        return;
                    }
                }
            }

            var candidates = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include);
            for (int i = 0; i < candidates.Length; i++)
            {
                if (candidates[i] is IFlagChangeSource candidateEvents)
                {
                    progressEventsSource = candidates[i];
                    progressEvents = candidateEvents;
                    return;
                }
            }
        }

        private void CacheInputManager()
        {
            inputManager = inputManagerSource as ISymbolInputRouter;
            if (inputManager != null)
                return;

            var candidates = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include);
            for (int i = 0; i < candidates.Length; i++)
            {
                if (candidates[i] is ISymbolInputRouter router)
                {
                    inputManagerSource = candidates[i];
                    inputManager = router;
                    return;
                }
            }
        }
    }
}

