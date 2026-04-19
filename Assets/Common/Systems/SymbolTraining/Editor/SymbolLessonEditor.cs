using System.Linq;
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
            var registry = cachedRegistry ??= FindFlagRegistry();
            if (registry != null && registry.Flags != null && registry.Flags.Count > 0)
            {
                var keys = cachedKeys.Length > 0 ? cachedKeys : (cachedKeys = GetFlagKeys(registry));
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
