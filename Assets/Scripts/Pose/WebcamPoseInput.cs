using System;
using System.Collections;
using UnityEngine;

// MediaPipe namespaces — only used when the plugin is installed.
using Mediapipe;
using Mediapipe.Tasks.Core;
using Mediapipe.Tasks.Vision.Core;
using Mediapipe.Tasks.Vision.HandLandmarker;
using Mediapipe.Unity;
using Mediapipe.Unity.Experimental;

/// <summary>
/// Native webcam hand-pose detection using MediaPipe Unity Plugin (v0.16+).
///
/// Replaces the old Python UDP bridge entirely — webcam capture, hand landmark
/// detection, and gesture classification all run inside Unity.
///
/// This component is optional — if the webcam or MediaPipe fails to initialise,
/// the game falls back to keyboard input (handled by PlayerInput).
///
/// Setup:
///   1. Install MediaPipeUnityPlugin v0.16+ via UPM.
///   2. Place hand_landmarker.task in Assets/StreamingAssets/.
///   3. Attach this component to the Player GameObject.
/// </summary>
[DefaultExecutionOrder(-200)]
public class WebcamPoseInput : MonoBehaviour
{
    // ------------------------------------------------------------------ //
    //  Inspector settings
    // ------------------------------------------------------------------ //

    [Header("Webcam")]
    [SerializeField] private int requestedWidth = 640;
    [SerializeField] private int requestedHeight = 480;
    [SerializeField] private int requestedFPS = 30;

    [Header("MediaPipe Hand Landmarker")]
    [Tooltip("Model file name inside Assets/StreamingAssets/")]
    [SerializeField] private string modelFileName = "hand_landmarker.task";
    [SerializeField, Range(0f, 1f)] private float minDetectionConfidence = 0.6f;
    [SerializeField, Range(0f, 1f)] private float minPresenceConfidence = 0.5f;
    [SerializeField, Range(0f, 1f)] private float minTrackingConfidence = 0.5f;

    // ------------------------------------------------------------------ //
    //  Public properties consumed by PlayerInput
    // ------------------------------------------------------------------ //

    public bool IsActive { get; private set; }
    public float HorizontalAxis { get; private set; }
    public bool IsReaching { get; private set; }
    public bool IsCollecting { get; private set; }
    public bool IsJumpHeld => isActiveAndEnabled && IsActive && jumpGestureHeld;
    private bool jumpGestureHeld;
    public WebCamTexture PreviewTexture => webcamTexture;
    public bool HasHandPosition { get; private set; }
    // Mirrored camera coordinates, with the origin at the bottom left.
    public Vector2 HandPosition { get; private set; }
    private string trackedHandLabel;

    private void Awake()
    {
        if (GetComponent<WebcamPreview>() == null)
            gameObject.AddComponent<WebcamPreview>();
    }

    // ------------------------------------------------------------------ //
    //  Private state
    // ------------------------------------------------------------------ //

    private WebCamTexture webcamTexture;
    private HandLandmarker handLandmarker;
    private TextureFrame textureFrame;
    private bool initialized;
    private long frameTimestampMs;
    private string currentGesture = "idle";
    private GestureClassifier.HandData[] handDataBuffer;

    // Pre-allocated result struct to avoid GC pressure.
    private HandLandmarkerResult cachedResult;

    // ------------------------------------------------------------------ //
    //  Lifecycle
    // ------------------------------------------------------------------ //

    private IEnumerator Start()
    {
        // Wrap entire init in try/catch so a failure here can NEVER
        // break keyboard input on the same GameObject.
        bool success = false;
        IEnumerator routine = null;
        try
        {
            routine = InitializeAsync();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[WebcamPoseInput] Init failed (outer): {ex.Message}");
        }

        if (routine != null)
        {
            // Run the coroutine step by step so we can catch exceptions.
            while (true)
            {
                object current;
                try
                {
                    if (!routine.MoveNext())
                    {
                        success = initialized;
                        break;
                    }
                    current = routine.Current;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[WebcamPoseInput] Init failed: {ex.Message}");
                    break;
                }
                yield return current;
            }
        }

        if (!success)
        {
            Debug.Log("[WebcamPoseInput] Webcam pose disabled — keyboard fallback is active.");
            Cleanup();
        }
    }

    private IEnumerator InitializeAsync()
    {
        // ---- 1. Start webcam ----
        webcamTexture = new WebCamTexture(requestedWidth, requestedHeight, requestedFPS);
        webcamTexture.Play();

        int timeout = 300;
        while (webcamTexture.width <= 16 && timeout-- > 0)
            yield return null;

        if (webcamTexture.width <= 16)
        {
            Debug.LogWarning("[WebcamPoseInput] Webcam failed to initialise.");
            yield break;
        }

        Debug.Log($"[WebcamPoseInput] Webcam ready: {webcamTexture.width}x{webcamTexture.height}");

        // ---- 2. Initialise MediaPipe resource manager ----
        // This tells the native MediaPipe layer where to find model files.
        // PrepareAssetAsync is an explicit interface implementation, so cast to IResourceManager.
        IResourceManager resourceManager = new StreamingAssetsResourceManager();
        yield return resourceManager.PrepareAssetAsync(modelFileName);

        Debug.Log("[WebcamPoseInput] Model asset prepared via StreamingAssetsResourceManager.");

        // ---- 3. Create HandLandmarker ----
        var baseOptions = new BaseOptions(
            delegateCase: BaseOptions.Delegate.CPU,
            modelAssetPath: modelFileName
        );

        var options = new HandLandmarkerOptions(
            baseOptions,
            runningMode: RunningMode.VIDEO,
            numHands: 2,
            minHandDetectionConfidence: minDetectionConfidence,
            minHandPresenceConfidence: minPresenceConfidence,
            minTrackingConfidence: minTrackingConfidence
        );

        handLandmarker = HandLandmarker.CreateFromOptions(options);

        // ---- 4. Create TextureFrame for pixel transfer ----
        textureFrame = new TextureFrame(
            webcamTexture.width,
            webcamTexture.height,
            TextureFormat.RGBA32
        );

        handDataBuffer = new GestureClassifier.HandData[2];
        cachedResult = HandLandmarkerResult.Alloc(2);
        initialized = true;
        IsActive = true;
        frameTimestampMs = 0;

        Debug.Log("[WebcamPoseInput] MediaPipe HandLandmarker initialised — webcam pose active.");
    }

    private void Update()
    {
        if (!initialized || webcamTexture == null || !webcamTexture.didUpdateThisFrame)
            return;

        try
        {
            RunDetection();
            ApplyGesture();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[WebcamPoseInput] Detection error: {ex.Message}");
            // If detection keeps failing, disable webcam so keyboard takes over.
            initialized = false;
            IsActive = false;
            HasHandPosition = false;
        }
    }

    private void OnDestroy()
    {
        Cleanup();
    }

    private void Cleanup()
    {
        initialized = false;
        IsActive = false;
        HasHandPosition = false;

        if (handLandmarker != null)
        {
            try { handLandmarker.Close(); } catch { /* ignore cleanup errors */ }
            handLandmarker = null;
        }

        if (textureFrame != null)
        {
            try { textureFrame.Dispose(); } catch { /* ignore */ }
            textureFrame = null;
        }

        if (webcamTexture != null)
        {
            webcamTexture.Stop();
            webcamTexture = null;
        }
    }

    // ------------------------------------------------------------------ //
    //  Detection
    // ------------------------------------------------------------------ //

    private void RunDetection()
    {
        jumpGestureHeld = false;
        HasHandPosition = false;
        // Copy webcam pixels into the TextureFrame (CPU path).
        // flipHorizontally mirrors like cv2.flip(frame, 1) so handedness labels
        // match the original Python mapping.
        // flipVertically converts Unity bottom-up to MediaPipe top-down.
        textureFrame.ReadTextureOnCPU(webcamTexture, flipHorizontally: true, flipVertically: true);

        using (var image = textureFrame.BuildCPUImage())
        {
            // Release the texture frame now that the Image owns the data.
            textureFrame.Release();

            // Timestamp must be monotonically increasing (milliseconds).
            frameTimestampMs += 33;

            if (handLandmarker.TryDetectForVideo(image, frameTimestampMs, null, ref cachedResult))
            {
                currentGesture = ExtractGesture(cachedResult);
            }
            else
            {
                currentGesture = "idle";
            }
        }
    }

    private string ExtractGesture(HandLandmarkerResult result)
    {
        if (result.handLandmarks == null || result.handLandmarks.Count == 0)
            return "idle";

        int handCount = result.handLandmarks.Count;
        // Keep the same hand when MediaPipe changes the ordering of two hands.
        int selectedHand = 0;
        for (int h = 0; h < handCount; h++)
        {
            var labels = result.handedness[h].categories;
            if (labels != null && labels.Count > 0 && labels[0].categoryName == trackedHandLabel)
            {
                selectedHand = h;
                break;
            }
        }
        var selectedLabels = result.handedness[selectedHand].categories;
        trackedHandLabel = selectedLabels != null && selectedLabels.Count > 0
            ? selectedLabels[0].categoryName : null;
        var palm = result.handLandmarks[selectedHand].landmarks;
        if (palm != null && palm.Count >= 21)
        {
            // Wrist and four knuckles form a stable palm center.
            float x = (palm[0].x + palm[5].x + palm[9].x + palm[13].x + palm[17].x) / 5f;
            float y = (palm[0].y + palm[5].y + palm[9].y + palm[13].y + palm[17].y) / 5f;
            HandPosition = new Vector2(x, 1f - y);
            HasHandPosition = true;
        }
        if (handDataBuffer.Length < handCount)
            handDataBuffer = new GestureClassifier.HandData[handCount];

        for (int h = 0; h < handCount; h++)
        {
            var landmarks = result.handLandmarks[h].landmarks;
            var categories = result.handedness[h].categories;

            string label = (categories != null && categories.Count > 0)
                ? categories[0].categoryName
                : "Unknown";

            var points = new GestureClassifier.LandmarkPoint[landmarks.Count];
            for (int i = 0; i < landmarks.Count; i++)
            {
                points[i].x = landmarks[i].x;
                points[i].y = landmarks[i].y;
            }

            handDataBuffer[h] = new GestureClassifier.HandData
            {
                label = label,
                landmarks = points
            };
        }

        var slice = new GestureClassifier.HandData[handCount];
        System.Array.Copy(handDataBuffer, slice, handCount);
        jumpGestureHeld = GestureClassifier.HasJumpGesture(slice);
        return GestureClassifier.Classify(slice);
    }

    // ------------------------------------------------------------------ //
    //  Gesture → properties
    // ------------------------------------------------------------------ //

    private void ApplyGesture()
    {
        switch (currentGesture)
        {
            case "right":
                HorizontalAxis = 1f; IsReaching = false; IsCollecting = false;
                break;
            case "left":
                HorizontalAxis = -1f; IsReaching = false; IsCollecting = false;
                break;
            case "reach":
                HorizontalAxis = 0f; IsReaching = true; IsCollecting = false;
                break;
            case "collect":
                HorizontalAxis = 0f; IsReaching = false; IsCollecting = true;
                break;
            default:
                HorizontalAxis = 0f; IsReaching = false; IsCollecting = false;
                break;
        }
    }
}
