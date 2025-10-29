using Quests;
using UnityEngine;

namespace Common.World.Orb
{
    /// <summary>
    /// Triggers an <see cref="OrbHoverAnimator"/> when the associated quest starts.
    /// </summary>
    [RequireComponent(typeof(OrbHoverAnimator))]
    public class QuestOrbActivator : MonoBehaviour, IActivateOnQuestStart
    {
        [SerializeField] private string questId;
        [SerializeField] private QuestManager questManager;
        [SerializeField] private OrbHoverAnimator hoverAnimator;
        [SerializeField] private Collider orbCollider;
        [SerializeField] private bool restartIfRunning = false;
        [SerializeField] private bool recaptureStartHeight = true;
        [SerializeField] private bool disableColliderOnAwake = true;
        [SerializeField] private bool triggerIfAlreadyActive = true;

        private bool hasActivated;

        private void Awake()
        {
            if (hoverAnimator == null)
                hoverAnimator = GetComponent<OrbHoverAnimator>();

            if (orbCollider == null)
                orbCollider = GetComponent<Collider>();

            if (disableColliderOnAwake && orbCollider != null)
                orbCollider.enabled = false;

            EnsureQuestManagerReference();
        }

        private void OnEnable()
        {
            GameEvents.onQuestStarted += HandleQuestStarted;

            if (triggerIfAlreadyActive && !hasActivated)
            {
                EnsureQuestManagerReference();
                if (!string.IsNullOrEmpty(questId) && questManager != null && questManager.IsQuestActive(questId))
                    Activate();
            }
        }

        private void OnDisable()
        {
            GameEvents.onQuestStarted -= HandleQuestStarted;
        }

        public void ActivateOnQuestStart(string questId)
        {
            if (!string.IsNullOrEmpty(questId) && !IsMatchingQuest(questId))
                return;

            Activate();
        }

        void HandleQuestStarted(string startedQuestId)
        {
            if (IsMatchingQuest(startedQuestId))
                Activate();
        }

        bool IsMatchingQuest(string startedQuestId) =>
            !string.IsNullOrEmpty(questId) &&
            string.Equals(questId, startedQuestId, System.StringComparison.OrdinalIgnoreCase);

        void Activate()
        {
            if (hasActivated)
                return;

            hasActivated = true;

            if (hoverAnimator != null)
                hoverAnimator.BeginHover(restartIfRunning, recaptureStartHeight);

            if (orbCollider != null)
                orbCollider.enabled = true;
        }

        void EnsureQuestManagerReference()
        {
            if (questManager == null)
                questManager = FindFirstObjectByType<QuestManager>();
        }
    }
}
