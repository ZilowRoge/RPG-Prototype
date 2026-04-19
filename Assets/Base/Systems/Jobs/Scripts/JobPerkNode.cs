using System;
using System.Collections.Generic;
using UnityEngine;
using Systems.Perks;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Systems.Jobs
{
    /// <summary>
    /// ScriptableObject representing a single perk node within a job graph.
    /// Stores references to its perk, layout data, and adjacent nodes.
    /// </summary>
#if UNITY_EDITOR
    [CreateAssetMenu(menuName = "Jobs/Job Perk Node")]
#endif
    public class JobPerkNode : ScriptableObject
    {
        [SerializeField] private string nodeId;
        [SerializeField] private PerkData perk;
        [SerializeField] private Vector2 position;
        [SerializeField, FormerlySerializedAs("prerequisites")] private List<JobPerkNode> connectedNodes = new();

        public string NodeId => nodeId;
        public PerkData Perk => perk;
        public bool HasPerk => perk != null;
        public bool IsInitial => perk == null;
        public Vector2 Position
        {
            get => position;
            set => position = value;
        }

        public IReadOnlyList<JobPerkNode> ConnectedNodes => connectedNodes;

        internal List<JobPerkNode> ConnectionsInternal
        {
            get
            {
                connectedNodes ??= new List<JobPerkNode>();
                return connectedNodes;
            }
        }

        public void EnsureStableId()
        {
            if (!string.IsNullOrWhiteSpace(nodeId)) return;

            nodeId = Guid.NewGuid().ToString("N");
        }

        public void Initialize(PerkData perkData, Vector2 fallbackPosition)
        {
            perk = perkData;
            position = fallbackPosition;
            EnsureStableId();
#if UNITY_EDITOR
            name = string.IsNullOrWhiteSpace(perkData?.perkName)
                ? "JobPerkNode"
                : $"Node_{perkData.perkName}";
#endif
        }

        public bool IsConnectedTo(JobPerkNode node)
        {
            return node != null && connectedNodes != null && connectedNodes.Contains(node);
        }

        public void SetConnections(IEnumerable<JobPerkNode> nodes)
        {
            ConnectionsInternal.Clear();

            if (nodes == null) return;

            foreach (var other in nodes)
            {
                if (AddConnection(other))
                {
                    other?.AddConnection(this);
                }
            }
        }

        internal bool AddConnection(JobPerkNode node)
        {
            if (node == null || node == this) return false;

            var list = ConnectionsInternal;
            if (list.Contains(node)) return false;

            list.Add(node);
#if UNITY_EDITOR
            if (!Application.isPlaying)
                EditorUtility.SetDirty(this);
#endif
            return true;
        }

        internal bool RemoveConnection(JobPerkNode node)
        {
            if (node == null || connectedNodes == null) return false;

            var removed = connectedNodes.Remove(node);
#if UNITY_EDITOR
            if (removed && !Application.isPlaying)
                EditorUtility.SetDirty(this);
#endif
            return removed;
        }

        private void OnValidate()
        {
            EnsureStableId();

            if (connectedNodes == null)
            {
                connectedNodes = new List<JobPerkNode>();
                return;
            }

            for (int i = connectedNodes.Count - 1; i >= 0; i--)
            {
                var connection = connectedNodes[i];
                if (connection == null || connection == this)
                {
                    connectedNodes.RemoveAt(i);
                }
            }

            // Remove duplicates while preserving order.
            for (int i = 0; i < connectedNodes.Count; i++)
            {
                var current = connectedNodes[i];
                for (int j = connectedNodes.Count - 1; j > i; j--)
                {
                    if (connectedNodes[j] == current)
                        connectedNodes.RemoveAt(j);
                }
            }
        }
    }
}
