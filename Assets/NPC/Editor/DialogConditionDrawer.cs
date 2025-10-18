using System.Linq;
using Common.Progress;
using NPC.Dialog;
using UnityEditor;
using UnityEngine;

namespace NPC.Editor
{
    [CustomPropertyDrawer(typeof(DialogCondition))]
    public class DialogConditionDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var typeProp = property.FindPropertyRelative("conditionType");
            var expectedProp = property.FindPropertyRelative("expectedValue");
            float h = EditorGUIUtility.singleLineHeight; // type
            h += 2f;

            // Identifier line or dropdown
            h += EditorGUIUtility.singleLineHeight + 2f;

            // Expected value line always shown
            h += EditorGUIUtility.singleLineHeight + 2f;

            return h;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            float y = position.y;
            float line = EditorGUIUtility.singleLineHeight;
            float pad = 2f;
            var r = new Rect(position.x, y, position.width, line);

            var typeProp = property.FindPropertyRelative("conditionType");
            var idProp = property.FindPropertyRelative("identifier");
            var expectedProp = property.FindPropertyRelative("expectedValue");

            EditorGUI.PropertyField(r, typeProp);
            y += line + pad;

            var type = (DialogCondition.ConditionType)typeProp.enumValueIndex;
            r = new Rect(position.x, y, position.width, line);

            if (type == DialogCondition.ConditionType.FlagEquals)
            {
                var registry = FindRegistry();
                if (registry != null && registry.Flags != null && registry.Flags.Count > 0)
                {
                    var keys = registry.GetKeys().ToArray();
                    int currentIndex = Mathf.Max(0, System.Array.IndexOf(keys, idProp.stringValue));
                    // Draw label + popup manually to include label
                    var labelWidth = EditorGUIUtility.labelWidth;
                    var labelRect = new Rect(r.x, r.y, labelWidth, r.height);
                    var fieldRect = new Rect(r.x + labelWidth, r.y, r.width - labelWidth, r.height);
                    EditorGUI.LabelField(labelRect, new GUIContent("Flag Id"));
                    int newIndex = EditorGUI.Popup(fieldRect, currentIndex, keys);
                    if (newIndex >= 0 && newIndex < keys.Length)
                        idProp.stringValue = keys[newIndex];

                    // Show description under the same line using HelpBox-like label
                    var def = registry.Flags.FirstOrDefault(f => f.key == idProp.stringValue);
                    if (def != null && !string.IsNullOrEmpty(def.description))
                    {
                        y += line + pad;
                        var hr = new Rect(position.x, y, position.width, line * 2f);
                        EditorGUI.HelpBox(hr, def.description, MessageType.None);
                        y += line * 2f + pad;
                    }
                    else
                    {
                        y += line + pad;
                    }
                }
                else
                {
                    EditorGUI.PropertyField(r, idProp, new GUIContent("Flag Id"));
                    y += line + pad;
                }
            }
            else
            {
                EditorGUI.PropertyField(r, idProp, new GUIContent("Identifier"));
                y += line + pad;
            }

            r = new Rect(position.x, y, position.width, line);
            EditorGUI.PropertyField(r, expectedProp, new GUIContent("Expected Value"));

            EditorGUI.EndProperty();
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
