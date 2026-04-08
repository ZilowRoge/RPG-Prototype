using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Systems.Jobs;

namespace UI.Player.Perks
{
    /// <summary>
    /// Controls perk graph presentation, queued unlock flow, and connection visuals for a job.
    /// </summary>
    public class PerkControllerUI : MonoBehaviour
    {
        [Header("Graph Layout")]
        [SerializeField] private RectTransform graphContainer;
        [SerializeField] private GameObject nodePrefab;
        [SerializeField] private GameObject connectionPrefab;
        [SerializeField] private float connectionThickness = 6f;

        [Header("UI Controls")]
        [SerializeField] private Button applyButton;
        [SerializeField] private Button clearSelectionButton;
        [SerializeField] private TMP_Text remainingPointsLabel;

        [Header("Tooltip")]
        [SerializeField] private PerkTooltipUI tooltip;

        [Header("Connection Colors")]
        [SerializeField] private Color connectionLockedColor = new(0.3f, 0.3f, 0.3f, 0.7f);
        [SerializeField] private Color connectionAvailableColor = new(0.85f, 0.76f, 0.28f, 0.9f);
        [SerializeField] private Color connectionQueuedColor = new(1f, 0.82f, 0.4f, 0.95f);
        [SerializeField] private Color connectionUnlockedColor = new(0.2f, 0.82f, 0.32f, 0.95f);

        private readonly Dictionary<JobPerkNode, NodeView> nodeLookup = new();
        private readonly List<ConnectionView> connections = new();
        private readonly List<JobPerkNode> queuedOrder = new();
        private readonly HashSet<JobPerkNode> queuedSet = new();

        private JobInstance activeJob;

        private void Awake()
        {
            if (graphContainer == null)
            {
                graphContainer = GetComponent<RectTransform>();
            }

            if (applyButton != null)
            {
                applyButton.onClick.RemoveListener(OnApplyButtonClicked);
                applyButton.onClick.AddListener(OnApplyButtonClicked);
            }

            if (clearSelectionButton != null)
            {
                clearSelectionButton.onClick.RemoveListener(OnClearButtonClicked);
                clearSelectionButton.onClick.AddListener(OnClearButtonClicked);
            }
        }

        public void Show(JobInstance job)
        {
            activeJob = job;
            queuedOrder.Clear();
            queuedSet.Clear();
            tooltip?.Hide();

            RebuildGraph();
            RefreshView();
            UpdateControlState();
        }

        public void ToggleNode(PerkNodeUI nodeView)
        {
            if (activeJob == null || nodeView == null)
                return;

            var node = nodeView.NodeData;
            if (node == null || node.IsInitial)
                return;

            if (queuedSet.Contains(node))
            {
                if (queuedOrder.Count == 0 || queuedOrder[^1] != node)
                    return; // only the latest queued node can be toggled off

                queuedOrder.RemoveAt(queuedOrder.Count - 1);
                queuedSet.Remove(node);
                RefreshView();
                UpdateControlState();
                return;
            }

            if (!CanQueueNode(node))
                return;

            queuedOrder.Add(node);
            queuedSet.Add(node);
            RefreshView();
            UpdateControlState();
        }

        private bool CanQueueNode(JobPerkNode node)
        {
            if (node == null || node.IsInitial)
                return false;
            if (queuedSet.Contains(node))
                return false;
            if (activeJob == null || activeJob.IsNodeUnlocked(node))
                return false;
            if (!HasQueueCapacity())
                return false;

            return ArePrerequisitesSatisfied(node);
        }

        private bool HasQueueCapacity()
        {
            if (activeJob == null) return false;
            return activeJob.PerkPoints - queuedOrder.Count > 0;
        }

        private bool IsQueued(JobPerkNode node) => queuedSet.Contains(node);

        private bool IsUnlocked(JobPerkNode node)
        {
            if (node == null || activeJob == null)
                return false;

            return activeJob.IsNodeUnlocked(node);
        }

        private bool ArePrerequisitesSatisfied(JobPerkNode node)
        {
            var links = node?.ConnectedNodes;
            if (links == null || links.Count == 0)
                return true;

            foreach (var link in links)
            {
                if (link == null) continue;
                if (!link.HasPerk)
                    return true; // connected to initial/root node
                if (IsUnlocked(link) || IsQueued(link))
                    return true;
            }

            return false;
        }

        private void RebuildGraph()
        {
            ClearSpawnedObjects();

            if (activeJob?.Data == null || nodePrefab == null)
                return;

            var nodes = activeJob.Data.PerkNodes;
            if (nodes == null || nodes.Count == 0)
                return;

            foreach (var node in nodes)
            {
                if (node == null) continue;

                var instance = Instantiate(nodePrefab, graphContainer);
                instance.name = $"PerkNode_{node.NodeId}";

                if (!instance.TryGetComponent(out RectTransform rect))
                {
                    rect = instance.AddComponent<RectTransform>();
                }

                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = node.Position;

                var widget = instance.GetComponent<PerkNodeUI>();
                if (widget == null)
                {
                    Debug.LogWarning($"Perk node prefab is missing {nameof(PerkNodeUI)} component.", instance);
                    DestroyInstance(instance);
                    continue;
                }

                widget.Initialize(node, this);

                nodeLookup[node] = new NodeView(node, widget, rect);
            }

            BuildConnections();
        }

        private void BuildConnections()
        {
            if (connectionPrefab == null || nodeLookup.Count == 0)
                return;

            var spawnedKeys = new HashSet<string>();

            foreach (var kvp in nodeLookup)
            {
                var node = kvp.Key;
                var rect = kvp.Value.Rect;
                var neighbors = node.ConnectedNodes;
                if (neighbors == null || neighbors.Count == 0)
                    continue;

                foreach (var neighbor in neighbors)
                {
                    if (neighbor == null || !nodeLookup.TryGetValue(neighbor, out var neighborView))
                        continue;

                    var key = BuildEdgeKey(node, neighbor);
                    if (!spawnedKeys.Add(key))
                        continue;

                    var connection = Instantiate(connectionPrefab, graphContainer);
                    connection.name = $"Connection_{node.NodeId}_{neighbor.NodeId}";

                    if (!connection.TryGetComponent(out RectTransform connectionRect))
                        connectionRect = connection.AddComponent<RectTransform>();

                    connectionRect.anchorMin = connectionRect.anchorMax = new Vector2(0.5f, 0.5f);
                    connection.transform.SetAsFirstSibling();

                    var graphic = connection.GetComponent<Graphic>();
                    connections.Add(new ConnectionView(node, neighbor, connectionRect, graphic));

                    UpdateConnectionGeometry(connectionRect, rect, neighborView.Rect);
                }
            }
        }

        private static string BuildEdgeKey(JobPerkNode a, JobPerkNode b)
        {
            string idA = !string.IsNullOrEmpty(a.NodeId) ? a.NodeId : a.GetEntityId().ToString();
            string idB = !string.IsNullOrEmpty(b.NodeId) ? b.NodeId : b.GetEntityId().ToString();

            return string.CompareOrdinal(idA, idB) <= 0 ? $"{idA}|{idB}" : $"{idB}|{idA}";
        }

        private void RefreshView()
        {
            foreach (var view in nodeLookup.Values)
            {
                var node = view.Node;
                bool isInitial = node.IsInitial;
                bool isUnlocked = !isInitial && IsUnlocked(node);
                bool isQueued = !isInitial && IsQueued(node);
                bool hasCapacity = HasQueueCapacity();
                bool isAvailable = !isInitial && !isUnlocked && !isQueued && hasCapacity && ArePrerequisitesSatisfied(node);

                bool interactable = !isInitial && ((isQueued && queuedOrder.Count > 0 && queuedOrder[^1] == node) || isAvailable);

                view.Widget.ApplyState(new NodeVisualState(
                    isInitial,
                    isUnlocked,
                    isQueued,
                    isAvailable,
                    interactable));
            }

            foreach (var connection in connections)
            {
                UpdateConnectionGeometry(connection.Rect, nodeLookup[connection.A].Rect, nodeLookup[connection.B].Rect);
                ApplyConnectionColor(connection);
            }
        }

        private void ApplyConnectionColor(ConnectionView connection)
        {
            if (connection.Graphic == null)
                return;

            var state = EvaluateConnectionState(connection.A, connection.B);

            connection.Graphic.color = state switch
            {
                ConnectionState.Unlocked => connectionUnlockedColor,
                ConnectionState.Queued => connectionQueuedColor,
                ConnectionState.Available => connectionAvailableColor,
                _ => connectionLockedColor
            };
        }

        private ConnectionState EvaluateConnectionState(JobPerkNode a, JobPerkNode b)
        {
            int rankA = GetNodeRank(a);
            int rankB = GetNodeRank(b);

            int rank = Mathf.Min(rankA, rankB);
            return rank switch
            {
                >= 3 => ConnectionState.Unlocked,
                2 => ConnectionState.Queued,
                1 => ConnectionState.Available,
                _ => ConnectionState.Locked
            };
        }

        private int GetNodeRank(JobPerkNode node)
        {
            if (node == null)
                return 0;
            if (node.IsInitial)
                return 3;
            if (IsUnlocked(node))
                return 3;
            if (IsQueued(node))
                return 2;
            if (ArePrerequisitesSatisfied(node) && HasQueueCapacity())
                return 1;

            return 0;
        }

        private void ClearSpawnedObjects()
        {
            foreach (var connection in connections)
            {
                if (connection.Rect == null) continue;
                DestroyInstance(connection.Rect.gameObject);
            }
            connections.Clear();

            foreach (var node in nodeLookup.Values)
            {
                if (node.Rect == null) continue;
                DestroyInstance(node.Rect.gameObject);
            }
            nodeLookup.Clear();
        }

        private void DestroyInstance(GameObject instance)
        {
            if (instance == null) return;
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(instance);
            else
#endif
                Destroy(instance);
        }

        private void CommitQueuedUnlocks()
        {
            if (activeJob == null || queuedOrder.Count == 0)
                return;

            foreach (var node in queuedOrder)
            {
                if (node == null) continue;
                if (!activeJob.CanUnlock(node))
                    continue;

                activeJob.Unlock(node);
            }

            queuedOrder.Clear();
            queuedSet.Clear();

            RefreshView();
            UpdateControlState();
        }

        private void ClearQueuedNodes()
        {
            if (queuedOrder.Count == 0)
                return;

            queuedOrder.Clear();
            queuedSet.Clear();
            RefreshView();
            UpdateControlState();
        }

        private void UpdateControlState()
        {
            int queuedCount = queuedOrder.Count;
            int remainingPoints = activeJob != null ? Mathf.Max(0, activeJob.PerkPoints - queuedCount) : 0;

            if (applyButton != null)
                applyButton.interactable = queuedCount > 0;

            if (clearSelectionButton != null)
                clearSelectionButton.interactable = queuedCount > 0;

            if (remainingPointsLabel != null)
                remainingPointsLabel.text = remainingPoints.ToString();
        }

        private void UpdateConnectionGeometry(RectTransform connectionRect, RectTransform from, RectTransform to)
        {
            if (connectionRect == null || from == null || to == null)
                return;

            Vector2 start = from.anchoredPosition;
            Vector2 end = to.anchoredPosition;
            Vector2 direction = end - start;
            float length = direction.magnitude;

            float angle = direction == Vector2.zero
                ? 0f
                : Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            var size = connectionRect.sizeDelta;
            size.x = length;
            size.y = connectionThickness;
            connectionRect.sizeDelta = size;
            connectionRect.anchoredPosition = start + direction * 0.5f;
            connectionRect.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void OnApplyButtonClicked() => CommitQueuedUnlocks();

        private void OnClearButtonClicked() => ClearQueuedNodes();

        private void OnDisable() => tooltip?.Hide();

        internal void ShowTooltip(JobPerkNode node, Vector2 screenPosition)
        {
            if (tooltip == null)
                return;

            if (node == null || node.Perk == null)
            {
                tooltip.Hide();
                return;
            }

            tooltip.Show(node, screenPosition);
        }

        internal void HideTooltip(PerkNodeUI source)
        {
            _ = source;
            tooltip?.Hide();
        }

        private readonly struct NodeView
        {
            public JobPerkNode Node { get; }
            public PerkNodeUI Widget { get; }
            public RectTransform Rect { get; }

            public NodeView(JobPerkNode node, PerkNodeUI widget, RectTransform rect)
            {
                Node = node;
                Widget = widget;
                Rect = rect;
            }
        }

        private readonly struct ConnectionView
        {
            public JobPerkNode A { get; }
            public JobPerkNode B { get; }
            public RectTransform Rect { get; }
            public Graphic Graphic { get; }

            public ConnectionView(JobPerkNode a, JobPerkNode b, RectTransform rect, Graphic graphic)
            {
                A = a;
                B = b;
                Rect = rect;
                Graphic = graphic;
            }
        }

        private enum ConnectionState
        {
            Locked = 0,
            Available = 1,
            Queued = 2,
            Unlocked = 3
        }
    }

    public readonly struct NodeVisualState
    {
        public bool IsInitial { get; }
        public bool IsUnlocked { get; }
        public bool IsQueued { get; }
        public bool IsAvailable { get; }
        public bool IsInteractable { get; }

        public NodeVisualState(bool isInitial, bool isUnlocked, bool isQueued, bool isAvailable, bool isInteractable)
        {
            IsInitial = isInitial;
            IsUnlocked = isUnlocked;
            IsQueued = isQueued;
            IsAvailable = isAvailable;
            IsInteractable = isInteractable;
        }
    }
}
