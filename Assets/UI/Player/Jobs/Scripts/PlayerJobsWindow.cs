using UI.Player;
using UnityEngine;

namespace UI.Player.Jobs
{
    public class PlayerJobsWindow : PlayerWindowBase
    {
        [SerializeField] private JobControllerUI jobController;
        bool initialized;

        void EnsureDependencies()
        {
            if (initialized) return;
            if (jobController == null)
                jobController = GetComponentInChildren<JobControllerUI>(true);
            initialized = true;
        }

        protected override void Awake()
        {
            EnsureDependencies();
            base.Awake();
        }

        protected override void OnShow()
        {
            EnsureDependencies();
            base.OnShow();
            jobController?.Refresh();
        }
    }
}
