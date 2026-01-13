using System;
using UnityEditor;
using UnityEngine;

namespace Common.World.SceneTransitions.Editor
{
    [CustomEditor(typeof(SpawnPoint))]
    public class SpawnPointEditor : UnityEditor.Editor
    {
        private SerializedProperty spawnId;

        private void OnEnable()
        {
            spawnId = serializedObject.FindProperty("spawnId");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var registry = SceneSpawnRegistryEditorUtility.FindRegistry();
            if (registry == null)
            {
                EditorGUILayout.PropertyField(spawnId);
                serializedObject.ApplyModifiedProperties();
                return;
            }

            string sceneName = ResolveSceneName();
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                EditorGUILayout.PropertyField(spawnId);
                EditorGUILayout.HelpBox("SpawnPoint is not in a valid scene.", MessageType.Info);
                serializedObject.ApplyModifiedProperties();
                return;
            }

            var spawnIds = registry.GetSpawnIds(sceneName);
            if (spawnIds.Length == 0)
            {
                EditorGUILayout.PropertyField(spawnId);
                EditorGUILayout.HelpBox("Scene registry has no spawn ids for this scene.", MessageType.Info);
                serializedObject.ApplyModifiedProperties();
                return;
            }

            int currentIndex = Array.IndexOf(spawnIds, spawnId.stringValue);
            if (currentIndex < 0)
                currentIndex = 0;

            int newIndex = EditorGUILayout.Popup("Spawn Id", currentIndex, spawnIds);
            spawnId.stringValue = spawnIds[newIndex];

            serializedObject.ApplyModifiedProperties();
        }

        private string ResolveSceneName()
        {
            var spawnPoint = (SpawnPoint)target;
            var scene = spawnPoint.gameObject.scene;
            return scene.IsValid() ? scene.name : string.Empty;
        }
    }
}
