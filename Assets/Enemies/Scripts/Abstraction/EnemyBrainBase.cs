using Enemies.Combat;
using Enemies.Config;
using Enemies.Controllers;
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

        protected MovementConfig MovementConfig => behaviourConfig != null ? behaviourConfig.Movement : null;
        protected MovementConfig.IdleSettings IdleSettings => MovementConfig?.Idle ?? default;
        protected MovementConfig.ChaseSettings ChaseSettings => MovementConfig?.Chase ?? default;
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

        protected bool EnsureAgentOnNavMesh(string context)
        {
            if (navMeshAgent == null || !navMeshAgent.enabled)
                return false;

            if (navMeshAgent.isOnNavMesh)
                return true;

            if (NavMesh.SamplePosition(transform.position, out var hit, 1.5f, navMeshAgent.areaMask))
            {
                navMeshAgent.Warp(hit.position);
                logger.Log(ComponentLogger.LogFlag.Events,
                    "{0}: warped agent onto NavMesh.",
                    context);
                return true;
            }

            logger.LogWarning(ComponentLogger.LogFlag.Events,
                "{0}: failed to find NavMesh.",
                context);
            return false;
        }

        protected void SetDestinationSafe(Vector3 destination, string debugContext)
        {
            if (!EnsureAgentOnNavMesh(debugContext))
                return;

            if (!navMeshAgent.SetDestination(destination))
            {
                logger.LogWarning(ComponentLogger.LogFlag.Events,
                    "{0}: SetDestination failed.",
                    debugContext);
            }
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
