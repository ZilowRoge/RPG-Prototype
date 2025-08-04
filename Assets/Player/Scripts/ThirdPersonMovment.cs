using UnityEngine;
using UnityEngine.InputSystem;
namespace Player {
[RequireComponent(typeof(CharacterController))]
public class ThirdPersonMovment : MonoBehaviour
{
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float rotationSpeed = 10f;

    private CharacterController controller;
    private Transform cameraTransform;

    private Vector2 inputMovement = Vector2.zero;
    private bool isRunPressed = false;

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
        Debug.Log("OnRun");
        isRunPressed = value.isPressed;
    }

    void Update()
    {
        // Kierunki kamery
        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        // Kierunek ruchu względem kamery
        Vector3 moveDir = camForward * inputMovement.y + camRight * inputMovement.x;
        moveDir.Normalize();

        float forwardDot = Vector3.Dot(camForward, moveDir);
        bool isMovingBackward = forwardDot < -0.5f;
        bool isMoving = moveDir.sqrMagnitude > 0.01f;

        // Bieganie: tylko do przodu + Shift
        bool isRunning = isMoving && !isMovingBackward && isRunPressed;
        float currentSpeed = isRunning ? runSpeed : walkSpeed;

        // Obrót
        if (isMoving)
        {
            Vector3 facingDir = isMovingBackward ? -moveDir : moveDir;
            Quaternion targetRotation = Quaternion.LookRotation(facingDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // Ruch
        controller.SimpleMove(moveDir * currentSpeed);

        // Blend animacji (MoveY: -1 = back, 0 = idle, 1 = walk, 2 = run)
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