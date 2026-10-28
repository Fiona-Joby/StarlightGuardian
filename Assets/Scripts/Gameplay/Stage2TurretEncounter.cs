using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>Builds the Stage 2 turret encounter and handles its result UI.</summary>
public class Stage2TurretEncounter : MonoBehaviour
{
    private const int TurretGoal = 5;
    private const int HitLimit = 3;
    private const float TurretSpawnWalkDistance = 4f;
    private static readonly float[] TurretXs = { 5f, 15f, 25f, 35f, 45f };

    private int destroyedTurrets;
    private int playerHits;
    private bool finished;
    private Text hudText;
    private Canvas overlayCanvas;
    private Text overlayText;
    private Button retryButton;
    private Stage2PlayerHealth playerHealth;

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("[Stage2TurretEncounter] No Player tagged object found.");
            return;
        }

        playerHealth = player.GetComponent<Stage2PlayerHealth>();
        if (playerHealth == null) playerHealth = player.AddComponent<Stage2PlayerHealth>();
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
        Sprite shortSprite = Resources.Load<Sprite>("Turrets/Turret Short");
        Sprite tallSprite = Resources.Load<Sprite>("Turrets/Turret Tall");
        Sprite beamSprite = Resources.Load<Sprite>("Turrets/Turret Beam");
        if (shortSprite == null || tallSprite == null || beamSprite == null)
        {
            Debug.LogError("[Stage2TurretEncounter] Turret sprites are missing from Resources/Turrets.");
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
            ShowOverlay("SUCCESS!\n5 TURRETS DESTROYED");
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
        GameObject canvasGO = new GameObject("Stage2 HUD");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGO.AddComponent<GraphicRaycaster>();

        GameObject hudGO = new GameObject("Encounter Status", typeof(RectTransform));
        hudGO.transform.SetParent(canvasGO.transform, false);
        RectTransform hudRect = hudGO.GetComponent<RectTransform>();
        hudRect.anchorMin = new Vector2(0f, 1f);
        hudRect.anchorMax = new Vector2(0f, 1f);
        hudRect.pivot = new Vector2(0f, 1f);
        hudRect.anchoredPosition = new Vector2(32f, -24f);
        hudRect.sizeDelta = new Vector2(720f, 64f);
        hudText = hudGO.AddComponent<Text>();
        hudText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hudText.fontSize = 32;
        hudText.fontStyle = FontStyle.Bold;
        hudText.alignment = TextAnchor.MiddleLeft;
        hudText.color = Color.white;

        GameObject overlayGO = new GameObject("Encounter Result", typeof(RectTransform));
        overlayGO.transform.SetParent(canvasGO.transform, false);
        Image backdrop = overlayGO.AddComponent<Image>();
        backdrop.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
        backdrop.color = new Color(0f, 0f, 0f, 0.82f);
        RectTransform overlayRect = overlayGO.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        GameObject textGO = new GameObject("Result Text", typeof(RectTransform));
        textGO.transform.SetParent(overlayGO.transform, false);
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.1f, 0.35f);
        textRect.anchorMax = new Vector2(0.9f, 0.65f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        overlayText = textGO.AddComponent<Text>();
        overlayText.font = hudText.font;
        overlayText.fontSize = 64;
        overlayText.fontStyle = FontStyle.Bold;
        overlayText.alignment = TextAnchor.MiddleCenter;
        overlayText.color = new Color(1f, 0.9f, 0.35f, 1f);

        GameObject buttonGO = new GameObject("Retry Button", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGO.transform.SetParent(overlayGO.transform, false);
        RectTransform buttonRect = buttonGO.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.22f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.22f);
        buttonRect.sizeDelta = new Vector2(300f, 88f);
        Image buttonImage = buttonGO.GetComponent<Image>();
        buttonImage.color = new Color(0.18f, 0.42f, 0.72f, 1f);
        retryButton = buttonGO.GetComponent<Button>();
        retryButton.targetGraphic = buttonImage;
        retryButton.onClick.AddListener(RetryStage);
        GameObject buttonTextGO = new GameObject("Retry Label", typeof(RectTransform));
        buttonTextGO.transform.SetParent(buttonGO.transform, false);
        RectTransform buttonTextRect = buttonTextGO.GetComponent<RectTransform>();
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.offsetMin = Vector2.zero;
        buttonTextRect.offsetMax = Vector2.zero;
        Text buttonText = buttonTextGO.AddComponent<Text>();
        buttonText.font = hudText.font;
        buttonText.fontSize = 36;
        buttonText.fontStyle = FontStyle.Bold;
        buttonText.alignment = TextAnchor.MiddleCenter;
        buttonText.color = Color.white;
        buttonText.text = "RETRY";
        retryButton.gameObject.SetActive(false);

        if (EventSystem.current == null)
        {
            GameObject eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<EventSystem>();
            eventSystemGO.AddComponent<StandaloneInputModule>();
        }
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
