using System;
using System.Collections.Generic;
using Player.Progress;
using UnityEngine;

namespace NPC.Dialog
{
    public enum DialogChoiceLockMode
    {
        Hide,
        Disable
    }

    [Serializable]
    public class DialogEntryPoint
    {
        [SerializeField] private string id = System.Guid.NewGuid().ToString("N");
        [SerializeField] private string name = "Default";
        [SerializeField] private int priority;
        [SerializeField] private float weight = 1f;
        [SerializeField] private Vector2 position = new(40f, 120f);
        [SerializeField] private string targetNodeGuid;
        [SerializeReference] private List<DialogueConditionDefinition> conditions = new();

        public string Id => id;
        public string Name
        {
            get => string.IsNullOrWhiteSpace(name) ? "Entry Point" : name;
            set => name = value;
        }

        public int Priority
        {
            get => priority;
            set => priority = value;
        }

        public float Weight
        {
            get => weight;
            set => weight = Mathf.Max(0.01f, value);
        }

        public Vector2 Position
        {
            get => position;
            set => position = value;
        }

        public string TargetNodeGuid
        {
            get => targetNodeGuid;
            set => targetNodeGuid = value;
        }

        public List<DialogueConditionDefinition> Conditions => conditions;

        public bool AreConditionsMet(ProgressController progressController)
        {
            for (var index = 0; index < conditions.Count; index++)
            {
                var condition = conditions[index];
                if (condition == null)
                    continue;

                if (!condition.Evaluate(progressController))
                    return false;
            }

            return true;
        }
    }

    [Serializable]
    public abstract class DialogNodeDefinition
    {
        [SerializeField] private string guid = System.Guid.NewGuid().ToString("N");
        [SerializeField] private Vector2 position = new(200f, 200f);

        public string Guid
        {
            get => guid;
            set => guid = value;
        }

        public Vector2 Position
        {
            get => position;
            set => position = value;
        }

        public virtual string DisplayName => GetType().Name;
        public abstract void Validate(DialogAsset asset, List<string> issues);
    }

    [Serializable]
    public class DialogLineNode : DialogNodeDefinition
    {
        [SerializeField] private string title = "NPC Line";
        [TextArea(3, 8)]
        [SerializeField] private string text;
        [SerializeField] private List<DialogChoiceDefinition> choices = new();

        public override string DisplayName => string.IsNullOrWhiteSpace(title) ? "NPC Line" : title;
        public string Title
        {
            get => title;
            set => title = value;
        }

        public string Text
        {
            get => text;
            set => text = value;
        }

        public List<DialogChoiceDefinition> Choices => choices;

        public override void Validate(DialogAsset asset, List<string> issues)
        {
            for (var index = 0; index < choices.Count; index++)
            {
                var choice = choices[index];
                if (choice == null)
                {
                    issues.Add($"Line node '{DisplayName}' contains a null choice.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(choice.Text))
                    issues.Add($"Line node '{DisplayName}' has a choice without text.");

                if (!string.IsNullOrWhiteSpace(choice.NextNodeGuid) && asset.GetNode(choice.NextNodeGuid) == null)
                    issues.Add($"Choice '{choice.Text}' in node '{DisplayName}' points to a missing node.");

            }
        }
    }

    [Serializable]
    public class DialogEventNode : DialogNodeDefinition
    {
        [SerializeField] private string title = "Event";
        [SerializeField] private string nextNodeGuid;
        [SerializeReference] private List<DialogueActionDefinition> actions = new();

        public override string DisplayName => string.IsNullOrWhiteSpace(title) ? "Event" : title;

        public string Title
        {
            get => title;
            set => title = value;
        }

        public string NextNodeGuid
        {
            get => nextNodeGuid;
            set => nextNodeGuid = value;
        }

        public List<DialogueActionDefinition> Actions => actions;

        public override void Validate(DialogAsset asset, List<string> issues)
        {
            if (!string.IsNullOrWhiteSpace(nextNodeGuid) && asset.GetNode(nextNodeGuid) == null)
                issues.Add($"Event node '{DisplayName}' points to a missing node.");
        }
    }

    [Serializable]
    public class DialogRerouteNode : DialogNodeDefinition
    {
        [SerializeField] private string nextNodeGuid;

        public override string DisplayName => "Reroute";

        public string NextNodeGuid
        {
            get => nextNodeGuid;
            set => nextNodeGuid = value;
        }

        public override void Validate(DialogAsset asset, List<string> issues)
        {
            if (!string.IsNullOrWhiteSpace(nextNodeGuid) && asset.GetNode(nextNodeGuid) == null)
                issues.Add("Reroute node points to a missing node.");
        }
    }

    [Serializable]
    public class DialogEndNode : DialogNodeDefinition
    {
        [SerializeField] private string title = "End";
        [TextArea(2, 6)]
        [SerializeField] private string closingText;

        public override string DisplayName => string.IsNullOrWhiteSpace(title) ? "End" : title;

        public string Title
        {
            get => title;
            set => title = value;
        }

        public string ClosingText
        {
            get => closingText;
            set => closingText = value;
        }

        public override void Validate(DialogAsset asset, List<string> issues)
        {
        }
    }

    [Serializable]
    public class DialogChoiceDefinition
    {
        [SerializeField] private string id = System.Guid.NewGuid().ToString("N");
        [TextArea(2, 6)]
        [SerializeField] private string text;
        [SerializeField] private string nextNodeGuid;
        [SerializeField] private DialogChoiceLockMode lockMode;
        [SerializeReference] private List<DialogueConditionDefinition> conditions = new();

        public string Id => id;
        public string Text
        {
            get => text;
            set => text = value;
        }

        public string NextNodeGuid
        {
            get => nextNodeGuid;
            set => nextNodeGuid = value;
        }

        public DialogChoiceLockMode LockMode
        {
            get => lockMode;
            set => lockMode = value;
        }

        public List<DialogueConditionDefinition> Conditions => conditions;

        public bool AreConditionsMet(ProgressController progressController)
        {
            for (var index = 0; index < conditions.Count; index++)
            {
                var condition = conditions[index];
                if (condition == null)
                    continue;

                if (!condition.Evaluate(progressController))
                    return false;
            }

            return true;
        }
    }
}
