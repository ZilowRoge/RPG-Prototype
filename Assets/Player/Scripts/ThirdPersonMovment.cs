using UnityEngine;
using UnityEngine.InputSystem;
using Player.Statistics;

namespace Player
{
    [RequireComponent(typeof(CharacterController))]
    public class ThirdPersonMovment : MonoBehaviour
    {
        public float rotationSpeed = 10f;

        private CharacterController controller;
        private Transform cameraTransform;

        private Vector2 inputMovement = Vector2.zero;
        private bool isSprintActive = false;
        private bool inputBlocked;

        [SerializeField] private StatsController statsController;

        void Start()
        {
            controller = GetComponent<CharacterController>();
            ResolveCamera();
            if (statsController == null)
            {
                statsController = GetComponent<StatsController>();
                if (statsController == null)
                    statsController = FindAnyObjectByType<StatsController>();
            }
        }

        public void OnMove(InputValue value)
        {
            inputMovement = value.Get<Vector2>();
        }

        public void OnSprint(InputValue value)
        {
            if (value.isPressed)
            {
                isSprintActive = !isSprintActive;
            }
        }

        public void SetInputBlocked(bool blocked)
        {
            if (inputBlocked == blocked)
                return;

            inputBlocked = blocked;
            if (blocked)
            {
                inputMovement = Vector2.zero;
                isSprintActive = false;
                controller ??= GetComponent<CharacterController>();
                controller?.SimpleMove(Vector3.zero);
            }
        }

        void Update()
        {
            if (inputBlocked)
            {
                controller?.SimpleMove(Vector3.zero);
                return;
            }

            if (!EnsureCamera())
                return;

            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 moveDir = camForward * inputMovement.y + camRight * inputMovement.x;
            moveDir.Normalize();

            float forwardDot = Vector3.Dot(camForward, moveDir);
            bool isMovingBackward = forwardDot < -0.5f;
            bool isMoving = moveDir.sqrMagnitude > 0.01f;

            bool wantsToRun = isMoving && !isMovingBackward && isSprintActive;
            bool hasStamina = statsController == null || statsController.CurrentStamina > 0f;
            bool isRunning = wantsToRun && hasStamina;

            if (isRunning && statsController != null)
            {
                float costPerSecond = statsController.Statistics != null
                    ? statsController.Statistics.sprintStaminaCostPerSecond
                    : 0f;

                if (costPerSecond > 0f)
                {
                    if (!statsController.TryConsumeStamina(costPerSecond * Time.deltaTime))
                    {
                        isRunning = false;
                        isSprintActive = false;
                    }
                }
            }
            else if (!hasStamina && isSprintActive)
            {
                isSprintActive = false;
            }

            float walkSpeed = statsController != null ? statsController.walkSpeed : 0f;
            float runSpeed = statsController != null ? statsController.runSpeed : walkSpeed;
            float currentSpeed = isRunning ? runSpeed : walkSpeed;

            Vector3 desiredFacing = camForward.sqrMagnitude > 0.0001f ? camForward : transform.forward;
            Quaternion targetRotation = Quaternion.LookRotation(desiredFacing, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            controller.SimpleMove(moveDir * currentSpeed);

            float moveY = 0f;
            if (isMoving)
            {
                if (isMovingBackward)
                    moveY = -1f;
                else
                    moveY = isRunning ? 2f : 1f;
            }
        }

        private bool EnsureCamera()
        {
            if (cameraTransform != null)
                return true;

            return ResolveCamera();
        }

        private bool ResolveCamera()
        {
            var cam = Camera.main;
            if (cam == null)
                return false;

            cameraTransform = cam.transform;
            return true;
        }
    }
}

