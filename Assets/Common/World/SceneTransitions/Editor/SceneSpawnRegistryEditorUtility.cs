using Common.Editor;

namespace Common.World.SceneTransitions.Editor
{
    internal static class SceneSpawnRegistryEditorUtility
    {
        public static SceneSpawnRegistry FindRegistry()
        {
            return ProjectAssetCache.GetSceneSpawnRegistry();
        }
    }
}
