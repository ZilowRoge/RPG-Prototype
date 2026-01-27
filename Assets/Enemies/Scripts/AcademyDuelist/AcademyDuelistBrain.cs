using System.Collections;
using Enemies.Abstraction;
using Enemies.Combat;
using Enemies.Config;
using Enemies.Controllers;
using PlayerStats = Player.Statistics.StatsController;
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

        [Header("Configuration")]
        [SerializeField] private AttackRule heavyAttackRule;
        [SerializeField] private AttackRule quickBreakRule;

        [Header("References")]
        [SerializeField] private PlayerStats playerStats;

        [Header("Strafe")]
        [SerializeField] private float strafeAngularSpeed = 60f;
        [SerializeField] private Vector2 strafeMoveDurationRange = new Vector2(2f, 4f);
        [SerializeField] private Vector2 strafePauseDurationRange = new Vector2(0.5f, 1.25f);
        [SerializeField, Range(0f, 1f)] private float strafeDistanceBias = 0.5f;
        [SerializeField] private bool startStrafeRight = true;

        [Header("Quick Break")]
        [SerializeField] private Vector2 quickBreakDelayRange = new Vector2(0.15f, 0.4f);

        private DuelistState currentState = DuelistState.Idle;
        private Coroutine stateRoutine;
        private bool strafePaused;
        private float strafeTimer;
        private float strafeDirection;
        private Vector3 strafeHeading;
        private bool lastShieldActive;
        private float quickBreakDelayTimer;
        private AttackRule currentAttackRule;

        protected override void Awake()
        {
            base.Awake();

            if (playerTarget == null)
            {
                var playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                    playerTarget = playerObject.transform;
            }

            if (playerTarget != null && playerStats == null)
                playerStats = playerTarget.GetComponent<PlayerStats>();

            strafeDirection = startStrafeRight ? 1f : -1f;

            EnsureAgentOnNavMesh("Awake");
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
            if (playerTarget == null)
            {
                SetAgentStopped(true);
                return;
            }

            if (!IsPlayerWithinRange(DetectionRange))
            {
                SetAgentStopped(true);
                return;
            }

            UpdateStrafe();

            float distance = Vector3.Distance(transform.position, playerTarget.position);

            if (TryStartQuickBreak(distance))
                return;

            TryStartHeavyAttack(distance);
        }

        private void UpdateStrafe()
        {
            float moveSpeed = IdleSettings.moveSpeed > 0f ? IdleSettings.moveSpeed : navMeshAgent.speed;
            navMeshAgent.speed = moveSpeed;

            strafeTimer -= Time.deltaTime;
            if (strafeTimer <= 0f)
            {
                if (strafePaused)
                    BeginStrafeMove();
                else
                    BeginStrafePause();
            }

            if (strafePaused)
            {
                SetAgentStopped(true);
                return;
            }

            SetAgentStopped(false);

            if (playerTarget == null)
                return;

            Vector3 fromPlayer = transform.position - playerTarget.position;
            fromPlayer.y = 0f;

            if (strafeHeading.sqrMagnitude < 0.001f)
                strafeHeading = fromPlayer.sqrMagnitude > 0.001f ? fromPlayer.normalized : transform.forward;

            float angular = strafeAngularSpeed != 0f ? strafeAngularSpeed : 60f;
            Quaternion rotation = Quaternion.AngleAxis(angular * strafeDirection * Time.deltaTime, Vector3.up);
            strafeHeading = rotation * strafeHeading.normalized;

            float targetDistance = GetPreferredDistance();
            Vector3 desiredPosition = playerTarget.position + strafeHeading.normalized * targetDistance;

            SetDestinationSafe(desiredPosition, "Strafe");
        }

        private void BeginStrafeMove()
        {
            strafePaused = false;
            strafeTimer = GetRandomRange(strafeMoveDurationRange, 2f);
            if (Random.value > 0.5f)
                strafeDirection *= -1f;
        }

        private void BeginStrafePause()
        {
            strafePaused = true;
            strafeTimer = GetRandomRange(strafePauseDurationRange, 0.5f);
        }

        private float GetPreferredDistance()
        {
            float min = ChaseSettings.preferredMinDistance > 0f ? ChaseSettings.preferredMinDistance : 4f;
            float max = ChaseSettings.preferredMaxDistance > 0f ? ChaseSettings.preferredMaxDistance : min + 2f;
            if (max < min + 0.1f)
                max = min + 0.5f;

            float t = Mathf.Clamp01(strafeDistanceBias);
            return Mathf.Lerp(min, max, t);
        }

        private void UpdateShieldStatus()
        {
            if (playerStats == null && playerTarget != null)
                playerStats = playerTarget.GetComponent<PlayerStats>();

            if (playerStats == null)
                return;

            bool shieldActive = playerStats.IsShieldActive();
            if (shieldActive && !lastShieldActive && currentState != DuelistState.ChargeHeavy)
            {
                QueueQuickBreak();
            }

            lastShieldActive = shieldActive;
        }

        private void QueueQuickBreak()
        {
            if (quickBreakRule == null || quickBreakRule.Attack == null)
                return;

            quickBreakDelayTimer = GetRandomRange(quickBreakDelayRange, 0.25f);
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

            if (quickBreakRule == null || quickBreakRule.Attack == null)
            {
                quickBreakDelayTimer = 0f;
                return false;
            }

            if (!quickBreakRule.IsDistanceSatisfied(distance))
            {
                quickBreakDelayTimer = 0f;
                return false;
            }

            if (!IsAttackReady(quickBreakRule.Attack))
            {
                quickBreakDelayTimer = 0f;
                return false;
            }

            quickBreakDelayTimer = 0f;
            currentAttackRule = quickBreakRule;
            SwitchState(DuelistState.QuickBreak);
            return true;
        }

        private void TryStartHeavyAttack(float distance)
        {
            if (heavyAttackRule == null || heavyAttackRule.Attack == null)
                return;

            if (!heavyAttackRule.IsDistanceSatisfied(distance))
                return;

            if (!IsAttackReady(heavyAttackRule.Attack))
                return;

            currentAttackRule = heavyAttackRule;
            SwitchState(DuelistState.ChargeHeavy);
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
                    strafeHeading = Vector3.zero;
                    BeginStrafeMove();
                    SetAgentStopped(false);
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
            var rule = currentAttackRule ?? heavyAttackRule;
            SetAgentStopped(true);
            navMeshAgent.velocity = Vector3.zero;

            float chargeUp = rule != null ? Mathf.Max(0f, rule.ChargeUpDuration) : 0f;
            float elapsed = 0f;
            while (elapsed < chargeUp)
            {
                FaceTarget();
                elapsed += Time.deltaTime;
                yield return null;
            }

            SwitchState(DuelistState.HeavyAttack);
        }

        private IEnumerator HeavyAttackRoutine()
        {
            var rule = currentAttackRule ?? heavyAttackRule;
            SetAgentStopped(true);
            navMeshAgent.velocity = Vector3.zero;
            FaceTarget();

            if (rule != null && rule.Attack != null && attackController != null && playerTarget != null)
                attackController.TryUseAttack(rule.Attack, playerTarget, rule.CooldownModifier);

            float recovery = rule != null ? Mathf.Max(0f, rule.RecoveryDuration) : 0f;
            if (recovery > 0f)
                yield return new WaitForSeconds(recovery);

            if (VulnerableSettings.enabled)
                SwitchState(DuelistState.Vulnerable);
            else
                SwitchState(DuelistState.Idle);
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
            var rule = currentAttackRule ?? quickBreakRule;
            SetAgentStopped(true);
            navMeshAgent.velocity = Vector3.zero;

            float chargeUp = rule != null ? Mathf.Max(0f, rule.ChargeUpDuration) : 0f;
            if (chargeUp > 0f)
                yield return new WaitForSeconds(chargeUp);

            FaceTarget();

            if (rule != null && rule.Attack != null && attackController != null && playerTarget != null)
                attackController.TryUseAttack(rule.Attack, playerTarget, rule.CooldownModifier);

            float recovery = rule != null ? Mathf.Max(0f, rule.RecoveryDuration) : 0f;
            if (recovery > 0f)
                yield return new WaitForSeconds(recovery);

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
    }
}
