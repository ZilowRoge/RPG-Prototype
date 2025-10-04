using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NPC.Dialog
{
    [CreateAssetMenu(menuName = "Dialog/Nodes/NPC Line", fileName = "Node_NPCLine")]
    public class NpcLineNode : DialogNode
    {
        [TextArea(2, 6)] public string text;
        // TODO generate sprite and audio clips in later time
        // public Sprite portrait;
        // public AudioClip voiceOver;
        public List<DialogOption> options = new();
    }

    [Serializable]
    public class DialogOption
    {
        public string text;
        public string nextNodeId;
        public List<DialogCondition> conditions = new();
        public List<DialogAction> onSelect = new();
        public bool HideIfLocked = true;
    }
}
