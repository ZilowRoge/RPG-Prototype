using System;
using UnityEditor;
using UnityEngine;

namespace Common.World.SceneTransitions.Editor
{
    [CustomEditor(typeof(SceneExit))]
    public class SceneExitEditor : UnityEditor.Editor
    {
        private SerializedProperty targetSceneName;
        private SerializedProperty targetSpawnId;
        private SerializedProperty captureStateOnExit;

        private void OnEnable()
        {
            targetSceneName = serializedObject.FindProperty("targetSceneName");
            targetSpawnId = serializedObject.FindProperty("targetSpawnId");
            captureStateOnExit = serializedObject.FindProperty("captureStateOnExit");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var registry = SceneSpawnRegistryEditorUtility.FindRegistry();
            if (registry == null)
            {
                EditorGUILayout.PropertyField(targetSceneName);
                EditorGUILayout.PropertyField(targetSpawnId);
            }
            else
            {
                DrawScenePopup(registry);
                DrawSpawnPopup(registry);
            }

            EditorGUILayout.PropertyField(captureStateOnExit);
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawScenePopup(SceneSpawnRegistry registry)
        {
            var sceneNames = registry.GetSceneNames();
            if (sceneNames.Length == 0)
            {
                EditorGUILayout.PropertyField(targetSceneName);
                EditorGUILayout.HelpBox("Scene registry has no scene names.", MessageType.Info);
                return;
            }

            int currentIndex = Array.IndexOf(sceneNames, targetSceneName.stringValue);
            if (currentIndex < 0)
                currentIndex = 0;

            int newIndex = EditorGUILayout.Popup("Target Scene", currentIndex, sceneNames);
            targetSceneName.stringValue = sceneNames[newIndex];
        }

        private void DrawSpawnPopup(SceneSpawnRegistry registry)
        {
            if (string.IsNullOrWhiteSpace(targetSceneName.stringValue))
            {
                EditorGUILayout.PropertyField(targetSpawnId);
                return;
            }

            var spawnIds = registry.GetSpawnIds(targetSceneName.stringValue);
            if (spawnIds.Length == 0)
            {
                EditorGUILayout.PropertyField(targetSpawnId);
                EditorGUILayout.HelpBox("Scene registry has no spawn ids for this scene.", MessageType.Info);
                return;
            }

            int currentIndex = Array.IndexOf(spawnIds, targetSpawnId.stringValue);
            if (currentIndex < 0)
                currentIndex = 0;

            int newIndex = EditorGUILayout.Popup("Target Spawn Id", currentIndex, spawnIds);
            targetSpawnId.stringValue = spawnIds[newIndex];
        }
    }
}
