using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Unified input hub: reads from keyboard AND from the webcam pose bridge.
/// Keyboard movement overrides webcam movement while a direction key is held;
/// webcam gestures remain active when no keyboard direction is pressed.
///
/// Properties exposed to consumers (PlayerMovement, etc.):
///   HorizontalInput  — [-1, 1] movement axis
///   IsReaching        — true when the webcam pose detects the reach gesture
///   CollectPressed   — one-frame request from an open palm or Space
///   FacingRight       — last known facing direction
/// </summary>
[DefaultExecutionOrder(-100)]
public class PlayerInput : MonoBehaviour
{
    /// <summary>Normalised horizontal axis (–1 left, +1 right, 0 idle).</summary>
    public float HorizontalInput { get; private set; }

    /// <summary>True when the webcam pose detects the reach animation gesture.</summary>
    public bool IsReaching { get; private set; }

    /// <summary>True for one frame when the player requests a star pickup.</summary>
    public bool CollectPressed { get; private set; }
    public bool JumpHeld { get; private set; }
    public bool JumpPressed { get; private set; }
    public bool ShootPressed { get; private set; }

    /// <summary>True for one frame when the player makes a fist gesture (shield activation).</summary>
    public bool ShieldPressed { get; private set; }

    private bool jumpArmed = true;
    private float jumpReleaseTime;
    [Header("Jump Gesture")]
    [Tooltip("解除 V 手勢多久後，才允許下一次起跳（秒）。")]
    [SerializeField, Min(0f)] private float jumpRearmDelay = 0.12f;

    /// <summary>True if the character was last moving/facing right.</summary>
    public bool FacingRight { get; private set; } = true;

    // The pose bridge is optional — the game must work without a webcam.
    private WebcamPoseInput poseInput;
    private bool wasCollecting;
    private bool wasFisting;
    private bool wasShooting;

    private void Awake()
    {
        poseInput = GetComponent<WebcamPoseInput>();
    }

    private void Update()
    {
        bool isStage2 = SceneManager.GetActiveScene().name == "Stage2";
        bool shootingGesture = isStage2 && poseInput != null && poseInput.IsShooting;
        ShootPressed = isStage2 && (Input.GetKeyDown(KeyCode.X) || (shootingGesture && !wasShooting));
        wasShooting = shootingGesture;

        float keyboardAxis = Input.GetAxisRaw("Horizontal");

        JumpHeld = Input.GetKey(KeyCode.UpArrow) || (poseInput != null && poseInput.IsJumpHeld);
        JumpPressed = JumpHeld && jumpArmed;
        if (JumpHeld)
        {
            jumpArmed = false;
            jumpReleaseTime = 0f;
        }
        else
        {
            // Release ends the boost immediately. A short stable release is
            // required to rearm, so a single missed camera frame cannot retrigger.
            jumpReleaseTime += Time.deltaTime;
            if (jumpReleaseTime >= jumpRearmDelay)
                jumpArmed = true;
        }

        // ----- Combine keyboard movement with webcam gestures when active -----
        if (poseInput != null && poseInput.IsActive)
        {
            // A direction key overrides webcam movement and reach locking;
            // webcam gestures remain available when the keyboard is idle.
            bool keyboardOverride = Mathf.Abs(keyboardAxis) > 0.01f;
            HorizontalInput = keyboardOverride ? keyboardAxis : poseInput.HorizontalAxis;
            IsReaching = !keyboardOverride && poseInput.IsReaching;
            CollectPressed = Input.GetKeyDown(KeyCode.Space) ||
                             (poseInput.IsCollecting && !wasCollecting);
            wasCollecting = poseInput.IsCollecting;

            ShieldPressed = poseInput.IsFistHeld && !wasFisting;
            wasFisting = poseInput.IsFistHeld;
        }
        else
        {
            // ----- Keyboard fallback -----
            HorizontalInput = Input.GetAxis("Horizontal");
            IsReaching = false;
            CollectPressed = Input.GetKeyDown(KeyCode.Space);
            ShieldPressed = false;
            wasCollecting = false;
            wasFisting = false;
        }

        // Track facing direction.
        if (HorizontalInput > 0.01f) FacingRight = true;
        else if (HorizontalInput < -0.01f) FacingRight = false;
    }
}
