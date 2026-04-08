using System.Collections;
using UnityEngine;
using Player.Events;
using Systems.SaveSystem;

namespace Player.Save
{
    /// <summary>
    /// Listens for the player death signal and reloads the last persisted save.
    /// </summary>
    public class DeathReload : MonoBehaviour
    {
        [SerializeField] private PlayerEventHub playerEvents;
        [SerializeField, Tooltip("Optional delay before loading the last save after death.")]
        private float reloadDelay = 0.5f;

        private Coroutine reloadRoutine;

        private void Awake()
        {
            CacheEventHub();
        }

        private void OnEnable()
        {
            CacheEventHub();
            if (playerEvents != null)
                playerEvents.PlayerDied += OnPlayerDied;
        }

        private void OnDisable()
        {
            if (playerEvents != null)
                playerEvents.PlayerDied -= OnPlayerDied;

            if (reloadRoutine != null)
            {
                StopCoroutine(reloadRoutine);
                reloadRoutine = null;
            }
        }

        private void OnPlayerDied()
        {
            if (reloadRoutine == null)
                reloadRoutine = StartCoroutine(ReloadAfterDelay());
        }

        private IEnumerator ReloadAfterDelay()
        {
            if (reloadDelay > 0f)
                yield return new WaitForSeconds(reloadDelay);

            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.LoadGame();
            }
            else
            {
                Debug.LogWarning("[DeathReload] SaveManager instance not found. Cannot load last save after death.", this);
            }

            reloadRoutine = null;
        }

        private void CacheEventHub()
        {
            if (playerEvents != null)
                return;

            playerEvents = GetComponent<PlayerEventHub>()
                ?? GetComponentInParent<PlayerEventHub>()
                ?? FindAnyObjectByType<PlayerEventHub>();
        }
    }
}

