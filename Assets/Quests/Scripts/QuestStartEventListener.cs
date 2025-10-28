using System;
using UnityEngine;

namespace Quests
{
    /// <summary>
    /// Listens for a quest starting and forwards the notification to listeners implementing <see cref="IActivateOnQuestStart"/>.
    /// </summary>
    public class QuestStartEventListener : MonoBehaviour
    {
        [SerializeField] private string questId;
        [SerializeField] private QuestManager questManager;
        [SerializeField] private bool triggerOnce = true;

        private IActivateOnQuestStart listener;
        private bool hasTriggered;

        private void Awake()
        {
            listener = GetComponent<IActivateOnQuestStart>();
            EnsureQuestManagerReference();
        }

        private void OnEnable()
        {
            GameEvents.onQuestStarted += HandleQuestStarted;
            TryTriggerForAlreadyActiveQuest();
        }

        private void OnDisable()
        {
            GameEvents.onQuestStarted -= HandleQuestStarted;
        }

        private void HandleQuestStarted(string startedQuestId)
        {
            if (!IsMatchingQuest(startedQuestId))
                return;

            NotifyListeners(startedQuestId);
        }

        private void TryTriggerForAlreadyActiveQuest()
        {
            if (string.IsNullOrEmpty(questId) || hasTriggered)
                return;

            EnsureQuestManagerReference();
            if (questManager != null && questManager.IsQuestActive(questId))
                NotifyListeners(questId);
        }

        private void NotifyListeners(string startedQuestId)
        {
            if (triggerOnce && hasTriggered)
                return;

            hasTriggered = true;
            listener ??= GetComponent<IActivateOnQuestStart>();
            listener?.ActivateOnQuestStart(startedQuestId);
        }

        private bool IsMatchingQuest(string startedQuestId)
        {
            if (string.IsNullOrEmpty(questId))
                return false;

            return string.Equals(questId, startedQuestId, StringComparison.OrdinalIgnoreCase);
        }

        private void EnsureQuestManagerReference()
        {
            if (questManager != null)
                return;

            questManager = FindFirstObjectByType<QuestManager>();
        }
    }
}
