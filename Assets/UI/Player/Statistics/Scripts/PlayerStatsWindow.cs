using UI.Player;
using UnityEngine;

namespace UI.Player.Statistics
{
    public class PlayerStatsWindow : PlayerWindowBase
    {
        [SerializeField] private StatsListUI statsPanel;
        bool initialized;

        void EnsureDependencies()
        {
            if (initialized) return;
            if (statsPanel == null)
                statsPanel = GetComponentInChildren<StatsListUI>(true);
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
            statsPanel?.RefreshUI();
        }
    }
}
