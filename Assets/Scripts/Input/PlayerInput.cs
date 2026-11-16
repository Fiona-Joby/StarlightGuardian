using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Unified input hub with a selectable keyboard or webcam gesture mode.
///
/// Properties exposed to consumers (PlayerMovement, etc.):
///   HorizontalInput  — [-1, 1] movement axis
///   IsReaching        — true when the webcam pose detects the reach gesture
///   CollectPressed   — collection request from an open palm or Space
///   FacingRight       — last known facing direction
/// </summary>
[DefaultExecutionOrder(-100)]
public class PlayerInput : MonoBehaviour
{
    /// <summary>Normalised horizontal axis (–1 left, +1 right, 0 idle).</summary>
    public float HorizontalInput { get; private set; }

    /// <summary>True when the webcam pose detects the reach animation gesture.</summary>
    public bool IsReaching { get; private set; }

    /// <summary>True while the player requests a star pickup.</summary>
    public bool CollectPressed { get; private set; }
    public bool JumpHeld { get; private set; }
    public bool JumpPressed { get; private set; }
    public bool ShootPressed { get; private set; }
    /// <summary>True for one frame when the Stage 3 final-pose gesture or "star" is entered.</summary>
    public bool FinalPosePressed { get; private set; }

    /// <summary>True for one frame when the player makes a fist gesture (shield activation).</summary>
    public bool ShieldPressed { get; private set; }
    /// <summary>True only while webcam gestures are the active control source.</summary>
    public bool IsUsingGestureControls => PlayerPrefs.GetInt("GestureControls", 0) == 1 && poseInput != null && poseInput.IsActive;
    public int GestureLeftCount { get; private set; }
    public int GestureRightCount { get; private set; }
    public int GestureJumpCount { get; private set; }
    public int GestureReachCount { get; private set; }
    public int GestureCollectCount { get; private set; }

    private bool jumpArmed = true;
    private float jumpReleaseTime;
    [Header("Jump Gesture")]
    [Tooltip("解除 V 手勢多久後，才允許下一次起跳（秒）。")]
    [SerializeField, Min(0f)] private float jumpRearmDelay = 0.12f;

    /// <summary>True if the character was last moving/facing right.</summary>
    public bool FacingRight { get; private set; } = true;

    // The pose bridge is optional — the game must work without a webcam.
    private WebcamPoseInput poseInput;
    private bool wasFisting;
    private bool wasShooting;
    private bool wasPalmsTogether;
    private bool wasGestureLeft;
    private bool wasGestureRight;
    private bool wasGestureReaching;
    private bool wasGestureCollecting;
    private string finalPoseTypingBuffer = string.Empty;

    private void Awake()
    {
        poseInput = GetComponent<WebcamPoseInput>();
    }

    private void Update()
    {
        bool gestureControls = PlayerPrefs.GetInt("GestureControls", 0) == 1;
        FinalPosePressed = false;
        bool palmsTogether = gestureControls && poseInput != null && poseInput.IsActive && poseInput.ArePalmsTogether;
        if (palmsTogether && !wasPalmsTogether) FinalPosePressed = true;
        wasPalmsTogether = palmsTogether;

        bool keyboardAvailable = !gestureControls || poseInput == null || !poseInput.IsActive;
        if (keyboardAvailable)
        {
            string typed = Input.inputString.ToLowerInvariant();
            foreach (char character in typed)
            {
                if (!char.IsLetter(character)) continue;
                finalPoseTypingBuffer += character;
                if (finalPoseTypingBuffer.Length > 4)
                    finalPoseTypingBuffer = finalPoseTypingBuffer.Substring(finalPoseTypingBuffer.Length - 4);
                if (finalPoseTypingBuffer == "star")
                {
                    FinalPosePressed = true;
                    finalPoseTypingBuffer = string.Empty;
                }
            }
        }
        string activeScene = SceneManager.GetActiveScene().name;
        bool supportsCombat = activeScene == "Stage2" || activeScene == "Stage3";
        bool shootingGesture = gestureControls && supportsCombat && poseInput != null && poseInput.IsShooting;
        ShootPressed = supportsCombat && (gestureControls ? shootingGesture && !wasShooting : Input.GetKeyDown(KeyCode.X));
        wasShooting = shootingGesture;

        float keyboardAxis = gestureControls ? 0f : Input.GetAxisRaw("Horizontal");

        JumpHeld = gestureControls
            ? poseInput != null && poseInput.IsJumpHeld
            : Input.GetKey(KeyCode.UpArrow);
        JumpPressed = JumpHeld && jumpArmed;
        if (IsUsingGestureControls && JumpPressed)
            GestureJumpCount++;
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

        // ----- Read the selected input source -----
        if (gestureControls && poseInput != null && poseInput.IsActive)
        {
            HorizontalInput = poseInput.HorizontalAxis;
            IsReaching = poseInput.IsReaching;
            // Keep checking for stars for as long as the open-palm gesture is
            // held. A star may enter the catch zone after the gesture starts.
            CollectPressed = poseInput.IsCollecting;

            ShieldPressed = poseInput.IsFistHeld && !wasFisting;
            wasFisting = poseInput.IsFistHeld;
        }
        else
        {
            // ----- Keyboard fallback -----
            HorizontalInput = gestureControls ? 0f : Input.GetAxis("Horizontal");
            IsReaching = false;
            CollectPressed = !gestureControls && Input.GetKeyDown(KeyCode.Space);
            ShieldPressed = false;
            wasFisting = false;
        }

        // Track facing direction.
        if (HorizontalInput > 0.01f) FacingRight = true;
        else if (HorizontalInput < -0.01f) FacingRight = false;

        bool usingGestures = IsUsingGestureControls;
        bool gestureLeft = usingGestures && HorizontalInput < -0.01f;
        bool gestureRight = usingGestures && HorizontalInput > 0.01f;
        bool gestureReaching = usingGestures && IsReaching;
        bool gestureCollecting = usingGestures && CollectPressed;
        if (gestureLeft && !wasGestureLeft) GestureLeftCount++;
        if (gestureRight && !wasGestureRight) GestureRightCount++;
        if (gestureReaching && !wasGestureReaching) GestureReachCount++;
        if (gestureCollecting && !wasGestureCollecting) GestureCollectCount++;
        wasGestureLeft = gestureLeft;
        wasGestureRight = gestureRight;
        wasGestureReaching = gestureReaching;
        wasGestureCollecting = gestureCollecting;
    }
}
