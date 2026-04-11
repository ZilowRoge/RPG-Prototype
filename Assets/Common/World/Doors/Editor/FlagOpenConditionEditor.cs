using System.Linq;
using Common.Editor;
using Common.Progress;
using Common.Systems.SymbolTraining;
using UnityEditor;
using UnityEngine;

namespace Common.World.Doors.Editor
{
    [CustomEditor(typeof(FlagOpenCondition))]
    public class FlagOpenConditionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var idProp = serializedObject.FindProperty("id");
            var lockedProp = serializedObject.FindProperty("lockedWhenFlagIsTrue");

            EditorGUILayout.PropertyField(lockedProp);
            EditorGUILayout.Space(6);

            // Try find any FlagRegistry in project (Editor-only)
            var registry = ProjectAssetCache.GetFlagRegistry();

            if (registry != null && registry.Flags != null && registry.Flags.Count > 0)
            {
                var keys = ProjectAssetCache.GetFlagKeys();
                int currentIndex = Mathf.Max(0, System.Array.IndexOf(keys, idProp.stringValue));
                int newIndex = EditorGUILayout.Popup("Flag Id", currentIndex, keys);
                if (newIndex >= 0 && newIndex < keys.Length)
                {
                    idProp.stringValue = keys[newIndex];

                    // Tooltip/description preview for selected flag
                    var def = registry.Flags.FirstOrDefault(f => f.key == idProp.stringValue);
                    if (def != null)
                    {
                        if (!string.IsNullOrEmpty(def.description))
                            EditorGUILayout.HelpBox(def.description, MessageType.None);
                        EditorGUILayout.LabelField("Default", def.defaultValue ? "true" : "false");
                    }
                }
            }
            else
            {
                EditorGUILayout.PropertyField(idProp, new GUIContent("Flag Id"));
                EditorGUILayout.HelpBox("Create a FlagRegistry asset to get a dropdown of known flags.", MessageType.Info);
            }

            if (GUILayout.Button("Refresh Flags"))
                ProjectAssetCache.GetFlagKeys(forceRefresh: true);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
