using System;
using Common.World.Exams.Pressure;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI.Player.Exams
{
    /// <summary>
    /// Bridges the pressure exam controller with HUD elements and UnityEvents.
    /// </summary>
    [AddComponentMenu("Game/UI/Exams/Pressure Exam UI")]
    public class PressureExamUI : MonoBehaviour, IPressureExamPresenter
    {
        [Header("Bindings")]
        [SerializeField] private CanvasGroup rootGroup;
        [SerializeField] private Text hitsLabel;
        [SerializeField] private Text missesLabel;
        [SerializeField] private Text waveLabel;

        public void HandleExamPreparing(PressureExamController controller)
        {
            SetVisible(true);
        }

        public void HandleExamStarted(PressureExamController controller)
        {
            SetVisible(true);
        }

        public void HandleExamFailed(PressureExamController controller, int misses, int maxMisses)
        {
            UpdateMisses(misses, maxMisses);
        }

        public void HandleExamCompleted(PressureExamController controller, int hits, int misses, int maxMisses)
        {
            UpdateHits(hits);
            UpdateMisses(misses, maxMisses);
        }

        public void HandleExamAborted(PressureExamController controller)
        {
            SetVisible(false);
        }

        public void HandleHitCountChanged(int hits)
        {
            UpdateHits(hits);
        }

        public void HandleMissCountChanged(int misses, int maxMisses)
        {
            UpdateMisses(misses, maxMisses);
        }

        public void HandleWaveAdvanced(int currentWave, int totalWaves)
        {
            UpdateWave(currentWave, totalWaves);
        }

        private void UpdateHits(int hits)
        {
            if (hitsLabel != null)
                hitsLabel.text = hits.ToString();
        }

        private void UpdateMisses(int misses, int maxMisses)
        {
            if (missesLabel != null)
            {
                missesLabel.text = maxMisses > 0
                    ? $"{misses}/{maxMisses}"
                    : misses.ToString();
            }
        }

        private void UpdateWave(int currentWave, int totalWaves)
        {
            if (waveLabel != null)
            {
                waveLabel.text = totalWaves > 0
                    ? $"{currentWave + 1}/{totalWaves}"
                    : "-";
            }
        }

        private void SetVisible(bool visible)
        {
            if (rootGroup == null)
                return;

            rootGroup.alpha = visible ? 1f : 0f;
            rootGroup.interactable = visible;
            rootGroup.blocksRaycasts = visible;
        }
    }
}
