using Player.Interfaces;
using UnityEngine;

namespace Common.Progress
{
    /// <summary>
    /// Utility component that toggles a progress flag when invoked.
    /// </summary>
    public class ProgressFlagEmitter : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour progressSource;
        [SerializeField] private string flagKey;
        [SerializeField] private bool flagValue = true;
        [SerializeField] private bool emitOnEnable;
        [SerializeField] private bool emitOnce = true;

        private bool emitted;
        private IProgressFlagWriter progressController;

        private void Awake()
        {
            CacheProgressController();
        }

        private void OnEnable()
        {
            if (emitOnEnable)
                Emit();
        }

        public void Emit()
        {
            if (emitOnce && emitted)
                return;

            if (progressController == null)
                CacheProgressController();

            if (progressController == null || string.IsNullOrEmpty(flagKey))
                return;

            bool previousValue = progressController.GetFlag(flagKey);
            progressController.SetFlag(flagKey, flagValue);

            if (previousValue != flagValue)
            {
                Debug.Log($"[ProgressFlagEmitter] Flag '{flagKey}' changed: {previousValue} -> {flagValue}.", this);
            }

            emitted = true;
        }

        private void CacheProgressController()
        {
            progressController = progressSource as IProgressFlagWriter;
            if (progressController != null)
                return;

            var candidates = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < candidates.Length; i++)
            {
                if (candidates[i] is IProgressFlagWriter writer)
                {
                    progressSource = candidates[i];
                    progressController = writer;
                    return;
                }
            }
        }
    }
}

