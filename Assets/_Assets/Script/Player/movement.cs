using UnityEngine;

public class movement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;

    [Header("Referensi Visual (Tarik object Child ke sini)")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;

    // Referensi class Input System
    private InputSystem_Actions controls; 
    private Vector2 moveInput;

    // Referensi komponen fisik (Parent)
    private Rigidbody2D rb;

    private void Awake()
    {
        controls = new InputSystem_Actions();

        // Rigidbody tetap diambil dari objek Parent ini
        rb = GetComponent<Rigidbody2D>();

        controls.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled += ctx => moveInput = Vector2.zero;
    }

    private void OnEnable()
    {
        controls.Enable();
    }

    private void OnDisable()
    {
        controls.Disable();
    }

    private void Update()
    {
        // Untuk mencegah error jika referensi visual belum dimasukkan di Inspector
        if (spriteRenderer != null && animator != null)
        {
            HandleAnimationAndFlip();
        }
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = moveInput * moveSpeed;
    }

    private void HandleAnimationAndFlip()
    {
        bool isMoving = moveInput.sqrMagnitude > 0;
        animator.SetBool("isWalking", isMoving);

        if (moveInput.x > 0)
        {
            spriteRenderer.flipX = false; 
        }
        else if (moveInput.x < 0)
        {
            spriteRenderer.flipX = true; 
        }
    }
}
