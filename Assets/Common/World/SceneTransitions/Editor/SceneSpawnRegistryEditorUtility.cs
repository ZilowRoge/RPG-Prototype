using UnityEditor;

namespace Common.World.SceneTransitions.Editor
{
    internal static class SceneSpawnRegistryEditorUtility
    {
        public static SceneSpawnRegistry FindRegistry()
        {
            var guids = AssetDatabase.FindAssets("t:SceneSpawnRegistry");
            for (var index = 0; index < guids.Length; index++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[index]);
                var registry = AssetDatabase.LoadAssetAtPath<SceneSpawnRegistry>(path);
                if (registry != null)
                    return registry;
            }

            return null;
        }
    }
}
