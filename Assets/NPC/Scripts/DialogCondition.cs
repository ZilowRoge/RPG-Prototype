using System;using Player.Progress;
using UnityEngine;
using UnityEngine.Serialization;

namespace NPC.Dialog
{
    [Serializable]
    public class DialogCondition
    {
        public enum ConditionType
        {
            FlagEquals,
            QuestStarted,
            QuestActive,
            QuestCompleted,
            HasJob,
            KnowsSymbol
        }

        [SerializeField] private ConditionType conditionType = ConditionType.FlagEquals;
        [FormerlySerializedAs("flagKey")]
        [SerializeField] private string identifier;
        [FormerlySerializedAs("requiredValue")]
        [SerializeField] private bool expectedValue = true;

        public bool Evaluate(ProgressController state)
        {
            Debug.Log($"Evaluate condition with id {identifier}");
            if (state == null)
                return false;

            switch (conditionType)
            {
                case ConditionType.FlagEquals:
                    return state.GetFlag(identifier) == expectedValue;
                case ConditionType.QuestStarted:
                {
                    Debug.Log("Checking condition for quest started");
                    bool isStarted = state.IsQuestStarted(identifier);
                    return expectedValue ? isStarted : !isStarted;
                }
                case ConditionType.QuestActive:
                {
                    bool isActive = state.IsQuestActive(identifier);
                    return expectedValue ? isActive : !isActive;
                }
                case ConditionType.QuestCompleted:
                {
                    bool isCompleted = state.IsQuestCompleted(identifier);
                    return expectedValue ? isCompleted : !isCompleted;
                }
                case ConditionType.HasJob:
                {
                    bool hasJob = state.HasJob(identifier);
                    return expectedValue ? hasJob : !hasJob;
                }
                case ConditionType.KnowsSymbol:
                {
                    bool knows = state.KnowsSymbol(identifier);
                    return expectedValue ? knows : !knows;
                }
            }
            return false;
        }
    }
}
