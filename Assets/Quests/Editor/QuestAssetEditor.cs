using System.Linq;
using Common.Progress;
using UnityEditor;
using UnityEngine;

namespace Quests.Editor
{
    [CustomEditor(typeof(QuestAsset))]
    public class QuestAssetEditor : UnityEditor.Editor
    {
        private static string[] cachedKeys = System.Array.Empty<string>();
        private static FlagRegistry cachedRegistry;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Identity
            EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("questId"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("title"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("shortDescription"));
            EditorGUILayout.Space(6);

            // Rewards
            EditorGUILayout.LabelField("Rewards", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("rewardXp"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("itemRewards"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("rewardNote"));
            EditorGUILayout.Space(6);

            // Stages
            var stagesProp = serializedObject.FindProperty("stages");
            EditorGUILayout.LabelField("Stages", EditorStyles.boldLabel);
            int newSize = Mathf.Max(0, EditorGUILayout.IntField("Size", stagesProp.arraySize));
            if (newSize != stagesProp.arraySize) stagesProp.arraySize = newSize;

            var registry = cachedRegistry ??= FindFlagRegistry();
            string[] keys = cachedKeys.Length > 0 ? cachedKeys : (cachedKeys = GetFlagKeys(registry));

            for (int i = 0; i < stagesProp.arraySize; i++)
            {
                var stage = stagesProp.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.LabelField($"Stage {i}", EditorStyles.miniBoldLabel);
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(stage.FindPropertyRelative("id"));
                EditorGUILayout.PropertyField(stage.FindPropertyRelative("title"));
                EditorGUILayout.PropertyField(stage.FindPropertyRelative("description"));

                // Objectives
                var objs = stage.FindPropertyRelative("objectives");
                EditorGUILayout.LabelField("Objectives", EditorStyles.boldLabel);
                int oSize = Mathf.Max(0, EditorGUILayout.IntField("Size", objs.arraySize));
                if (oSize != objs.arraySize) objs.arraySize = oSize;
                for (int j = 0; j < objs.arraySize; j++)
                {
                    var obj = objs.GetArrayElementAtIndex(j);
                    EditorGUILayout.BeginVertical(GUI.skin.box);
                    EditorGUILayout.LabelField($"Objective {j}", EditorStyles.miniBoldLabel);
                    EditorGUI.indentLevel++;
                    var idProp = obj.FindPropertyRelative("id");
                    var displayNameProp = obj.FindPropertyRelative("displayName");
                    var typeProp = obj.FindPropertyRelative("type");
                    var targetProp = obj.FindPropertyRelative("targetId");
                    var extraProp = obj.FindPropertyRelative("extraId");
                    var countProp = obj.FindPropertyRelative("requiredCount");
                    var visibleProp = obj.FindPropertyRelative("visibleInJournal");

                    EditorGUILayout.PropertyField(idProp);
                    EditorGUILayout.PropertyField(displayNameProp);
                    EditorGUILayout.PropertyField(typeProp);
                    var type = (ObjectiveType)typeProp.enumValueIndex;
                    if (type == ObjectiveType.FlagTrue && keys != null && keys.Length > 0)
                    {
                        int currentIndex = System.Array.IndexOf(keys, targetProp.stringValue);
                        if (currentIndex < 0) currentIndex = 0;
                        int newIndex2 = EditorGUILayout.Popup("Flag Id", currentIndex, keys);
                        if (newIndex2 >= 0 && newIndex2 < keys.Length)
                            targetProp.stringValue = keys[newIndex2];

                        var def = registry.Flags.FirstOrDefault(f => f.key == targetProp.stringValue);
                        if (def != null && !string.IsNullOrEmpty(def.description))
                            EditorGUILayout.HelpBox(def.description, MessageType.None);
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(targetProp);
                    }

                    EditorGUILayout.PropertyField(extraProp);
                    EditorGUILayout.PropertyField(countProp);
                    if (type == ObjectiveType.Elimination && countProp.intValue < 1)
                        countProp.intValue = 1;
                    EditorGUILayout.PropertyField(visibleProp);
                    EditorGUI.indentLevel--;
                    if (GUILayout.Button("Remove Objective"))
                    {
                        objs.DeleteArrayElementAtIndex(j);
                        EditorGUILayout.EndVertical();
                        break;
                    }
                    EditorGUILayout.EndVertical();
                }

                if (GUILayout.Button("Add Objective"))
                {
                    objs.InsertArrayElementAtIndex(objs.arraySize);
                }

                EditorGUI.indentLevel--;
                if (GUILayout.Button("Remove Stage"))
                {
                    stagesProp.DeleteArrayElementAtIndex(i);
                    EditorGUILayout.EndVertical();
                    break;
                }
                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("Add Stage"))
            {
                stagesProp.InsertArrayElementAtIndex(stagesProp.arraySize);
            }

            if (GUILayout.Button("Refresh Flags"))
            {
                cachedRegistry = FindFlagRegistry();
                cachedKeys = cachedRegistry != null ? GetFlagKeys(cachedRegistry) : System.Array.Empty<string>();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static FlagRegistry FindFlagRegistry()
        {
            var guids = AssetDatabase.FindAssets("t:FlagRegistry");
            for (int i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var registry = AssetDatabase.LoadAssetAtPath<FlagRegistry>(path);
                if (registry != null)
                    return registry;
            }

            return null;
        }

        private static string[] GetFlagKeys(FlagRegistry registry)
        {
            return registry != null && registry.Flags != null
                ? registry.GetKeys().ToArray()
                : System.Array.Empty<string>();
        }
    }
}

