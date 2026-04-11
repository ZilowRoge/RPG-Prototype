using System.Linq;
using Common.Editor;
using Common.Progress;
using UnityEditor;
using UnityEngine;

namespace Common.Systems.SymbolTraining.Editor
{
    [CustomEditor(typeof(SymbolTrainer), true)]
    public class SymbolTrainerEditor : UnityEditor.Editor
    {
        private static string[] cachedKeys = System.Array.Empty<string>();
        private static FlagRegistry cachedRegistry;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Draw known refs first
            EditorGUILayout.PropertyField(serializedObject.FindProperty("progressController"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("inputManager"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("lessonConsumer"));

            // Lesson triggers with dropdown for startFlag
            var triggersProp = serializedObject.FindProperty("lessonTriggers");
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Lesson Triggers", EditorStyles.boldLabel);

            int newSize = Mathf.Max(0, EditorGUILayout.IntField("Size", triggersProp.arraySize));
            if (newSize != triggersProp.arraySize) triggersProp.arraySize = newSize;

            var registry = cachedRegistry ??= ProjectAssetCache.GetFlagRegistry();
            var keys = cachedKeys.Length > 0 ? cachedKeys : (cachedKeys = ProjectAssetCache.GetFlagKeys());

            for (int i = 0; i < triggersProp.arraySize; i++)
            {
                var elem = triggersProp.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.LabelField($"Trigger {i}", EditorStyles.miniBoldLabel);
                EditorGUI.indentLevel++;

                var flagProp = elem.FindPropertyRelative("startFlag");
                if (keys != null && keys.Length > 0)
                {
                    int currentIndex = System.Array.IndexOf(keys, flagProp.stringValue);
                    if (currentIndex < 0) currentIndex = 0;
                    int newIndex = EditorGUILayout.Popup("Start Flag", currentIndex, keys);
                    if (newIndex >= 0 && newIndex < keys.Length)
                        flagProp.stringValue = keys[newIndex];

                    var def = registry.Flags.FirstOrDefault(f => f.key == flagProp.stringValue);
                    if (def != null && !string.IsNullOrEmpty(def.description))
                        EditorGUILayout.HelpBox(def.description, MessageType.None);
                }
                else
                {
                    EditorGUILayout.PropertyField(flagProp, new GUIContent("Start Flag"));
                }

                EditorGUILayout.PropertyField(elem.FindPropertyRelative("lesson"));
                EditorGUILayout.PropertyField(elem.FindPropertyRelative("resetFlagAfterStart"));

                EditorGUI.indentLevel--;
                if (GUILayout.Button("Remove Trigger"))
                {
                    triggersProp.DeleteArrayElementAtIndex(i);
                    EditorGUILayout.EndVertical();
                    break;
                }
                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("Add Trigger"))
            {
                triggersProp.InsertArrayElementAtIndex(triggersProp.arraySize);
            }

            if (GUILayout.Button("Refresh Flags"))
            {
                cachedRegistry = ProjectAssetCache.GetFlagRegistry(forceRefresh: true);
                cachedKeys = ProjectAssetCache.GetFlagKeys(forceRefresh: true);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
