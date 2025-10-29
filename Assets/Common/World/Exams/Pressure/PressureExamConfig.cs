using System;
using System.Collections.Generic;
using UnityEngine;

namespace Common.World.Exams.Pressure
{
    /// <summary>
    /// Configuration asset describing pacing and thresholds for the pressure exam.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/World/Exams/Pressure/Exam Config", fileName = "PressureExamConfig")]
    public class PressureExamConfig : ScriptableObject
    {
        [Header("Flow")]
        [SerializeField, Min(0f)] private float introDelay = 1f;
        [SerializeField, Min(0f)] private float restartDelay = 1.5f;
        [SerializeField, Min(0f)] private float completionHoldDuration = 1f;

        [Header("Rules")]
        [SerializeField, Min(0)] private int maxMisses = 3;

        [Header("Waves")]
        [SerializeField] private List<WaveDefinition> waves = new();

        public float IntroDelay => Mathf.Max(0f, introDelay);
        public float RestartDelay => Mathf.Max(0f, restartDelay);
        public float CompletionHoldDuration => Mathf.Max(0f, completionHoldDuration);
        public int MaxMisses => Mathf.Max(0, maxMisses);
        public IReadOnlyList<WaveDefinition> Waves => waves;

        [Serializable]
        public class WaveDefinition
        {
            [SerializeField, Min(1)] private int dummyCount = 5;
            [SerializeField, Min(0f)] private float spawnInterval = 1f;
            [SerializeField, Min(0.1f)] private float dummySpeed = 4f;
            [SerializeField, Min(0f)] private float delayAfterWave = 0.75f;

            public int DummyCount => Mathf.Max(0, dummyCount);
            public float SpawnInterval => Mathf.Max(0f, spawnInterval);
            public float DummySpeed => Mathf.Max(0.1f, dummySpeed);
            public float DelayAfterWave => Mathf.Max(0f, delayAfterWave);
        }
    }
}
