using UnityEngine;

namespace Common.World.SceneTransitions
{
    [AddComponentMenu("Game/World/Scene/Spawn Point")]
    public class SpawnPoint : MonoBehaviour
    {
        [SerializeField] private string spawnId = "Spawn_01";

        public string SpawnId => spawnId;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(spawnId))
                spawnId = gameObject.name;
        }
    }
}
