using System.Collections;
using Enemies.Abstraction;
using Enemies.Interfaces;
using Enemies.Combat;
using Enemies.Config;
using Enemies.Controllers;
using Player.Interfaces;
using Systems.Debugging;
using UnityEngine;
using UnityEngine.AI;

namespace Enemies.AcademyDuelist
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class AcademyDuelistBrain : EnemyBrainBase
    {
        private enum DuelistState
        {
            Idle,
            ChargeHeavy,
            HeavyAttack,
            Vulnerable,
            QuickBreak
        }

        [Header("References")]
        [SerializeField] private MonoBehaviour playerShieldSource;
        private IShieldState playerShieldState;

        [Header("Movement")]
        [SerializeField] private AcademyDuelistMovement movement = new AcademyDuelistMovement();

        [Header("Quick Break")]
        [SerializeField] private Vector2 quickBreakDelayRange = new Vector2(0.15f, 0.4f);

        private DuelistState currentState = DuelistState.Idle;
        private Coroutine stateRoutine;
        private float quickBreakDelayTimer;
        private AttackRule currentAttackRule;
        private const string HeavyAttackId = "academy_duelist_heavy_attack";
        private const string QuickBreakId = "academy_duelist_magic_missile";
        private const string ChargeUpForAbilityId = "heavy_attack_charge_up";

        protected override IEnemyMovement Movement => movement;

        protected override void Awake()
        {
            base.Awake();

            if (playerTarget == null)
            {
                var playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                    playerTarget = playerObject.transform;
            }

            ResolvePlayerShieldState();

            movement.Initialize(new EnemyMovementContext(
                transform,
                navMeshAgent,
                logger));

            movement.EnsureAgentOnNavMesh("Awake");
        }

        private void OnEnable()
        {
            SwitchState(DuelistState.Idle);
        }

        private void Update()
        {
            UpdateShieldStatus();
            switch (currentState)
            {
                case DuelistState.Idle:
                    UpdateIdle();
                    break;
                case DuelistState.ChargeHeavy:
                case DuelistState.HeavyAttack:
                case DuelistState.Vulnerable:
                case DuelistState.QuickBreak:
                    FaceTarget();
                    break;
            }
        }

        private void UpdateIdle()
        {
            logger.Log(ComponentLogger.LogFlag.Events, "[AcademyDuelistBrain] UpdateIdle.");
            if (playerTarget == null)
            {
                movement.Move(EnemyMovementState.None, null, Time.deltaTime);
                return;
            }

            if (!IsPlayerWithinRange(DetectionRange))
            {
                movement.Move(EnemyMovementState.None, playerTarget, Time.deltaTime);
                return;
            }

            float distance = movement.Move(EnemyMovementState.Idle, playerTarget, Time.deltaTime);

            if (TryStartQuickBreak(distance))
                return;

            TryStartHeavyAttack(distance);
        }

        private void UpdateShieldStatus()
        {
            ResolvePlayerShieldState();
            if (playerShieldState == null)
                return;

            bool shieldActive = playerShieldState.IsShieldActive();
            if (!shieldActive || currentState != DuelistState.Idle || quickBreakDelayTimer > 0f)
                return;

            logger.Log(ComponentLogger.LogFlag.Events,
                "[AcademyDuelistBrain] Shield active. Queueing QuickBreak (state={0}).",
                currentState);
            QueueQuickBreak();
        }

        private void QueueQuickBreak()
        {
            if (FindRuleById(QuickBreakId) == null)
            {
                logger.Log(ComponentLogger.LogFlag.Events,
                    "[AcademyDuelistBrain] QuickBreak queue skipped (rule missing).");
                return;
            }

            quickBreakDelayTimer = GetRandomRange(quickBreakDelayRange, 0.25f);
            logger.Log(ComponentLogger.LogFlag.Events,
                "[AcademyDuelistBrain] QuickBreak queued delay={0:F2} state={1}.",
                quickBreakDelayTimer,
                currentState);
        }

        private bool TryStartQuickBreak(float distance)
        {
            if (quickBreakDelayTimer <= 0f)
                return false;

            quickBreakDelayTimer -= Time.deltaTime;
            if (quickBreakDelayTimer > 0f)
                return false;

            if (currentState != DuelistState.Idle)
                return false;

            if (!CanPerformQuickBreak(distance))
            {
                quickBreakDelayTimer = 0f;
                logger.Log(ComponentLogger.LogFlag.Events,
                    "[AcademyDuelistBrain] QuickBreak ready but cannot perform (distance={0:F2}).",
                    distance);
                return false;
            }

            quickBreakDelayTimer = 0f;
            currentAttackRule = FindRuleById(QuickBreakId);
            logger.Log(ComponentLogger.LogFlag.Events,
                "[AcademyDuelistBrain] QuickBreak triggered (distance={0:F2}).",
                distance);
            SwitchState(DuelistState.QuickBreak);
            return true;
        }

        private void TryStartHeavyAttack(float distance)
        {
            if (!CanPerformHeavyAttack(distance))
                return;

            currentAttackRule = FindRuleById(HeavyAttackId);
            SwitchState(DuelistState.ChargeHeavy);
        }

        private bool CanPerformHeavyAttack(float distance)
        {
            var rule = FindRuleById(HeavyAttackId);
            return rule != null &&
                rule.Attack != null &&
                rule.IsDistanceSatisfied(distance) &&
                IsAttackReady(rule.Attack);
        }

        private bool CanPerformQuickBreak(float distance)
        {
            var rule = FindRuleById(QuickBreakId);
            return rule != null &&
                rule.Attack != null &&
                rule.IsDistanceSatisfied(distance) &&
                IsAttackReady(rule.Attack);
        }
 
        private AttackRule FindRuleById(string ruleId)
        {
            if (behaviourConfig == null || behaviourConfig.Attacks == null)
                return null;

            foreach (var rule in behaviourConfig.Attacks)
            {
                if (rule == null)
                    continue;
                if (string.IsNullOrWhiteSpace(rule.RuleId))
                    continue;
                if (rule.RuleId == ruleId)
                    return rule;
            }

            return null;
        }

        private void SwitchState(DuelistState newState)
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

        private void EnterState(DuelistState newState)
        {
            switch (newState)
            {
                case DuelistState.Idle:
                    break;
                case DuelistState.ChargeHeavy:
                    stateRoutine = StartCoroutine(ChargeHeavyRoutine());
                    break;
                case DuelistState.HeavyAttack:
                    stateRoutine = StartCoroutine(HeavyAttackRoutine());
                    break;
                case DuelistState.Vulnerable:
                    stateRoutine = StartCoroutine(VulnerableRoutine());
                    break;
                case DuelistState.QuickBreak:
                    stateRoutine = StartCoroutine(QuickBreakRoutine());
                    break;
            }
        }

        private void ExitState(DuelistState state)
        {
            switch (state)
            {
                case DuelistState.ChargeHeavy:
                case DuelistState.HeavyAttack:
                case DuelistState.QuickBreak:
                    currentAttackRule = null;
                    break;
            }
        }

        private IEnumerator ChargeHeavyRoutine()
        {
            logger.Log(ComponentLogger.LogFlag.Events, "[AcademyDuelistBrain] ChargeHeavyRoutine start.");
            SetAgentStopped(true);
            navMeshAgent.velocity = Vector3.zero;

            GameObject chargeUpForAbilityInstance = null;
            float chargeUp = 0f;
            if (TryGetChargeUpForAbilityEntry(ChargeUpForAbilityId, out var entry))
            {
                logger.Log(ComponentLogger.LogFlag.Events,
                    "[AcademyDuelistBrain] ChargeUp entry found id={0} chargingTime={1:F2}.",
                    ChargeUpForAbilityId,
                    entry.chargingTime);
                chargeUp = Mathf.Max(0f, entry.chargingTime);
                TrySpawnChargeUpForAbility(entry, ref chargeUpForAbilityInstance);
            }
            else
            {
                logger.Log(ComponentLogger.LogFlag.Events,
                    "[AcademyDuelistBrain] ChargeUp entry missing for id={0}.",
                    ChargeUpForAbilityId);
            }

            float elapsed = 0f;
            while (elapsed < chargeUp)
            {
                FaceTarget();
                elapsed += Time.deltaTime;
                yield return null;
            }

            logger.Log(ComponentLogger.LogFlag.Events, "[AcademyDuelistBrain] ChargeHeavyRoutine end (chargeUp={0:F2}).", chargeUp);
            SwitchState(DuelistState.HeavyAttack);
        }

        private IEnumerator HeavyAttackRoutine()
        {
            var rule = currentAttackRule ?? FindRuleById(HeavyAttackId);
            SetAgentStopped(true);
            navMeshAgent.velocity = Vector3.zero;
            FaceTarget();

            if (rule != null && rule.Attack != null && attackController != null && playerTarget != null)
                attackController.TryUseAttack(rule.Attack, playerTarget);

            float recovery = rule != null ? Mathf.Max(0f, rule.RecoveryDuration) : 0f;
            if (recovery > 0f)
                yield return new WaitForSeconds(recovery);

            SwitchState(DuelistState.Vulnerable);
        }

        private IEnumerator VulnerableRoutine()
        {
            float duration = VulnerableSettings.duration > 0f ? VulnerableSettings.duration : 2f;
            SetAgentStopped(true);
            navMeshAgent.velocity = Vector3.zero;

            yield return new WaitForSeconds(duration);

            SetAgentStopped(false);
            SwitchState(DuelistState.Idle);
        }

        private IEnumerator QuickBreakRoutine()
        {
            var rule = currentAttackRule ?? FindRuleById(QuickBreakId);
            SetAgentStopped(true);
            navMeshAgent.velocity = Vector3.zero;

            float chargeUp = rule != null ? Mathf.Max(0f, rule.ChargeUpDuration) : 0f;
            logger.Log(ComponentLogger.LogFlag.Events,
                "[AcademyDuelistBrain] QuickBreakRoutine start (chargeUp={0:F2}).",
                chargeUp);
            if (chargeUp > 0f)
                yield return new WaitForSeconds(chargeUp);

            FaceTarget();

            if (rule != null && rule.Attack != null && attackController != null && playerTarget != null)
            {
                logger.Log(ComponentLogger.LogFlag.Events,
                    "[AcademyDuelistBrain] QuickBreak cast (ruleId={0}).",
                    rule.RuleId);
                attackController.TryUseAttack(rule.Attack, playerTarget);
            }

            float recovery = rule != null ? Mathf.Max(0f, rule.RecoveryDuration) : 0f;
            if (recovery > 0f)
                yield return new WaitForSeconds(recovery);

            logger.Log(ComponentLogger.LogFlag.Events,
                "[AcademyDuelistBrain] QuickBreakRoutine end (recovery={0:F2}).",
                recovery);
            SwitchState(DuelistState.Idle);
        }

        private float GetRandomRange(Vector2 range, float fallback)
        {
            float min = Mathf.Min(range.x, range.y);
            float max = Mathf.Max(range.x, range.y);
            if (max <= 0f)
                return fallback;

            min = Mathf.Max(0f, min);
            if (min > max)
                min = max;

            return Random.Range(min, max);
        }

        private bool TryGetChargeUpForAbilityEntry(string vfxId, out BehaviourConfig.ChargeUpForAbilityEntry entry)
        {
            logger.Log(ComponentLogger.LogFlag.Events,
                "[AcademyDuelistBrain] TryGetChargeUpForAbilityEntry id={0}.",
                vfxId);
            entry = default;
            if (string.IsNullOrWhiteSpace(vfxId))
                return false;

            if (behaviourConfig == null || behaviourConfig.ChargeUpForAbilityEntries == null)
                return false;

            var entries = behaviourConfig.ChargeUpForAbilityEntries;
            for (int i = 0; i < entries.Count; i++)
            {
                var candidate = entries[i];
                if (candidate.vfxId != vfxId)
                    continue;

                entry = candidate;
                logger.Log(ComponentLogger.LogFlag.Events,
                    "[AcademyDuelistBrain] ChargeUp entry matched id={0}.",
                    vfxId);
                return true;
            }

            logger.Log(ComponentLogger.LogFlag.Events,
                "[AcademyDuelistBrain] ChargeUp entry not found id={0}.",
                vfxId);
            return false;
        }

        private void TrySpawnChargeUpForAbility(BehaviourConfig.ChargeUpForAbilityEntry entry, ref GameObject instance)
        {
            if (entry.prefab == null || entry.chargingTime <= 0f)
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
        }

        private void ResolvePlayerShieldState()
        {
            if (playerShieldState != null)
                return;

            if (playerShieldSource != null)
                playerShieldState = playerShieldSource as IShieldState;

            if (playerShieldState == null && playerTarget != null)
                playerShieldState = playerTarget.GetComponent<IShieldState>();
        }
    }
}
