using System;
using System.Collections;
using Enemies.Config;
using Player.Interfaces;
using Systems.Statistics;
using UnityEngine;
using UnityEngine.AI;

namespace Enemies.Controllers
{
    [DisallowMultipleComponent]
    public class StatsController : MonoBehaviour, IResourceStatsReadOnly, IDamageable, IKnockbackable, IHealthProvider
    {
        [SerializeField] private StatsConfig statsConfig;
        [SerializeField] private StatsRuntime runtime = new();
        [SerializeField, Tooltip("Delay before NavMeshAgent resumes after a knockback.")]
        private float navMeshKnockbackResumeDelay = 0.2f;
        [SerializeField, Tooltip("Delay before destroying the enemy GameObject after death.")]
        private float deathDestroyDelay = 5f;

        public StatsConfig Config => statsConfig;

        public float maxHealth => statsConfig != null ? statsConfig.maxHealth : 0f;
        public float maxMana => statsConfig != null ? statsConfig.maxMana : 0f;
        public float maxStamina => statsConfig != null ? statsConfig.maxStamina : 0f;

        public float CurrentHealth => runtime.CurrentHealth;
        public float CurrentMana => runtime.CurrentMana;
        public float CurrentStamina => runtime.CurrentStamina;
        public bool IsDead => isDead;

        private Coroutine navMeshKnockbackRoutine;
        public event Action<StatsController, Transform> Died;

        private void Awake()
        {
            if (statsConfig == null)
            {
                EnemyDebug.LogWarning($"[StatsController] StatsConfig missing on {name}");
                return;
            }

            runtime.Initialize(maxHealth, maxMana, maxStamina);
        }

        private void Update()
        {
            if (isDead)
                return;

            if (statsConfig == null)
                return;

            float delta = Time.deltaTime;
            if (delta <= 0f)
                return;

            runtime.Regenerate(
                delta,
                maxHealth,
                statsConfig.healthRegenPerSecond,
                maxMana,
                statsConfig.manaRegenPerSecond,
                maxStamina,
                statsConfig.staminaRegenPerSecond);
        }

        public void ReceiveDamage(float amount, Transform source = null)
        {
            if (isDead)
                return;

            EnemyDebug.Log($"[StatsController] {name} received damage {amount} from {(source != null ? source.name : "unknown")}.", this);
            runtime.ReceiveDamage(amount);
            if (runtime.CurrentHealth <= 0f)
                HandleDeath(source);
        }

        public void ApplyKnockback(Vector3 direction, float force)
        {
            if (isDead)
                return;

            if (force <= 0f)
                return;

            direction.y = 0f;
            direction = direction == Vector3.zero ? transform.forward : direction.normalized;

            EnemyDebug.Log($"[StatsController] Applying knockback force {force} in direction {direction} to {name}.", this);

            if (TryApplyNavMeshKnockback(direction, force))
                return;

            var body = GetComponent<Rigidbody>();
            if (body != null)
            {
                body.AddForce(direction * force, ForceMode.Impulse);
                return;
            }

            var controller = GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.Move(direction * force);
            }
        }

        public bool UseMana(float amount)
        {
            if (isDead)
                return false;
            return runtime.UseMana(amount);
        }

        public bool TryConsumeStamina(float amount)
        {
            if (isDead)
                return false;
            return runtime.TryConsumeStamina(amount);
        }

        public void RefillAll()
        {
            if (isDead)
                return;

            runtime.Refill(maxHealth, maxMana, maxStamina);
        }

        public void ClampToMax()
        {
            if (isDead)
                return;

            runtime.ClampToMax(maxHealth, maxMana, maxStamina);
        }

        public StatsRuntime Runtime => runtime;

        private bool TryApplyNavMeshKnockback(Vector3 direction, float force)
        {
            if (isDead)
                return false;

            var agent = GetComponent<NavMeshAgent>();
            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
                return false;

            if (navMeshKnockbackRoutine != null)
                StopCoroutine(navMeshKnockbackRoutine);

            navMeshKnockbackRoutine = StartCoroutine(NavMeshKnockbackRoutine(agent, direction, force));
            return true;
        }

        private IEnumerator NavMeshKnockbackRoutine(NavMeshAgent agent, Vector3 direction, float force)
        {
            if (isDead)
            {
                navMeshKnockbackRoutine = null;
                yield break;
            }

            if (!agent.isOnNavMesh)
            {
                EnemyDebug.LogWarning($"[StatsController] NavMeshAgent not on NavMesh during knockback for {name}.", this);
                navMeshKnockbackRoutine = null;
                yield break;
            }

            agent.isStopped = true;
            agent.Move(direction * force);
            EnemyDebug.Log($"[StatsController] NavMeshAgent knocked back for {name}.", this);

            float delay = Mathf.Max(0f, navMeshKnockbackResumeDelay);
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            agent.isStopped = false;
            EnemyDebug.Log($"[StatsController] NavMeshAgent resume for {name}.", this);
            navMeshKnockbackRoutine = null;
        }

        private bool isDead;

        private void HandleDeath(Transform killer)
        {
            if (isDead)
                return;

            isDead = true;
            EnemyDebug.Log($"[StatsController] {name} died.", this);

            var agent = GetComponent<NavMeshAgent>();
            if (agent != null)
                agent.enabled = false;

            if (navMeshKnockbackRoutine != null)
            {
                StopCoroutine(navMeshKnockbackRoutine);
                navMeshKnockbackRoutine = null;
            }

            foreach (var collider in GetComponentsInChildren<Collider>())
            {
                if (collider != null)
                    collider.enabled = false;
            }

            foreach (var rigidbody in GetComponentsInChildren<Rigidbody>())
            {
                if (rigidbody == null)
                    continue;
                rigidbody.isKinematic = true;
                rigidbody.linearVelocity = Vector3.zero;
                rigidbody.angularVelocity = Vector3.zero;
            }

            foreach (var behaviour in GetComponents<MonoBehaviour>())
            {
                if (behaviour == null || behaviour == this)
                    continue;
                behaviour.enabled = false;
            }

            var anim = GetComponentInChildren<Animator>();
            if (anim != null)
                anim.SetTrigger("Die");

            var characterController = GetComponent<CharacterController>();
            if (characterController != null)
                characterController.enabled = false;

            Died?.Invoke(this, killer);

            float destroyDelay = Mathf.Max(0.1f, deathDestroyDelay);
            Destroy(gameObject, destroyDelay);
        }
    }
}
