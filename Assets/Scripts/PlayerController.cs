using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Gerak")]
    public float walkSpeed = 4f;
    public float runSpeed = 7f;
    public float rotationSpeed = 12f;

    [Header("Lompat & Gravitasi")]
    public float jumpHeight = 1.2f;
    public float gravity = -20f;

    [Header("Kontrol")]
    // Nanti diset false saat dialog / inventaris / pause
    public bool inputEnabled = true;

    CharacterController controller;
    Transform cam;
    float verticalVelocity;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    void Start()
    {
        if (Camera.main != null) cam = Camera.main.transform;
    }

    void Update()
    {
        Vector2 input = Vector2.zero;
        bool run = false;
        bool jumpPressed = false;

        Keyboard kb = Keyboard.current;
        if (inputEnabled && kb != null)
        {
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) input.y += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) input.y -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) input.x += 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) input.x -= 1f;

            run = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
            jumpPressed = kb.spaceKey.wasPressedThisFrame;
        }

        // Arah gerak relatif terhadap kamera
        Vector3 forward = cam != null ? cam.forward : Vector3.forward;
        Vector3 right = cam != null ? cam.right : Vector3.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 moveDir = forward * input.y + right * input.x;
        if (moveDir.sqrMagnitude > 1f) moveDir.Normalize();

        // Putar badan menghadap arah gerak
        if (moveDir.sqrMagnitude > 0.01f)
        {
            Quaternion target = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, rotationSpeed * Time.deltaTime);
        }

        // Gravitasi & lompat
        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        if (jumpPressed && controller.isGrounded) verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        verticalVelocity += gravity * Time.deltaTime;

        float speed = run ? runSpeed : walkSpeed;
        Vector3 velocity = moveDir * speed + Vector3.up * verticalVelocity;
        controller.Move(velocity * Time.deltaTime);
    }
}
