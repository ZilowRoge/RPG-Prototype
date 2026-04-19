using System;
using System.Collections.Generic;
using Items;
using UnityEditor;
using UnityEngine;

namespace Crafting.Editor
{
    [CustomEditor(typeof(CraftingRecipe))]
    public class CraftingRecipeEditor : UnityEditor.Editor
    {
        private SerializedProperty recipeIdProp;
        private SerializedProperty stationTypeProp;
        private SerializedProperty materialsProp;
        private SerializedProperty productsProp;

        private ItemDefinitionDatabase cachedDatabase;
        private string[] itemIdOptions = Array.Empty<string>();
        private string[] itemDisplayOptions = Array.Empty<string>();

        private void OnEnable()
        {
            recipeIdProp = serializedObject.FindProperty("recipeId");
            stationTypeProp = serializedObject.FindProperty("stationType");
            materialsProp = serializedObject.FindProperty("materials");
            productsProp = serializedObject.FindProperty("products");

            LoadItemOptions();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(recipeIdProp);

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Recipe", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(stationTypeProp);
            DrawItemDatabaseInfo();

            EditorGUILayout.Space(6f);
            DrawItemList(materialsProp, "Materials", "Material", "Add Material");

            EditorGUILayout.Space(6f);
            DrawItemList(productsProp, "Products", "Product", "Add Product");

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawItemDatabaseInfo()
        {
            if (cachedDatabase != null && itemIdOptions.Length > 0)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField("Item Database", cachedDatabase, typeof(ItemDefinitionDatabase), false);
                EditorGUI.EndDisabledGroup();

                if (GUILayout.Button("Refresh Item Ids"))
                    LoadItemOptions(forceRefresh: true);

                return;
            }

            EditorGUILayout.HelpBox("No ItemDefinitionDatabase with items was found. You can still type item IDs manually.", MessageType.Info);

            if (GUILayout.Button("Refresh Item Ids"))
                LoadItemOptions(forceRefresh: true);
        }

        private void DrawItemList(SerializedProperty listProp, string header, string entryLabel, string addButtonLabel)
        {
            EditorGUILayout.LabelField(header, EditorStyles.boldLabel);

            for (int i = 0; i < listProp.arraySize; i++)
            {
                var element = listProp.GetArrayElementAtIndex(i);
                var itemIdProp = element.FindPropertyRelative("itemId");
                var amountProp = element.FindPropertyRelative("amount");

                EditorGUILayout.BeginVertical(GUI.skin.box);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{entryLabel} {i}", EditorStyles.miniBoldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("X", EditorStyles.miniButton, GUILayout.Width(22f)))
                {
                    listProp.DeleteArrayElementAtIndex(i);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }
                EditorGUILayout.EndHorizontal();

                DrawItemIdField(itemIdProp);
                EditorGUILayout.PropertyField(amountProp);
                amountProp.intValue = Mathf.Max(1, amountProp.intValue);

                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button(addButtonLabel))
            {
                int newIndex = listProp.arraySize;
                listProp.InsertArrayElementAtIndex(newIndex);

                var newElement = listProp.GetArrayElementAtIndex(newIndex);
                newElement.FindPropertyRelative("itemId").stringValue = GetDefaultItemId();
                newElement.FindPropertyRelative("amount").intValue = 1;
            }
        }

        private void DrawItemIdField(SerializedProperty itemIdProp)
        {
            if (itemIdOptions.Length == 0)
            {
                EditorGUILayout.PropertyField(itemIdProp, new GUIContent("Item Id"));
                return;
            }

            string currentItemId = itemIdProp.stringValue;
            int currentIndex = Array.IndexOf(itemIdOptions, currentItemId);

            if (currentIndex >= 0)
            {
                int newIndex = EditorGUILayout.Popup("Item Id", currentIndex, itemDisplayOptions);
                if (newIndex >= 0 && newIndex < itemIdOptions.Length)
                    itemIdProp.stringValue = itemIdOptions[newIndex];

                return;
            }

            var fallbackIds = new string[itemIdOptions.Length + 1];
            var fallbackLabels = new string[itemDisplayOptions.Length + 1];

            fallbackIds[0] = currentItemId;
            fallbackLabels[0] = string.IsNullOrWhiteSpace(currentItemId) ? "<Missing Item Id>" : $"{currentItemId} (missing)";

            Array.Copy(itemIdOptions, 0, fallbackIds, 1, itemIdOptions.Length);
            Array.Copy(itemDisplayOptions, 0, fallbackLabels, 1, itemDisplayOptions.Length);

            int fallbackIndex = EditorGUILayout.Popup("Item Id", 0, fallbackLabels);
            if (fallbackIndex >= 0 && fallbackIndex < fallbackIds.Length)
                itemIdProp.stringValue = fallbackIds[fallbackIndex];

            if (!string.IsNullOrWhiteSpace(currentItemId))
            {
                EditorGUILayout.HelpBox($"Item id '{currentItemId}' does not exist in the current item database.", MessageType.Warning);
            }
        }

        private string GetDefaultItemId()
        {
            return itemIdOptions.Length > 0 ? itemIdOptions[0] : string.Empty;
        }

        private void LoadItemOptions(bool forceRefresh = false)
        {
            cachedDatabase = FindItemDatabase();

            if (cachedDatabase == null || cachedDatabase.Definitions == null || cachedDatabase.Definitions.Count == 0)
            {
                itemIdOptions = Array.Empty<string>();
                itemDisplayOptions = Array.Empty<string>();
                return;
            }

            var ids = new List<string>();
            var labels = new List<string>();
            var usedIds = new HashSet<string>();

            foreach (var definition in cachedDatabase.Definitions)
            {
                if (definition == null || string.IsNullOrWhiteSpace(definition.Id) || !usedIds.Add(definition.Id))
                    continue;

                ids.Add(definition.Id);
                labels.Add(string.IsNullOrWhiteSpace(definition.Name)
                    ? definition.Id
                    : $"{definition.Id} ({definition.Name})");
            }

            itemIdOptions = ids.ToArray();
            itemDisplayOptions = labels.ToArray();
        }

        private static ItemDefinitionDatabase FindItemDatabase()
        {
            var guids = AssetDatabase.FindAssets("t:ItemDefinitionDatabase");
            for (int i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var database = AssetDatabase.LoadAssetAtPath<ItemDefinitionDatabase>(path);
                if (database != null && database.Definitions != null && database.Definitions.Count > 0)
                    return database;
            }

            return null;
        }
    }
}
