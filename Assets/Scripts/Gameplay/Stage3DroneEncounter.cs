using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Runs Stage 3's drone encounter and returns to the main menu on completion.</summary>
public class Stage3DroneEncounter : MonoBehaviour
{
    private static readonly float[] DroneXs = { 18f, 34f, 50f, 66f, 82f };
    private const float SpawnWalkDistance = 4f;

    private Stage3ArtReferences artReferences;
    private int destroyedDrones;
    private int playerHits;
    private bool finished;
    private Text statusText;
    private Image[] lifeIcons;
    private Canvas overlayCanvas;
    private Text resultText;
    private Text transitionText;
    private Button retryButton;

    public bool IsFinished => finished;
    public int DroneGoal => artReferences != null ? artReferences.DroneGoal : 5;
    public int LifeLimit => artReferences != null ? Mathf.Max(1, artReferences.LifeLimit) : 5;

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("[Stage3DroneEncounter] No Player tagged object found.");
            return;
        }

        artReferences = FindAnyObjectByType<Stage3ArtReferences>();
        if (player.GetComponent<Stage2Shooter>() == null)
            player.AddComponent<Stage2Shooter>();
        if (player.GetComponent<Stage2PlayerHealth>() == null)
            player.AddComponent<Stage2PlayerHealth>();

        BuildHud();
        RefreshHud();
        StartCoroutine(SpawnAfterPlayerWalks(player.transform));
    }

    private IEnumerator SpawnAfterPlayerWalks(Transform player)
    {
        float spawnX = player.position.x + SpawnWalkDistance;
        while (player != null && player.position.x < spawnX)
            yield return null;
        if (player == null || finished) yield break;

        Sprite[] frames = artReferences != null ? artReferences.DroneFloatFrames : null;
        Sprite beam = artReferences != null ? artReferences.DroneBeam : null;
        if (frames == null || frames.Length < 4 || frames[0] == null || frames[1] == null ||
            frames[2] == null || frames[3] == null || beam == null)
        {
            Debug.LogError("[Stage3DroneEncounter] Drone frames or beam are not assigned on Stage3 Main Camera.");
            yield break;
        }

        for (int i = 0; i < DroneGoal; i++)
        {
            float x = i < DroneXs.Length
                ? DroneXs[i]
                : DroneXs[DroneXs.Length - 1] + (i - DroneXs.Length + 1) * 16f;
            GameObject drone = new GameObject("Floating Drone");
            drone.transform.position = new Vector3(x, -1.6f + (i % 2) * 0.6f, 0f);
            GameObject art = new GameObject("Drone Art");
            art.transform.SetParent(drone.transform, false);
            art.transform.localScale = Vector3.one * 0.15f;
            SpriteRenderer renderer = art.AddComponent<SpriteRenderer>();
            renderer.sprite = frames[0];
            renderer.sortingOrder = 8;
            BoxCollider2D collider = drone.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(1.125f, 0.9375f);
            drone.AddComponent<Stage3Drone>().Initialize(this, frames, beam, drone.transform.position);
        }
    }

    public void OnDroneDestroyed()
    {
        if (finished) return;
        destroyedDrones++;
        RefreshHud();
        if (destroyedDrones < DroneGoal) return;

        finished = true;
        ShowResult("Drones destroyed successfully", "Stages Completed!", false);
        Time.timeScale = 0f;
        StartCoroutine(ReturnToMainMenu());
    }

    public void OnPlayerHit()
    {
        if (finished) return;
        playerHits++;
        RefreshHud();
        if (playerHits < LifeLimit) return;

        finished = true;
        ShowResult("GAME OVER", string.Empty, true);
        Time.timeScale = 0f;
    }

    private IEnumerator ReturnToMainMenu()
    {
        yield return new WaitForSecondsRealtime(3f);
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    private void BuildHud()
    {
        Canvas canvas = RuntimeUiUtility.CreateOverlayCanvas("Stage3 HUD", 100, new Vector2(1920f, 1080f));
        RuntimeUiUtility.EnsureEventSystem();
        lifeIcons = new Image[LifeLimit];
        RectTransform hud = RuntimeUiUtility.CreateRect("Encounter Status", canvas.transform);
        hud.anchorMin = hud.anchorMax = new Vector2(0f, 1f);
        hud.pivot = new Vector2(0f, 1f);
        hud.anchoredPosition = new Vector2(32f, -24f);
        hud.sizeDelta = new Vector2(720f, 88f);
        statusText = RuntimeUiUtility.CreateText("Drone Count", hud, null, 32,
            TextAnchor.MiddleLeft, Color.white, FontStyle.Bold);
        statusText.rectTransform.anchorMin = new Vector2(0f, 0f);
        statusText.rectTransform.anchorMax = new Vector2(0f, 1f);
        statusText.rectTransform.pivot = new Vector2(0f, 0.5f);
        statusText.rectTransform.anchoredPosition = new Vector2(LifeLimit * 80f + 16f, 0f);
        statusText.rectTransform.sizeDelta = new Vector2(360f, 0f);

        Sprite heart = artReferences != null ? artReferences.Heart : null;
        for (int i = 0; i < lifeIcons.Length; i++)
        {
            GameObject icon = new GameObject($"Life {i + 1}", typeof(RectTransform), typeof(Image));
            icon.transform.SetParent(hud, false);
            RectTransform rect = icon.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(i * 80f, 0f);
            rect.sizeDelta = new Vector2(80f, 80f);
            lifeIcons[i] = icon.GetComponent<Image>();
            lifeIcons[i].sprite = heart;
            lifeIcons[i].preserveAspect = true;
        }

        RectTransform overlay = RuntimeUiUtility.CreateRect("Encounter Result", canvas.transform);
        overlay.anchorMin = Vector2.zero;
        overlay.anchorMax = Vector2.one;
        overlay.offsetMin = overlay.offsetMax = Vector2.zero;
        Image backdrop = overlay.gameObject.AddComponent<Image>();
        backdrop.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), Vector2.one * 0.5f);
        backdrop.color = new Color(0f, 0f, 0f, 0.82f);
        resultText = RuntimeUiUtility.CreateText("Result Text", overlay, statusText.font, 58,
            TextAnchor.MiddleCenter, new Color(1f, 0.9f, 0.35f, 1f), FontStyle.Bold);
        resultText.rectTransform.anchorMin = new Vector2(0.1f, 0.48f);
        resultText.rectTransform.anchorMax = new Vector2(0.9f, 0.72f);
        transitionText = RuntimeUiUtility.CreateText("Completion Text", overlay, resultText.font, 38,
            TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
        transitionText.rectTransform.anchorMin = new Vector2(0.1f, 0.34f);
        transitionText.rectTransform.anchorMax = new Vector2(0.9f, 0.46f);
        retryButton = RuntimeUiUtility.CreateButton("Retry Button", overlay, "RETRY", 36,
            new Color(0.18f, 0.42f, 0.72f, 1f), Color.white, new Vector2(300f, 88f));
        retryButton.GetComponent<RectTransform>().anchorMin = retryButton.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.22f);
        retryButton.onClick.AddListener(RetryStage);
        overlay.gameObject.SetActive(false);
        overlayCanvas = canvas;
    }

    private void RefreshHud()
    {
        if (statusText != null) statusText.text = $"Drones: {destroyedDrones}/{DroneGoal}";
        int remaining = Mathf.Max(0, LifeLimit - playerHits);
        for (int i = 0; i < lifeIcons.Length; i++)
            if (lifeIcons[i] != null) lifeIcons[i].gameObject.SetActive(i < remaining);
    }

    private void ShowResult(string message, string subMessage, bool showRetry)
    {
        resultText.text = message;
        transitionText.text = subMessage;
        transitionText.gameObject.SetActive(!string.IsNullOrEmpty(subMessage));
        retryButton.gameObject.SetActive(showRetry);
        overlayCanvas.transform.Find("Encounter Result").gameObject.SetActive(true);
    }

    private void RetryStage()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
