using System;
using Systems.SaveSystem;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Common.World.SceneTransitions
{
    public class SceneTransitionManager : MonoBehaviour
    {
        public static SceneTransitionManager Instance { get; private set; }

        [SerializeField] private bool fallbackToFirstSpawn = true;

        private string pendingSpawnId;
        private bool hasPendingSpawn;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        public void RequestSceneChange(string sceneName, string spawnId, bool captureState = true)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogWarning("[SceneTransitionManager] Target scene name is empty.", this);
                return;
            }

            if (captureState)
                SaveManager.Instance?.CaptureState();

            pendingSpawnId = spawnId;
            hasPendingSpawn = !string.IsNullOrWhiteSpace(spawnId) || fallbackToFirstSpawn;

            SceneManager.LoadScene(sceneName);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!hasPendingSpawn)
                return;

            var spawn = ResolveSpawnPoint(pendingSpawnId);
            if (spawn == null && fallbackToFirstSpawn)
                spawn = FindAnySpawnPoint();

            if (spawn == null)
            {
                Debug.LogWarning($"[SceneTransitionManager] SpawnPoint '{pendingSpawnId}' not found in scene '{scene.name}'.", this);
                hasPendingSpawn = false;
                pendingSpawnId = null;
                return;
            }

            PlacePlayer(spawn.transform);

            hasPendingSpawn = false;
            pendingSpawnId = null;
        }

        private static SpawnPoint ResolveSpawnPoint(string spawnId)
        {
            if (string.IsNullOrWhiteSpace(spawnId))
                return null;

            var points = FindObjectsByType<SpawnPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var point in points)
            {
                if (point != null && string.Equals(point.SpawnId, spawnId, StringComparison.Ordinal))
                    return point;
            }

            return null;
        }

        private static SpawnPoint FindAnySpawnPoint()
        {
            var points = FindObjectsByType<SpawnPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (points != null && points.Length > 0)
                return points[0];

            return null;
        }

        private static void PlacePlayer(Transform spawn)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogWarning("[SceneTransitionManager] Player with tag 'Player' not found.");
                return;
            }

            var target = player.transform;
            CharacterController controller = null;
            bool controllerWasEnabled = false;

            if (player.TryGetComponent(out controller))
            {
                controllerWasEnabled = controller.enabled;
                controller.enabled = false;
            }

            target.SetPositionAndRotation(spawn.position, spawn.rotation);

            var body = player.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            if (controller != null)
                controller.enabled = controllerWasEnabled;
        }
    }
}
