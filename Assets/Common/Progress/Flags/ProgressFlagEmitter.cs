using Player.Progress;
using UnityEngine;

namespace Common.Progress
{
    /// <summary>
    /// Utility component that toggles a progress flag when invoked.
    /// </summary>
    public class ProgressFlagEmitter : MonoBehaviour
    {
        [SerializeField] private ProgressController progressController;
        [SerializeField] private string flagKey;
        [SerializeField] private bool flagValue = true;
        [SerializeField] private bool emitOnEnable;
        [SerializeField] private bool emitOnce = true;

        private bool emitted;

        private void Awake()
        {
            if (progressController == null)
                progressController = FindFirstObjectByType<ProgressController>();
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

            if (progressController == null || string.IsNullOrEmpty(flagKey))
                return;

            progressController.SetFlag(flagKey, flagValue);
            emitted = true;
        }
    }
}
