using System;
using System.Collections.Generic;
using UnityEngine;

namespace Common.World.Exams.Combat
{
    /// <summary>
    /// Configuration for combat exams: activation timing and enemy spawn definitions.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/World/Exams/Combat/Exam Config", fileName = "CombatExamConfig")]
    public class CombatExamConfig : ScriptableObject
    {
        [Header("Flow")]
        [SerializeField, Min(0f)] private float activationCountdown = 3f;
        [SerializeField, Min(0f)] private float restartDelay = 1.5f;

        [Header("Enemy Spawns")]
        [SerializeField] private List<EnemySpawnDefinition> enemySpawns = new();

        public float ActivationCountdown => Mathf.Max(0f, activationCountdown);
        public float RestartDelay => Mathf.Max(0f, restartDelay);
        public IReadOnlyList<EnemySpawnDefinition> EnemySpawns => enemySpawns;

        [Serializable]
        public class EnemySpawnDefinition
        {
            [SerializeField] private GameObject enemyPrefab;
            [SerializeField] private Vector3 positionOffset;
            [SerializeField] private Vector3 rotationOffset;

            public GameObject EnemyPrefab => enemyPrefab;
            public Vector3 PositionOffset => positionOffset;

            public Quaternion Rotation
            {
                get
                {
                    return rotationOffset.sqrMagnitude > 0f
                        ? Quaternion.Euler(rotationOffset)
                        : Quaternion.identity;
                }
            }
        }
    }
}
