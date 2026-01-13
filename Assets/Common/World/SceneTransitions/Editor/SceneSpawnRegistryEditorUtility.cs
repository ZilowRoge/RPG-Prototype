using UnityEditor;

namespace Common.World.SceneTransitions.Editor
{
    internal static class SceneSpawnRegistryEditorUtility
    {
        private const string RegistryFilter = "t:SceneSpawnRegistry";

        public static SceneSpawnRegistry FindRegistry()
        {
            string[] guids = AssetDatabase.FindAssets(RegistryFilter);
            if (guids == null || guids.Length == 0)
                return null;

            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<SceneSpawnRegistry>(path);
        }
    }
}
