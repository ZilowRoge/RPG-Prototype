using System.Linq;
using Common.Editor;
using Common.Progress;
using UnityEditor;
using UnityEngine;

namespace Common.Systems.SymbolTraining.Editor
{
    [CustomEditor(typeof(SymbolLesson))]
    public class SymbolLessonEditor : UnityEditor.Editor
    {
        private static string[] cachedKeys = System.Array.Empty<string>();
        private static FlagRegistry cachedRegistry;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var completionProp = serializedObject.FindProperty("completionFlagKey");

            // Draw everything except completionFlagKey and script reference
            DrawPropertiesExcluding(serializedObject, "m_Script", "completionFlagKey");

            // Dropdown for completionFlagKey
            var registry = cachedRegistry ??= ProjectAssetCache.GetFlagRegistry();
            if (registry != null && registry.Flags != null && registry.Flags.Count > 0)
            {
                var keys = cachedKeys.Length > 0 ? cachedKeys : (cachedKeys = ProjectAssetCache.GetFlagKeys());
                int currentIndex = System.Array.IndexOf(keys, completionProp.stringValue);
                if (currentIndex < 0) currentIndex = 0;
                int newIndex = EditorGUILayout.Popup("Completion Flag Key", currentIndex, keys);
                if (newIndex >= 0 && newIndex < keys.Length)
                    completionProp.stringValue = keys[newIndex];

                var def = registry.Flags.FirstOrDefault(f => f.key == completionProp.stringValue);
                if (def != null && !string.IsNullOrEmpty(def.description))
                    EditorGUILayout.HelpBox(def.description, MessageType.None);
            }
            else
            {
                EditorGUILayout.PropertyField(completionProp);
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
