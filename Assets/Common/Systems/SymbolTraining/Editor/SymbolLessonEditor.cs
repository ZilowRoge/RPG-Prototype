using System.Linq;
using Common.Progress;
using UnityEditor;

namespace Common.Systems.SymbolTraining.Editor
{
    [CustomEditor(typeof(SymbolLesson))]
    public class SymbolLessonEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var completionProp = serializedObject.FindProperty("completionFlagKey");

            // Draw everything except completionFlagKey and script reference
            DrawPropertiesExcluding(serializedObject, "m_Script", "completionFlagKey");

            // Dropdown for completionFlagKey
            var registry = FindRegistry();
            if (registry != null && registry.Flags != null && registry.Flags.Count > 0)
            {
                var keys = registry.GetKeys().ToArray();
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

            serializedObject.ApplyModifiedProperties();
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

