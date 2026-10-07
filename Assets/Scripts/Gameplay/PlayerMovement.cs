using UnityEngine;

/// <summary>
/// Moves the player horizontally based on PlayerInput.
/// Clamps position between configurable left/right boundaries.
/// Drives the Animator's "isMoving" and "isReaching" parameters.
///
/// The reach animation triggers automatically when collecting a fallen star
/// (via CatchZone.StarCaught) OR when the webcam pose detects the reach gesture.
/// While reaching, horizontal movement is disabled.
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    [SerializeField, Min(0f)] private float moveSpeed = 5f;
    [SerializeField] private float leftBoundary = -6f;
    [SerializeField] private float rightBoundary = 50f;
    [SerializeField, Min(0f)] private float reachDuration = 0.875f; // 7 frames at 8fps

    private PlayerInput playerInput;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private CatchZone catchZone;

    private float reachTimer;
    private bool isReaching;

    // Animator parameter hashes for performance.
    private static readonly int IsMoving = Animator.StringToHash("isMoving");
    private static readonly int IsReaching = Animator.StringToHash("isReaching");

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Find the CatchZone on a child object.
        catchZone = GetComponentInChildren<CatchZone>();
    }

    private void OnEnable()
    {
        if (catchZone != null)
            catchZone.StarCaught += OnStarCaught;
    }

    private void OnDisable()
    {
        if (catchZone != null)
            catchZone.StarCaught -= OnStarCaught;
    }

    /// <summary>
    /// Called when a star is collected — triggers the reach animation.
    /// </summary>
    private void OnStarCaught(StarMovement star)
    {
        StartReach();
    }

    /// <summary>Start the reach animation for a fixed duration.</summary>
    public void StartReach()
    {
        isReaching = true;
        reachTimer = reachDuration;
    }

    private void Update()
    {
        if (playerInput == null) return;

        // --- Reach from pose gesture (both hands open) ---
        if (playerInput.IsReaching)
            StartReach();

        // --- Reach timer countdown ---
        if (isReaching)
        {
            reachTimer -= Time.deltaTime;
            if (reachTimer <= 0f)
                isReaching = false;
        }

        // Drive reaching animation state.
        if (animator != null)
            animator.SetBool(IsReaching, isReaching);

        // While reaching, the character cannot move.
        if (isReaching)
        {
            if (animator != null)
                animator.SetBool(IsMoving, false);
            return;
        }

        float movement = playerInput.HorizontalInput;
        transform.position += Vector3.right * movement * moveSpeed * Time.deltaTime;

        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, leftBoundary, rightBoundary);
        transform.position = pos;

        // Drive animation state.
        bool moving = Mathf.Abs(movement) > 0.01f;
        if (animator != null)
            animator.SetBool(IsMoving, moving);

        // Flip sprite when moving left (run frames face right).
        if (spriteRenderer != null && moving)
            spriteRenderer.flipX = movement < 0f;
    }
}
