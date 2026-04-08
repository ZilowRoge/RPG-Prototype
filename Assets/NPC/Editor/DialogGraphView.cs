using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace NPC.Dialog.Editor
{
    public class DialogGraphView : GraphView
    {
        private readonly DialogGraphEditorWindow window;
        private DialogAsset asset;
        private readonly GridBackground gridBackground;
        private readonly Dictionary<string, Port> entryPorts = new();
        private readonly Dictionary<string, Port> choicePorts = new();
        private readonly Dictionary<string, Port> eventPorts = new();
        private readonly Dictionary<string, Port> inputPorts = new();
        private bool suppressGraphChanges;
        private bool isPanning;
        private Vector2 panStartMouse;
        private Vector3 panStartPosition;

        public DialogGraphView(DialogGraphEditorWindow window)
        {
            this.window = window;
            style.flexGrow = 1f;
            focusable = true;
            gridBackground = new GridBackground();
            gridBackground.StretchToParentSize();
            Insert(0, gridBackground);
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            this.AddManipulator(new ContextualMenuManipulator(BuildGraphMenu));
            RegisterPanCallbacks(this);
            RegisterPanCallbacks(gridBackground);
            RegisterCallback<ContextualMenuPopulateEvent>(BuildGraphMenu, TrickleDown.TrickleDown);
            gridBackground.RegisterCallback<ContextualMenuPopulateEvent>(BuildGraphMenu, TrickleDown.TrickleDown);

            graphViewChanged = OnGraphViewChanged;
        }

        public void Bind(DialogAsset dialogAsset)
        {
            suppressGraphChanges = true;
            asset = dialogAsset;
            DeleteElements(graphElements.ToList());
            entryPorts.Clear();
            choicePorts.Clear();
            eventPorts.Clear();
            inputPorts.Clear();

            if (asset == null)
            {
                suppressGraphChanges = false;
                return;
            }

            for (var index = 0; index < asset.EntryPoints.Count; index++)
            {
                var entry = asset.EntryPoints[index];
                if (entry == null)
                    continue;

                AddElement(CreateEntryNode(entry, entry.Position));
            }

            foreach (var dialogNode in asset.Nodes)
            {
                if (dialogNode == null)
                    continue;

                AddElement(CreateNodeView(dialogNode));
            }

            CreateEdges();
            suppressGraphChanges = false;
        }

        public void CreateEntryPoint()
        {
            window.CreateEntryPoint();
        }

        private void BuildGraphMenu(ContextualMenuPopulateEvent evt)
        {
            var mousePosition = evt.mousePosition;
            if (evt.currentTarget is VisualElement currentTarget)
                mousePosition = currentTarget.ChangeCoordinatesTo(contentViewContainer, evt.localMousePosition);
            else
                mousePosition = contentViewContainer.WorldToLocal(evt.mousePosition);

            evt.menu.AppendAction("Create/Line Node", _ => window.CreateNode(typeof(DialogLineNode), mousePosition));
            evt.menu.AppendAction("Create/Event Node", _ => window.CreateNode(typeof(DialogEventNode), mousePosition));
            evt.menu.AppendAction("Create/Reroute Node", _ => window.CreateNode(typeof(DialogRerouteNode), mousePosition));
            evt.menu.AppendAction("Create/End Node", _ => window.CreateNode(typeof(DialogEndNode), mousePosition));
            evt.menu.AppendAction("Create/Entry Point", _ => window.CreateEntryPoint());
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            if (suppressGraphChanges)
                return change;

            if (change.movedElements != null)
            {
                foreach (var element in change.movedElements.OfType<EntryPointNodeView>().ToList())
                {
                    window.UpdateEntryPointPosition(element.EntryId, element.GetPosition().position);
                }

                foreach (var element in change.movedElements.OfType<DialogNodeView>().ToList())
                {
                    window.UpdateNodePosition(element.NodeGuid, element.GetPosition().position);
                }
            }

            if (change.edgesToCreate != null)
            {
                foreach (var edge in change.edgesToCreate.ToList())
                {
                    if (edge.output.userData is not PortBinding outputBinding || edge.input.userData is not PortBinding inputBinding)
                        continue;

                    ApplyConnection(outputBinding, inputBinding.OwnerId);
                }
            }

            if (change.elementsToRemove != null)
            {
                foreach (var element in change.elementsToRemove.ToList())
                {
                    switch (element)
                    {
                        case Edge edge when edge.output.userData is PortBinding binding:
                            ApplyConnection(binding, string.Empty);
                            break;
                        case EntryPointNodeView entryView:
                            window.DeleteEntryPoint(entryView.EntryId);
                            break;
                        case DialogNodeView dialogNodeView:
                            window.DeleteNode(dialogNodeView.NodeGuid);
                            break;
                    }
                }
            }

            return change;
        }

        private void ApplyConnection(PortBinding outputBinding, string targetNodeGuid)
        {
            switch (outputBinding.Kind)
            {
                case PortBindingKind.EntryPoint:
                    window.SetEntryPointTarget(outputBinding.OwnerId, targetNodeGuid);
                    break;
                case PortBindingKind.Choice:
                    window.SetChoiceTarget(outputBinding.OwnerId, outputBinding.SecondaryId, targetNodeGuid);
                    break;
                case PortBindingKind.Event:
                    window.SetEventTarget(outputBinding.OwnerId, targetNodeGuid);
                    break;
                case PortBindingKind.Reroute:
                    window.SetRerouteTarget(outputBinding.OwnerId, targetNodeGuid);
                    break;
            }
        }

        private EntryPointNodeView CreateEntryNode(DialogEntryPoint entry, Vector2 position)
        {
            var node = new EntryPointNodeView(entry);
            node.SetPosition(new Rect(position, new Vector2(220f, 120f)));
            node.OnSelectedAction += () => window.NotifySelectionChanged(DialogGraphEditorWindow.SelectionDescriptor.ForEntry(entry.Id));
            node.RegisterCallback<MouseDownEvent>(_ => window.NotifySelectionChanged(DialogGraphEditorWindow.SelectionDescriptor.ForEntry(entry.Id)));
            node.RegisterCallback<ContextualMenuPopulateEvent>(BuildGraphMenu, TrickleDown.TrickleDown);

            var output = node.InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            output.portName = "Start";
            output.userData = new PortBinding(PortBindingKind.EntryPoint, entry.Id);
            node.outputContainer.Add(output);
            entryPorts[entry.Id] = output;

            node.RefreshExpandedState();
            node.RefreshPorts();
            return node;
        }

        private DialogNodeView CreateNodeView(DialogNodeDefinition definition)
        {
            var node = new DialogNodeView(definition);
            node.SetPosition(new Rect(definition.Position, new Vector2(260f, 160f)));
            node.OnSelectedAction += () => window.NotifySelectionChanged(DialogGraphEditorWindow.SelectionDescriptor.ForNode(definition.Guid));
            node.RegisterCallback<MouseDownEvent>(_ => window.NotifySelectionChanged(DialogGraphEditorWindow.SelectionDescriptor.ForNode(definition.Guid)));
            node.RegisterCallback<ContextualMenuPopulateEvent>(BuildGraphMenu, TrickleDown.TrickleDown);

            var input = node.InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
            input.portName = "In";
            input.userData = new PortBinding(PortBindingKind.Input, definition.Guid);
            node.inputContainer.Add(input);
            inputPorts[definition.Guid] = input;

            switch (definition)
            {
                case DialogLineNode lineNode:
                    for (var index = 0; index < lineNode.Choices.Count; index++)
                    {
                        var choice = lineNode.Choices[index];
                        var port = node.InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
                        port.portName = string.IsNullOrWhiteSpace(choice.Text) ? $"Choice {index + 1}" : choice.Text;
                        port.userData = new PortBinding(PortBindingKind.Choice, lineNode.Guid, choice.Id);
                        port.RegisterCallback<MouseDownEvent>(_ => window.NotifySelectionChanged(DialogGraphEditorWindow.SelectionDescriptor.ForChoice(lineNode.Guid, choice.Id)));
                        node.outputContainer.Add(port);
                        choicePorts[ComposeKey(lineNode.Guid, choice.Id)] = port;
                    }
                    break;
                case DialogEventNode eventNode:
                    var eventPort = node.InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
                    eventPort.portName = "Next";
                    eventPort.userData = new PortBinding(PortBindingKind.Event, eventNode.Guid);
                    node.outputContainer.Add(eventPort);
                    eventPorts[eventNode.Guid] = eventPort;
                    break;
                case DialogRerouteNode rerouteNode:
                    node.title = string.Empty;
                    node.style.width = 70f;
                    node.style.minWidth = 70f;
                    node.style.height = 34f;
                    node.style.minHeight = 34f;
                    node.mainContainer.style.paddingLeft = 0f;
                    node.mainContainer.style.paddingRight = 0f;
                    node.mainContainer.style.paddingTop = 0f;
                    node.mainContainer.style.paddingBottom = 0f;
                    node.titleContainer.style.display = DisplayStyle.None;
                    node.inputContainer.style.flexDirection = FlexDirection.Row;
                    node.outputContainer.style.flexDirection = FlexDirection.Row;
                    node.inputContainer.style.justifyContent = Justify.FlexStart;
                    node.outputContainer.style.justifyContent = Justify.FlexEnd;

                    input.portName = string.Empty;
                    input.style.flexGrow = 1f;
                    input.style.marginLeft = 2f;

                    var rerouteOutput = node.InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
                    rerouteOutput.portName = string.Empty;
                    rerouteOutput.userData = new PortBinding(PortBindingKind.Reroute, rerouteNode.Guid);
                    rerouteOutput.style.flexGrow = 1f;
                    rerouteOutput.style.marginRight = 2f;
                    node.outputContainer.Add(rerouteOutput);
                    eventPorts[rerouteNode.Guid] = rerouteOutput;
                    break;
            }

            node.RefreshExpandedState();
            node.RefreshPorts();
            return node;
        }

        private void CreateEdges()
        {
            foreach (var entry in asset.EntryPoints)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.TargetNodeGuid))
                    continue;

                if (entryPorts.TryGetValue(entry.Id, out var output) && inputPorts.TryGetValue(entry.TargetNodeGuid, out var input))
                    AddElement(output.ConnectTo(input));
            }

            foreach (var dialogNode in asset.Nodes)
            {
                switch (dialogNode)
                {
                    case DialogLineNode lineNode:
                        foreach (var choice in lineNode.Choices)
                        {
                            if (choice == null || string.IsNullOrWhiteSpace(choice.NextNodeGuid))
                                continue;

                            if (choicePorts.TryGetValue(ComposeKey(lineNode.Guid, choice.Id), out var output) &&
                                inputPorts.TryGetValue(choice.NextNodeGuid, out var input))
                            {
                                AddElement(output.ConnectTo(input));
                            }
                        }
                        break;
                    case DialogEventNode eventNode:
                        if (string.IsNullOrWhiteSpace(eventNode.NextNodeGuid))
                            continue;

                        if (eventPorts.TryGetValue(eventNode.Guid, out var eventOutput) &&
                            inputPorts.TryGetValue(eventNode.NextNodeGuid, out var eventInput))
                        {
                            AddElement(eventOutput.ConnectTo(eventInput));
                        }
                        break;
                    case DialogRerouteNode rerouteNode:
                        if (string.IsNullOrWhiteSpace(rerouteNode.NextNodeGuid))
                            continue;

                        if (eventPorts.TryGetValue(rerouteNode.Guid, out var rerouteOutput) &&
                            inputPorts.TryGetValue(rerouteNode.NextNodeGuid, out var rerouteInput))
                        {
                            AddElement(rerouteOutput.ConnectTo(rerouteInput));
                        }
                        break;
                }
            }
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatiblePorts = new List<Port>();
            ports.ForEach(port =>
            {
                if (port == null || port == startPort)
                    return;

                if (port.node == startPort.node)
                    return;

                if (port.direction == startPort.direction)
                    return;

                if (port.portType != startPort.portType)
                    return;

                compatiblePorts.Add(port);
            });

            return compatiblePorts;
        }

        private static string ComposeKey(string ownerId, string secondaryId) => $"{ownerId}:{secondaryId}";

        private void OnMouseDown(MouseDownEvent evt)
        {
            if (!CanStartPan(evt))
                return;

            isPanning = true;
            panStartMouse = evt.mousePosition;
            var matrix = viewTransform.matrix;
            panStartPosition = new Vector3(matrix.m03, matrix.m13, matrix.m23);
            MouseCaptureController.CaptureMouse(this);
            evt.StopImmediatePropagation();
        }

        private void OnMouseMove(MouseMoveEvent evt)
        {
            if (!isPanning)
                return;

            var delta = evt.mousePosition - panStartMouse;
            UpdateViewTransform(panStartPosition + (Vector3)delta, viewTransform.matrix.lossyScale);
            evt.StopImmediatePropagation();
        }

        private void OnMouseUp(MouseUpEvent evt)
        {
            if (!isPanning)
                return;

            StopPan();
            evt.StopImmediatePropagation();
        }

        private void OnMouseCaptureOut(MouseCaptureOutEvent evt)
        {
            StopPan();
        }

        private void StopPan()
        {
            if (!isPanning)
                return;

            isPanning = false;
            MouseCaptureController.ReleaseMouse(this);
        }

        private bool CanStartPan(MouseDownEvent evt)
        {
            if (evt == null)
                return false;

            var isPanButton = evt.button == 2 || evt.button == 0;
            if (!isPanButton)
                return false;

            return IsBackgroundTarget(evt.target);
        }

        private static bool IsBackgroundTarget(IEventHandler target)
        {
            if (target is not VisualElement element)
                return false;

            var current = element;
            while (current != null)
            {
                if (current is Node || current is Port || current is Edge)
                    return false;

                current = current.parent;
            }

            return true;
        }

        private void RegisterPanCallbacks(VisualElement element)
        {
            if (element == null)
                return;

            element.RegisterCallback<MouseDownEvent>(OnMouseDown, TrickleDown.TrickleDown);
            element.RegisterCallback<MouseMoveEvent>(OnMouseMove, TrickleDown.TrickleDown);
            element.RegisterCallback<MouseUpEvent>(OnMouseUp, TrickleDown.TrickleDown);
            element.RegisterCallback<MouseCaptureOutEvent>(OnMouseCaptureOut, TrickleDown.TrickleDown);
        }

        private sealed class EntryPointNodeView : Node
        {
            public string EntryId { get; }
            public Action OnSelectedAction;

            public EntryPointNodeView(DialogEntryPoint entry)
            {
                EntryId = entry.Id;
                title = entry.Name;
            }

            public override void OnSelected()
            {
                base.OnSelected();
                OnSelectedAction?.Invoke();
            }
        }

        private sealed class DialogNodeView : Node
        {
            public string NodeGuid { get; }
            public Action OnSelectedAction;

            public DialogNodeView(DialogNodeDefinition node)
            {
                NodeGuid = node.Guid;
                title = node.DisplayName;
            }

            public override void OnSelected()
            {
                base.OnSelected();
                OnSelectedAction?.Invoke();
            }
        }

        private sealed class PortBinding
        {
            public PortBindingKind Kind { get; }
            public string OwnerId { get; }
            public string SecondaryId { get; }

            public PortBinding(PortBindingKind kind, string ownerId, string secondaryId = null)
            {
                Kind = kind;
                OwnerId = ownerId;
                SecondaryId = secondaryId;
            }
        }

        private enum PortBindingKind
        {
            Input,
            EntryPoint,
            Choice,
            Event,
            Reroute
        }
    }
}
