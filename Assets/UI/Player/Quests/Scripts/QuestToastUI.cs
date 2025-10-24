using System.Collections;
using TMPro;
using UnityEngine;
using Quests;

namespace UI.Player.Quests
{
    public class QuestToastUI : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private string startedPrefix = "Quest Started: ";
        [SerializeField] private string objectivePrefix = "Objective Completed: ";
        [SerializeField] private string stagePrefix = "Quest Log Updated: ";
        [SerializeField] private bool notificationsEnabled = true;
        [SerializeField] private GameObject toastRoot;

        [Header("Behavior")] 
        [SerializeField] private float visibleSeconds = 2.5f;
        [SerializeField] private float fadeSeconds = 0.35f;

        [Header("Data (optional)")]
        [SerializeField] private QuestDatabase questDatabase;

        private Coroutine running;

        private void Awake()
        {
            if (toastRoot == null && canvasGroup != null && canvasGroup.gameObject != gameObject)
                toastRoot = canvasGroup.gameObject;
            if (toastRoot != null && toastRoot != gameObject)
                toastRoot.SetActive(notificationsEnabled);

            if (canvasGroup == null) return;
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        private void OnEnable()
        {
            GameEvents.onQuestStarted += OnQuestStarted;
            GameEvents.onQuestObjectiveCompleted += OnQuestObjectiveCompleted;
            GameEvents.onQuestStageCompleted += OnQuestStageCompleted;
        }

        private void OnDisable()
        {
            GameEvents.onQuestStarted -= OnQuestStarted;
            GameEvents.onQuestObjectiveCompleted -= OnQuestObjectiveCompleted;
            GameEvents.onQuestStageCompleted -= OnQuestStageCompleted;
        }

        private void OnQuestStarted(string questId)
        {
            string display = ResolveQuestTitle(questId);
            ShowToast(startedPrefix + display);
        }

        private void OnQuestObjectiveCompleted(string questId, string stageId, string objectiveId)
        {
            string display = ResolveObjectiveTitle(questId, stageId, objectiveId);
            ShowToast(objectivePrefix + display);
        }

        private void OnQuestStageCompleted(string questId, string stageId)
        {
            string display = ResolveStageTitle(questId, stageId);
            ShowToast(stagePrefix + display);
        }

        private void ShowToast(string message)
        {
            if (!notificationsEnabled)
                return;

            if (toastRoot != null && toastRoot != gameObject && !toastRoot.activeSelf)
                toastRoot.SetActive(true);

            if (titleText != null)
                titleText.text = message;

            if (running != null)
                StopCoroutine(running);

            if (canvasGroup != null)
                canvasGroup.alpha = 0f;

            running = StartCoroutine(ShowRoutine());
        }

        private IEnumerator ShowRoutine()
        {
            if (canvasGroup == null)
            {
                running = null;
                yield break;
            }
            // Fade in
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            float t = 0f;
            while (t < fadeSeconds)
            {
                t += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, Mathf.Clamp01(t / fadeSeconds));
                yield return null;
            }
            canvasGroup.alpha = 1f;

            // Hold
            float hold = visibleSeconds;
            while (hold > 0f)
            {
                hold -= Time.unscaledDeltaTime;
                yield return null;
            }

            // Fade out
            t = 0f;
            while (t < fadeSeconds)
            {
                t += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, Mathf.Clamp01(t / fadeSeconds));
                yield return null;
            }
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            running = null;

            if (toastRoot != null && toastRoot != gameObject)
                toastRoot.SetActive(false);
        }

        public void SetNotificationsEnabled(bool enabled)
        {
            notificationsEnabled = enabled;

            if (!enabled)
            {
                if (running != null)
                {
                    StopCoroutine(running);
                    running = null;
                }

                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 0f;
                    canvasGroup.interactable = false;
                    canvasGroup.blocksRaycasts = false;
                }

                if (toastRoot != null && toastRoot != gameObject)
                    toastRoot.SetActive(false);
            }
            else
            {
                if (toastRoot != null && toastRoot != gameObject)
                    toastRoot.SetActive(true);

                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 0f;
                    canvasGroup.interactable = false;
                    canvasGroup.blocksRaycasts = false;
                }
            }
        }

        private string ResolveQuestTitle(string questId)
        {
            if (questDatabase == null || string.IsNullOrEmpty(questId))
                return questId;

            var asset = questDatabase.Get(questId);
            return asset != null && !string.IsNullOrEmpty(asset.title) ? asset.title : questId;
        }

        private string ResolveObjectiveTitle(string questId, string stageId, string objectiveId)
        {
            if (questDatabase == null)
                return SelectBestFallback(questId, stageId, objectiveId);

            var asset = questDatabase.Get(questId);
            if (asset == null)
                return SelectBestFallback(questId, stageId, objectiveId);

            StageDef matchedStage = null;
            if (!string.IsNullOrEmpty(stageId))
            {
                for (int i = 0; i < asset.stages.Count; i++)
                {
                    var candidate = asset.stages[i];
                    if (candidate == null) continue;
                    if (candidate.id == stageId)
                    {
                        matchedStage = candidate;
                        break;
                    }
                }
            }

            ObjectiveDef matchedObjective = null;
            if (matchedStage == null && !string.IsNullOrEmpty(objectiveId))
            {
                for (int i = 0; i < asset.stages.Count; i++)
                {
                    var candidate = asset.stages[i];
                    if (candidate == null) continue;
                    for (int j = 0; j < candidate.objectives.Count; j++)
                    {
                        var obj = candidate.objectives[j];
                        if (obj == null) continue;
                        if (obj.id == objectiveId)
                        {
                            matchedStage = candidate;
                            matchedObjective = obj;
                            break;
                        }
                    }
                    if (matchedStage != null) break;
                }
            }

            if (matchedObjective == null && matchedStage != null && !string.IsNullOrEmpty(objectiveId))
            {
                for (int j = 0; j < matchedStage.objectives.Count; j++)
                {
                    var obj = matchedStage.objectives[j];
                    if (obj == null) continue;
                    if (obj.id == objectiveId)
                    {
                        matchedObjective = obj;
                        break;
                    }
                }
            }

            if (matchedObjective != null)
            {
                if (!string.IsNullOrEmpty(matchedObjective.displayName))
                    return matchedObjective.displayName;
                if (!string.IsNullOrEmpty(matchedObjective.extraId))
                    return matchedObjective.extraId;
                if (!string.IsNullOrEmpty(matchedObjective.targetId))
                    return matchedObjective.targetId;
                if (!string.IsNullOrEmpty(matchedObjective.id))
                    return matchedObjective.id;
            }

            if (matchedStage != null && !string.IsNullOrEmpty(matchedStage.title))
                return matchedStage.title;

            return SelectBestFallback(questId, stageId, objectiveId);
        }

        private string ResolveStageTitle(string questId, string stageId)
        {
            if (questDatabase == null)
                return SelectBestFallback(questId, stageId, null);

            var asset = questDatabase.Get(questId);
            if (asset == null)
                return SelectBestFallback(questId, stageId, null);

            StageDef matchedStage = null;
            if (!string.IsNullOrEmpty(stageId))
            {
                for (int i = 0; i < asset.stages.Count; i++)
                {
                    var candidate = asset.stages[i];
                    if (candidate == null) continue;
                    if (candidate.id == stageId)
                    {
                        matchedStage = candidate;
                        break;
                    }
                }
            }

            if (matchedStage != null)
            {
                if (!string.IsNullOrEmpty(matchedStage.title))
                    return matchedStage.title;
                if (!string.IsNullOrEmpty(matchedStage.id))
                    return matchedStage.id;
            }

            return SelectBestFallback(questId, stageId, null);
        }

        private static string SelectBestFallback(string questId, string stageId, string objectiveId)
        {
            if (!string.IsNullOrEmpty(objectiveId))
                return objectiveId;
            if (!string.IsNullOrEmpty(stageId))
                return stageId;
            return questId;
        }
    }
}
