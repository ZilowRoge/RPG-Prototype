using System;
using System.Collections.Generic;
using UnityEngine;

namespace NPC.Dialog
{
    [CreateAssetMenu(menuName = "Dialog/Dialog Asset", fileName = "NewDialog")]
    public class DialogAsset : ScriptableObject
    {
        [SerializeField] private string dialogId = "npc_id_or_unique_key";
        [SerializeField] private string startNodeId = "start";
        [SerializeField] private List<DialogNode> nodes = new();

        public string DialogId => dialogId;
        public string StartNodeId => startNodeId;

        public DialogNode GetNode(string nodeId)
            => nodes.Find(n => n != null && n.Id == nodeId);

        public IReadOnlyList<DialogNode> Nodes => nodes;
    }
}
