using Enemies.Combat;
using Enemies.Config;
using Enemies.Controllers;
using Enemies.Interfaces;
using Systems.Debugging;
using UnityEngine;
using UnityEngine.AI;

namespace Enemies.Abstraction
{
    public abstract class EnemyBrainBase : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] protected BehaviourConfig behaviourConfig;

        [Header("References")]
        [SerializeField] protected Transform playerTarget;
        [SerializeField] protected AttackController attackController;
        [SerializeField] protected NavMeshAgent navMeshAgent;
        [SerializeField] protected ComponentLogger logger = new ComponentLogger();

        protected abstract IEnemyMovement Movement { get; }

        protected BehaviourConfig.DetectionSettings DetectionSettings =>
            behaviourConfig != null ? behaviourConfig.Detection : default;
        protected BehaviourConfig.VulnerableSettings VulnerableSettings =>
            behaviourConfig != null ? behaviourConfig.Vulnerable : default;

        protected float DetectionRange =>
            DetectionSettings.detectionRange > 0f ? DetectionSettings.detectionRange : 10f;

        protected virtual void Awake()
        {
            InitializeLogger();

            if (navMeshAgent == null)
                navMeshAgent = GetComponent<NavMeshAgent>();

            if (attackController == null)
                attackController = GetComponent<AttackController>();
        }

        public void SetBehaviourConfig(BehaviourConfig config)
        {
            behaviourConfig = config;
            OnBehaviourConfigChanged();
        }

        protected virtual void OnBehaviourConfigChanged()
        {
            if (Movement != null)
            {
                Movement.Initialize(new EnemyMovementContext(
                    transform,
                    navMeshAgent,
                    logger));
            }
        }

        public virtual void SetPlayerTarget(Transform target)
        {
            playerTarget = target;
        }

        protected bool IsPlayerWithinRange(float range)
        {
            if (playerTarget == null)
                return false;

            return Vector3.SqrMagnitude(playerTarget.position - transform.position) <= range * range;
        }

        protected bool IsAttackReady(AttackDefinition attackDefinition)
        {
            if (attackDefinition == null || attackController == null)
                return false;

            float currentTime = Time.time;
            return attackController.RuntimeState.IsReady(attackDefinition, currentTime);
        }

        protected void FaceTarget()
        {
            if (playerTarget == null)
                return;

            Vector3 direction = playerTarget.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f)
                return;

            Quaternion lookRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 10f);
        }

        protected void SetAgentStopped(bool stop)
        {
            if (navMeshAgent == null || !navMeshAgent.enabled)
                return;

            if (!navMeshAgent.isOnNavMesh)
            {
                logger.LogWarning(ComponentLogger.LogFlag.Events,
                    "Tried to set isStopped={0} while agent off NavMesh.",
                    stop);
                return;
            }

            navMeshAgent.isStopped = stop;
        }

        protected virtual void OnValidate()
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
