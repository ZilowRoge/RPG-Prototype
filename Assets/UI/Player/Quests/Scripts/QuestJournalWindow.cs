using System.Collections.Generic;
using UnityEngine;
using Quests;
using UI.Player;

namespace UI.Player.Quests
{
    public class QuestJournalWindow : PlayerWindowBase
    {
        [Header("Data Sources")]
        [SerializeField] private QuestManager questManager;
        [SerializeField] private QuestDatabase questDatabase;
        [SerializeField] private bool hideCompletedQuestsInList;

        [SerializeField] private GameObject journalRoot;

        [Header("Quest List")]
        [SerializeField] private QuestListUI questListUI;

        [Header("Quest Info")]
        [SerializeField] private QuestInfoUI questInfoUI;

        private string selectedQuestId;
        private bool dependenciesInitialized;

        void EnsureDependencies()
        {
            if (dependenciesInitialized)
                return;

            if (questListUI == null)
                questListUI = GetComponentInChildren<QuestListUI>(true);
            if (questListUI == null)
                Debug.LogWarning("[QuestJournalWindow] QuestListUI reference not assigned.", this);

            if (questInfoUI == null)
                questInfoUI = GetComponentInChildren<QuestInfoUI>(true);
            if (questInfoUI == null)
                Debug.LogWarning("[QuestJournalWindow] QuestInfoUI reference not assigned.", this);

            if (questManager == null)
                questManager = FindAnyObjectByType<QuestManager>();
            if (questDatabase == null && questManager != null)
                questDatabase = questManager.Database;

            dependenciesInitialized = true;
        }

        protected override void Awake()
        {
            if (journalRoot != null)
                SetWindowRoot(journalRoot);
            else if (WindowRoot == null && transform.childCount > 0)
                SetWindowRoot(transform.GetChild(0).gameObject);

            EnsureDependencies();
            base.Awake();
        }

        private void OnEnable()
        {
            EnsureDependencies();

            if (questListUI != null)
                questListUI.QuestSelected += OnQuestSelectedFromList;

            GameEvents.onQuestStarted += HandleQuestStarted;
            GameEvents.onQuestStageCompleted += HandleQuestStageCompleted;
            GameEvents.onQuestObjectiveCompleted += HandleQuestObjectiveCompleted;
            GameEvents.onQuestObjectiveProgressed += HandleQuestObjectiveProgressed;

            RefreshUI();
        }

        private void OnDisable()
        {
            if (questListUI != null)
                questListUI.QuestSelected -= OnQuestSelectedFromList;

            GameEvents.onQuestStarted -= HandleQuestStarted;
            GameEvents.onQuestStageCompleted -= HandleQuestStageCompleted;
            GameEvents.onQuestObjectiveCompleted -= HandleQuestObjectiveCompleted;
            GameEvents.onQuestObjectiveProgressed -= HandleQuestObjectiveProgressed;
        }

        public void ToggleVisible() => Toggle();

        protected override void OnShow()
        {
            EnsureDependencies();
            RefreshUI();
        }

        private void HandleQuestStarted(string questId) => RefreshUI();
        private void HandleQuestStageCompleted(string questId, string stageId) => RefreshUI();
        private void HandleQuestObjectiveCompleted(string questId, string stageId, string objectiveId) => RefreshUI();
        private void HandleQuestObjectiveProgressed(string questId, string stageId, string objectiveId, int current, int required) => RefreshUI();

        private void RefreshUI()
        {
            RefreshQuestList();
            RefreshQuestDetails();
        }

        private void RefreshQuestList()
        {
            if (questListUI == null)
                return;

            IReadOnlyList<QuestProgress> active = questManager != null ? questManager.ActiveQuests : null;
            selectedQuestId = questListUI.Refresh(active, questDatabase, hideCompletedQuestsInList, selectedQuestId);
        }

        private void OnQuestSelectedFromList(string questId)
        {
            SelectQuest(questId);
        }

        public void SelectQuest(string questId)
        {
            selectedQuestId = questId;
            questListUI?.ApplySelection(selectedQuestId);
            RefreshQuestDetails();
        }

        private void RefreshQuestDetails()
        {
            if (questInfoUI == null)
                return;

            if (string.IsNullOrEmpty(selectedQuestId) || questManager == null)
            {
                questInfoUI.Clear();
                return;
            }

            var progress = questManager.GetProgress(selectedQuestId);
            QuestAsset asset = questDatabase != null ? questDatabase.Get(selectedQuestId) : null;

            if (progress == null && asset == null)
            {
                questInfoUI.Clear();
                return;
            }

            string questTitle = asset != null && !string.IsNullOrEmpty(asset.title)
                ? asset.title
                : selectedQuestId;

            int stageIndex = DetermineStageIndex(progress, asset);
            StageDef stageDef = null;
            StageProgress stageProg = null;

            if (stageIndex >= 0)
            {
                if (asset != null && stageIndex < asset.stages.Count)
                    stageDef = asset.stages[stageIndex];
                if (progress != null && stageIndex < progress.stages.Count)
                    stageProg = progress.stages[stageIndex];
            }

            string description = ResolveDescription(asset, stageDef);

            questInfoUI.Display(questTitle, description, asset, stageDef, stageProg);
        }

        private string ResolveDescription(QuestAsset asset, StageDef stage)
        {
            if (stage != null && !string.IsNullOrEmpty(stage.description))
                return stage.description;

            if (asset != null && !string.IsNullOrEmpty(asset.shortDescription))
                return asset.shortDescription;

            return string.Empty;
        }

        private static int DetermineStageIndex(QuestProgress progress, QuestAsset asset)
        {
            if (progress == null)
            {
                if (asset == null || asset.stages.Count == 0)
                    return -1;
                return Mathf.Clamp(asset.stages.Count - 1, 0, asset.stages.Count - 1);
            }

            if (progress.stages == null || progress.stages.Count == 0)
                return -1;

            int maxIndex = progress.stages.Count - 1;
            if (progress.state == QuestState.Completed)
                return maxIndex;

            return Mathf.Clamp(progress.stageIndex, 0, maxIndex);
        }
    }
}

