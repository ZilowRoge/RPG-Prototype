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
    public class PressureExamUI : MonoBehaviour
    {
        [Serializable] private class IntEvent : UnityEvent<int> { }
        [Serializable] private class IntPairEvent : UnityEvent<int, int> { }

        [Header("Bindings")]
        [SerializeField] private CanvasGroup rootGroup;
        [SerializeField] private Text hitsLabel;
        [SerializeField] private Text missesLabel;
        [SerializeField] private Text waveLabel;

        [Header("Lifecycle Events")]
        [SerializeField] private UnityEvent onPreparing;
        [SerializeField] private UnityEvent onExamStarted;
        [SerializeField] private UnityEvent onExamFailed;
        [SerializeField] private UnityEvent onExamCompleted;
        [SerializeField] private UnityEvent onExamAborted;
        [SerializeField] private UnityEvent onReadyForRetry;

        [Header("Value Events")]
        [SerializeField] private IntEvent onHitsChanged;
        [SerializeField] private IntPairEvent onMissesChanged;
        [SerializeField] private IntPairEvent onWaveChanged;

        public void HandleExamPreparing(PressureExamController controller)
        {
            SetVisible(true);
            onPreparing?.Invoke();
        }

        public void HandleExamStarted(PressureExamController controller)
        {
            SetVisible(true);
            onExamStarted?.Invoke();
        }

        public void HandleExamFailed(PressureExamController controller, int misses, int maxMisses)
        {
            UpdateMisses(misses, maxMisses);
            onExamFailed?.Invoke();
        }

        public void HandleExamCompleted(PressureExamController controller, int hits, int misses, int maxMisses)
        {
            UpdateHits(hits);
            UpdateMisses(misses, maxMisses);
            onExamCompleted?.Invoke();
        }

        public void HandleExamAborted(PressureExamController controller)
        {
            onExamAborted?.Invoke();
            SetVisible(false);
        }

        public void HandleReadyForRetry(PressureExamController controller)
        {
            onReadyForRetry?.Invoke();
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

            onHitsChanged?.Invoke(hits);
        }

        private void UpdateMisses(int misses, int maxMisses)
        {
            if (missesLabel != null)
            {
                missesLabel.text = maxMisses > 0
                    ? $"{misses}/{maxMisses}"
                    : misses.ToString();
            }

            onMissesChanged?.Invoke(misses, maxMisses);
        }

        private void UpdateWave(int currentWave, int totalWaves)
        {
            if (waveLabel != null)
            {
                waveLabel.text = totalWaves > 0
                    ? $"{currentWave + 1}/{totalWaves}"
                    : "-";
            }

            onWaveChanged?.Invoke(currentWave, totalWaves);
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
