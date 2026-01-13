using System;
using System.Collections;
using Systems.SaveSystem;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Player;
using Unity.Cinemachine;
using UI.Player.Common;
using Player.Save;

namespace Common.World.SceneTransitions
{
    public class SceneTransitionManager : MonoBehaviour
    {
        public static SceneTransitionManager Instance { get; private set; }
        public bool IsTransitioning => isTransitioning;

        [SerializeField] private bool fallbackToFirstSpawn = true;
        [SerializeField] private float fadeOutSeconds = 0.35f;
        [SerializeField] private float fadeInSeconds = 0.35f;
        [SerializeField] private float fadeHoldSeconds = 0.1f;
        [SerializeField] private Color fadeColor = Color.black;
        [SerializeField] private CanvasGroup fadeCanvasGroup;
        [SerializeField] private bool spawnNewPlayerPerScene = true;
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private bool rebindCinemachineOnSpawn = true;

        private string pendingSpawnId;
        private bool hasPendingSpawn;
        private bool isTransitioning;
        private Coroutine transitionRoutine;
        private Coroutine fadeRoutine;
        private const string PlayerTag = "Player";

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

            if (isTransitioning)
                return;

            if (captureState)
            {
                SaveManager.Instance?.CaptureState();
            }

            pendingSpawnId = spawnId;
            hasPendingSpawn = !string.IsNullOrWhiteSpace(spawnId) || fallbackToFirstSpawn;

            transitionRoutine = StartCoroutine(TransitionRoutine(sceneName));
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!hasPendingSpawn)
            {
                BindPlayerUiReferences(GameObject.FindGameObjectWithTag(PlayerTag));
                return;
            }

            var spawn = ResolveSpawnPoint(pendingSpawnId);
            if (spawn == null && fallbackToFirstSpawn)
                spawn = FindAnySpawnPoint();

            if (spawn == null)
            {
                Debug.LogWarning($"[SceneTransitionManager] SpawnPoint '{pendingSpawnId}' not found in scene '{scene.name}'.", this);
                hasPendingSpawn = false;
                pendingSpawnId = null;
                BindPlayerUiReferences(GameObject.FindGameObjectWithTag(PlayerTag));
                StartFadeInIfNeeded();
                return;
            }

            var player = spawnNewPlayerPerScene
                ? SpawnPlayer(spawn.transform)
                : PlacePlayer(spawn.transform);

            PlayerInputLockService.TryGetInstance()?.ClearAllLocks();
            if (rebindCinemachineOnSpawn)
                RebindCinemachineCameras(player != null ? player.transform : null);

            BindPlayerUiReferences(player);

            hasPendingSpawn = false;
            pendingSpawnId = null;

            StartFadeInIfNeeded();
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

        private GameObject SpawnPlayer(Transform spawn)
        {
            if (playerPrefab == null)
            {
                Debug.LogWarning("[SceneTransitionManager] Player prefab is not assigned. Repositioning existing player.", this);
                return PlacePlayer(spawn);
            }

            DestroyExistingPlayers();

            SaveState.SuppressNextTransformRestore = true;
            var playerInstance = Instantiate(playerPrefab);
            EnsurePlayerTag(playerInstance);
            ResetPlayerTransform(playerInstance, spawn);
            return playerInstance;
        }

        private static void DestroyExistingPlayers()
        {
            var players = GameObject.FindGameObjectsWithTag(PlayerTag);
            if (players == null || players.Length == 0)
                return;

            for (int i = 0; i < players.Length; i++)
            {
                if (players[i] != null)
                    Destroy(players[i]);
            }
        }

        private static GameObject PlacePlayer(Transform spawn)
        {
            var player = GameObject.FindGameObjectWithTag(PlayerTag);
            if (player == null)
            {
                Debug.LogWarning("[SceneTransitionManager] Player with tag 'Player' not found.");
                return null;
            }

            ResetPlayerTransform(player, spawn);
            return player;
        }

        private static void EnsurePlayerTag(GameObject player)
        {
            if (player != null && !player.CompareTag(PlayerTag))
                player.tag = PlayerTag;
        }

        private static void ResetPlayerTransform(GameObject player, Transform spawn)
        {
            if (player == null || spawn == null)
                return;

            var target = player.transform;
            CharacterController controller = null;
            bool controllerWasEnabled = false;

            if (player.TryGetComponent(out controller))
            {
                controllerWasEnabled = controller.enabled;
                controller.enabled = false;
            }

            target.SetPositionAndRotation(spawn.position, spawn.rotation);

            if (controller != null)
                controller.enabled = controllerWasEnabled;
        }

        private static void RebindCinemachineCameras(Transform target)
        {
            if (target == null)
            {
                var player = GameObject.FindGameObjectWithTag(PlayerTag);
                target = player != null ? player.transform : null;
            }

            if (target == null)
                return;

            var cameras = FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (cameras == null || cameras.Length == 0)
                return;

            for (int i = 0; i < cameras.Length; i++)
            {
                var camera = cameras[i];
                if (camera == null)
                    continue;

                camera.Follow = target;
                camera.LookAt = target;
            }
        }

        private IEnumerator TransitionRoutine(string sceneName)
        {
            isTransitioning = true;

            var group = EnsureFadeCanvas();
            if (group != null)
                yield return FadeTo(group, 1f, fadeOutSeconds);

            var operation = SceneManager.LoadSceneAsync(sceneName);
            if (operation != null)
            {
                while (!operation.isDone)
                    yield return null;
            }

            transitionRoutine = null;
        }

        private void StartFadeInIfNeeded()
        {
            if (!isTransitioning)
                return;

            if (fadeRoutine != null)
                StopCoroutine(fadeRoutine);

            var group = EnsureFadeCanvas();
            fadeRoutine = StartCoroutine(FadeInRoutine(group));
        }

        private IEnumerator FadeInRoutine(CanvasGroup group)
        {
            if (fadeHoldSeconds > 0f)
                yield return new WaitForSecondsRealtime(fadeHoldSeconds);

            if (group != null)
                yield return FadeTo(group, 0f, fadeInSeconds);

            fadeRoutine = null;
            isTransitioning = false;
            SaveState.SuppressNextTransformRestore = false;
        }

        private CanvasGroup EnsureFadeCanvas()
        {
            if (fadeCanvasGroup != null)
                return fadeCanvasGroup;

            var root = new GameObject("SceneFadeCanvas");
            root.transform.SetParent(transform, false);

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10000;

            root.AddComponent<GraphicRaycaster>();

            var imageObject = new GameObject("FadeImage");
            imageObject.transform.SetParent(root.transform, false);

            var image = imageObject.AddComponent<Image>();
            image.color = fadeColor;

            var rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            fadeCanvasGroup = root.AddComponent<CanvasGroup>();
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.interactable = false;
            fadeCanvasGroup.blocksRaycasts = false;

            return fadeCanvasGroup;
        }

        private static IEnumerator FadeTo(CanvasGroup group, float targetAlpha, float duration)
        {
            if (group == null)
                yield break;

            if (duration <= 0f)
            {
                SetFadeAlpha(group, targetAlpha);
                yield break;
            }

            float startAlpha = group.alpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                SetFadeAlpha(group, Mathf.Lerp(startAlpha, targetAlpha, t));
                yield return null;
            }

            SetFadeAlpha(group, targetAlpha);
        }

        private static void SetFadeAlpha(CanvasGroup group, float alpha)
        {
            group.alpha = alpha;
            group.blocksRaycasts = alpha > 0.001f;
        }

        private static void BindPlayerUiReferences(GameObject player)
        {
            var binder = FindFirstObjectByType<PlayerUIReferenceBinder>(FindObjectsInactive.Include);
            if (binder != null)
                binder.BindPlayer(player);
        }
    }
}
