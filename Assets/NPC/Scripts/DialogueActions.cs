using System;
using Player.Interfaces;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace NPC.Dialog
{
    [Serializable]
    public abstract class DialogueActionDefinition
    {
        public abstract void Execute(IDialogueProgressContext progressController);
        public abstract string GetSummary();
    }

    [Serializable]
    [MovedFrom(true, sourceNamespace: "NPC.Dialog", sourceAssembly: "Assembly-CSharp")]
    public sealed class SetFlagActionDefinition : DialogueActionDefinition
    {
        [SerializeField] private string flagKey;
        [SerializeField] private bool value = true;

        public override void Execute(IDialogueProgressContext progressController)
        {
            progressController?.SetFlag(flagKey, value);
        }

        public override string GetSummary() => $"Set flag '{flagKey}' = {value}";
    }

    [Serializable]
    [MovedFrom(true, sourceNamespace: "NPC.Dialog", sourceAssembly: "Assembly-CSharp")]
    public sealed class StartQuestActionDefinition : DialogueActionDefinition
    {
        [SerializeField] private string questId;

        public override void Execute(IDialogueProgressContext progressController)
        {
            progressController?.StartQuest(questId);
        }

        public override string GetSummary() => $"Start quest '{questId}'";
    }

    [Serializable]
    [MovedFrom(true, sourceNamespace: "NPC.Dialog", sourceAssembly: "Assembly-CSharp")]
    public sealed class AddJobActionDefinition : DialogueActionDefinition
    {
        [SerializeField] private string jobId;

        public override void Execute(IDialogueProgressContext progressController)
        {
            progressController?.AddJob(jobId);
        }

        public override string GetSummary() => $"Add job '{jobId}'";
    }

    [Serializable]
    [MovedFrom(true, sourceNamespace: "NPC.Dialog", sourceAssembly: "Assembly-CSharp")]
    public sealed class LearnSymbolActionDefinition : DialogueActionDefinition
    {
        [SerializeField] private string symbolKey;

        public override void Execute(IDialogueProgressContext progressController)
        {
            progressController?.LearnSymbol(symbolKey);
        }

        public override string GetSummary() => $"Learn symbol '{symbolKey}'";
    }
}
