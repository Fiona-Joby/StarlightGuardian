using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Builds the Stage 2 turret encounter and handles its result UI.</summary>
public class Stage2TurretEncounter : MonoBehaviour
{
    private const float TurretSpawnWalkDistance = 4f;
    private static readonly float[] TurretXs = { 10f, 26f, 42f, 58f, 74f, 90f };
    private static readonly float[] DroneXs = { 18f, 34f, 50f, 66f, 82f };

    private int destroyedTurrets;
    private int destroyedDrones;
    private int playerHits;
    private bool finished;
    private Text hudText;
    private Image[] lifeIcons = new Image[0];
    private Canvas overlayCanvas;
    private Text overlayText;
    private Text transitionText;
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
        {
            SpawnTurrets();
            SpawnDrones();
            LimitPlayerAtEnd(player);
        }
    }

    private void LimitPlayerAtEnd(Transform player)
    {
        if (player == null) return;
        float furthestEnemyX = Mathf.Max(GetTurretX(TurretGoal - 1), GetDroneX(DroneGoal - 1) + 2.2f);
        Camera camera = Camera.main;
        float halfViewWidth = camera != null ? camera.orthographicSize * camera.aspect : 9f;
        PlayerMovement movement = player.GetComponent<PlayerMovement>();
        if (movement != null)
            movement.SetRightBoundary(furthestEnemyX + halfViewWidth + 0.5f);
    }

    private static float GetTurretX(int index) =>
        index < TurretXs.Length ? TurretXs[index] : TurretXs[TurretXs.Length - 1] + (index - TurretXs.Length + 1) * 16f;

    private static float GetDroneX(int index) =>
        index < DroneXs.Length ? DroneXs[index] : DroneXs[DroneXs.Length - 1] + (index - DroneXs.Length + 1) * 16f;

    private void SpawnDrones()
    {
        Sprite[] frames = artReferences != null ? artReferences.DroneFloatFrames : null;
        Sprite beam = artReferences != null ? artReferences.TurretBeam : null;
        if (frames == null || frames.Length < 4 || frames[0] == null || frames[1] == null || frames[2] == null || frames[3] == null || beam == null)
        {
            Debug.LogError("[Stage2TurretEncounter] Drone frames or turret beam are not assigned on Stage2ArtReferences.");
            return;
        }

        for (int i = 0; i < DroneGoal; i++)
        {
            float droneX = GetDroneX(i);
            var droneGO = new GameObject("Floating Drone");
            droneGO.transform.position = new Vector3(droneX, -1.6f + (i % 2) * 0.6f, 0f);
            var art = new GameObject("Drone Art");
            art.transform.SetParent(droneGO.transform, false);
            art.transform.localScale = Vector3.one * 0.1f;
            var renderer = art.AddComponent<SpriteRenderer>();
            renderer.sprite = frames[0];
            renderer.sortingOrder = 8;
            var collider = droneGO.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(0.8f, 0.65f);
            droneGO.AddComponent<Stage2Drone>().Initialize(this, frames, beam, droneGO.transform.position);
        }
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

        for (int i = 0; i < TurretGoal; i++)
        {
            float turretX = GetTurretX(i);
            bool tall = i % 2 == 1;
            float scale = 3f;
            float artHeight = tall ? 633f : 291f;
            float imageCenterY = tall ? 650f : 805f;
            // Stage2's Ground collider top is y = -4.9 (center -5, half-height 0.1).
            const float groundLevel = -4.9f;
            float rootY = groundLevel + ((951f - imageCenterY) * scale / 700f);
            GameObject turretGO = new GameObject(tall ? "Tall Turret" : "Short Turret");
            turretGO.transform.position = new Vector3(turretX, rootY, 0f);

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
            turret.Initialize(this, muzzle.transform, beamSprite, i * 0.45f + 0.7f, tall);
        }
    }

    public bool IsFinished => finished;
    public int TurretGoal => artReferences != null ? artReferences.TurretGoal : 6;
    public int DroneGoal => artReferences != null ? artReferences.DroneGoal : 5;
    public int LifeLimit => artReferences != null ? Mathf.Max(1, artReferences.LifeLimit) : 5;

    public void OnTurretDestroyed()
    {
        if (finished) return;
        destroyedTurrets++;
        RefreshHud();
        CheckObjectiveComplete();
    }

    public void OnDroneDestroyed()
    {
        if (finished) return;
        destroyedDrones++;
        RefreshHud();
        CheckObjectiveComplete();
    }

    private void CheckObjectiveComplete()
    {
        if (destroyedTurrets < TurretGoal || destroyedDrones < DroneGoal) return;
        finished = true;
        ShowOverlay("TURRETS AND DRONES\nDESTROYED SUCCESSFULLY", true);
        Time.timeScale = 0f;
        StartCoroutine(LoadStage3());
    }

    public void OnPlayerHit()
    {
        if (finished) return;
        playerHits++;
        RefreshHud();
        if (playerHits >= LifeLimit)
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
        lifeIcons = new Image[LifeLimit];

        RectTransform hudRect = RuntimeUiUtility.CreateRect("Encounter Status", canvasGO.transform);
        hudRect.anchorMin = new Vector2(0f, 1f);
        hudRect.anchorMax = new Vector2(0f, 1f);
        hudRect.pivot = new Vector2(0f, 1f);
        hudRect.anchoredPosition = new Vector2(32f, -24f);
        hudRect.sizeDelta = new Vector2(Mathf.Max(720f, 32f + lifeIcons.Length * 80f), 88f);
        hudText = RuntimeUiUtility.CreateText(
            "Turrets Destroyed", hudRect, null, 32, TextAnchor.MiddleLeft,
            Color.white, FontStyle.Bold);
        hudText.rectTransform.anchorMin = new Vector2(0f, 0f);
        hudText.rectTransform.anchorMax = new Vector2(0f, 1f);
        hudText.rectTransform.pivot = new Vector2(0f, 0.5f);
        hudText.rectTransform.anchoredPosition = new Vector2(lifeIcons.Length * 80f + 16f, 0f);
        hudText.rectTransform.sizeDelta = new Vector2(400f, 0f);

        Sprite heartSprite = artReferences != null ? artReferences.Heart : null;
        if (heartSprite == null)
            Debug.LogError("[Stage2TurretEncounter] Heart sprite is not assigned on Stage2ArtReferences.");
        for (int i = 0; i < lifeIcons.Length; i++)
        {
            GameObject heart = new GameObject($"Life {i + 1}", typeof(RectTransform), typeof(Image));
            heart.transform.SetParent(hudRect, false);
            RectTransform heartRect = heart.GetComponent<RectTransform>();
            heartRect.anchorMin = new Vector2(0f, 0.5f);
            heartRect.anchorMax = new Vector2(0f, 0.5f);
            heartRect.pivot = new Vector2(0f, 0.5f);
            heartRect.anchoredPosition = new Vector2(i * 80f, 0f);
            heartRect.sizeDelta = new Vector2(80f, 80f);
            lifeIcons[i] = heart.GetComponent<Image>();
            lifeIcons[i].sprite = heartSprite;
            lifeIcons[i].preserveAspect = true;
        }

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
            "Result Text", overlayGO.transform, hudText.font, 58,
            TextAnchor.MiddleCenter, new Color(1f, 0.9f, 0.35f, 1f), FontStyle.Bold);
        RectTransform textRect = overlayText.rectTransform;
        textRect.anchorMin = new Vector2(0.1f, 0.48f);
        textRect.anchorMax = new Vector2(0.9f, 0.72f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        transitionText = RuntimeUiUtility.CreateText(
            "Transition Text", overlayGO.transform, overlayText.font, 34,
            TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
        RectTransform transitionRect = transitionText.rectTransform;
        transitionRect.anchorMin = new Vector2(0.1f, 0.34f);
        transitionRect.anchorMax = new Vector2(0.9f, 0.46f);
        transitionRect.offsetMin = Vector2.zero;
        transitionRect.offsetMax = Vector2.zero;
        transitionText.text = "Transitioning to Stage 3...";
        transitionText.gameObject.SetActive(false);
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
            hudText.text = $"Turrets: {destroyedTurrets}/{TurretGoal}    Drones: {destroyedDrones}/{DroneGoal}";
        int livesRemaining = Mathf.Max(0, LifeLimit - playerHits);
        for (int i = 0; i < lifeIcons.Length; i++)
            if (lifeIcons[i] != null)
                lifeIcons[i].gameObject.SetActive(i < livesRemaining);
    }

    private void ShowOverlay(string message, bool showTransition = false)
    {
        if (overlayText == null) return;
        overlayText.text = message;
        if (transitionText != null)
            transitionText.gameObject.SetActive(showTransition);
        overlayCanvas.transform.Find("Encounter Result").gameObject.SetActive(true);
    }
}
