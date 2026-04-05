using UnityEditor;
using UnityEngine;

namespace Crafting.Editor
{
    [CustomEditor(typeof(CraftingRecipeDatabase))]
    public class CraftingRecipeDatabaseEditor : UnityEditor.Editor
    {
        private SerializedProperty recipesProp;

        private void OnEnable()
        {
            recipesProp = serializedObject.FindProperty("recipes");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Recipes", EditorStyles.boldLabel);

            int newSize = Mathf.Max(0, EditorGUILayout.IntField("Size", recipesProp.arraySize));
            if (newSize != recipesProp.arraySize)
                recipesProp.arraySize = newSize;

            for (int i = 0; i < recipesProp.arraySize; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(recipesProp.GetArrayElementAtIndex(i), new GUIContent($"Recipe {i}"));
                if (GUILayout.Button("Remove", GUILayout.Width(70f)))
                {
                    recipesProp.DeleteArrayElementAtIndex(i);
                    EditorGUILayout.EndHorizontal();
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("Add Recipe"))
            {
                recipesProp.InsertArrayElementAtIndex(recipesProp.arraySize);
            }

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(8f);
            DrawDictionaryPreview((CraftingRecipeDatabase)target);
        }

        private static void DrawDictionaryPreview(CraftingRecipeDatabase database)
        {
            if (database == null)
                return;

            EditorGUILayout.LabelField("Dictionary Preview", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Unity does not serialize dictionaries by default, so this dictionary is rebuilt automatically from the recipe list using each recipe's recipeId.", MessageType.None);

            var lookup = database.RecipesById;
            EditorGUILayout.LabelField("Count", lookup.Count.ToString());

            if (lookup.Count == 0)
                return;

            EditorGUI.BeginDisabledGroup(true);
            foreach (var pair in lookup)
            {
                EditorGUILayout.ObjectField(pair.Key, pair.Value, typeof(CraftingRecipe), false);
            }
            EditorGUI.EndDisabledGroup();
        }
    }
}
