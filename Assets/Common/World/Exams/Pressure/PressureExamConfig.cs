using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Common.World.Exams.Pressure
{
    /// <summary>
    /// Configuration asset describing pacing and thresholds for the pressure exam.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/World/Exams/Pressure/Exam Config", fileName = "PressureExamConfig")]
    public class PressureExamConfig : ScriptableObject
    {
        public enum StageMode
        {
            StationaryTimed = 0,
            Advancing = 1
        }

        [Header("Flow")]
        [SerializeField, Min(0f)] private float introDelay = 1f;
        [SerializeField, Min(0f)] private float restartDelay = 1.5f;
        [SerializeField, Min(0f)] private float completionHoldDuration = 1f;

        [Header("Stages")]
        [SerializeField, FormerlySerializedAs("waves")] private List<StageDefinition> stages = new();

        public float IntroDelay => Mathf.Max(0f, introDelay);
        public float RestartDelay => Mathf.Max(0f, restartDelay);
        public float CompletionHoldDuration => Mathf.Max(0f, completionHoldDuration);
        public IReadOnlyList<StageDefinition> Stages => stages;

        [Serializable]
        public class StageDefinition
        {
            [SerializeField] private StageMode mode = StageMode.StationaryTimed;
            [SerializeField, Min(1), FormerlySerializedAs("dummyCount")] private int shieldCount = 3;
            [SerializeField, Min(1)] private int requiredHits = 2;
            [SerializeField, Min(0f)] private float spawnInterval = 1f;
            [SerializeField, Min(0f)] private float stationaryLifetime = 2.25f;
            [SerializeField, Min(0.1f), FormerlySerializedAs("dummySpeed")] private float shieldSpeed = 4f;
            [SerializeField, Min(0f), FormerlySerializedAs("delayAfterWave")] private float delayAfterStage = 0.75f;

            public StageMode Mode => mode;
            public int ShieldCount => Mathf.Max(1, shieldCount);
            public int RequiredHits => Mathf.Clamp(requiredHits, 1, ShieldCount);
            public float SpawnInterval => Mathf.Max(0f, spawnInterval);
            public float StationaryLifetime => Mathf.Max(0f, stationaryLifetime);
            public float ShieldSpeed => Mathf.Max(0.1f, shieldSpeed);
            public float DelayAfterStage => Mathf.Max(0f, delayAfterStage);
        }
    }
}
