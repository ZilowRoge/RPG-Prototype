using Common.Editor;
using UnityEditor;
using UnityEngine;

namespace Common.Progress.Editor
{
    [CustomEditor(typeof(ProgressFlagEmitter))]
    public class ProgressFlagEmitterEditor : UnityEditor.Editor
    {
        SerializedProperty progressControllerProp;
        SerializedProperty flagKeyProp;
        SerializedProperty flagValueProp;
        SerializedProperty emitOnEnableProp;
        SerializedProperty emitOnceProp;

        string[] flagOptions;
        FlagRegistry cachedRegistry;

        void OnEnable()
        {
            progressControllerProp = serializedObject.FindProperty("progressController");
            flagKeyProp = serializedObject.FindProperty("flagKey");
            flagValueProp = serializedObject.FindProperty("flagValue");
            emitOnEnableProp = serializedObject.FindProperty("emitOnEnable");
            emitOnceProp = serializedObject.FindProperty("emitOnce");

            LoadFlagOptions();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(progressControllerProp);
            EditorGUILayout.PropertyField(flagValueProp);
            EditorGUILayout.PropertyField(emitOnEnableProp);
            EditorGUILayout.PropertyField(emitOnceProp);

            DrawFlagSelector();

            serializedObject.ApplyModifiedProperties();
        }

        void LoadFlagOptions(bool forceRefresh = false)
        {
            if (forceRefresh)
            {
                cachedRegistry = null;
                flagOptions = null;
            }

            flagOptions = null;

            if (cachedRegistry == null)
                cachedRegistry = ProjectAssetCache.GetFlagRegistry(forceRefresh);

            if (cachedRegistry == null || cachedRegistry.Flags == null || cachedRegistry.Flags.Count == 0)
            {
                cachedRegistry = null;
                return;
            }

            int count = cachedRegistry.Flags.Count;
            flagOptions = new string[count];
            for (int i = 0; i < count; i++)
            {
                flagOptions[i] = cachedRegistry.Flags[i]?.key ?? string.Empty;
            }
        }

        void DrawFlagSelector()
        {
            if (flagOptions == null || flagOptions.Length == 0)
            {
                EditorGUILayout.PropertyField(flagKeyProp);
                EditorGUILayout.HelpBox("No FlagRegistry found or registry has no flags.", MessageType.Info);
                if (GUILayout.Button("Refresh Flags"))
                    LoadFlagOptions(forceRefresh: true);
                return;
            }

            int currentIndex = Mathf.Max(0, System.Array.IndexOf(flagOptions, flagKeyProp.stringValue));
            int newIndex = EditorGUILayout.Popup("Flag Key", currentIndex, flagOptions);
            if (newIndex >= 0 && newIndex < flagOptions.Length)
                flagKeyProp.stringValue = flagOptions[newIndex];

            if (GUILayout.Button("Refresh Flags"))
                LoadFlagOptions(forceRefresh: true);
        }
    }
}
