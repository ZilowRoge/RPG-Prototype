using System;
using UnityEngine;
using UnityEngine.Events;
using Player.Progress;

namespace NPC.Dialog
{
    [Serializable]
    public class DialogAction
    {
        public enum ActionType { SetFlag, StartQuest, AddJob }

        public ActionType type = ActionType.SetFlag;
        public string actionId;
        public bool actionValue = true;

        // Możesz rozszerzyć switch i dodać UnityEvents jeśli chcesz:
        public UnityEvent onRun;

        public void Run(ProgressController controller)
        {
            switch (type)
            {
                case ActionType.SetFlag:
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
            }
            onRun?.Invoke();
        }
    }
}
