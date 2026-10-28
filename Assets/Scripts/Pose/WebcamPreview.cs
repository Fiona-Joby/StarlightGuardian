using UnityEngine;
using UnityEngine.UI;

/// <summary>Small camera monitor and a palm cursor with a centered, expanded mapping.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(WebcamPoseInput))]
public sealed class WebcamPreview : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float mappingScale = 1.8f;
    [SerializeField, Range(0.15f, 0.4f)] private float previewWidthFraction = 0.24f;

    private bool HasScreenPoint => source != null && source.isActiveAndEnabled && source.HasHandPosition;

    private WebcamPoseInput source;
    private GameObject overlay;
    private RectTransform panel;
    private RawImage preview;
    private RectTransform previewMarker;
    private RectTransform screenMarker;
    private Text status;

    private void Awake()
    {
        source = GetComponent<WebcamPoseInput>();
        overlay = new GameObject("Webcam Preview Overlay", typeof(RectTransform), typeof(Canvas));
        var canvas = overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        panel = CreateRect("Webcam Monitor", overlay.transform);
        panel.anchorMin = panel.anchorMax = panel.pivot = Vector2.zero;
        panel.anchoredPosition = new Vector2(16f, 16f);
        var background = panel.gameObject.AddComponent<Image>();
        background.color = new Color(0.025f, 0.04f, 0.06f, 0.95f);
        background.raycastTarget = false;

        var videoRect = CreateRect("Mirrored Camera", panel);
        preview = videoRect.gameObject.AddComponent<RawImage>();
        preview.raycastTarget = false;
        previewMarker = CreateMarker("Palm In Preview", videoRect, 12f);
        screenMarker = CreateMarker("Mapped Palm Point", overlay.transform, 24f);

        var labelRect = CreateRect("Tracking Status", panel);
        labelRect.anchorMin = new Vector2(0f, 1f);
        labelRect.anchorMax = Vector2.one;
        labelRect.pivot = new Vector2(0.5f, 1f);
        labelRect.sizeDelta = new Vector2(-12f, 26f);
        status = labelRect.gameObject.AddComponent<Text>();
        status.font = RuntimeUiUtility.GetFont();
        status.fontSize = 13;
        status.alignment = TextAnchor.MiddleLeft;
        status.color = Color.white;
        status.raycastTarget = false;
        previewMarker.gameObject.SetActive(false);
        screenMarker.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        var cameraTexture = source.PreviewTexture;
        bool ready = cameraTexture != null && cameraTexture.isPlaying && cameraTexture.width > 16;
        preview.gameObject.SetActive(ready);
        bool tracking = ready && HasScreenPoint;
        previewMarker.gameObject.SetActive(tracking);
        screenMarker.gameObject.SetActive(tracking);
        status.text = !ready ? "WEBCAM | Waiting for camera"
            : !tracking ? "WEBCAM | No hand detected" : $"WEBCAM | Palm mapping {mappingScale:0.0}x";

        float width = Mathf.Min(Screen.width * previewWidthFraction, 360f);
        float angle = ready ? cameraTexture.videoRotationAngle : 0f;
        bool sideways = Mathf.RoundToInt(angle) % 180 != 0;
        float aspect = ready ? (float)cameraTexture.width / cameraTexture.height : 4f / 3f;
        float displayAspect = sideways ? 1f / aspect : aspect;
        float height = width / displayAspect;
        // Keep the whole monitor visible in short or portrait windows.
        if (height > Screen.height * 0.32f)
        {
            height = Screen.height * 0.32f;
            width = height * displayAspect;
        }
        panel.sizeDelta = new Vector2(width + 8f, height + 34f);
        preview.rectTransform.anchoredPosition = new Vector2(0f, -13f);
        preview.rectTransform.sizeDelta = sideways ? new Vector2(height, width) : new Vector2(width, height);
        preview.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -angle);
        if (!ready) return;

        preview.texture = cameraTexture;
        // Match the horizontal mirror used by WebcamPoseInput's detection frame.
        bool flipY = cameraTexture.videoVerticallyMirrored;
        preview.uvRect = new Rect(1f, flipY ? 1f : 0f, -1f, flipY ? -1f : 1f);
        if (!tracking) return;

        Vector2 point = source.HandPosition;
        if (flipY) point.y = 1f - point.y;
        previewMarker.anchorMin = previewMarker.anchorMax = point;
        previewMarker.anchoredPosition = Vector2.zero;

        // Rotate normalized camera coordinates exactly as the preview is rotated.
        Vector2 mapped = MapHandToViewport(source.HandPosition, mappingScale, angle, flipY);
        screenMarker.anchorMin = screenMarker.anchorMax = mapped;
        screenMarker.anchoredPosition = Vector2.zero;
    }

    public static Vector2 MapToViewport(Vector2 cameraPoint, float scale)
    {
        return Vector2.one * 0.5f + (cameraPoint - Vector2.one * 0.5f) * scale;
    }

    public bool TryGetMappedHandViewport(out Vector2 viewportPoint)
    {
        viewportPoint = Vector2.zero;
        WebCamTexture cameraTexture = source != null ? source.PreviewTexture : null;
        if (!HasScreenPoint || cameraTexture == null || !cameraTexture.isPlaying)
            return false;

        viewportPoint = MapHandToViewport(
            source.HandPosition,
            mappingScale,
            cameraTexture.videoRotationAngle,
            cameraTexture.videoVerticallyMirrored);
        return true;
    }

    private static Vector2 MapHandToViewport(Vector2 point, float scale, float angle, bool flipY)
    {
        if (flipY) point.y = 1f - point.y;
        Vector2 centered = point - Vector2.one * 0.5f;
        Vector3 rotated = Quaternion.Euler(0f, 0f, -angle) * new Vector3(centered.x, centered.y, 0f);
        Vector2 oriented = new Vector2(rotated.x, rotated.y) + Vector2.one * 0.5f;
        return MapToViewport(oriented, scale);
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
        return rect;
    }

    private static RectTransform CreateMarker(string name, Transform parent, float size)
    {
        var marker = CreateRect(name, parent);
        marker.sizeDelta = Vector2.one * size;
        for (int i = 0; i < 2; i++)
        {
            var bar = CreateRect(i == 0 ? "Horizontal" : "Vertical", marker);
            bar.sizeDelta = i == 0 ? new Vector2(size, 3f) : new Vector2(3f, size);
            var graphic = bar.gameObject.AddComponent<Image>();
            graphic.color = new Color(0.2f, 1f, 0.75f);
            graphic.raycastTarget = false;
        }
        return marker;
    }

    private void OnEnable() { if (overlay != null) overlay.SetActive(true); }
    private void OnDisable() { if (overlay != null) overlay.SetActive(false); }
    private void OnDestroy() { if (overlay != null) Destroy(overlay); }
}
