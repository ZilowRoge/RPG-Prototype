using System.Linq;
using Common.Progress;
using NPC.Dialog;
using UnityEditor;
using UnityEngine;

namespace NPC.Editor
{
    [CustomPropertyDrawer(typeof(DialogAction))]
    public class DialogActionDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float h = 0f;
            float line = EditorGUIUtility.singleLineHeight + 2f;

            var typeProp = property.FindPropertyRelative("type");
            var onRunProp = property.FindPropertyRelative("onRun");

            h += line; // type
            h += line; // id
            h += line; // value
            h += EditorGUI.GetPropertyHeight(onRunProp, true) + 2f; // UnityEvent

            return h;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            float y = position.y;
            float lineH = EditorGUIUtility.singleLineHeight;
            float pad = 2f;

            var typeProp = property.FindPropertyRelative("type");
            var idProp = property.FindPropertyRelative("actionId");
            var valueProp = property.FindPropertyRelative("actionValue");
            var onRunProp = property.FindPropertyRelative("onRun");

            var r = new Rect(position.x, y, position.width, lineH);
            EditorGUI.PropertyField(r, typeProp);
            y += lineH + pad;

            var type = (DialogAction.ActionType)typeProp.enumValueIndex;
            r = new Rect(position.x, y, position.width, lineH);
            if (type == DialogAction.ActionType.SetFlag)
            {
                var registry = FindRegistry();
                if (registry != null && registry.Flags != null && registry.Flags.Count > 0)
                {
                    var keys = registry.GetKeys().ToArray();
                    int currentIndex = Mathf.Max(0, System.Array.IndexOf(keys, idProp.stringValue));
                    var labelWidth = EditorGUIUtility.labelWidth;
                    var labelRect = new Rect(r.x, r.y, labelWidth, r.height);
                    var fieldRect = new Rect(r.x + labelWidth, r.y, r.width - labelWidth, r.height);
                    EditorGUI.LabelField(labelRect, new GUIContent("Flag Id"));
                    int newIndex = EditorGUI.Popup(fieldRect, currentIndex, keys);
                    if (newIndex >= 0 && newIndex < keys.Length)
                        idProp.stringValue = keys[newIndex];

                    var def = registry.Flags.FirstOrDefault(f => f.key == idProp.stringValue);
                    if (def != null && !string.IsNullOrEmpty(def.description))
                    {
                        y += lineH + pad;
                        var hr = new Rect(position.x, y, position.width, lineH * 2f);
                        EditorGUI.HelpBox(hr, def.description, MessageType.None);
                        y += lineH * 2f + pad;
                    }
                    else
                    {
                        y += lineH + pad;
                    }
                }
                else
                {
                    EditorGUI.PropertyField(r, idProp, new GUIContent("Flag Id"));
                    y += lineH + pad;
                }
            }
            else
            {
                EditorGUI.PropertyField(r, idProp, new GUIContent("Identifier"));
                y += lineH + pad;
            }

            r = new Rect(position.x, y, position.width, lineH);
            EditorGUI.PropertyField(r, valueProp, new GUIContent("Value"));
            y += lineH + pad;

            var onRunRect = new Rect(position.x, y, position.width, EditorGUI.GetPropertyHeight(onRunProp, true));
            EditorGUI.PropertyField(onRunRect, onRunProp, true);

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
