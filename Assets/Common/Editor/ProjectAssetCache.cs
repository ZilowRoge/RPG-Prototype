using System;
using System.Collections.Generic;
using System.Linq;
using Common.Progress;
using Common.Systems.SymbolTraining;
using Common.World.SceneTransitions;
using Items;
using Quests;
using Systems.Jobs;
using UnityEditor;
using UnityEngine;

namespace Common.Editor
{
    [InitializeOnLoad]
    internal static class ProjectAssetCache
    {
        private static FlagRegistry cachedFlagRegistry;
        private static string[] cachedFlagKeys;
        private static SceneSpawnRegistry cachedSceneSpawnRegistry;
        private static ItemDefinitionDatabase cachedItemDatabase;
        private static string[] cachedQuestIds;
        private static string[] cachedSymbolIds;
        private static string[] cachedJobIds;
        private static readonly Dictionary<Type, UnityEngine.Object> FirstAssetByType = new();

        static ProjectAssetCache()
        {
            EditorApplication.projectChanged += InvalidateAll;
        }

        public static void InvalidateAll()
        {
            cachedFlagRegistry = null;
            cachedFlagKeys = null;
            cachedSceneSpawnRegistry = null;
            cachedItemDatabase = null;
            cachedQuestIds = null;
            cachedSymbolIds = null;
            cachedJobIds = null;
            FirstAssetByType.Clear();
        }

        public static FlagRegistry GetFlagRegistry(bool forceRefresh = false)
        {
            if (forceRefresh)
            {
                cachedFlagRegistry = null;
                cachedFlagKeys = null;
            }

            if (cachedFlagRegistry != null)
                return cachedFlagRegistry;

            cachedFlagRegistry = LoadFirstAsset<FlagRegistry>(forceRefresh);
            return cachedFlagRegistry;
        }

        public static string[] GetFlagKeys(bool forceRefresh = false)
        {
            if (forceRefresh)
                cachedFlagKeys = null;

            if (cachedFlagKeys != null)
                return cachedFlagKeys;

            var registry = GetFlagRegistry(forceRefresh);
            cachedFlagKeys = registry != null && registry.Flags != null && registry.Flags.Count > 0
                ? registry.GetKeys().ToArray()
                : Array.Empty<string>();
            return cachedFlagKeys;
        }

        public static SceneSpawnRegistry GetSceneSpawnRegistry(bool forceRefresh = false)
        {
            if (forceRefresh)
                cachedSceneSpawnRegistry = null;

            if (cachedSceneSpawnRegistry != null)
                return cachedSceneSpawnRegistry;

            cachedSceneSpawnRegistry = LoadFirstAsset<SceneSpawnRegistry>(forceRefresh);
            return cachedSceneSpawnRegistry;
        }

        public static ItemDefinitionDatabase GetItemDatabase(bool forceRefresh = false)
        {
            if (forceRefresh)
                cachedItemDatabase = null;

            if (cachedItemDatabase != null)
                return cachedItemDatabase;

            var database = FindLoadedObjectWithContent<ItemDefinitionDatabase>(candidate =>
                candidate.Definitions != null && candidate.Definitions.Count > 0);
            cachedItemDatabase = database != null ? database : FindFirstProjectAsset<ItemDefinitionDatabase>(candidate =>
                candidate.Definitions != null && candidate.Definitions.Count > 0);
            return cachedItemDatabase;
        }

        public static string[] GetQuestIds(bool forceRefresh = false)
        {
            if (forceRefresh)
                cachedQuestIds = null;

            if (cachedQuestIds != null)
                return cachedQuestIds;

            var database = LoadFirstAsset<QuestDatabase>(forceRefresh);
            cachedQuestIds = database?.All == null
                ? Array.Empty<string>()
                : database.All
                    .Where(asset => asset != null && !string.IsNullOrWhiteSpace(asset.questId))
                    .Select(asset => asset.questId)
                    .Distinct()
                    .ToArray();
            return cachedQuestIds;
        }

        public static string[] GetSymbolIds(bool forceRefresh = false)
        {
            if (forceRefresh)
                cachedSymbolIds = null;

            if (cachedSymbolIds != null)
                return cachedSymbolIds;

            var result = new List<string>();
            var guids = AssetDatabase.FindAssets("t:SymbolLesson");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var lesson = AssetDatabase.LoadAssetAtPath<SymbolLesson>(path);
                if (lesson == null || string.IsNullOrWhiteSpace(lesson.SymbolId) || result.Contains(lesson.SymbolId))
                    continue;

                result.Add(lesson.SymbolId);
            }

            cachedSymbolIds = result.ToArray();
            return cachedSymbolIds;
        }

        public static string[] GetJobIds(bool forceRefresh = false)
        {
            if (forceRefresh)
                cachedJobIds = null;

            if (cachedJobIds != null)
                return cachedJobIds;

            var result = new List<string>();
            var guids = AssetDatabase.FindAssets("t:JobData");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var job = AssetDatabase.LoadAssetAtPath<JobData>(path);
                if (job == null || string.IsNullOrWhiteSpace(job.id) || result.Contains(job.id))
                    continue;

                result.Add(job.id);
            }

            cachedJobIds = result.ToArray();
            return cachedJobIds;
        }

        public static T LoadFirstAsset<T>(bool forceRefresh = false) where T : UnityEngine.Object
        {
            if (forceRefresh)
                FirstAssetByType.Remove(typeof(T));

            if (FirstAssetByType.TryGetValue(typeof(T), out var cached))
                return cached as T;

            var asset = FindFirstProjectAsset<T>(_ => true);
            FirstAssetByType[typeof(T)] = asset;
            return asset;
        }

        private static T FindLoadedObjectWithContent<T>(Func<T, bool> predicate) where T : UnityEngine.Object
        {
            var loadedObjects = Resources.FindObjectsOfTypeAll<T>();
            for (var i = 0; i < loadedObjects.Length; i++)
            {
                var candidate = loadedObjects[i];
                if (candidate != null && predicate(candidate))
                    return candidate;
            }

            return null;
        }

        private static T FindFirstProjectAsset<T>(Func<T, bool> predicate) where T : UnityEngine.Object
        {
            var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null && predicate(asset))
                    return asset;
            }

            return null;
        }
    }
}
