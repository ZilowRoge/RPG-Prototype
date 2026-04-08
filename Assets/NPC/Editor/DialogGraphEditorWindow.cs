using System;
using System.Collections.Generic;
using System.Linq;
using Common.Progress;
using Common.Systems.SymbolTraining;
using Quests;
using Systems.Jobs;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace NPC.Dialog.Editor
{
    public class DialogGraphEditorWindow : EditorWindow
    {
        [SerializeField] private DialogAsset persistedAsset;
        private DialogAsset asset;
        private SerializedObject serializedAsset;
        private DialogGraphView graphView;
        private ScrollView inspectorPanel;
        private ObjectField assetField;
        private SelectionDescriptor selection;

        [MenuItem("Window/Dialog Graph Editor")]
        public static void OpenEmpty()
        {
            GetWindow<DialogGraphEditorWindow>("Dialogue Graph");
        }

        public static void Open(DialogAsset dialogAsset)
        {
            var window = GetWindow<DialogGraphEditorWindow>("Dialogue Graph");
            window.LoadAsset(dialogAsset);
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Dialogue Graph");
            if (asset == null && persistedAsset != null)
                asset = persistedAsset;
            ConstructUi();
            if (asset != null)
                LoadAsset(asset);
        }

        private void ConstructUi()
        {
            rootVisualElement.Clear();

            var toolbar = new Toolbar();
            assetField = new ObjectField("Dialogue")
            {
                objectType = typeof(DialogAsset),
                allowSceneObjects = false,
                value = asset
            };
            assetField.RegisterValueChangedCallback(evt => LoadAsset(evt.newValue as DialogAsset));
            toolbar.Add(assetField);

            toolbar.Add(new ToolbarButton(() => graphView?.CreateEntryPoint()) { text = "Add Entry" });
            toolbar.Add(new ToolbarButton(SaveAsset) { text = "Save" });
            toolbar.Add(new ToolbarButton(ValidateAsset) { text = "Validate" });
            rootVisualElement.Add(toolbar);

            var body = new VisualElement();
            body.style.flexGrow = 1f;
            body.style.flexDirection = FlexDirection.Row;

            var graphHost = new VisualElement();
            graphHost.style.flexGrow = 1f;
            graphHost.style.minWidth = 300f;

            graphView = new DialogGraphView(this);
            graphView.StretchToParentSize();
            graphHost.Add(graphView);
            body.Add(graphHost);

            inspectorPanel = new ScrollView();
            inspectorPanel.style.width = 360f;
            inspectorPanel.style.minWidth = 360f;
            inspectorPanel.style.flexShrink = 0f;
            inspectorPanel.style.borderLeftWidth = 1f;
            inspectorPanel.style.borderLeftColor = new Color(0.18f, 0.18f, 0.18f, 1f);
            body.Add(inspectorPanel);

            rootVisualElement.Add(body);

            if (asset != null)
                graphView.Bind(asset);

            RefreshInspector();
        }

        private void LoadAsset(DialogAsset dialogAsset)
        {
            asset = dialogAsset;
            persistedAsset = dialogAsset;
            if (asset != null)
                NormalizeManagedReferenceOwnership(asset);
            serializedAsset = asset != null ? new SerializedObject(asset) : null;

            if (assetField != null && assetField.value != asset)
                assetField.SetValueWithoutNotify(asset);

            if (graphView != null)
                graphView.Bind(asset);

            selection = asset != null ? SelectionDescriptor.ForAsset() : default;
            RefreshInspector();
        }

        private void ValidateAsset()
        {
            if (asset == null)
                return;

            var issues = asset.ValidateGraph();
            if (issues.Count == 0)
                Debug.Log($"[DialogueGraph] '{asset.name}' passed validation.", asset);
            else
                Debug.LogWarning($"[DialogueGraph] '{asset.name}' validation issues:{Environment.NewLine}- {string.Join(Environment.NewLine + "- ", issues)}", asset);
        }

        public void NotifySelectionChanged(SelectionDescriptor descriptor)
        {
            selection = descriptor;
            RefreshInspector();
        }

        public void RebuildGraph()
        {
            graphView?.Bind(asset);
        }

        public void SaveAsset()
        {
            if (asset == null || serializedAsset == null)
                return;

            serializedAsset.ApplyModifiedProperties();
            NormalizeManagedReferenceOwnership(asset);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
        }

        private void RefreshInspector()
        {
            if (inspectorPanel == null)
                return;

            inspectorPanel.Clear();
            inspectorPanel.Add(new IMGUIContainer(DrawInspector));
        }

        private void DrawInspector()
        {
            if (asset == null || serializedAsset == null)
            {
                EditorGUILayout.HelpBox("Select a dialogue graph asset to begin editing.", MessageType.Info);
                return;
            }

            serializedAsset.Update();

            switch (selection.Kind)
            {
                case SelectionKind.Asset:
                    DrawAssetInspector();
                    break;
                case SelectionKind.EntryPoint:
                    DrawEntryPointInspector();
                    break;
                case SelectionKind.Node:
                    DrawNodeInspector();
                    break;
                case SelectionKind.Choice:
                    DrawChoiceInspector();
                    break;
                default:
                    DrawAssetInspector();
                    break;
            }

            serializedAsset.ApplyModifiedProperties();
        }

        private void DrawAssetInspector()
        {
            EditorGUILayout.LabelField("Graph", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedAsset.FindProperty("dialogId"));
            EditorGUILayout.LabelField("Nodes", asset.Nodes.Count.ToString());
            EditorGUILayout.LabelField("Entry Points", asset.EntryPoints.Count.ToString());
        }

        private void DrawEntryPointInspector()
        {
            var property = FindEntryPointProperty(selection.PrimaryId);
            if (property == null)
            {
                EditorGUILayout.HelpBox("Entry point not found.", MessageType.Warning);
                return;
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField("Entry Point", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(property.FindPropertyRelative("name"));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("priority"));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("weight"));
            EditorGUILayout.LabelField("Target", ResolveNodeName(property.FindPropertyRelative("targetNodeGuid").stringValue));
            DrawManagedReferenceList(property.FindPropertyRelative("conditions"), "Conditions", typeof(DialogueConditionDefinition));
            if (EditorGUI.EndChangeCheck())
                ApplyAndRebuildGraph();

            if (GUILayout.Button("Delete Entry Point"))
            {
                DeleteEntryPoint(selection.PrimaryId);
                GUIUtility.ExitGUI();
            }
        }

        private void DrawNodeInspector()
        {
            var property = FindNodeProperty(selection.PrimaryId);
            if (property == null)
            {
                EditorGUILayout.HelpBox("Node not found.", MessageType.Warning);
                return;
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField("Node", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Type", property.managedReferenceValue?.GetType().Name ?? "Unknown");

            switch (property.managedReferenceValue)
            {
                case DialogLineNode:
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("title"));
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("text"));
                    DrawChoicesInspector(property.FindPropertyRelative("choices"), selection.PrimaryId);
                    break;
                case DialogEventNode:
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("title"));
                    EditorGUILayout.LabelField("Next", ResolveNodeName(property.FindPropertyRelative("nextNodeGuid").stringValue));
                    DrawManagedReferenceList(property.FindPropertyRelative("actions"), "Actions", typeof(DialogueActionDefinition));
                    break;
                case DialogRerouteNode:
                    EditorGUILayout.LabelField("Next", ResolveNodeName(property.FindPropertyRelative("nextNodeGuid").stringValue));
                    EditorGUILayout.HelpBox("Reroute node is a draggable pass-through point used to organize connections.", MessageType.None);
                    break;
                case DialogEndNode:
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("title"));
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("closingText"));
                    break;
            }
            if (EditorGUI.EndChangeCheck())
                ApplyAndRebuildGraph();
        }

        private void DrawChoiceInspector()
        {
            var choiceProperty = FindChoiceProperty(selection.PrimaryId, selection.SecondaryId);
            if (choiceProperty == null)
            {
                EditorGUILayout.HelpBox("Choice not found.", MessageType.Warning);
                return;
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField("Choice", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(choiceProperty.FindPropertyRelative("text"));
            EditorGUILayout.PropertyField(choiceProperty.FindPropertyRelative("lockMode"));
            DrawLockModeTooltip(choiceProperty.FindPropertyRelative("lockMode"));
            EditorGUILayout.LabelField("Next", ResolveNodeName(choiceProperty.FindPropertyRelative("nextNodeGuid").stringValue));
            DrawManagedReferenceList(choiceProperty.FindPropertyRelative("conditions"), "Conditions", typeof(DialogueConditionDefinition));
            if (EditorGUI.EndChangeCheck())
                ApplyAndRebuildGraph();

            if (GUILayout.Button("Delete Choice"))
            {
                DeleteChoice(selection.PrimaryId, selection.SecondaryId);
                GUIUtility.ExitGUI();
            }
        }

        private void DrawChoicesInspector(SerializedProperty choicesProperty, string nodeGuid)
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Choices", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+", GUILayout.Width(22f)))
            {
                choicesProperty.arraySize++;
                var newChoice = choicesProperty.GetArrayElementAtIndex(choicesProperty.arraySize - 1);
                newChoice.FindPropertyRelative("id").stringValue = System.Guid.NewGuid().ToString("N");
                newChoice.FindPropertyRelative("text").stringValue = "New choice";
                newChoice.FindPropertyRelative("lockMode").enumValueIndex = (int)DialogChoiceLockMode.Hide;
                ApplyAndRebuildGraph();
                GUIUtility.ExitGUI();
            }
            EditorGUILayout.EndHorizontal();

            for (var index = 0; index < choicesProperty.arraySize; index++)
            {
                var choice = choicesProperty.GetArrayElementAtIndex(index);
                var choiceId = choice.FindPropertyRelative("id").stringValue;
                EditorGUILayout.BeginVertical(GUI.skin.box);
                DrawChoiceHeader(choice, nodeGuid, choiceId, choicesProperty, index);
                EditorGUILayout.PropertyField(choice.FindPropertyRelative("text"));
                EditorGUILayout.PropertyField(choice.FindPropertyRelative("lockMode"));
                DrawLockModeTooltip(choice.FindPropertyRelative("lockMode"));
                EditorGUILayout.LabelField("Next", ResolveNodeName(choice.FindPropertyRelative("nextNodeGuid").stringValue));
                DrawManagedReferenceList(choice.FindPropertyRelative("conditions"), "Conditions", typeof(DialogueConditionDefinition));
                EditorGUILayout.EndVertical();

                if (index < choicesProperty.arraySize - 1)
                {
                    var separatorRect = EditorGUILayout.GetControlRect(false, 10f);
                    separatorRect.height = 2f;
                    separatorRect.y += 4f;
                    EditorGUI.DrawRect(separatorRect, new Color(0.85f, 0.85f, 0.85f, 1f));
                    GUILayout.Space(6f);
                }
            }
        }

        private void DrawChoiceHeader(SerializedProperty choice, string nodeGuid, string choiceId, SerializedProperty choicesProperty, int index)
        {
            EditorGUILayout.BeginHorizontal();
            var title = choice.FindPropertyRelative("text").stringValue;
            EditorGUILayout.LabelField(string.IsNullOrWhiteSpace(title) ? $"Choice {index + 1}" : title, EditorStyles.miniBoldLabel);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("O", GUILayout.Width(22f)))
            {
                NotifySelectionChanged(SelectionDescriptor.ForChoice(nodeGuid, choiceId));
                GUIUtility.ExitGUI();
            }

            if (GUILayout.Button("X", GUILayout.Width(22f)))
            {
                choicesProperty.DeleteArrayElementAtIndex(index);
                ApplyAndRebuildGraph();
                GUIUtility.ExitGUI();
            }

            EditorGUILayout.EndHorizontal();
        }

        private static void DrawLockModeTooltip(SerializedProperty lockModeProperty)
        {
            if (lockModeProperty == null)
                return;

            var mode = (DialogChoiceLockMode)lockModeProperty.enumValueIndex;
            var message = mode switch
            {
                DialogChoiceLockMode.Hide => "Hide: opcja nie pojawi sie wcale, jesli warunki nie zostana spelnione.",
                DialogChoiceLockMode.Disable => "Disable: opcja bedzie widoczna, ale zablokowana, jesli warunki nie zostana spelnione.",
                _ => string.Empty
            };

            if (!string.IsNullOrWhiteSpace(message))
                EditorGUILayout.HelpBox(message, MessageType.None);
        }

        private void DrawManagedReferenceList(SerializedProperty listProperty, string label, Type baseType)
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+", GUILayout.Width(22f)))
            {
                ShowAddManagedReferenceMenu(listProperty, baseType);
            }
            EditorGUILayout.EndHorizontal();

            for (var index = 0; index < listProperty.arraySize; index++)
            {
                var element = listProperty.GetArrayElementAtIndex(index);
                EditorGUILayout.BeginVertical(GUI.skin.box);
                DrawManagedReferenceHeader(element, index);
                DrawManagedReferenceFields(element);
                EditorGUILayout.EndVertical();
            }
        }

        private void ShowAddManagedReferenceMenu(SerializedProperty listProperty, Type baseType)
        {
            if (listProperty == null || baseType == null)
                return;

            var propertyPath = listProperty.propertyPath;
            var menu = new GenericMenu();
            foreach (var type in GetConcreteTypes(baseType))
            {
                menu.AddItem(new GUIContent(type.Name), false, () =>
                {
                    serializedAsset.Update();
                    var targetList = serializedAsset.FindProperty(propertyPath);
                    if (targetList == null)
                        return;

                    targetList.arraySize++;
                    var newElement = targetList.GetArrayElementAtIndex(targetList.arraySize - 1);
                    newElement.managedReferenceValue = Activator.CreateInstance(type);
                    ApplyAndRebuildGraph();
                });
            }
            menu.ShowAsContext();
        }

        private void DrawManagedReferenceHeader(SerializedProperty element, int index)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(GetManagedReferenceSummary(element, index), EditorStyles.miniBoldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("X", GUILayout.Width(22f)))
            {
                DeleteManagedReferenceElement(element);
                GUIUtility.ExitGUI();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawManagedReferenceFields(SerializedProperty element)
        {
            if (element == null)
                return;

            if (DrawSpecializedManagedReferenceFields(element))
                return;

            var iterator = element.Copy();
            var endProperty = iterator.GetEndProperty();
            var enterChildren = true;

            while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, endProperty))
            {
                enterChildren = false;
                if (iterator.propertyPath == element.propertyPath)
                    continue;

                EditorGUILayout.PropertyField(iterator, true);
            }
        }

        private bool DrawSpecializedManagedReferenceFields(SerializedProperty element)
        {
            switch (element.managedReferenceValue)
            {
                case FlagConditionDefinition:
                    DrawDropdownProperty(element.FindPropertyRelative("flagKey"), "Flag Key", GetFlagKeys());
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("expectedValue"));
                    return true;
                case SetFlagActionDefinition:
                    DrawDropdownProperty(element.FindPropertyRelative("flagKey"), "Flag Key", GetFlagKeys());
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("value"));
                    return true;
                case QuestStartedConditionDefinition:
                case QuestActiveConditionDefinition:
                case QuestCompletedConditionDefinition:
                    DrawDropdownProperty(element.FindPropertyRelative("questId"), "Quest Id", GetQuestIds());
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("expectedValue"));
                    return true;
                case StartQuestActionDefinition:
                    DrawDropdownProperty(element.FindPropertyRelative("questId"), "Quest Id", GetQuestIds());
                    return true;
                case HasJobConditionDefinition:
                    DrawDropdownProperty(element.FindPropertyRelative("jobId"), "Job Id", GetJobIds());
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("expectedValue"));
                    return true;
                case AddJobActionDefinition:
                    DrawDropdownProperty(element.FindPropertyRelative("jobId"), "Job Id", GetJobIds());
                    return true;
                case KnowsSymbolConditionDefinition:
                    DrawDropdownProperty(element.FindPropertyRelative("symbolKey"), "Symbol Key", GetSymbolKeys());
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("expectedValue"));
                    return true;
                case LearnSymbolActionDefinition:
                    DrawDropdownProperty(element.FindPropertyRelative("symbolKey"), "Symbol Key", GetSymbolKeys());
                    return true;
                default:
                    return false;
            }
        }

        private static void DrawDropdownProperty(SerializedProperty property, string label, IReadOnlyList<string> options)
        {
            if (property == null)
                return;

            if (options == null || options.Count == 0)
            {
                EditorGUILayout.PropertyField(property, new GUIContent(label));
                return;
            }

            var values = new List<string>(options.Count + 1);
            values.AddRange(options.Where(value => !string.IsNullOrWhiteSpace(value)));

            if (!string.IsNullOrWhiteSpace(property.stringValue) && !values.Contains(property.stringValue))
                values.Insert(0, property.stringValue);

            var currentIndex = Mathf.Max(0, values.IndexOf(property.stringValue));
            var newIndex = EditorGUILayout.Popup(label, currentIndex, values.ToArray());
            if (newIndex >= 0 && newIndex < values.Count)
                property.stringValue = values[newIndex];
        }

        private void DeleteManagedReferenceElement(SerializedProperty element)
        {
            if (element == null)
                return;

            var propertyPath = element.propertyPath;
            var arrayStart = propertyPath.LastIndexOf(".Array.data[", StringComparison.Ordinal);
            if (arrayStart < 0)
                return;

            var listPath = propertyPath.Substring(0, arrayStart);
            var indexStart = propertyPath.IndexOf('[', arrayStart) + 1;
            var indexEnd = propertyPath.IndexOf(']', indexStart);
            if (indexStart <= 0 || indexEnd <= indexStart)
                return;

            if (!int.TryParse(propertyPath.Substring(indexStart, indexEnd - indexStart), out var index))
                return;

            var list = serializedAsset.FindProperty(listPath);
            if (list == null || index < 0 || index >= list.arraySize)
                return;

            list.DeleteArrayElementAtIndex(index);
            ApplyAndRebuildGraph();
        }

        private static string GetManagedReferenceTypeName(SerializedProperty property)
        {
            if (property == null || string.IsNullOrWhiteSpace(property.managedReferenceFullTypename))
                return "Undefined";

            var split = property.managedReferenceFullTypename.Split(' ');
            return split.Length > 1 ? split[1].Split('.').Last() : property.managedReferenceFullTypename;
        }

        private static IEnumerable<Type> GetConcreteTypes(Type baseType)
        {
            return TypeCache.GetTypesDerivedFrom(baseType).Where(type => !type.IsAbstract && !type.IsGenericType);
        }

        private static string GetManagedReferenceSummary(SerializedProperty property, int index)
        {
            if (property?.managedReferenceValue is DialogueConditionDefinition condition)
                return condition.GetSummary();

            if (property?.managedReferenceValue is DialogueActionDefinition action)
                return action.GetSummary();

            return $"{GetManagedReferenceTypeName(property)} {index + 1}";
        }

        public readonly struct SelectionDescriptor
        {
            public SelectionKind Kind { get; }
            public string PrimaryId { get; }
            public string SecondaryId { get; }

            private SelectionDescriptor(SelectionKind kind, string primaryId, string secondaryId = null)
            {
                Kind = kind;
                PrimaryId = primaryId;
                SecondaryId = secondaryId;
            }

            public static SelectionDescriptor ForAsset() => new(SelectionKind.Asset, null);
            public static SelectionDescriptor ForEntry(string entryId) => new(SelectionKind.EntryPoint, entryId);
            public static SelectionDescriptor ForNode(string nodeGuid) => new(SelectionKind.Node, nodeGuid);
            public static SelectionDescriptor ForChoice(string nodeGuid, string choiceId) => new(SelectionKind.Choice, nodeGuid, choiceId);
        }

        public enum SelectionKind
        {
            None,
            Asset,
            EntryPoint,
            Node,
            Choice
        }

        public void CreateNode(Type nodeType, Vector2 position)
        {
            if (asset == null)
                return;

            Undo.RecordObject(asset, "Create dialogue node");
            var nodesProperty = serializedAsset.FindProperty("nodes");
            nodesProperty.arraySize++;
            var element = nodesProperty.GetArrayElementAtIndex(nodesProperty.arraySize - 1);
            element.managedReferenceValue = Activator.CreateInstance(nodeType);
            serializedAsset.ApplyModifiedProperties();

            var createdNodeProperty = nodesProperty.GetArrayElementAtIndex(nodesProperty.arraySize - 1);
            createdNodeProperty.FindPropertyRelative("guid").stringValue = System.Guid.NewGuid().ToString("N");
            createdNodeProperty.FindPropertyRelative("position").vector2Value = position;
            serializedAsset.ApplyModifiedProperties();

            SaveAsset();
            RebuildGraph();
        }

        public void CreateEntryPoint()
        {
            if (asset == null)
                return;

            Undo.RecordObject(asset, "Create dialogue entry point");
            var entries = serializedAsset.FindProperty("entryPoints");
            entries.arraySize++;
            var element = entries.GetArrayElementAtIndex(entries.arraySize - 1);
            var entryId = System.Guid.NewGuid().ToString("N");
            element.FindPropertyRelative("id").stringValue = entryId;
            element.FindPropertyRelative("name").stringValue = $"Entry {entries.arraySize}";
            element.FindPropertyRelative("priority").intValue = 0;
            element.FindPropertyRelative("weight").floatValue = 1f;
            element.FindPropertyRelative("position").vector2Value = new Vector2(40f, 120f + ((entries.arraySize - 1) * 180f));
            serializedAsset.ApplyModifiedProperties();

            SaveAsset();
            RebuildGraph();
            NotifySelectionChanged(SelectionDescriptor.ForEntry(entryId));
        }

        public void UpdateEntryPointPosition(string entryId, Vector2 position)
        {
            var entry = FindEntryPointProperty(entryId);
            if (entry == null)
                return;

            entry.FindPropertyRelative("position").vector2Value = position;
            serializedAsset.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        public void DeleteNode(string nodeGuid)
        {
            var nodesProperty = serializedAsset.FindProperty("nodes");
            for (var index = 0; index < nodesProperty.arraySize; index++)
            {
                var node = nodesProperty.GetArrayElementAtIndex(index);
                if (node.FindPropertyRelative("guid").stringValue != nodeGuid)
                    continue;

                nodesProperty.DeleteArrayElementAtIndex(index);
                break;
            }

            ClearReferencesToNode(nodeGuid);
            serializedAsset.ApplyModifiedProperties();
            SaveAsset();
            NotifySelectionChanged(SelectionDescriptor.ForAsset());
            RebuildGraph();
        }

        public void DeleteEntryPoint(string entryId)
        {
            var entries = serializedAsset.FindProperty("entryPoints");
            for (var index = 0; index < entries.arraySize; index++)
            {
                var entry = entries.GetArrayElementAtIndex(index);
                if (entry.FindPropertyRelative("id").stringValue != entryId)
                    continue;

                entries.DeleteArrayElementAtIndex(index);
                break;
            }

            serializedAsset.ApplyModifiedProperties();
            SaveAsset();
            NotifySelectionChanged(SelectionDescriptor.ForAsset());
            RebuildGraph();
        }

        public void DeleteChoice(string nodeGuid, string choiceId)
        {
            var node = FindNodeProperty(nodeGuid);
            if (node == null || node.managedReferenceValue is not DialogLineNode)
                return;

            var choicesProperty = node.FindPropertyRelative("choices");
            for (var index = 0; index < choicesProperty.arraySize; index++)
            {
                if (choicesProperty.GetArrayElementAtIndex(index).FindPropertyRelative("id").stringValue != choiceId)
                    continue;

                choicesProperty.DeleteArrayElementAtIndex(index);
                break;
            }

            serializedAsset.ApplyModifiedProperties();
            SaveAsset();
            NotifySelectionChanged(SelectionDescriptor.ForNode(nodeGuid));
            RebuildGraph();
        }

        private void ClearReferencesToNode(string nodeGuid)
        {
            var entries = serializedAsset.FindProperty("entryPoints");
            for (var index = 0; index < entries.arraySize; index++)
            {
                var target = entries.GetArrayElementAtIndex(index).FindPropertyRelative("targetNodeGuid");
                if (target.stringValue == nodeGuid)
                    target.stringValue = string.Empty;
            }

            var nodes = serializedAsset.FindProperty("nodes");
            for (var index = 0; index < nodes.arraySize; index++)
            {
                var node = nodes.GetArrayElementAtIndex(index);
                if (node.managedReferenceValue is DialogLineNode)
                {
                    var choices = node.FindPropertyRelative("choices");
                    for (var choiceIndex = 0; choiceIndex < choices.arraySize; choiceIndex++)
                    {
                        var next = choices.GetArrayElementAtIndex(choiceIndex).FindPropertyRelative("nextNodeGuid");
                        if (next.stringValue == nodeGuid)
                            next.stringValue = string.Empty;
                    }
                }
                else if (node.managedReferenceValue is DialogEventNode)
                {
                    var next = node.FindPropertyRelative("nextNodeGuid");
                    if (next.stringValue == nodeGuid)
                        next.stringValue = string.Empty;
                }
                else if (node.managedReferenceValue is DialogRerouteNode)
                {
                    var next = node.FindPropertyRelative("nextNodeGuid");
                    if (next.stringValue == nodeGuid)
                        next.stringValue = string.Empty;
                }
            }
        }

        public void SetEntryPointTarget(string entryId, string targetNodeGuid)
        {
            var entry = FindEntryPointProperty(entryId);
            if (entry == null)
                return;

            entry.FindPropertyRelative("targetNodeGuid").stringValue = targetNodeGuid;
            serializedAsset.ApplyModifiedProperties();
            SaveAsset();
        }

        public void SetChoiceTarget(string nodeGuid, string choiceId, string targetNodeGuid)
        {
            var choice = FindChoiceProperty(nodeGuid, choiceId);
            if (choice == null)
                return;

            choice.FindPropertyRelative("nextNodeGuid").stringValue = targetNodeGuid;
            serializedAsset.ApplyModifiedProperties();
            SaveAsset();
        }

        public void SetEventTarget(string nodeGuid, string targetNodeGuid)
        {
            var node = FindNodeProperty(nodeGuid);
            if (node == null)
                return;

            node.FindPropertyRelative("nextNodeGuid").stringValue = targetNodeGuid;
            serializedAsset.ApplyModifiedProperties();
            SaveAsset();
        }

        public void SetRerouteTarget(string nodeGuid, string targetNodeGuid)
        {
            var node = FindNodeProperty(nodeGuid);
            if (node == null)
                return;

            node.FindPropertyRelative("nextNodeGuid").stringValue = targetNodeGuid;
            serializedAsset.ApplyModifiedProperties();
            SaveAsset();
        }

        public void UpdateNodePosition(string nodeGuid, Vector2 position)
        {
            var node = FindNodeProperty(nodeGuid);
            if (node == null)
                return;

            node.FindPropertyRelative("position").vector2Value = position;
            serializedAsset.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private SerializedProperty FindEntryPointProperty(string entryId)
        {
            var entries = serializedAsset.FindProperty("entryPoints");
            for (var index = 0; index < entries.arraySize; index++)
            {
                var entry = entries.GetArrayElementAtIndex(index);
                if (entry.FindPropertyRelative("id").stringValue == entryId)
                    return entry;
            }

            return null;
        }

        private SerializedProperty FindNodeProperty(string nodeGuid)
        {
            var nodes = serializedAsset.FindProperty("nodes");
            for (var index = 0; index < nodes.arraySize; index++)
            {
                var node = nodes.GetArrayElementAtIndex(index);
                if (node.FindPropertyRelative("guid").stringValue == nodeGuid)
                    return node;
            }

            return null;
        }

        private SerializedProperty FindChoiceProperty(string nodeGuid, string choiceId)
        {
            var node = FindNodeProperty(nodeGuid);
            if (node == null || node.managedReferenceValue is not DialogLineNode)
                return null;

            var choices = node.FindPropertyRelative("choices");
            for (var index = 0; index < choices.arraySize; index++)
            {
                var choice = choices.GetArrayElementAtIndex(index);
                if (choice.FindPropertyRelative("id").stringValue == choiceId)
                    return choice;
            }

            return null;
        }

        private string ResolveNodeName(string nodeGuid)
        {
            if (string.IsNullOrWhiteSpace(nodeGuid))
                return "(Ends dialogue)";

            return asset.GetNode(nodeGuid)?.DisplayName ?? "(Missing)";
        }

        private void ApplyAndRebuildGraph()
        {
            serializedAsset.ApplyModifiedProperties();
            NormalizeManagedReferenceOwnership(asset);
            SaveAsset();
            RebuildGraph();
            serializedAsset.Update();
        }

        private static void NormalizeManagedReferenceOwnership(DialogAsset dialogAsset)
        {
            if (dialogAsset == null)
                return;

            var visitedConditions = new HashSet<DialogueConditionDefinition>(ReferenceEqualityComparer<DialogueConditionDefinition>.Instance);
            var visitedActions = new HashSet<DialogueActionDefinition>(ReferenceEqualityComparer<DialogueActionDefinition>.Instance);
            var changed = false;

            foreach (var entryPoint in dialogAsset.EntryPoints)
            {
                if (entryPoint?.Conditions == null)
                    continue;

                changed |= NormalizeList(entryPoint.Conditions, visitedConditions, CloneCondition);
            }

            foreach (var node in dialogAsset.Nodes)
            {
                switch (node)
                {
                    case DialogLineNode lineNode:
                        if (lineNode.Choices == null)
                            break;

                        foreach (var choice in lineNode.Choices)
                        {
                            if (choice?.Conditions != null)
                                changed |= NormalizeList(choice.Conditions, visitedConditions, CloneCondition);
                        }
                        break;

                    case DialogEventNode eventNode when eventNode.Actions != null:
                        changed |= NormalizeList(eventNode.Actions, visitedActions, CloneAction);
                        break;
                }
            }

            if (changed)
                EditorUtility.SetDirty(dialogAsset);
        }

        private static bool NormalizeList<T>(IList<T> list, ISet<T> visited, Func<T, T> clone)
            where T : class
        {
            if (list == null || visited == null || clone == null)
                return false;

            var changed = false;
            for (var index = 0; index < list.Count; index++)
            {
                var item = list[index];
                if (item == null)
                    continue;

                if (visited.Add(item))
                    continue;

                list[index] = clone(item);
                changed = true;
            }

            return changed;
        }

        private static DialogueConditionDefinition CloneCondition(DialogueConditionDefinition source)
        {
            if (source == null)
                return null;

            var json = JsonUtility.ToJson(source);
            return (DialogueConditionDefinition)JsonUtility.FromJson(json, source.GetType());
        }

        private static DialogueActionDefinition CloneAction(DialogueActionDefinition source)
        {
            if (source == null)
                return null;

            var json = JsonUtility.ToJson(source);
            return (DialogueActionDefinition)JsonUtility.FromJson(json, source.GetType());
        }

        private sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T>
            where T : class
        {
            public static readonly ReferenceEqualityComparer<T> Instance = new();

            public bool Equals(T x, T y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(T obj)
            {
                return obj != null ? System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj) : 0;
            }
        }

        private static IReadOnlyList<string> GetFlagKeys()
        {
            var registry = LoadFirstAsset<FlagRegistry>();
            return registry != null
                ? registry.GetKeys().ToList()
                : Array.Empty<string>();
        }

        private static IReadOnlyList<string> GetQuestIds()
        {
            var database = LoadFirstAsset<QuestDatabase>();
            if (database?.All == null)
                return Array.Empty<string>();

            return database.All
                .Where(asset => asset != null && !string.IsNullOrWhiteSpace(asset.questId))
                .Select(asset => asset.questId)
                .Distinct()
                .ToList();
        }

        private static IReadOnlyList<string> GetSymbolKeys()
        {
            var guids = AssetDatabase.FindAssets("t:SymbolLesson");
            var result = new List<string>();

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var lesson = AssetDatabase.LoadAssetAtPath<SymbolLesson>(path);
                if (lesson == null || string.IsNullOrWhiteSpace(lesson.SymbolId))
                    continue;

                if (!result.Contains(lesson.SymbolId))
                    result.Add(lesson.SymbolId);
            }

            return result;
        }

        private static IReadOnlyList<string> GetJobIds()
        {
            var database = LoadFirstAsset<JobDatabase>();
            if (database == null)
                return Array.Empty<string>();

            var guids = AssetDatabase.FindAssets("t:JobData");
            var result = new List<string>();
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var job = AssetDatabase.LoadAssetAtPath<JobData>(path);
                if (job == null || string.IsNullOrWhiteSpace(job.id))
                    continue;

                if (!result.Contains(job.id))
                    result.Add(job.id);
            }

            return result;
        }

        private static T LoadFirstAsset<T>() where T : UnityEngine.Object
        {
            var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            if (guids == null || guids.Length == 0)
                return null;

            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }
    }
}
