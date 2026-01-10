using System;
using System.Collections.Generic;

namespace Systems.SaveSystem.SaveData {
    [Serializable]
    public class GameData {
        public PlayerStatisticsData playerData = new PlayerStatisticsData();
        public PlayerProgressData progressData = new PlayerProgressData();
        public List<SceneStateData> sceneStates = new List<SceneStateData>();
        public PlayerInventoryData inventoryData = new PlayerInventoryData();

        public SceneStateData FindSceneState(string sceneId)
        {
            var normalized = NormalizeSceneId(sceneId);
            if (sceneStates == null || sceneStates.Count == 0)
                return null;

            for (int i = 0; i < sceneStates.Count; i++)
            {
                var entry = sceneStates[i];
                if (entry != null && string.Equals(entry.sceneId, normalized, StringComparison.Ordinal))
                    return entry;
            }

            return null;
        }

        public SceneStateData GetOrCreateSceneState(string sceneId)
        {
            var normalized = NormalizeSceneId(sceneId);
            sceneStates ??= new List<SceneStateData>();

            for (int i = 0; i < sceneStates.Count; i++)
            {
                var entry = sceneStates[i];
                if (entry != null && string.Equals(entry.sceneId, normalized, StringComparison.Ordinal))
                    return entry;
            }

            var created = new SceneStateData { sceneId = normalized };
            sceneStates.Add(created);
            return created;
        }

        private static string NormalizeSceneId(string sceneId)
        {
            return string.IsNullOrWhiteSpace(sceneId) ? "Scene" : sceneId;
        }
    }
}
