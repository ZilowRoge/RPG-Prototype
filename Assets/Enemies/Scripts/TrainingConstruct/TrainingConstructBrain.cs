using System.Collections;
using Enemies.Combat;
using Enemies.Controllers;
using Enemies.Config;
using UnityEngine;
using UnityEngine.AI;

namespace Enemies.TrainingConstruct
{
    /// <summary>
    /// Behaviour controller for the Training Construct enemy.
    /// Uses external configuration assets for movement and attacks.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class TrainingConstructBrain : MonoBehaviour
    {
        private enum ConstructState
        {
            Idle,
            Detect,
            Chase,
            ImpulseAttack,
            ChargeAttack,
            Reboot
        }

        [Header("Configuration")]
        [SerializeField] private BehaviourConfig behaviourConfig;

        [Header("References")]
        [SerializeField] private Transform playerTarget;
        [SerializeField] private AttackController attackController;
        [SerializeField] private NavMeshAgent navMeshAgent;
        [SerializeField] private ChargeHitbox chargeHitbox;
        [Header("Damage Awareness")]
        [SerializeField, Tooltip("When true, any incoming damage will force the construct to detect and chase the attacker.")]
        private bool aggroOnDamage = true;
        [SerializeField, Tooltip("How long (seconds) the construct remembers being damaged for aggro purposes.")]
        private float damageAggroDuration = 5f;

        [SerializeField] private ConstructState currentState = ConstructState.Idle;
        private Coroutine stateRoutine;
        private float idleTimer;
        private Vector3 idleAnchor;
        private float baseSpeed;
        private float baseAcceleration;
        private AttackRule currentAttackRule;
        private float damageAggroTimer;
        private StatsController statsController;

        private MovementConfig MovementConfig => behaviourConfig != null ? behaviourConfig.Movement : null;
        private MovementConfig.IdleSettings IdleSettings => MovementConfig?.Idle ?? default;
        private MovementConfig.ChaseSettings ChaseSettings => MovementConfig?.Chase ?? default;
        private MovementConfig.ChargeSettings ChargeSettings => MovementConfig?.Charge ?? default;

        private BehaviourConfig.DetectionSettings DetectionSettings =>
            behaviourConfig != null ? behaviourConfig.Detection : default;

        private BehaviourConfig.RebootSettings RebootSettings =>
            behaviourConfig != null ? behaviourConfig.Reboot : default;

        private float DetectionRange => DetectionSettings.detectionRange > 0f ? DetectionSettings.detectionRange : 10f;
        private float LeashRangeMultiplier => DetectionSettings.leashRangeMultiplier > 0f ? DetectionSettings.leashRangeMultiplier : 1.25f;
        private float LeashRange => DetectionRange * LeashRangeMultiplier;
        private bool HasDamageAggro => aggroOnDamage && damageAggroTimer > 0f;

        private void Awake()
        {
            if (navMeshAgent == null)
                navMeshAgent = GetComponent<NavMeshAgent>();

            if (attackController == null)
                attackController = GetComponent<AttackController>();

            if (chargeHitbox == null)
                chargeHitbox = GetComponentInChildren<ChargeHitbox>(true);
            if (chargeHitbox == null)
                EnemyDebug.LogWarning("[TrainingConstructBrain] ChargeHitbox is not assigned.", this);

            statsController = GetComponent<StatsController>();
            if (statsController == null)
                EnemyDebug.LogWarning("[TrainingConstructBrain] StatsController is not assigned.", this);
            damageAggroDuration = Mathf.Max(0f, damageAggroDuration);

            if (playerTarget == null)
            {
                var playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null) {
                    Debug.Log("[TrainingCOntructBrain] Player found");
                    playerTarget = playerObject.transform;
                }
            }

            idleAnchor = transform.position;
            baseSpeed = navMeshAgent.speed;
            baseAcceleration = navMeshAgent.acceleration;

            EnsureAgentOnNavMesh("Awake");
        }

        private void OnEnable()
        {
            if (statsController != null)
                statsController.Damaged += OnConstructDamaged;

            SwitchState(ConstructState.Idle);
        }

        private void OnDisable()
        {
            if (statsController != null)
                statsController.Damaged -= OnConstructDamaged;
        }

        private void Update()
        {
            if (playerTarget == null)
                return;

            if (HasDamageAggro)
            {
                damageAggroTimer -= Time.deltaTime;
                if (damageAggroTimer < 0f)
                    damageAggroTimer = 0f;
            }

            switch (currentState)
            {
                case ConstructState.Idle:
                    UpdateIdle();
                    break;
                case ConstructState.Detect:
                    UpdateDetect();
                    break;
                case ConstructState.Chase:
                    UpdateChase();
                    break;
                case ConstructState.ImpulseAttack:
                    FaceTarget();
                    break;
                case ConstructState.Reboot:
                    FaceTarget();
                    break;
            }
        }

        private void UpdateIdle()
        {
            float rotationSpeed = IdleSettings.rotationSpeed != 0f ? IdleSettings.rotationSpeed : 20f;
            float moveInterval = IdleSettings.moveInterval > 0f ? IdleSettings.moveInterval : 5f;
            float moveSpeed = IdleSettings.moveSpeed > 0f ? IdleSettings.moveSpeed : baseSpeed * 0.5f;

            navMeshAgent.speed = moveSpeed;
            navMeshAgent.acceleration = ChaseSettings.acceleration > 0f ? ChaseSettings.acceleration : baseAcceleration;
            SetAgentStopped(false);

            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

            idleTimer += Time.deltaTime;
            if (idleTimer >= moveInterval)
            {
                idleTimer = 0f;
                TryMoveToRandomIdlePoint();
            }

            if (HasDamageAggro || IsPlayerWithinRange(DetectionRange))
            {
                SwitchState(ConstructState.Detect);
            }
        }

        private void UpdateDetect()
        {
            if (!HasDamageAggro && !IsPlayerWithinRange(DetectionRange))
            {
                SwitchState(ConstructState.Idle);
                return;
            }

            SwitchState(ConstructState.Chase);
        }

        private void UpdateChase()
        {
            if (!HasDamageAggro && !IsPlayerWithinRange(LeashRange))
            {
                SwitchState(ConstructState.Idle);
                return;
            }

            float chaseSpeed = ChaseSettings.speed > 0f ? ChaseSettings.speed : baseSpeed;
            float chaseAcceleration = ChaseSettings.acceleration > 0f ? ChaseSettings.acceleration : baseAcceleration;
            float preferredMinDistance = ChaseSettings.preferredMinDistance > 0f ? ChaseSettings.preferredMinDistance : 3f;
            float preferredMaxDistance = ChaseSettings.preferredMaxDistance > 0f ? ChaseSettings.preferredMaxDistance : 5f;

            navMeshAgent.speed = chaseSpeed;
            navMeshAgent.acceleration = chaseAcceleration;
            SetAgentStopped(false);

            float distance = Vector3.Distance(transform.position, playerTarget.position);
            EnemyDebug.Log($"[TrainingConstructBrain] Distance to player: {distance:F2}", this);
            Debug.DrawLine(transform.position, playerTarget.position, Color.green);
            if (distance > preferredMaxDistance)
            {
                SetDestinationSafe(playerTarget.position, "Chase towards player");
            }
            else if (distance < preferredMinDistance)
            {
                Vector3 retreatDir = (transform.position - playerTarget.position).normalized;
                Vector3 retreatPoint = transform.position + retreatDir * 2f;
                if (NavMesh.SamplePosition(retreatPoint, out var hit, 2f, navMeshAgent.areaMask))
                    SetDestinationSafe(hit.position, "Retreat from player");
            }
            else
            {
                navMeshAgent.velocity = Vector3.zero;
                SetAgentStopped(true);
            }

            TryBeginAttack(distance);
        }

        private void TryBeginAttack(float distance)
        {
            if (behaviourConfig == null || attackController == null)
                return;

            foreach (var rule in behaviourConfig.Attacks)
            {
                if (rule == null)
                    continue;

                var attack = rule.Attack;
                if (attack == null)
                    continue;

                bool withinRange = rule.IsDistanceSatisfied(distance);
                EnemyDebug.Log($"[TrainingConstructBrain] Checking attack '{attack.name}': distance {distance:F2}, required {rule.MinDistance}-{rule.MaxDistance}, in range = {withinRange}", this);

                if (!withinRange)
                    continue;

                if (!IsAttackReady(attack))
                {
                    EnemyDebug.Log($"[TrainingConstructBrain] Attack '{attack.name}' still on cooldown.", this);
                    continue;
                }

                BeginAttack(rule);
                break;
            }
        }

        private void BeginAttack(AttackRule rule)
        {
            currentAttackRule = rule;

            switch (rule.Type)
            {
                case AttackType.Impulse:
                    SwitchState(ConstructState.ImpulseAttack);
                    break;
                case AttackType.Charge:
                    SwitchState(ConstructState.ChargeAttack);
                    break;
            }
        }

        private void TryMoveToRandomIdlePoint()
        {
            float radius = IdleSettings.moveRadius > 0f ? IdleSettings.moveRadius : 4f;
            Vector3 randomPoint = idleAnchor + Random.insideUnitSphere * radius;
            randomPoint.y = idleAnchor.y;

            if (NavMesh.SamplePosition(randomPoint, out var hit, radius, navMeshAgent.areaMask))
            {
                SetDestinationSafe(hit.position, "Idle wander");
            }
        }

        private bool IsPlayerWithinRange(float range)
        {
            if (playerTarget == null)
                return false;

            return Vector3.SqrMagnitude(playerTarget.position - transform.position) <= range * range;
        }

        private void SwitchState(ConstructState newState)
        {
            if (currentState == newState)
                return;

            if (stateRoutine != null)
            {
                StopCoroutine(stateRoutine);
                stateRoutine = null;
            }

            ExitState(currentState);
            currentState = newState;
            EnemyDebug.Log($"[TrainingConstructBrain] Switching state to {currentState}", this);
            EnterState(newState);
        }

        private void EnterState(ConstructState newState)
        {
            switch (newState)
            {
                case ConstructState.Idle:
                    idleTimer = 0f;
                    navMeshAgent.speed = IdleSettings.moveSpeed > 0f ? IdleSettings.moveSpeed : baseSpeed * 0.5f;
                    navMeshAgent.acceleration = ChaseSettings.acceleration > 0f ? ChaseSettings.acceleration : baseAcceleration;
                    SetAgentStopped(false);
                    break;
                case ConstructState.Chase:
                    navMeshAgent.speed = ChaseSettings.speed > 0f ? ChaseSettings.speed : baseSpeed;
                    navMeshAgent.acceleration = ChaseSettings.acceleration > 0f ? ChaseSettings.acceleration : baseAcceleration;
                    SetAgentStopped(false);
                    break;
                case ConstructState.ImpulseAttack:
                    stateRoutine = StartCoroutine(ImpulseAttackRoutine());
                    break;
                case ConstructState.ChargeAttack:
                    stateRoutine = StartCoroutine(ChargeAttackRoutine());
                    break;
                case ConstructState.Reboot:
                    stateRoutine = StartCoroutine(RebootRoutine());
                    break;
            }
        }

        private void ExitState(ConstructState state)
        {
            switch (state)
            {
                case ConstructState.Chase:
                    navMeshAgent.speed = baseSpeed;
                    navMeshAgent.acceleration = baseAcceleration;
                    break;
                case ConstructState.ImpulseAttack:
                case ConstructState.ChargeAttack:
                    currentAttackRule = null;
                    SetAgentStopped(false);
                    break;
            }
        }

        private IEnumerator ImpulseAttackRoutine()
        {
            var rule = currentAttackRule;
            SetAgentStopped(true);

            float chargeUp = rule != null ? Mathf.Max(0f, rule.ChargeUpDuration) : 0f;
            GameObject chargingIndicator = null;

            var impulseBehaviour = rule?.Attack?.Behaviour as ImpulseAoEAttackBehaviour;
            if (impulseBehaviour != null && impulseBehaviour.ChargingVfx != null)
            {
                chargingIndicator = Instantiate(
                    impulseBehaviour.ChargingVfx,
                    transform.position,
                    Quaternion.identity,
                    transform);

                float indicatorLifetime = chargeUp + 0.2f;
                if (indicatorLifetime > 0f)
                    Destroy(chargingIndicator, indicatorLifetime);
            }

            float elapsed = 0f;
            while (elapsed < chargeUp)
            {
                if (chargingIndicator != null && chargingIndicator.transform.parent == null)
                    chargingIndicator.transform.position = transform.position;

                float delta = Time.deltaTime;
                elapsed += delta;
                yield return null;
            }

            FaceTarget();

            if (rule != null && rule.Attack != null && attackController != null && playerTarget != null)
            {
                attackController.TryUseAttack(rule.Attack, playerTarget, rule.CooldownModifier);
            }

            float recovery = rule != null ? Mathf.Max(0f, rule.RecoveryDuration) : 0f;
            if (recovery > 0f)
                yield return new WaitForSeconds(recovery);

            SetAgentStopped(false);
            SwitchState(ConstructState.Chase);
        }
        private IEnumerator ChargeAttackRoutine()
        {
            var rule = currentAttackRule;
            SetAgentStopped(true);
            navMeshAgent.velocity = Vector3.zero;

            float chargeUp = rule != null ? Mathf.Max(0f, rule.ChargeUpDuration) : 0f;
            if (chargeUp > 0f)
                yield return new WaitForSeconds(chargeUp);

            Vector3 direction = transform.forward;
            if (playerTarget != null)
            {
                Vector3 dir = (playerTarget.position - transform.position);
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.0001f)
                    direction = dir.normalized;
            }
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

            bool performedAttack = false;
            bool agentInitiallyEnabled = navMeshAgent.enabled;

            float dashSpeed = ChargeSettings.dashSpeed > 0f ? ChargeSettings.dashSpeed : 12f;
            float dashDuration = ChargeSettings.dashDuration > 0f ? ChargeSettings.dashDuration : 0.75f;

            var chargeBehaviour = rule?.Attack?.Behaviour as ChargeAttackBehaviour;
            GameObject chargeVfxInstance = null;
            bool destroyVfxManually = false;
            if (chargeBehaviour != null)
            {
                var vfxPrefab = chargeBehaviour.ChargeVfxPrefab;
                if (vfxPrefab != null)
                {
                    Quaternion vfxRotation = Quaternion.LookRotation(direction, Vector3.up);
                    chargeVfxInstance = Instantiate(vfxPrefab, transform.position, vfxRotation, transform);
                    float offset = Mathf.Max(0f, chargeBehaviour.ChargeVfxOffset);
                    chargeVfxInstance.transform.localPosition = Vector3.forward * offset;
                    if (dashDuration > 0f)
                    {
                        var particleSystems = chargeVfxInstance.GetComponentsInChildren<ParticleSystem>(true);
                        for (int i = 0; i < particleSystems.Length; i++)
                        {
                            var ps = particleSystems[i];
                            if (ps == null)
                                continue;

                            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                            var main = ps.main;
                            main.duration = dashDuration - 0.15f;
                            ps.Play();
                        }
                    }
                    if (dashDuration > 0f)
                    {
                        Destroy(chargeVfxInstance, dashDuration + 0.1f);
                    }
                    else
                    {
                        destroyVfxManually = true;
                    }
                }
            }

            System.Action<Collider> onChargeHit = null;
            if (chargeHitbox != null)
            {
                onChargeHit = collider =>
                {
                    if (performedAttack)
                        return;

                    if (TryDealChargeDamage(rule, collider != null ? collider.transform : null))
                    {
                        performedAttack = true;
                        chargeHitbox.Deactivate();
                    }
                };
                chargeHitbox.Hit += onChargeHit;
                chargeHitbox.Activate();
            }
            else
            {
                EnemyDebug.LogWarning("[TrainingConstructBrain] Charge hitbox is not assigned; charge will rely on physics overlap.", this);
            }

            if (agentInitiallyEnabled)
                navMeshAgent.enabled = false;

            float elapsed = 0f;
            while (elapsed < dashDuration && !performedAttack)
            {
                float step = dashSpeed * Time.deltaTime;
                Vector3 nextPosition = transform.position + direction * step;
                transform.position = nextPosition;
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (agentInitiallyEnabled)
                navMeshAgent.enabled = true;

            if (chargeHitbox != null)
            {
                chargeHitbox.Hit -= onChargeHit;
                chargeHitbox.Deactivate();
            }

            if (chargeVfxInstance != null && destroyVfxManually)
                Destroy(chargeVfxInstance);

            EnsureAgentOnNavMesh("Charge end");
            SetAgentStopped(true);
            navMeshAgent.velocity = Vector3.zero;

            float recovery = rule != null ? Mathf.Max(0f, rule.RecoveryDuration) : 0f;


            if (RebootSettings.enabled)
            {
                SwitchState(ConstructState.Reboot);
            }
            else
            {
                if (recovery > 0f)
                    yield return new WaitForSeconds(recovery);

                SwitchState(ConstructState.Chase);
            }
        }

        private IEnumerator RebootRoutine()
        {
            float duration = RebootSettings.duration > 0f ? RebootSettings.duration : 1f;
            SetAgentStopped(true);
            navMeshAgent.velocity = Vector3.zero;

            yield return new WaitForSeconds(duration);

            SetAgentStopped(false);

            SwitchState(ConstructState.Idle);
        }

        private void FaceTarget()
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

        private void FaceDirection(Vector3 direction)
        {
            if (direction.sqrMagnitude < 0.001f)
                return;

            Quaternion lookRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = lookRotation;
        }

        private bool IsAttackReady(AttackDefinition attackDefinition)
        {
            if (attackDefinition == null || attackController == null)
                return false;

            float currentTime = Time.time;
            return attackController.RuntimeState.IsReady(attackDefinition, currentTime);
        }

        public void SetBehaviourConfig(BehaviourConfig config)
        {
            behaviourConfig = config;
        }

        public void SetPlayerTarget(Transform target)
        {
            playerTarget = target;
        }

        public void ResetBehaviour()
        {
            damageAggroTimer = 0f;
            SwitchState(ConstructState.Idle);
        }

        private bool TryDealChargeDamage(AttackRule rule, Transform targetHint)
        {
            if (rule == null || rule.Attack == null || attackController == null)
                return false;

            if (targetHint != null && attackController.TryUseAttack(rule.Attack, targetHint, rule.CooldownModifier))
            {
                if (targetHint.GetComponent<Player.Statistics.StatsController>() != null)
                {
                    float hitDistance = Vector3.Distance(transform.position, targetHint.position);
                    EnemyDebug.Log($"[TrainingConstructBrain] Charge hit player at distance {hitDistance:F2}", this);
                }
                else
                {
                    EnemyDebug.Log($"[TrainingConstructBrain] Charge hit {targetHint.name}", this);
                }
                return true;
            }

            return false;
        }

        private bool EnsureAgentOnNavMesh(string context)
        {
            if (navMeshAgent == null || !navMeshAgent.enabled)
                return false;

            if (navMeshAgent.isOnNavMesh)
                return true;

            if (NavMesh.SamplePosition(transform.position, out var hit, 1.5f, navMeshAgent.areaMask))
            {
                navMeshAgent.Warp(hit.position);
                EnemyDebug.Log($"[TrainingConstructBrain] {context}: warped agent onto NavMesh.", this);
                return true;
            }

            EnemyDebug.LogWarning($"[TrainingConstructBrain] {context}: failed to find NavMesh.", this);
            return false;
        }

        private void SetDestinationSafe(Vector3 destination, string debugContext)
        {
            if (!EnsureAgentOnNavMesh(debugContext))
                return;

            if (!navMeshAgent.SetDestination(destination))
            {
                EnemyDebug.LogWarning($"[TrainingConstructBrain] {debugContext}: SetDestination failed.", this);
            }
        }

        private void SetAgentStopped(bool stop)
        {
            if (navMeshAgent == null || !navMeshAgent.enabled)
                return;

            if (!navMeshAgent.isOnNavMesh)
            {
                EnemyDebug.LogWarning($"[TrainingConstructBrain] Tried to set isStopped={stop} while agent off NavMesh.", this);
                return;
            }

            navMeshAgent.isStopped = stop;
        }

        private void OnDrawGizmos()
        {
            if (behaviourConfig == null)
                return;

            var attacks = behaviourConfig.Attacks;
            if (attacks == null)
                return;

            foreach (var rule in attacks)
            {
                var attackDefinition = rule?.Attack;
                if (attackDefinition?.Behaviour is ImpulseAoEAttackBehaviour impulse)
                {
                    impulse.DrawGizmos(transform.position);
                }
            }
        }

        private void OnConstructDamaged(StatsController controller, float damage, Transform source)
        {
            if (!aggroOnDamage || controller != statsController)
                return;

            if (damage <= 0f)
                return;

            damageAggroTimer = Mathf.Max(damageAggroTimer, Mathf.Max(0.01f, damageAggroDuration));

            if (source != null && playerTarget == null)
                playerTarget = source;

            if (currentState == ConstructState.Idle || currentState == ConstructState.Detect)
                SwitchState(ConstructState.Detect);
        }
    }
}




