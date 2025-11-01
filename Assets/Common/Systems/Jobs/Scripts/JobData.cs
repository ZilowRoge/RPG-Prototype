using UnityEngine;
using System;
using System.Collections.Generic;
using Systems.Perks;
using UnityEngine.Serialization;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Systems.Jobs {
[CreateAssetMenu(menuName = "Jobs/Job")]
public class JobData : ScriptableObject
{
    [Header("Identification")]
    public string id;
    public string displayName;
    public JobTier tier;
    public int maxLevel = 20;

    [Header("Descritption and apperance")]
    [TextArea] public string description;
    public Sprite icon;

    [Header("EXP Settings")]
    public int baseExpToLevel = 100;
    public float expGrowthRate = 1.25f;

    [Header("Perk Graph")]
    [SerializeField] private List<JobPerkNode> perkNodes = new();

    public IReadOnlyList<JobPerkNode> PerkNodes
    {
        get
        {
            if (perkNodes == null)
                perkNodes = new List<JobPerkNode>();
            return perkNodes;
        }
    }

    public int GetRequiredExperience(int level)
    {
        return Mathf.RoundToInt(baseExpToLevel * Mathf.Pow(expGrowthRate, level));
    }

    public JobPerkNode GetNodeById(string nodeId)
    {
        if (string.IsNullOrWhiteSpace(nodeId) || perkNodes == null) return null;
        return perkNodes.FirstOrDefault(node =>
            node != null &&
            string.Equals(node.NodeId, nodeId, StringComparison.OrdinalIgnoreCase));
    }

    public JobPerkNode GetNodeByPerk(PerkData perk)
    {
        if (perk == null || perkNodes == null) return null;
        return perkNodes.FirstOrDefault(node => node != null && node.Perk == perk);
    }

    public IEnumerable<JobPerkNode> GetRootNodes()
    {
        if (perkNodes == null) yield break;

        foreach (var node in perkNodes)
        {
            if (node == null) continue;
            var connections = node.ConnectedNodes;
            if (connections == null || connections.Count == 0)
                yield return node;
        }
    }

#if UNITY_EDITOR
    private void Reset()
    {
        EnsureNodeAssets();
        EnsureBidirectionalConnections();
        EnsureNodeIds();
    }
#endif

    private void OnValidate()
    {
        RemoveNullEntries();
#if UNITY_EDITOR
        EnsureNodeAssets();
        EnsureBidirectionalConnections();
#else
#endif
        EnsureNodeIds();
    }

    private void RemoveNullEntries()
    {
        if (perkNodes == null)
        {
            perkNodes = new List<JobPerkNode>();
            return;
        }

        for (int i = perkNodes.Count - 1; i >= 0; i--)
        {
            if (perkNodes[i] == null)
                perkNodes.RemoveAt(i);
        }
    }

    private void EnsureNodeIds()
    {
        if (perkNodes == null) return;

        foreach (var node in perkNodes)
        {
            node?.EnsureStableId();
        }
    }

#if UNITY_EDITOR
    private void EnsureBidirectionalConnections()
    {
        if (perkNodes == null) return;

        foreach (var node in perkNodes)
        {
            if (node == null) continue;

            var connections = node.ConnectionsInternal;
            for (int i = connections.Count - 1; i >= 0; i--)
            {
                var connection = connections[i];
                if (connection == null || connection == node || (perkNodes != null && !perkNodes.Contains(connection)))
                {
                    connections.RemoveAt(i);
                    if (!Application.isPlaying)
                        EditorUtility.SetDirty(node);
                    continue;
                }

                if (!connection.IsConnectedTo(node))
                {
                    connection.AddConnection(node);
                }
            }
        }
    }

    private void EnsureNodeAssets()
    {
        if (perkNodes == null) return;

        var jobAssetPath = AssetDatabase.GetAssetPath(this);
        foreach (var node in perkNodes.Where(n => n != null))
        {
            var nodePath = AssetDatabase.GetAssetPath(node);

            if (string.IsNullOrEmpty(nodePath))
            {
                node.hideFlags = HideFlags.HideInHierarchy;
                AssetDatabase.AddObjectToAsset(node, this);
            }
            else if (nodePath != jobAssetPath)
            {
                // Node is stored as a separate asset; leave it but ensure a stable id.
                continue;
            }

            node.hideFlags = HideFlags.HideInHierarchy;
            EditorUtility.SetDirty(node);
        }
    }

    public JobPerkNode CreateNodeAsset(PerkData perkData, Vector2 position)
    {
        var node = ScriptableObject.CreateInstance<JobPerkNode>();
        node.hideFlags = HideFlags.HideInHierarchy;
        node.Initialize(perkData, position);
        AssetDatabase.AddObjectToAsset(node, this);
        EditorUtility.SetDirty(node);
        return node;
    }
#endif
}
}
