using System;
using System.Collections.Generic;
using System.Linq;
using Player.Progress;
using UnityEngine;

namespace NPC.Dialog
{
    [CreateAssetMenu(menuName = "Dialog/Dialog Graph", fileName = "NewDialogGraph")]
    public class DialogAsset : ScriptableObject
    {
        [SerializeField] private string dialogId = "npc_id_or_unique_key";
        [SerializeField] private int graphVersion = 1;
        [SerializeField] private Vector2 editorPan;
        [SerializeReference] private List<DialogNodeDefinition> nodes = new();
        [SerializeField] private List<DialogEntryPoint> entryPoints = new();

        public string DialogId => dialogId;
        public int GraphVersion => graphVersion;
        public Vector2 EditorPan
        {
            get => editorPan;
            set => editorPan = value;
        }

        public IReadOnlyList<DialogNodeDefinition> Nodes => nodes;
        public IReadOnlyList<DialogEntryPoint> EntryPoints => entryPoints;

        public DialogNodeDefinition GetNode(string guid)
        {
            if (string.IsNullOrWhiteSpace(guid))
                return null;

            return nodes.FirstOrDefault(node => node != null && node.Guid == guid);
        }

        public DialogNodeDefinition GetStartNode(ProgressController progressController)
        {
            var startEntry = ResolveEntryPoint(progressController);
            if (startEntry == null)
                return null;

            return GetNode(startEntry.TargetNodeGuid);
        }

        public DialogEntryPoint ResolveEntryPoint(ProgressController progressController)
        {
            if (entryPoints == null || entryPoints.Count == 0)
                return null;

            List<DialogEntryPoint> validEntries = null;
            var highestPriority = int.MinValue;

            for (var index = 0; index < entryPoints.Count; index++)
            {
                var entry = entryPoints[index];
                if (entry == null || string.IsNullOrWhiteSpace(entry.TargetNodeGuid))
                    continue;

                if (!entry.AreConditionsMet(progressController))
                    continue;

                if (entry.Priority < highestPriority)
                    continue;

                if (entry.Priority > highestPriority)
                {
                    highestPriority = entry.Priority;
                    validEntries = new List<DialogEntryPoint>();
                }

                validEntries ??= new List<DialogEntryPoint>();
                validEntries.Add(entry);
            }

            if (validEntries == null || validEntries.Count == 0)
                return null;

            if (validEntries.Count == 1)
                return validEntries[0];

            var totalWeight = 0f;
            foreach (var entry in validEntries)
            {
                totalWeight += Mathf.Max(0.01f, entry.Weight);
            }

            var roll = UnityEngine.Random.Range(0f, totalWeight);
            foreach (var entry in validEntries)
            {
                roll -= Mathf.Max(0.01f, entry.Weight);
                if (roll <= 0f)
                    return entry;
            }

            return validEntries[validEntries.Count - 1];
        }

        public void SetNodes(List<DialogNodeDefinition> newNodes)
        {
            nodes = newNodes ?? new List<DialogNodeDefinition>();
        }

        public void SetEntryPoints(List<DialogEntryPoint> newEntryPoints)
        {
            entryPoints = newEntryPoints ?? new List<DialogEntryPoint>();
        }

        public List<string> ValidateGraph()
        {
            var issues = new List<string>();
            var guidSet = new HashSet<string>();
            var referencedNodeIds = new HashSet<string>();
            var reachableNodeIds = new HashSet<string>();

            for (var index = 0; index < nodes.Count; index++)
            {
                var node = nodes[index];
                if (node == null)
                {
                    issues.Add($"Node at index {index} is null.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(node.Guid))
                {
                    issues.Add($"Node '{node.DisplayName}' has no guid.");
                    continue;
                }

                if (!guidSet.Add(node.Guid))
                    issues.Add($"Duplicate node guid detected: {node.Guid}");

                node.Validate(this, issues);

                foreach (var target in EnumerateTargets(node))
                {
                    if (!string.IsNullOrWhiteSpace(target))
                        referencedNodeIds.Add(target);
                }
            }

            for (var index = 0; index < entryPoints.Count; index++)
            {
                var entry = entryPoints[index];
                if (entry == null)
                {
                    issues.Add($"Entry point at index {index} is null.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(entry.TargetNodeGuid))
                {
                    issues.Add($"Entry point '{entry.Name}' has no target node.");
                    continue;
                }

                if (GetNode(entry.TargetNodeGuid) == null)
                    issues.Add($"Entry point '{entry.Name}' points to a missing node.");
                else
                    MarkReachable(entry.TargetNodeGuid, reachableNodeIds);
            }

            if (entryPoints.Count == 0)
                issues.Add("Graph has no entry points.");

            foreach (var node in nodes)
            {
                if (node == null || string.IsNullOrWhiteSpace(node.Guid))
                    continue;

                if (!reachableNodeIds.Contains(node.Guid))
                    issues.Add($"Node '{node.DisplayName}' is unreachable from any entry point.");

                if (node is not DialogEndNode && !EnumerateTargets(node).Any(target => !string.IsNullOrWhiteSpace(target)))
                    issues.Add($"Node '{node.DisplayName}' has no outgoing connection.");
            }

            foreach (var target in referencedNodeIds)
            {
                if (GetNode(target) == null)
                    issues.Add($"Graph contains a reference to missing node guid '{target}'.");
            }

            return issues;
        }

        private IEnumerable<string> EnumerateTargets(DialogNodeDefinition node)
        {
            switch (node)
            {
                case DialogLineNode lineNode:
                    for (var index = 0; index < lineNode.Choices.Count; index++)
                    {
                        var choice = lineNode.Choices[index];
                        if (choice != null)
                            yield return choice.NextNodeGuid;
                    }
                    break;
                case DialogEventNode eventNode:
                    yield return eventNode.NextNodeGuid;
                    break;
                case DialogRerouteNode rerouteNode:
                    yield return rerouteNode.NextNodeGuid;
                    break;
            }
        }

        private void MarkReachable(string startGuid, HashSet<string> reachable)
        {
            if (string.IsNullOrWhiteSpace(startGuid) || reachable == null)
                return;

            var pending = new Stack<string>();
            pending.Push(startGuid);

            while (pending.Count > 0)
            {
                var guid = pending.Pop();
                if (string.IsNullOrWhiteSpace(guid) || !reachable.Add(guid))
                    continue;

                var node = GetNode(guid);
                if (node == null)
                    continue;

                foreach (var target in EnumerateTargets(node))
                {
                    if (!string.IsNullOrWhiteSpace(target))
                        pending.Push(target);
                }
            }
        }
    }
}
