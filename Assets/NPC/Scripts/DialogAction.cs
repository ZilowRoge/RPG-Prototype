using System;
using UnityEngine;
using Player.Progress;

namespace NPC.Dialog
{
    [Serializable]
    public class DialogAction
    {
        public enum ActionType { SetFlag, StartQuest, AddJob, LearnSymbol }

        [SerializeField] private ActionType type = ActionType.SetFlag;
        [SerializeField] private string actionId;
        [SerializeField] private bool actionValue = true;

        public void Run(ProgressController controller)
        {
            switch (type)
            {
                case ActionType.SetFlag:
                    var value = actionValue ? "True" : "False";
                    Debug.Log($"Set flag {actionId} to {value}");
                    controller?.SetFlag(actionId, actionValue);
                    break;
                case ActionType.StartQuest:
                    Debug.Log($"Action start quest with id {actionId}");
                    controller?.StartQuest(actionId);
                    break;
                case ActionType.AddJob:
                    Debug.Log($"Action add job with id {actionId}");
                    controller?.AddJob(actionId);
                    break;
                case ActionType.LearnSymbol:
                    Debug.Log($"Action learn symbol with id {actionId}");
                    controller?.LearnSymbol(actionId);
                    break;
            }

        }
    }
}
