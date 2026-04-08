using System;
using Player.Progress;
using UnityEngine;

namespace NPC.Dialog
{
    [Serializable]
    public abstract class DialogueActionDefinition
    {
        public abstract void Execute(ProgressController progressController);
        public abstract string GetSummary();
    }

    [Serializable]
    public sealed class SetFlagActionDefinition : DialogueActionDefinition
    {
        [SerializeField] private string flagKey;
        [SerializeField] private bool value = true;

        public override void Execute(ProgressController progressController)
        {
            progressController?.SetFlag(flagKey, value);
        }

        public override string GetSummary() => $"Set flag '{flagKey}' = {value}";
    }

    [Serializable]
    public sealed class StartQuestActionDefinition : DialogueActionDefinition
    {
        [SerializeField] private string questId;

        public override void Execute(ProgressController progressController)
        {
            progressController?.StartQuest(questId);
        }

        public override string GetSummary() => $"Start quest '{questId}'";
    }

    [Serializable]
    public sealed class AddJobActionDefinition : DialogueActionDefinition
    {
        [SerializeField] private string jobId;

        public override void Execute(ProgressController progressController)
        {
            progressController?.AddJob(jobId);
        }

        public override string GetSummary() => $"Add job '{jobId}'";
    }

    [Serializable]
    public sealed class LearnSymbolActionDefinition : DialogueActionDefinition
    {
        [SerializeField] private string symbolKey;

        public override void Execute(ProgressController progressController)
        {
            progressController?.LearnSymbol(symbolKey);
        }

        public override string GetSummary() => $"Learn symbol '{symbolKey}'";
    }
}
