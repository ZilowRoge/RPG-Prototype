using System;
using System.Collections.Generic;
using Items;
using UnityEngine;

namespace Quests
{
    public enum ObjectiveType
    {
        TalkTo,
        AcquireJob,
        LearnSymbol,
        FlagTrue,
        Elimination
    }

    [Serializable]
    public class ObjectiveDef
    {
        public string id = "obj_id";
        public ObjectiveType type = ObjectiveType.TalkTo;
        public string targetId = "";
        public string displayName = "";
        public string extraId = "";
        public int requiredCount = 1;
        public bool visibleInJournal = true;
    }

    [Serializable]
    public class StageDef
    {
        public string id = "stage_id";
        public string title = "Stage Title";
        [TextArea(2, 6)] public string description = "";
        public List<ObjectiveDef> objectives = new();
    }

    [CreateAssetMenu(menuName = "Quests/Quest Asset", fileName = "QuestAsset")]
    public class QuestAsset : ScriptableObject
    {
        [Header("Identity")]
        public string questId = "q_id";
        public string title = "Quest Title";
        [TextArea(2, 6)] public string shortDescription = "";
        [Header("Rewards")]
        public int rewardXp = 0;
        public string rewardNote = "";
        public List<ItemInstance> itemRewards = new();
        [Header("Flow")]
        public List<StageDef> stages = new();
    }
}
