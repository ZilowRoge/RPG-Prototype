using System;
using UnityEngine;
using UnityEngine.Events;

namespace NPC.Dialog
{
    [Serializable]
    public class DialogCondition
    {
        public string flagKey;
        public bool requiredValue = true;

        public bool Evaluate(Player.Progress.ProgressController state)
        {
            return state != null && state.GetFlag(flagKey) == requiredValue;
        }
    }
}
