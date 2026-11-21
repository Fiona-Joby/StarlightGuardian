using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Builds the Stage 2 turret encounter and handles its result UI.</summary>
public class Stage2TurretEncounter : MonoBehaviour
{
    private const float TurretSpawnWalkDistance = 4f;
    private static readonly float[] TurretXs = { 10f, 26f, 42f, 58f, 74f, 90f };

    private int destroyedTurrets;
    private int destroyedBlobBeasts;
    private float playerDamage;
    private bool finished;
    private Text hudText;
    private Image[] lifeIcons = new Image[0];
    private Canvas overlayCanvas;
    private Text overlayText;
    private Text transitionText;
    private Button retryButton;
    private Stage2ArtReferences artReferences;
    private float stageStartedAt;

    private void Start()
    {
        stageStartedAt = Time.unscaledTime;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("[Stage2TurretEncounter] No Player tagged object found.");
            return;
        }

        if (player.GetComponent<Stage2PlayerHealth>() == null)
            player.AddComponent<Stage2PlayerHealth>();
        artReferences = FindAnyObjectByType<Stage2ArtReferences>();
        StarSpiritCompanion.Attach(player.transform,
            artReferences != null ? artReferences.CompanionMoveFrames : null);
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
            SpawnBlobBeasts();
            LimitPlayerAtEnd(player);
        }
    }

    private void SpawnBlobBeasts()
    {
        Sprite[] walk = artReferences != null ? artReferences.BlobWalkFrames : null;
        Sprite jump = artReferences != null ? artReferences.BlobJump : null;
        Sprite stomp = artReferences != null ? artReferences.BlobStomp : null;
        if (walk == null || walk.Length < 5 || jump == null || stomp == null ||
            System.Array.Exists(walk, frame => frame == null))
        {
            Debug.LogError("[Stage2TurretEncounter] Blob Beast sprites are not assigned on Stage2ArtReferences.");
            return;
        }

        // Keep a full turret interval between each beast and the nearest turret.
        // These positions are centered in the gaps between the fixed turret line.
        int beastGoal = BlobBeastGoal;
        int slotCount = Mathf.Max(TurretGoal, beastGoal);
        GameObject ground = GameObject.Find("Ground");
        Collider2D groundCollider = ground != null ? ground.GetComponent<Collider2D>() : null;
        float groundTop = groundCollider != null ? groundCollider.bounds.max.y : -4.9f;
        for (int i = 0; i < beastGoal; i++)
        {
            int slot = Mathf.FloorToInt((i + 0.5f) * slotCount / beastGoal);
            float blobX = GetTurretX(slot) + TurretSpacing * 0.5f;
            GameObject blob = new GameObject("Blob Beast");
            blob.transform.position = new Vector3(blobX, groundTop, 0f);
            GameObject art = new GameObject("Blob Beast Art");
            art.transform.SetParent(blob.transform, false);
            art.transform.localPosition = new Vector3(-1f, 0f, 0f);
            art.transform.localScale = Vector3.one * 0.24f;
            SpriteRenderer renderer = art.AddComponent<SpriteRenderer>();
            renderer.sprite = walk[0];
            renderer.sortingOrder = 8;
            // Imported beast sprites may use a nonzero pivot. Align the rendered
            // sprite's actual lower bound with the floor instead of trusting it.
            Vector3 artPosition = art.transform.localPosition;
            artPosition.y += groundTop - renderer.bounds.min.y - 0.12f;
            art.transform.localPosition = artPosition;
            BoxCollider2D collider = blob.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(1.9f, 2.2f);
            collider.offset = new Vector2(0f, 1.1f);
            blob.AddComponent<Stage2BlobBeast>().Initialize(
                this, walk, jump, stomp, i % 2 == 1,
                artReferences != null ? artReferences.BlobBeastAI : null);
        }
    }

    private void LimitPlayerAtEnd(Transform player)
    {
        if (player == null) return;
        float furthestEnemyX = Mathf.Max(GetTurretX(TurretGoal - 1), GetBlobSpawnX(BlobBeastGoal - 1));
        Camera camera = Camera.main;
        float halfViewWidth = camera != null ? camera.orthographicSize * camera.aspect : 9f;
        PlayerMovement movement = player.GetComponent<PlayerMovement>();
        if (movement != null)
            movement.SetRightBoundary(furthestEnemyX + halfViewWidth + 0.5f);
    }

    private static float GetTurretX(int index) =>
        index < TurretXs.Length ? TurretXs[index] : TurretXs[TurretXs.Length - 1] + (index - TurretXs.Length + 1) * 16f;

    private const float TurretSpacing = 16f;

    private float GetBlobSpawnX(int index)
    {
        int slotCount = Mathf.Max(TurretGoal, BlobBeastGoal);
        int slot = Mathf.FloorToInt((index + 0.5f) * slotCount / BlobBeastGoal);
        return GetTurretX(slot) + TurretSpacing * 0.5f;
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
            float rootY = groundLevel - 0.2f + ((951f - imageCenterY) * scale / 700f);
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
    public int TurretGoal => GameDifficulty.Stage2TurretGoal(artReferences != null ? artReferences.TurretGoal : 6);
    public int BlobBeastGoal => GameDifficulty.Stage2BeastGoal(artReferences != null ? artReferences.BlobBeastGoal : 2);
    public int DestroyedTurrets => destroyedTurrets;
    public int DestroyedBlobBeasts => destroyedBlobBeasts;
    public int LifeLimit => artReferences != null ? Mathf.Max(1, artReferences.LifeLimit) : 5;

    public void OnTurretDestroyed()
    {
        if (finished) return;
        destroyedTurrets++;
        RefreshHud();
        CheckEncounterComplete();
    }

    public void OnBlobBeastDestroyed()
    {
        if (finished) return;
        destroyedBlobBeasts++;
        RefreshHud();
        CheckEncounterComplete();
    }

    private void CheckEncounterComplete()
    {
        if (finished || destroyedTurrets < TurretGoal || destroyedBlobBeasts < BlobBeastGoal) return;
        finished = true;
        GameDifficulty.AdjustAfterCombat(playerDamage, LifeLimit);
        ShowOverlay("All turrets and beasts defeated successfully", true);
        Time.timeScale = 0f;
        overlayText.gameObject.SetActive(false);
        transitionText.gameObject.SetActive(false);
        AiCoachInteraction.Show(overlayCanvas.transform, "Stage 2", Time.unscaledTime - stageStartedAt,
            0, 0, $"The player defeated {destroyedTurrets} of {TurretGoal} turrets and {destroyedBlobBeasts} of {BlobBeastGoal} blob beasts, taking {playerDamage:0.#} hits.",
            FindPlayerInput(), ContinueToStage3);
    }

    public void OnPlayerHit(float damage = 1f)
    {
        if (finished) return;
        playerDamage = Mathf.Min(LifeLimit, playerDamage + Mathf.Max(0f, damage));
        RefreshHud();
        if (playerDamage >= LifeLimit)
        {
            finished = true;
            GameDifficulty.AdjustAfterCombat(playerDamage, LifeLimit);
            ShowOverlay("GAME OVER");
            if (retryButton != null) retryButton.gameObject.SetActive(false);
            Time.timeScale = 0f;
            StartCoroutine(ShowGameOverCoach());
        }
    }

    private IEnumerator ShowGameOverCoach()
    {
        yield return new WaitForSecondsRealtime(1.5f);
        AiCoachInteraction.Show(overlayCanvas.transform, "Stage 2", Time.unscaledTime - stageStartedAt,
            0, 0, $"The player lost after defeating {destroyedTurrets} of {TurretGoal} turrets and {destroyedBlobBeasts} of {BlobBeastGoal} blob beasts, taking {playerDamage:0.#} hits.",
            FindPlayerInput(), null, false);
    }

    private void ContinueToStage3()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Stage3");
    }

    private static PlayerInput FindPlayerInput()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        return player != null ? player.GetComponent<PlayerInput>() : null;
    }

    private void BuildHud()
    {
        Canvas canvas = RuntimeUiUtility.CreateOverlayCanvas(
            "Stage2 HUD", 100, new Vector2(1920f, 1080f));
        RuntimeUiUtility.EnsureEventSystem();
        GameObject canvasGO = canvas.gameObject;
        lifeIcons = new Image[LifeLimit];

        RectTransform hudRect = RuntimeUiUtility.CreateRect("Encounter Status", canvasGO.transform);
        hudRect.anchorMin = new Vector2(1f, 1f);
        hudRect.anchorMax = new Vector2(1f, 1f);
        hudRect.pivot = new Vector2(1f, 1f);
        hudRect.anchoredPosition = new Vector2(-32f, -24f);
        hudRect.sizeDelta = new Vector2(Mathf.Max(1200f, 32f + lifeIcons.Length * 80f), 88f);
        Sprite heartSprite = artReferences != null ? artReferences.Heart : null;
        if (heartSprite == null)
            Debug.LogError("[Stage2TurretEncounter] Heart sprite is not assigned on Stage2ArtReferences.");
        for (int i = 0; i < lifeIcons.Length; i++)
        {
            GameObject heart = new GameObject($"Life {i + 1}", typeof(RectTransform), typeof(Image));
            heart.transform.SetParent(hudRect, false);
            RectTransform heartRect = heart.GetComponent<RectTransform>();
            heartRect.anchorMin = new Vector2(1f, 0.5f);
            heartRect.anchorMax = new Vector2(1f, 0.5f);
            heartRect.pivot = new Vector2(1f, 0.5f);
            heartRect.anchoredPosition = new Vector2(-i * 92f, 0f);
            heartRect.sizeDelta = new Vector2(112f, 112f);
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
            "Result Text", overlayGO.transform, RuntimeUiUtility.GetFont(), 58,
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
        float livesRemaining = Mathf.Max(0f, LifeLimit - playerDamage);
        for (int i = 0; i < lifeIcons.Length; i++)
            if (lifeIcons[i] != null)
            {
                lifeIcons[i].gameObject.SetActive(i < livesRemaining);
                lifeIcons[i].type = Image.Type.Filled;
                lifeIcons[i].fillMethod = Image.FillMethod.Horizontal;
                lifeIcons[i].fillOrigin = (int)Image.OriginHorizontal.Left;
                lifeIcons[i].fillAmount = Mathf.Clamp01(livesRemaining - i);
            }
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
