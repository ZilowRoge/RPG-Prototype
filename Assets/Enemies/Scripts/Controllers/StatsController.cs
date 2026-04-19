using System;
using System.Collections;
using Enemies.Config;
using Player.Interfaces;
using Systems.Debugging;
using Systems.Statistics;
using UnityEngine;
using UnityEngine.AI;

namespace Enemies.Controllers
{
    [DisallowMultipleComponent]
    public class StatsController : MonoBehaviour, IResourceStatsReadOnly, IDamageable, IKnockbackable, IHealthProvider, IDeathState
    {
        [SerializeField] private StatsConfig statsConfig;
        [SerializeField] private StatsRuntime runtime = new();
        [SerializeField, Tooltip("Delay before NavMeshAgent resumes after a knockback.")]
        private float navMeshKnockbackResumeDelay = 0.2f;
        [SerializeField, Tooltip("Delay before destroying the enemy GameObject after death.")]
        private float deathDestroyDelay = 5f;
        [SerializeField] private ComponentLogger logger = new ComponentLogger();

        public StatsConfig Config => statsConfig;

        public float maxHealth => statsConfig != null ? statsConfig.maxHealth : 0f;
        public float maxMana => statsConfig != null ? statsConfig.maxMana : 0f;
        public float maxStamina => statsConfig != null ? statsConfig.maxStamina : 0f;

        public float CurrentHealth => runtime.CurrentHealth;
        public float CurrentMana => runtime.CurrentMana;
        public float CurrentStamina => runtime.CurrentStamina;
        public bool IsDead => isDead;

        private Coroutine navMeshKnockbackRoutine;
        public event Action<StatsController, float, Transform> Damaged;
        public event Action<StatsController, Transform> Died;

        private void Awake()
        {
            InitializeLogger();
            if (statsConfig == null)
            {
                logger.LogWarning(ComponentLogger.LogFlag.Events, "StatsConfig missing on {0}", name);
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

            logger.Log(ComponentLogger.LogFlag.Events,
                "{0} received damage {1} from {2}.",
                name,
                amount,
                source != null ? source.name : "unknown");
            runtime.ReceiveDamage(amount);
            Damaged?.Invoke(this, amount, source);
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

            logger.Log(ComponentLogger.LogFlag.Events,
                "Applying knockback force {0} in direction {1} to {2}.",
                force,
                direction,
                name);

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
                logger.LogWarning(ComponentLogger.LogFlag.Events,
                    "NavMeshAgent not on NavMesh during knockback for {0}.",
                    name);
                navMeshKnockbackRoutine = null;
                yield break;
            }

            agent.isStopped = true;
            agent.Move(direction * force);
            logger.Log(ComponentLogger.LogFlag.Events,
                "NavMeshAgent knocked back for {0}.",
                name);

            float delay = Mathf.Max(0f, navMeshKnockbackResumeDelay);
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            agent.isStopped = false;
            logger.Log(ComponentLogger.LogFlag.Events,
                "NavMeshAgent resume for {0}.",
                name);
            navMeshKnockbackRoutine = null;
        }

        private bool isDead;

        private void HandleDeath(Transform killer)
        {
            if (isDead)
                return;

            isDead = true;
            logger.Log(ComponentLogger.LogFlag.Events, "{0} died.", name);

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

        private void OnValidate()
        {
            InitializeLogger();
        }

        private void InitializeLogger()
        {
            if (logger == null)
                logger = new ComponentLogger();
            logger.BindContext(this);
        }
    }
}
