using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NPC.Dialog
{
    public abstract class DialogNode : ScriptableObject
    {
        [SerializeField] private string id = "node_id";
        public string Id => id;
    }

}
