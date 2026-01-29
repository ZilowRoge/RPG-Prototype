using System.Collections;
using Enemies.Abstraction;
using Enemies.Interfaces;
using Enemies.Combat;
using Enemies.Config;
using Enemies.Controllers;
using Systems.Debugging;
using UnityEngine;
using UnityEngine.AI;

namespace Enemies.TrainingConstruct
{
    /// <summary>
    /// Behaviour controller for the Training Construct enemy.
    /// Uses external configuration assets for movement and attacks.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class TrainingConstructBrain : EnemyBrainBase
    {
        private enum ConstructState
        {
            Idle,
            Detect,
            Chase,
            ImpulseAttack,
            ChargeAttack,
            Vulnerable
        }

        [Header("References")]
        [SerializeField] private ChargeHitbox chargeHitbox;
        [SerializeField] private TrainingConstructMovement movement = new TrainingConstructMovement();
        [Header("Charge")]
        [SerializeField] private float chargeDashSpeed = 12f;
        [SerializeField] private float chargeDashDuration = 0.75f;
        [Header("Damage Awareness")]
        [SerializeField, Tooltip("When true, any incoming damage will force the construct to detect and chase the attacker.")]
        private bool aggroOnDamage = true;
        [SerializeField, Tooltip("How long (seconds) the construct remembers being damaged for aggro purposes.")]
        private float damageAggroDuration = 5f;

        [SerializeField] private ConstructState currentState = ConstructState.Idle;
        private Coroutine stateRoutine;
        private AttackRule currentAttackRule;
        private float damageAggroTimer;
        private StatsController statsController;

        private const string ImpulseAttackId = "training_construct_impulse_attack";
        private const string ChargeAttackId = "training_construct_charge_attack";
        private const string ChargeUpForAbilityId = "training_construct_charge";

        protected override IEnemyMovement Movement => movement;

        private float LeashRangeMultiplier => DetectionSettings.leashRangeMultiplier > 0f ? DetectionSettings.leashRangeMultiplier : 1.25f;
        private float LeashRange => DetectionRange * LeashRangeMultiplier;
        private bool HasDamageAggro => aggroOnDamage && damageAggroTimer > 0f;

        protected override void Awake()
        {
            base.Awake();

            if (chargeHitbox == null)
                chargeHitbox = GetComponentInChildren<ChargeHitbox>(true);
            if (chargeHitbox == null)
                logger.LogWarning(ComponentLogger.LogFlag.Events, "ChargeHitbox is not assigned.");

            statsController = GetComponent<StatsController>();
            if (statsController == null)
                logger.LogWarning(ComponentLogger.LogFlag.Events, "StatsController is not assigned.");
            damageAggroDuration = Mathf.Max(0f, damageAggroDuration);

            if (playerTarget == null)
            {
                var playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null) {
                    Debug.Log("[TrainingCOntructBrain] Player found");
                    playerTarget = playerObject.transform;
                }
            }

            movement.Initialize(new EnemyMovementContext(
                transform,
                navMeshAgent,
                logger));

            movement.EnsureAgentOnNavMesh("Awake");
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
                case ConstructState.Vulnerable:
                    FaceTarget();
                    break;
            }
        }

        private void UpdateIdle()
        {
            movement.Move(EnemyMovementState.Idle, playerTarget, Time.deltaTime);

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
            float distance = movement.Move(EnemyMovementState.Chase, playerTarget, Time.deltaTime);
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
                logger.Log(ComponentLogger.LogFlag.Events,
                    "Checking attack '{0}': distance {1:F2}, required {2}-{3}, in range = {4}",
                    attack.name,
                    distance,
                    rule.MinDistance,
                    rule.MaxDistance,
                    withinRange);

                if (!withinRange)
                    continue;

                if (!IsAttackReady(attack))
                {
                    logger.Log(ComponentLogger.LogFlag.Events,
                        "Attack '{0}' still on cooldown.",
                        attack.name);
                    continue;
                }

                BeginAttack(rule);
                break;
            }
        }

        private void BeginAttack(AttackRule rule)
        {
            currentAttackRule = rule;

            if (rule != null && rule.RuleId == ChargeAttackId)
            {
                SwitchState(ConstructState.ChargeAttack);
                return;
            }

            SwitchState(ConstructState.ImpulseAttack);
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
            logger.Log(ComponentLogger.LogFlag.StateChange, "Switching state to {0}", currentState);
            EnterState(newState);
        }

        private void EnterState(ConstructState newState)
        {
            switch (newState)
            {
                case ConstructState.Idle:
                    break;
                case ConstructState.Chase:
                    break;
                case ConstructState.ImpulseAttack:
                    stateRoutine = StartCoroutine(ImpulseAttackRoutine());
                    break;
                case ConstructState.ChargeAttack:
                    stateRoutine = StartCoroutine(ChargeAttackRoutine());
                    break;
                case ConstructState.Vulnerable:
                    stateRoutine = StartCoroutine(VulnerableRoutine());
                    break;
            }
        }

        private void ExitState(ConstructState state)
        {
            switch (state)
            {
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
                attackController.TryUseAttack(rule.Attack, playerTarget);
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

            GameObject chargeUpForAbilityInstance = null;
            TrySpawnChargeUpForAbility(ChargeUpForAbilityId, rule, ref chargeUpForAbilityInstance);

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

            float dashSpeed = chargeDashSpeed > 0f ? chargeDashSpeed : 12f;
            float dashDuration = chargeDashDuration > 0f ? chargeDashDuration : 0.75f;

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
                logger.LogWarning(ComponentLogger.LogFlag.Events,
                    "Charge hitbox is not assigned; charge will rely on physics overlap.");
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

            movement.EnsureAgentOnNavMesh("Charge end");
            SetAgentStopped(true);
            navMeshAgent.velocity = Vector3.zero;

            float recovery = rule != null ? Mathf.Max(0f, rule.RecoveryDuration) : 0f;


            if (VulnerableSettings.enabled)
            {
                SwitchState(ConstructState.Vulnerable);
            }
            else
            {
                if (recovery > 0f)
                    yield return new WaitForSeconds(recovery);

                SwitchState(ConstructState.Chase);
            }
        }

        private IEnumerator VulnerableRoutine()
        {
            float duration = VulnerableSettings.duration > 0f ? VulnerableSettings.duration : 1f;
            SetAgentStopped(true);
            navMeshAgent.velocity = Vector3.zero;

            yield return new WaitForSeconds(duration);

            SetAgentStopped(false);

            SwitchState(ConstructState.Idle);
        }

        private void FaceDirection(Vector3 direction)
        {
            if (direction.sqrMagnitude < 0.001f)
                return;

            Quaternion lookRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = lookRotation;
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

            if (targetHint != null && attackController.TryUseAttack(rule.Attack, targetHint))
            {
                if (targetHint.GetComponent<Player.Statistics.StatsController>() != null)
                {
                    float hitDistance = Vector3.Distance(transform.position, targetHint.position);
                    logger.Log(ComponentLogger.LogFlag.Events,
                        "Charge hit player at distance {0:F2}",
                        hitDistance);
                }
                else
                {
                    logger.Log(ComponentLogger.LogFlag.Events,
                        "Charge hit {0}",
                        targetHint.name);
                }
                return true;
            }

            return false;
        }

        private void TrySpawnChargeUpForAbility(string vfxId, AttackRule rule, ref GameObject instance)
        {
            if (string.IsNullOrWhiteSpace(vfxId))
                return;

            if (behaviourConfig == null || behaviourConfig.ChargeUpForAbilityEntries == null)
                return;

            var entries = behaviourConfig.ChargeUpForAbilityEntries;
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry.prefab == null || entry.vfxId != vfxId)
                    continue;

                if (entry.chargingTime <= 0f)
                    return;

                Vector3 worldOffset = transform.TransformDirection(entry.offset);
                Vector3 spawnPosition = transform.position + worldOffset;
                if (entry.attachToOwner)
                {
                    instance = Instantiate(entry.prefab, transform.position, transform.rotation, transform);
                    instance.transform.localPosition = entry.offset;
                }
                else
                {
                    instance = Instantiate(entry.prefab, spawnPosition, transform.rotation);
                }

                Destroy(instance, entry.chargingTime);

                return;
            }
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
