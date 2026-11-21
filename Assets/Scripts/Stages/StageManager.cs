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
        subscribedToGoal = false;
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

        Time.timeScale = 0f;
        ScoreManager score = ScoreManager.Instance;
        int currentScore = score != null ? score.Score : 0;
        int starsCollected = score != null ? score.StarsCollected : 0;
        GameDifficulty.AdjustAfterStage1(Time.timeSinceLevelLoad, starsCollected);
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        PlayerInput playerInput = player != null ? player.GetComponent<PlayerInput>() : null;
        AiCoachInteraction.Show(overlayCanvas.transform, "Stage 1", Time.timeSinceLevelLoad,
            currentScore, starsCollected,
            $"The player collected {starsCollected} of {score?.StageGoal ?? GameDifficulty.Stage1StarGoal} stars, scored {currentScore} points, and took {Time.timeSinceLevelLoad:0} seconds.",
            playerInput, ContinueToNextStage);
    }

    private void ContinueToNextStage()
    {
        StoryArc.AfterStage1(() =>
        {
            Time.timeScale = 1f;
            if (Application.CanStreamedLevelBeLoaded(stage2SceneName)) SceneManager.LoadScene(stage2SceneName);
            else Debug.Log($"[StageManager] Scene \"{stage2SceneName}\" not found in Build Settings. Add it to transition to Stage 2.");
        });
    }

    // ------------------------------------------------------------------ //
    //  Procedural overlay construction
    // ------------------------------------------------------------------ //

    private void BuildOverlay()
    {
        // --- Canvas ---
        overlayCanvas = RuntimeUiUtility.CreateOverlayCanvas(
            "CompletionOverlay", 200, new Vector2(1920f, 1080f));
        GameObject canvasGO = overlayCanvas.gameObject;
        canvasGO.transform.SetParent(transform, false);

        // CanvasGroup for fade.
        overlayGroup = canvasGO.AddComponent<CanvasGroup>();
        overlayGroup.alpha = 0f;
        overlayGroup.blocksRaycasts = true;

        // --- Dark backdrop ---
        GameObject bgGO = RuntimeUiUtility.CreateRect("Backdrop", canvasGO.transform).gameObject;
        RectTransform bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;
        Image bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.02f, 0.02f, 0.08f, 0.85f);

        // --- "Level 1 Completed!" label ---
        completionLabel = RuntimeUiUtility.CreateText(
            "CompletionLabel", canvasGO.transform, null, 64,
            TextAnchor.MiddleCenter, new Color(1f, 0.92f, 0.4f, 1f), FontStyle.Bold);
        RectTransform labelRT = completionLabel.rectTransform;
        labelRT.anchorMin = new Vector2(0f, 0.4f);
        labelRT.anchorMax = new Vector2(1f, 0.7f);
        labelRT.offsetMin = Vector2.zero;
        labelRT.offsetMax = Vector2.zero;
        completionLabel.text = "Level 1 Completed!";

        // --- Sub-label ---
        subLabel = RuntimeUiUtility.CreateText(
            "SubLabel", canvasGO.transform, completionLabel.font, 28,
            TextAnchor.MiddleCenter, new Color(0.8f, 0.85f, 1f, 0.8f));
        RectTransform subRT = subLabel.rectTransform;
        subRT.anchorMin = new Vector2(0f, 0.28f);
        subRT.anchorMax = new Vector2(1f, 0.42f);
        subRT.offsetMin = Vector2.zero;
        subRT.offsetMax = Vector2.zero;
        subLabel.text = "Transitioning to Stage 2...";
    }
}
