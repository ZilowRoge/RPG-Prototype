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
        private bool isRunPressed = false;

        [SerializeField] private Statistics.Statistics stats;

        void Start()
        {
            controller = GetComponent<CharacterController>();
            cameraTransform = Camera.main.transform;
        }

        public void OnMove(InputValue value)
        {
            inputMovement = value.Get<Vector2>();
        }

        public void OnRun(InputValue value)
        {
            isRunPressed = value.isPressed;
        }

        void Update()
        {
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

            bool isRunning = isMoving && !isMovingBackward && isRunPressed;
            float currentSpeed = isRunning ? stats.RunSpeed : stats.WalkSpeed;

            if (isMoving)
            {
                Vector3 facingDir = isMovingBackward ? -moveDir : moveDir;
                Quaternion targetRotation = Quaternion.LookRotation(facingDir, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

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
    }
}
