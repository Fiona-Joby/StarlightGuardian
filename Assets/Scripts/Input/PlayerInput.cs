using UnityEngine;

/// <summary>
/// Unified input hub: reads from keyboard AND from the webcam pose bridge.
/// Pose input takes priority when available; keyboard is always the fallback.
///
/// Properties exposed to consumers (PlayerMovement, etc.):
///   HorizontalInput  — [-1, 1] movement axis
///   IsReaching        — true when the webcam pose detects both-hands-open gesture
///   FacingRight       — last known facing direction
/// </summary>
public class PlayerInput : MonoBehaviour
{
    /// <summary>Normalised horizontal axis (–1 left, +1 right, 0 idle).</summary>
    public float HorizontalInput { get; private set; }

    /// <summary>True when the webcam pose detects the reach/catch gesture.</summary>
    public bool IsReaching { get; private set; }

    /// <summary>True if the character was last moving/facing right.</summary>
    public bool FacingRight { get; private set; } = true;

    // The pose bridge is optional — the game must work without a webcam.
    private WebcamPoseInput poseInput;

    private void Awake()
    {
        poseInput = GetComponent<WebcamPoseInput>();
    }

    private void Update()
    {
        // ----- Pose input (webcam) takes priority when active -----
        if (poseInput != null && poseInput.IsActive)
        {
            HorizontalInput = poseInput.HorizontalAxis;
            IsReaching = poseInput.IsReaching;
        }
        else
        {
            // ----- Keyboard fallback -----
            HorizontalInput = Input.GetAxis("Horizontal");
            // No manual reach trigger from keyboard —
            // reach triggers automatically on star collection.
            IsReaching = false;
        }

        // Track facing direction.
        if (HorizontalInput > 0.01f) FacingRight = true;
        else if (HorizontalInput < -0.01f) FacingRight = false;
    }
}
