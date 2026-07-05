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

    [Header("Status")]
    public bool canMove = true; // Saklar pergerakan
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
        // BARIS BARU: Jika tidak boleh bergerak, hentikan semua logika visual & input di bawahnya
        if (!canMove) return; 

        // Untuk mencegah error jika referensi visual belum dimasukkan di Inspector
        if (spriteRenderer != null && animator != null)
        {
            HandleAnimationAndFlip();
        }
    }
    private void FixedUpdate()
    {
        // Jika canMove false, hentikan pergerakan seketika dan jangan baca input
        if (!canMove)
        {
            rb.linearVelocity = Vector2.zero;
            animator.SetBool("isWalking", false); // Matikan animasi jalan
            return;
        }

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
    // Fungsi publik ini akan dipanggil oleh script Cutscene atau Fungus
    public void SetMovementEnabled(bool isEnabled)
    {
        canMove = isEnabled;
    }
}
