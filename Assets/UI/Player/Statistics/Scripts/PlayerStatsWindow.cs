using UI.Player;
using UnityEngine;

namespace UI.Player.Statistics
{
    public class PlayerStatsWindow : PlayerWindowBase
    {
        [SerializeField] private StatsListUI statsPanel;
        bool dependenciesInitialized;

        void EnsureDependencies()
        {
            if (dependenciesInitialized) return;
            if (statsPanel == null)
                statsPanel = GetComponentInChildren<StatsListUI>(true);
            dependenciesInitialized = true;
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
