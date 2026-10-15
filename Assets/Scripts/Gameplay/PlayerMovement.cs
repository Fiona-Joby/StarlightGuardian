using UnityEngine;

/// <summary>
/// Moves the player horizontally based on PlayerInput.
/// Clamps position between configurable left/right boundaries.
/// Drives the Animator's "isMoving" and "isReaching" parameters.
///
/// The reach animation triggers when collecting a star (via CatchZone.StarCaught)
/// or when the webcam pose detects the reach gesture.
/// Reaching stops ground movement; airborne momentum continues until landing.
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    [SerializeField, Min(0f)] private float moveSpeed = 5f;
    [SerializeField] private float leftBoundary = -6f;
    [SerializeField] private float rightBoundary = 50f;
    [SerializeField, Min(0f)] private float reachDuration = 0.875f; // 7 frames at 8fps

    [Header("V Gesture Jump")]
    [Tooltip("起跳速度，越大跳得越高。單位：世界單位／秒。")]
    [SerializeField, Min(0.1f)] private float jumpSpeed = 7f;
    [Tooltip("收手後的上升重力，越大越快停止上升。")]
    [SerializeField, Min(0.1f)] private float jumpGravity = 24f;
    [Tooltip("維持 V 時的上升重力，越小越容易跳高。")]
    [SerializeField, Min(0.1f)] private float heldJumpGravity = 8f;
    [Tooltip("維持 V 可延長上升的最長時間（秒）。")]
    [SerializeField, Min(0f)] private float maxJumpHoldTime = 0.4f;

    [Header("Jump Landing Weight")]
    [Tooltip("下落專用重力，越大落地越快、越有重量感，不影響起跳高度。")]
    [SerializeField, Min(0.1f)] private float fallGravity = 48f;
    [Tooltip("最大下落速度。調高可避免下落速度太早被限制。")]
    [SerializeField, Min(0.1f)] private float maxFallSpeed = 20f;

    [Header("Running Jump Momentum")]
    [Tooltip("起跳時保留的跑速倍率。1 = 原跑速，1.2 = 多 20% 前衝；原地起跳不受影響。")]
    [SerializeField, Min(0f)] private float takeoffSpeedMultiplier = 1.2f;
    [Tooltip("跑動起跳時額外增加的水平速度（世界單位／秒）；原地起跳不增加。")]
    [SerializeField, Min(0f)] private float takeoffForwardBoost = 0f;
    [Tooltip("空中用方向手勢調整速度的加速度。0 = 完全保留起跳方向；越大越容易轉向。")]
    [SerializeField, Min(0f)] private float airControlAcceleration = 6f;
    [Tooltip("空中水平減速量（世界單位／秒平方）。0 = 保留慣性；越大越快減速。")]
    [SerializeField, Min(0f)] private float airDrag = 0f;

    private float lastGroundSpeed;
    private float airborneSpeed;
    private float airborneControlSpeed;

    public bool IsGrounded => !isJumping;
    private float groundY;
    private float verticalSpeed;
    private float jumpHoldTime;
    private bool isJumping;
    private bool jumpBoostEnded;

    private PlayerInput playerInput;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private CatchZone catchZone;

    private float reachTimer;
    private bool isReaching;

    // Animator parameter hashes for performance.
    private static readonly int IsMoving = Animator.StringToHash("isMoving");
    private static readonly int IsReaching = Animator.StringToHash("isReaching");
    private static readonly int IsJumping = Animator.StringToHash("isJumping");

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        // The existing kinematic player moves along a flat ground baseline.
        groundY = transform.position.y;

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

        float groundSpeed = isReaching ? 0f : playerInput.HorizontalInput * moveSpeed;
        bool airborneAtFrameStart = isJumping;
        // Preserve the final airborne step on the landing frame as well.
        UpdateJump(groundSpeed);
        bool airborneThisFrame = airborneAtFrameStart || isJumping;
        float horizontalSpeed = groundSpeed;
        if (airborneThisFrame)
        {
            // No direction input means coasting, not an instant stop.
            if (Mathf.Abs(playerInput.HorizontalInput) > 0.01f)
                airborneSpeed = Mathf.MoveTowards(airborneSpeed,
                    playerInput.HorizontalInput * airborneControlSpeed,
                    airControlAcceleration * Time.deltaTime);
            airborneSpeed = Mathf.MoveTowards(airborneSpeed, 0f, airDrag * Time.deltaTime);
            horizontalSpeed = airborneSpeed;
        }

        float previousX = transform.position.x;
        transform.position += Vector3.right * horizontalSpeed * Time.deltaTime;

        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, leftBoundary, rightBoundary);
        transform.position = pos;
        // Hitting a boundary must not keep a hidden outward velocity.
        if ((pos.x <= leftBoundary && horizontalSpeed < 0f) ||
            (pos.x >= rightBoundary && horizontalSpeed > 0f))
            airborneSpeed = 0f;
        if (!isJumping && Time.deltaTime > 0f)
        {
            lastGroundSpeed = airborneThisFrame ? 0f : (pos.x - previousX) / Time.deltaTime;
            airborneSpeed = 0f;
        }

        // Drive animation state.
        bool moving = Mathf.Abs(horizontalSpeed) > 0.01f;
        if (animator != null)
        {
            animator.SetBool(IsMoving, moving && !isJumping && !isReaching);
            animator.SetBool(IsJumping, isJumping);
        }

        // Flip sprite when moving left (run frames face right).
        if (spriteRenderer != null && moving)
            spriteRenderer.flipX = horizontalSpeed < 0f;
    }

    private void UpdateJump(float groundSpeed)
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        if (!isJumping && playerInput.JumpPressed)
        {
            // A gesture can switch from index to V on the takeoff frame.
            // Retain the actual previous frame's run speed in that case only.
            float launchSpeed = isReaching ? 0f :
                (Mathf.Abs(groundSpeed) > 0.01f ? groundSpeed : lastGroundSpeed);
            if ((transform.position.x <= leftBoundary && launchSpeed < 0f) ||
                (transform.position.x >= rightBoundary && launchSpeed > 0f))
                launchSpeed = 0f;
            airborneSpeed = Mathf.Abs(launchSpeed) > 0.01f
                ? launchSpeed * takeoffSpeedMultiplier + Mathf.Sign(launchSpeed) * takeoffForwardBoost
                : 0f;
            airborneControlSpeed = Mathf.Max(moveSpeed, Mathf.Abs(airborneSpeed));
            groundY = transform.position.y;
            isJumping = true;
            verticalSpeed = jumpSpeed;
            jumpHoldTime = 0f;
            jumpBoostEnded = false;
        }
        if (!isJumping) return;

        if (!playerInput.JumpHeld || verticalSpeed <= 0f || jumpHoldTime >= maxJumpHoldTime)
            jumpBoostEnded = true;

        // After release, a second V in midair cannot restore the boost.
        float boostTime = jumpBoostEnded ? 0f : Mathf.Min(dt, maxJumpHoldTime - jumpHoldTime);
        float normalTime = dt - boostTime;
        float heldGravity = Mathf.Min(heldJumpGravity, jumpGravity);
        float displacement = AdvanceVertical(boostTime, heldGravity);
        displacement += AdvanceVertical(normalTime, jumpGravity);
        jumpHoldTime += boostTime;

        Vector3 position = transform.position;
        position.y += displacement;
        if (position.y <= groundY && verticalSpeed <= 0f)
        {
            position.y = groundY;
            verticalSpeed = 0f;
            isJumping = false;
        }
        transform.position = position;
    }

    private float AdvanceVertical(float duration, float ascentGravity)
    {
        if (duration <= 0f) return 0f;
        float displacement = 0f;
        if (verticalSpeed > 0f)
        {
            // Split at the apex: heavier falling starts immediately, even when
            // the apex occurs partway through a frame or V is still held.
            float ascentTime = Mathf.Min(duration, verticalSpeed / Mathf.Max(0.1f, ascentGravity));
            displacement = verticalSpeed * ascentTime - 0.5f * ascentGravity * ascentTime * ascentTime;
            verticalSpeed = Mathf.Max(0f, verticalSpeed - ascentGravity * ascentTime);
            duration -= ascentTime;
        }
        if (duration > 0f)
        {
            float gravity = Mathf.Max(0.1f, fallGravity);
            float speedLimit = Mathf.Max(0.1f, maxFallSpeed);
            verticalSpeed = Mathf.Max(verticalSpeed, -speedLimit);
            float accelerationTime = Mathf.Min(duration, Mathf.Max(0f, (speedLimit + verticalSpeed) / gravity));
            displacement += verticalSpeed * accelerationTime - 0.5f * gravity * accelerationTime * accelerationTime;
            verticalSpeed = Mathf.Max(-speedLimit, verticalSpeed - gravity * accelerationTime);
            displacement += verticalSpeed * (duration - accelerationTime);
        }
        return displacement;
    }
}
