
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Animator animator;
    private Vector2 movement;
    private InputAction moveAction;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();

        // Prevent player from rotating
        rb.constraints |= RigidbodyConstraints2D.FreezeRotation;

        moveAction = new InputAction("Move", InputActionType.Value);

        // Arcade joystick
        moveAction.AddBinding("<Joystick>/stick");

        // Gamepad
        moveAction.AddBinding("<Gamepad>/leftStick");
        moveAction.AddBinding("<Gamepad>/dpad");

        // Keyboard (WASD)
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
    }

    void OnEnable()
    {
        moveAction.Enable();
    }

    void OnDisable()
    {
        moveAction.Disable();
    }

    void Update()
    {
        movement = Vector2.ClampMagnitude(
            moveAction.ReadValue<Vector2>(), 1f
        );

        if (movement.sqrMagnitude < 0.01f)
            movement = Vector2.zero;

        UpdateAnimation();
    }

    void UpdateAnimation()
    {
        if (animator == null)
            return;

        bool up = false;
        bool down = false;
        bool left = false;
        bool right = false;

        if (movement != Vector2.zero)
        {
            // Prioritize the strongest movement direction
            if (Mathf.Abs(movement.x) > Mathf.Abs(movement.y))
            {
                right = movement.x > 0;
                left = movement.x < 0;
            }
            else
            {
                up = movement.y > 0;
                down = movement.y < 0;
            }
        }

        animator.SetBool("Walk Up", up);
        animator.SetBool("Walk Down", down);
        animator.SetBool("Walk Left", left);
        animator.SetBool("Walk Right", right);
    }

    void FixedUpdate()
    {
        rb.linearVelocity = movement * moveSpeed;
    }

    void OnDestroy()
    {
        moveAction?.Dispose();
    }
}
