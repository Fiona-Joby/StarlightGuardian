using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Listens for the stage goal being reached and displays a
/// "Level 1 Completed!" overlay, then transitions to Stage 2.
///
/// Like ScoreProgressBar, builds its own UI in code so no manual
/// Canvas setup is required.
/// </summary>
public class StageManager : MonoBehaviour
{
    [Header("Transition")]
    [SerializeField] private float displayDuration = 3f;
    [SerializeField] private string stage2SceneName = "Stage2";

    private Canvas overlayCanvas;
    private CanvasGroup overlayGroup;
    private Text completionLabel;
    private Text subLabel;

    private bool subscribedToGoal;

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void Update()
    {
        TrySubscribe();
    }

    private void TrySubscribe()
    {
        if (subscribedToGoal) return;
        if (ScoreManager.Instance == null) return;
        ScoreManager.Instance.StageGoalReached += OnGoalReached;
        subscribedToGoal = true;
    }

    private void OnDisable()
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.StageGoalReached -= OnGoalReached;
    }

    private void OnGoalReached()
    {
        BuildOverlay();
        StartCoroutine(ShowCompletionSequence());
    }

    private IEnumerator ShowCompletionSequence()
    {
        // Fade in.
        float elapsed = 0f;
        float fadeIn = 0.6f;
        while (elapsed < fadeIn)
        {
            elapsed += Time.deltaTime;
            if (overlayGroup != null)
                overlayGroup.alpha = Mathf.Clamp01(elapsed / fadeIn);
            yield return null;
        }

        // Hold.
        yield return new WaitForSeconds(displayDuration);

        // Transition to Stage 2.
        // If the Stage 2 scene exists in the build settings, load it.
        // Otherwise, log a message and stay on the current scene.
        if (Application.CanStreamedLevelBeLoaded(stage2SceneName))
        {
            SceneManager.LoadScene(stage2SceneName);
        }
        else
        {
            Debug.Log($"[StageManager] Scene \"{stage2SceneName}\" not found in Build Settings. " +
                       "Add it to transition to Stage 2.");
        }
    }

    // ------------------------------------------------------------------ //
    //  Procedural overlay construction
    // ------------------------------------------------------------------ //

    private void BuildOverlay()
    {
        // --- Canvas ---
        GameObject canvasGO = new GameObject("CompletionOverlay");
        canvasGO.transform.SetParent(transform);
        overlayCanvas = canvasGO.AddComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 200;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        // CanvasGroup for fade.
        overlayGroup = canvasGO.AddComponent<CanvasGroup>();
        overlayGroup.alpha = 0f;
        overlayGroup.blocksRaycasts = true;

        // --- Dark backdrop ---
        GameObject bgGO = CreateRect("Backdrop", canvasGO.transform);
        RectTransform bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;
        Image bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.02f, 0.02f, 0.08f, 0.85f);

        // --- "Level 1 Completed!" label ---
        GameObject labelGO = CreateRect("CompletionLabel", canvasGO.transform);
        RectTransform labelRT = labelGO.GetComponent<RectTransform>();
        labelRT.anchorMin = new Vector2(0f, 0.4f);
        labelRT.anchorMax = new Vector2(1f, 0.7f);
        labelRT.offsetMin = Vector2.zero;
        labelRT.offsetMax = Vector2.zero;
        completionLabel = labelGO.AddComponent<Text>();
        completionLabel.text = "Level 1 Completed!";
        completionLabel.font = GetFont();
        completionLabel.fontSize = 64;
        completionLabel.fontStyle = FontStyle.Bold;
        completionLabel.alignment = TextAnchor.MiddleCenter;
        completionLabel.color = new Color(1f, 0.92f, 0.4f, 1f); // gold

        // --- Sub-label ---
        GameObject subGO = CreateRect("SubLabel", canvasGO.transform);
        RectTransform subRT = subGO.GetComponent<RectTransform>();
        subRT.anchorMin = new Vector2(0f, 0.28f);
        subRT.anchorMax = new Vector2(1f, 0.42f);
        subRT.offsetMin = Vector2.zero;
        subRT.offsetMax = Vector2.zero;
        subLabel = subGO.AddComponent<Text>();
        subLabel.text = "Transitioning to Stage 2...";
        subLabel.font = GetFont();
        subLabel.fontSize = 28;
        subLabel.alignment = TextAnchor.MiddleCenter;
        subLabel.color = new Color(0.8f, 0.85f, 1f, 0.8f);
    }

    /// <summary>Returns a usable font with cascading fallback for all Unity versions.</summary>
    private static Font GetFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 14);
        return font;
    }

    private static GameObject CreateRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }
}
