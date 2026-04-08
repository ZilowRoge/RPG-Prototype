using UnityEditor;
using UnityEngine;

namespace NPC.Dialog.Editor
{
    [CustomEditor(typeof(DialogAsset))]
    public class DialogAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("dialogId"));

            EditorGUILayout.Space(8f);
            if (GUILayout.Button("Open Dialogue Graph"))
                DialogGraphEditorWindow.Open((DialogAsset)target);

            if (GUILayout.Button("Validate Graph"))
            {
                var issues = ((DialogAsset)target).ValidateGraph();
                if (issues.Count == 0)
                    Debug.Log($"[DialogAsset] '{target.name}' passed validation.");
                else
                    Debug.LogWarning($"[DialogAsset] '{target.name}' validation issues:{System.Environment.NewLine}- {string.Join(System.Environment.NewLine + "- ", issues)}");
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
