using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Builds the Stage 2 turret encounter and handles its result UI.</summary>
public class Stage2TurretEncounter : MonoBehaviour
{
    private const int TurretGoal = 6;
    private const int HitLimit = 3;
    private const float TurretSpawnWalkDistance = 4f;
    private static readonly float[] TurretXs = { 5f, 17f, 29f, 41f, 53f, 65f };

    private int destroyedTurrets;
    private int playerHits;
    private bool finished;
    private Text hudText;
    private Canvas overlayCanvas;
    private Text overlayText;
    private Button retryButton;
    private Stage2ArtReferences artReferences;

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("[Stage2TurretEncounter] No Player tagged object found.");
            return;
        }

        if (player.GetComponent<Stage2PlayerHealth>() == null)
            player.AddComponent<Stage2PlayerHealth>();
        artReferences = FindAnyObjectByType<Stage2ArtReferences>();
        BuildHud();
        StartCoroutine(SpawnTurretsAfterPlayerWalks(player.transform));
        RefreshHud();
    }

    private IEnumerator SpawnTurretsAfterPlayerWalks(Transform player)
    {
        float spawnX = player.position.x + TurretSpawnWalkDistance;
        while (player != null && player.position.x < spawnX)
            yield return null;
        if (player != null && !finished)
            SpawnTurrets();
    }

    private void SpawnTurrets()
    {
        Sprite shortSprite = artReferences != null ? artReferences.TurretShort : null;
        Sprite tallSprite = artReferences != null ? artReferences.TurretTall : null;
        Sprite beamSprite = artReferences != null ? artReferences.TurretBeam : null;
        if (shortSprite == null || tallSprite == null || beamSprite == null)
        {
            Debug.LogError("[Stage2TurretEncounter] Turret sprites are not assigned on Stage2ArtReferences.");
            return;
        }

        for (int i = 0; i < TurretXs.Length; i++)
        {
            bool tall = i % 2 == 1;
            float scale = 3f;
            float artHeight = tall ? 633f : 291f;
            float imageCenterY = tall ? 650f : 805f;
            // Stage2's Ground collider top is y = -4.9 (center -5, half-height 0.1).
            const float groundLevel = -4.9f;
            float rootY = groundLevel + ((951f - imageCenterY) * scale / 700f);
            GameObject turretGO = new GameObject(tall ? "Tall Turret" : "Short Turret");
            turretGO.transform.position = new Vector3(TurretXs[i], rootY, 0f);

            BoxCollider2D collider = turretGO.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(1.3f, artHeight * scale / 700f * 0.85f);

            GameObject art = new GameObject("Turret Art");
            art.transform.SetParent(turretGO.transform, false);
            art.transform.localScale = Vector3.one * scale;
            // The supplied PNG canvases include large transparent margins.
            art.transform.localPosition = tall
                ? new Vector3(-3.17f, 0.47f, 0f)
                : new Vector3(-0.9f, 1.14f, 0f);
            SpriteRenderer renderer = art.AddComponent<SpriteRenderer>();
            renderer.sprite = tall ? tallSprite : shortSprite;
            renderer.sortingOrder = 8;

            GameObject muzzle = new GameObject("Muzzle");
            muzzle.transform.SetParent(turretGO.transform, false);
            muzzle.transform.localPosition = tall
                ? new Vector3(-0.96f, 1.24f, 0f)
                : new Vector3(-0.97f, 0.39f, 0f);

            Stage2Turret turret = turretGO.AddComponent<Stage2Turret>();
            turret.Initialize(this, muzzle.transform, beamSprite, i * 0.45f + 0.7f);
        }
    }

    public bool IsFinished => finished;

    public void OnTurretDestroyed()
    {
        if (finished) return;
        destroyedTurrets++;
        RefreshHud();
        if (destroyedTurrets >= TurretGoal)
        {
            finished = true;
            ShowOverlay("SUCCESS!\n6 TURRETS DESTROYED");
            Time.timeScale = 0f;
            StartCoroutine(LoadStage3());
        }
    }

    public void OnPlayerHit()
    {
        if (finished) return;
        playerHits++;
        RefreshHud();
        if (playerHits >= HitLimit)
        {
            finished = true;
            ShowOverlay("GAME OVER");
            if (retryButton != null) retryButton.gameObject.SetActive(true);
            Time.timeScale = 0f;
        }
    }

    private IEnumerator LoadStage3()
    {
        yield return new WaitForSecondsRealtime(2f);
        Time.timeScale = 1f;
        SceneManager.LoadScene("Stage3");
    }

    private void BuildHud()
    {
        Canvas canvas = RuntimeUiUtility.CreateOverlayCanvas(
            "Stage2 HUD", 100, new Vector2(1920f, 1080f));
        RuntimeUiUtility.EnsureEventSystem();
        GameObject canvasGO = canvas.gameObject;

        RectTransform hudRect = RuntimeUiUtility.CreateRect("Encounter Status", canvasGO.transform);
        hudRect.anchorMin = new Vector2(0f, 1f);
        hudRect.anchorMax = new Vector2(0f, 1f);
        hudRect.pivot = new Vector2(0f, 1f);
        hudRect.anchoredPosition = new Vector2(32f, -24f);
        hudRect.sizeDelta = new Vector2(720f, 64f);
        hudText = RuntimeUiUtility.CreateText(
            "Status Text", hudRect, null, 32, TextAnchor.MiddleLeft,
            Color.white, FontStyle.Bold);

        RectTransform overlayRect = RuntimeUiUtility.CreateRect("Encounter Result", canvasGO.transform);
        GameObject overlayGO = overlayRect.gameObject;
        Image backdrop = overlayGO.AddComponent<Image>();
        backdrop.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
        backdrop.color = new Color(0f, 0f, 0f, 0.82f);
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        overlayText = RuntimeUiUtility.CreateText(
            "Result Text", overlayGO.transform, hudText.font, 64,
            TextAnchor.MiddleCenter, new Color(1f, 0.9f, 0.35f, 1f), FontStyle.Bold);
        RectTransform textRect = overlayText.rectTransform;
        textRect.anchorMin = new Vector2(0.1f, 0.35f);
        textRect.anchorMax = new Vector2(0.9f, 0.65f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        retryButton = RuntimeUiUtility.CreateButton(
            "Retry Button", overlayGO.transform, "RETRY", 36,
            new Color(0.18f, 0.42f, 0.72f, 1f), Color.white, new Vector2(300f, 88f));
        RectTransform buttonRect = retryButton.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.22f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.22f);
        retryButton.onClick.AddListener(RetryStage);
        retryButton.gameObject.SetActive(false);

        overlayGO.SetActive(false);
        overlayCanvas = canvas;
    }

    private void RetryStage()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void RefreshHud()
    {
        if (hudText != null)
            hudText.text = $"Hits: {playerHits}/{HitLimit}     Turrets: {destroyedTurrets}/{TurretGoal}";
    }

    private void ShowOverlay(string message)
    {
        if (overlayText == null) return;
        overlayText.text = message;
        overlayCanvas.transform.Find("Encounter Result").gameObject.SetActive(true);
    }
}
