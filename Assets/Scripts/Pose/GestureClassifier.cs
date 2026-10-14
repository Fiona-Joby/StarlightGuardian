/// <summary>
/// Pure C# gesture classifier — direct port of the Python pose_server.py logic.
/// Takes normalized hand landmark positions and classifies the gesture.
///
/// Gesture mapping (webcam is mirrored):
///   MediaPipe "Right" → player's LEFT hand
///   MediaPipe "Left"  → player's RIGHT hand
///
///   • Right hand index finger raised (others curled) → "right" (run right)
///   • Left hand index finger raised (others curled)  → "left"  (run left)
///   • Both hands open and raised                     → "reach"
///   • One open palm                                  → "collect"
///   • Anything else                                  → "idle"
/// </summary>
public static class GestureClassifier
{
    // ------------------------------------------------------------------ //
    //  Hand Landmark indices (MediaPipe convention, 21 landmarks per hand)
    // ------------------------------------------------------------------ //

    private const int WRIST = 0;
    private const int THUMB_TIP = 4;
    private const int THUMB_IP = 3;
    private const int INDEX_TIP = 8;
    private const int INDEX_PIP = 6;
    private const int MIDDLE_TIP = 12;
    private const int MIDDLE_PIP = 10;
    private const int RING_TIP = 16;
    private const int RING_PIP = 14;
    private const int PINKY_TIP = 20;
    private const int PINKY_PIP = 18;

    private static readonly int[] FingerTips = { THUMB_TIP, INDEX_TIP, MIDDLE_TIP, RING_TIP, PINKY_TIP };
    private static readonly int[] FingerPIPs = { THUMB_IP, INDEX_PIP, MIDDLE_PIP, RING_PIP, PINKY_PIP };

    // ------------------------------------------------------------------ //
    //  Lightweight data types (no MediaPipe dependency)
    // ------------------------------------------------------------------ //

    /// <summary>A single normalised 2D landmark (x, y in [0, 1]).</summary>
    public struct LandmarkPoint
    {
        public float x;
        public float y;
    }

    /// <summary>One detected hand: label ("Left"/"Right") + 21 landmarks.</summary>
    public struct HandData
    {
        public string label;
        public LandmarkPoint[] landmarks;
    }

    // ------------------------------------------------------------------ //
    //  Finger helpers
    // ------------------------------------------------------------------ //

    /// <summary>A finger is raised when its TIP is above (lower y) its PIP joint.</summary>
    private static bool IsFingerRaised(LandmarkPoint[] lm, int fingerIdx)
    {
        return lm[FingerTips[fingerIdx]].y < lm[FingerPIPs[fingerIdx]].y;
    }

    private static int CountRaisedFingers(LandmarkPoint[] lm)
    {
        int count = 0;
        for (int i = 0; i < 5; i++)
            if (IsFingerRaised(lm, i)) count++;
        return count;
    }

    /// <summary>True when ONLY the index finger is raised (others curled).</summary>
    private static bool IsIndexOnly(LandmarkPoint[] lm)
    {
        return IsFingerRaised(lm, 1) &&
               !IsFingerRaised(lm, 2) &&
               !IsFingerRaised(lm, 3) &&
               !IsFingerRaised(lm, 4);
    }

    /// <summary>True when at least 4 fingers are raised.</summary>
    private static bool IsHandOpen(LandmarkPoint[] lm) => CountRaisedFingers(lm) >= 4;

    /// <summary>True when the wrist is above the midpoint of the frame.</summary>
    private static bool IsHandRaised(LandmarkPoint[] lm) => lm[WRIST].y < 0.6f;

    // ------------------------------------------------------------------ //
    //  Classification
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Classify gesture from detected hands.
    /// Returns: "right", "left", "reach", "collect", or "idle".
    /// </summary>
    public static string Classify(HandData[] hands)
    {
        if (hands == null || hands.Length == 0)
            return "idle";

        // MediaPipe labels are from the camera's perspective (mirrored webcam):
        //   "Left"  in MediaPipe = player's RIGHT hand
        //   "Right" in MediaPipe = player's LEFT  hand
        LandmarkPoint[] leftLm = null;
        LandmarkPoint[] rightLm = null;

        for (int i = 0; i < hands.Length; i++)
        {
            if (hands[i].label == "Left") leftLm = hands[i].landmarks;
            else if (hands[i].label == "Right") rightLm = hands[i].landmarks;
        }

        // Rule 1: both hands open & raised → "reach"
        if (leftLm != null && rightLm != null &&
            IsHandOpen(leftLm) && IsHandRaised(leftLm) &&
            IsHandOpen(rightLm) && IsHandRaised(rightLm))
        {
            return "reach";
        }

        // One open palm → "collect"
        if ((leftLm != null && IsHandOpen(leftLm)) ||
            (rightLm != null && IsHandOpen(rightLm)))
        {
            return "collect";
        }

        // Rule 2: MediaPipe "Right" (player's LEFT) index only → run LEFT
        if (rightLm != null && IsIndexOnly(rightLm) &&
            !(leftLm != null && IsIndexOnly(leftLm)))
        {
            return "left";
        }

        // Rule 3: MediaPipe "Left" (player's RIGHT) index only → run RIGHT
        if (leftLm != null && IsIndexOnly(leftLm) &&
            !(rightLm != null && IsIndexOnly(rightLm)))
        {
            return "right";
        }

        return "idle";
    }
}
