using System;
using Player.Progress;
using UnityEngine;

namespace NPC.Dialog
{
    [Serializable]
    public abstract class DialogueConditionDefinition
    {
        public abstract bool Evaluate(ProgressController progressController);
        public abstract string GetSummary();
    }

    [Serializable]
    public sealed class FlagConditionDefinition : DialogueConditionDefinition
    {
        [SerializeField] private string flagKey;
        [SerializeField] private bool expectedValue = true;

        public override bool Evaluate(ProgressController progressController)
        {
            return progressController != null && progressController.GetFlag(flagKey) == expectedValue;
        }

        public override string GetSummary() => $"Flag '{flagKey}' == {expectedValue}";
    }

    [Serializable]
    public sealed class QuestStartedConditionDefinition : DialogueConditionDefinition
    {
        [SerializeField] private string questId;
        [SerializeField] private bool expectedValue = true;

        public override bool Evaluate(ProgressController progressController)
        {
            if (progressController == null)
                return false;

            var result = progressController.IsQuestStarted(questId);
            return expectedValue ? result : !result;
        }

        public override string GetSummary() => $"Quest '{questId}' started = {expectedValue}";
    }

    [Serializable]
    public sealed class QuestActiveConditionDefinition : DialogueConditionDefinition
    {
        [SerializeField] private string questId;
        [SerializeField] private bool expectedValue = true;

        public override bool Evaluate(ProgressController progressController)
        {
            if (progressController == null)
                return false;

            var result = progressController.IsQuestActive(questId);
            return expectedValue ? result : !result;
        }

        public override string GetSummary() => $"Quest '{questId}' active = {expectedValue}";
    }

    [Serializable]
    public sealed class QuestCompletedConditionDefinition : DialogueConditionDefinition
    {
        [SerializeField] private string questId;
        [SerializeField] private bool expectedValue = true;

        public override bool Evaluate(ProgressController progressController)
        {
            if (progressController == null)
                return false;

            var result = progressController.IsQuestCompleted(questId);
            return expectedValue ? result : !result;
        }

        public override string GetSummary() => $"Quest '{questId}' completed = {expectedValue}";
    }

    [Serializable]
    public sealed class HasJobConditionDefinition : DialogueConditionDefinition
    {
        [SerializeField] private string jobId;
        [SerializeField] private bool expectedValue = true;

        public override bool Evaluate(ProgressController progressController)
        {
            if (progressController == null)
                return false;

            var result = progressController.HasJob(jobId);
            return expectedValue ? result : !result;
        }

        public override string GetSummary() => $"Has job '{jobId}' = {expectedValue}";
    }

    [Serializable]
    public sealed class KnowsSymbolConditionDefinition : DialogueConditionDefinition
    {
        [SerializeField] private string symbolKey;
        [SerializeField] private bool expectedValue = true;

        public override bool Evaluate(ProgressController progressController)
        {
            if (progressController == null)
                return false;

            var result = progressController.KnowsSymbol(symbolKey);
            return expectedValue ? result : !result;
        }

        public override string GetSummary() => $"Knows symbol '{symbolKey}' = {expectedValue}";
    }
}
