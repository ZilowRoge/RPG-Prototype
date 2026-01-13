using System;
using System.Collections.Generic;
using UnityEngine;

namespace Common.World.SceneTransitions
{
    [CreateAssetMenu(menuName = "Game/World/Scene/Scene Spawn Registry", fileName = "SceneSpawnRegistry")]
    public class SceneSpawnRegistry : ScriptableObject
    {
        [SerializeField] private List<SceneSpawnEntry> scenes = new List<SceneSpawnEntry>();

        public IReadOnlyList<SceneSpawnEntry> Scenes => scenes;

        public SceneSpawnEntry GetScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName) || scenes == null)
                return null;

            for (int i = 0; i < scenes.Count; i++)
            {
                var entry = scenes[i];
                if (entry != null && string.Equals(entry.SceneName, sceneName, StringComparison.Ordinal))
                    return entry;
            }

            return null;
        }

        public string[] GetSceneNames()
        {
            if (scenes == null || scenes.Count == 0)
                return Array.Empty<string>();

            var result = new List<string>(scenes.Count);
            for (int i = 0; i < scenes.Count; i++)
            {
                var entry = scenes[i];
                if (entry == null)
                    continue;

                var name = entry.SceneName;
                if (!string.IsNullOrWhiteSpace(name))
                    result.Add(name);
            }

            return result.ToArray();
        }

        public string[] GetSpawnIds(string sceneName)
        {
            var entry = GetScene(sceneName);
            if (entry == null || entry.SpawnIds == null || entry.SpawnIds.Count == 0)
                return Array.Empty<string>();

            var result = new List<string>(entry.SpawnIds.Count);
            for (int i = 0; i < entry.SpawnIds.Count; i++)
            {
                var id = entry.SpawnIds[i];
                if (!string.IsNullOrWhiteSpace(id))
                    result.Add(id);
            }

            return result.ToArray();
        }
    }

    [Serializable]
    public class SceneSpawnEntry
    {
        [SerializeField] private string sceneName;
        [SerializeField] private List<string> spawnIds = new List<string>();

        public string SceneName => sceneName;
        public List<string> SpawnIds => spawnIds;
    }
}
