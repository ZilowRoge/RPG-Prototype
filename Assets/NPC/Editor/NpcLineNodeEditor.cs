using System.Linq;
using Common.Progress;
using NPC.Dialog;
using UnityEditor;
using UnityEngine;

namespace NPC.Editor
{
    [CustomEditor(typeof(NpcLineNode))]
    public class NpcLineNodeEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var textProp = serializedObject.FindProperty("text");
            var optionsProp = serializedObject.FindProperty("options");

            EditorGUILayout.PropertyField(textProp);

            DrawOptions(optionsProp);

            serializedObject.ApplyModifiedProperties();
        }

        void DrawOptions(SerializedProperty optionsProp)
        {
            EditorGUILayout.LabelField("Options", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            int newSize = Mathf.Max(0, EditorGUILayout.IntField("Size", optionsProp.arraySize));
            if (newSize != optionsProp.arraySize)
                optionsProp.arraySize = newSize;

            FlagRegistry registry = FindRegistry();
            string[] keys = registry != null && registry.Flags != null && registry.Flags.Count > 0
                ? registry.GetKeys().ToArray()
                : null;

            for (int i = 0; i < optionsProp.arraySize; i++)
            {
                var elem = optionsProp.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.LabelField($"Element {i}", EditorStyles.miniBoldLabel);
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(elem.FindPropertyRelative("text"));
                EditorGUILayout.PropertyField(elem.FindPropertyRelative("nextNodeId"));
                EditorGUILayout.PropertyField(elem.FindPropertyRelative("HideIfLocked"));

                // Conditions
                var conds = elem.FindPropertyRelative("conditions");
                EditorGUILayout.LabelField("Conditions", EditorStyles.boldLabel);
                int condSize = Mathf.Max(0, EditorGUILayout.IntField("Size", conds.arraySize));
                if (condSize != conds.arraySize) conds.arraySize = condSize;
                for (int c = 0; c < conds.arraySize; c++)
                {
                    var cond = conds.GetArrayElementAtIndex(c);
                    EditorGUILayout.BeginVertical(GUI.skin.box);
                    var typeProp = cond.FindPropertyRelative("conditionType");
                    var idProp = cond.FindPropertyRelative("identifier");
                    var expectedProp = cond.FindPropertyRelative("expectedValue");
                    EditorGUILayout.PropertyField(typeProp);
                    var type = (DialogCondition.ConditionType)typeProp.enumValueIndex;
                    if (type == DialogCondition.ConditionType.FlagEquals && keys != null && keys.Length > 0)
                    {
                        int currentIndex = Mathf.Max(0, System.Array.IndexOf(keys, idProp.stringValue));
                        int newIndex = EditorGUILayout.Popup("Flag Id", currentIndex, keys);
                        if (newIndex >= 0 && newIndex < keys.Length)
                            idProp.stringValue = keys[newIndex];
                        var def = registry.Flags.FirstOrDefault(f => f.key == idProp.stringValue);
                        if (def != null && !string.IsNullOrEmpty(def.description))
                            EditorGUILayout.HelpBox(def.description, MessageType.None);
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(idProp, new GUIContent("Identifier"));
                    }
                    EditorGUILayout.PropertyField(expectedProp, new GUIContent("Expected Value"));
                    EditorGUILayout.EndVertical();
                }

                // Actions
                var acts = elem.FindPropertyRelative("onSelect");
                EditorGUILayout.LabelField("On Select", EditorStyles.boldLabel);
                int actSize = Mathf.Max(0, EditorGUILayout.IntField("Size", acts.arraySize));
                if (actSize != acts.arraySize) acts.arraySize = actSize;
                for (int a = 0; a < acts.arraySize; a++)
                {
                    var act = acts.GetArrayElementAtIndex(a);
                    EditorGUILayout.BeginVertical(GUI.skin.box);
                    var typeProp = act.FindPropertyRelative("type");
                    var idProp = act.FindPropertyRelative("actionId");
                    var valProp = act.FindPropertyRelative("actionValue");
                    var evtProp = act.FindPropertyRelative("onRun");
                    EditorGUILayout.PropertyField(typeProp);
                    var type = (DialogAction.ActionType)typeProp.enumValueIndex;
                    if (type == DialogAction.ActionType.SetFlag && keys != null && keys.Length > 0)
                    {
                        int currentIndex = Mathf.Max(0, System.Array.IndexOf(keys, idProp.stringValue));
                        int newIndex = EditorGUILayout.Popup("Flag Id", currentIndex, keys);
                        if (newIndex >= 0 && newIndex < keys.Length)
                            idProp.stringValue = keys[newIndex];
                        var def = registry.Flags.FirstOrDefault(f => f.key == idProp.stringValue);
                        if (def != null && !string.IsNullOrEmpty(def.description))
                            EditorGUILayout.HelpBox(def.description, MessageType.None);
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(idProp, new GUIContent("Identifier"));
                    }
                    EditorGUILayout.PropertyField(valProp, new GUIContent("Value"));
                    EditorGUILayout.PropertyField(evtProp);
                    EditorGUILayout.EndVertical();
                }

                // Remove option button
                EditorGUI.indentLevel--;
                if (GUILayout.Button("Remove Option"))
                {
                    optionsProp.DeleteArrayElementAtIndex(i);
                    EditorGUILayout.EndVertical();
                    break;
                }
                EditorGUILayout.EndVertical();
            }

            // Add option button
            if (GUILayout.Button("Add Option"))
            {
                optionsProp.InsertArrayElementAtIndex(optionsProp.arraySize);
            }

            EditorGUI.indentLevel--;
        }

        private static FlagRegistry FindRegistry()
        {
            string[] guids = AssetDatabase.FindAssets("t:FlagRegistry");
            if (guids.Length == 0) return null;
            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<FlagRegistry>(path);
        }
    }
}

