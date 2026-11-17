using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Runs Stage 3's drone encounter and returns to the main menu on completion.</summary>
public class Stage3DroneEncounter : MonoBehaviour
{
    private static readonly float[] DroneXs = { 18f, 34f, 50f, 66f, 82f };
    // Lanes are centered in the gaps between the drone patrol ranges.
    private static readonly float[] SlimeCenters = { 10f, 26f, 42f, 58f, 74f, 90f };
    private const float SlimePatrolHalfWidth = 2f;
    private const float DronePatrolRadius = 2.2f;
    private const float HardBeastSpacing = 16f;
    private const float SpawnWalkDistance = 4f;

    private Stage3ArtReferences artReferences;
    private int destroyedDrones;
    private int destroyedSlimes;
    private int destroyedHardBeasts;
    private int playerHits;
    private bool finished;
    private bool enemiesDefeated;
    private bool lootBoxLanded;
    private bool lootBoxOpened;
    private Transform lootBoxTransform;
    private SpriteRenderer lootBoxRenderer;
    private PlayerInput playerInput;
    private StarSpiritCompanion companion;
    private Text statusText;
    private Image[] lifeIcons;
    private Canvas overlayCanvas;
    private Text resultText;
    private Text transitionText;
    private Button retryButton;
    private float stageStartedAt;

    public bool IsFinished => finished;
    public int DroneGoal => GameDifficulty.Stage3DroneGoal(artReferences != null ? artReferences.DroneGoal : 5);
    public int SlimeGoal => GameDifficulty.Stage3SlimeGoal(artReferences != null ? artReferences.SlimeGoal : 4);
    public int HardBeastGoal => GameDifficulty.Stage3HardBeastGoal(artReferences != null ? artReferences.HardBeastGoal : 2);
    public string ObjectiveProgress =>
        $"Drones: {destroyedDrones}/{DroneGoal}\nSlimes: {destroyedSlimes}/{SlimeGoal}\nHard Beasts: {destroyedHardBeasts}/{HardBeastGoal}";
    public int LifeLimit => artReferences != null ? Mathf.Max(1, artReferences.LifeLimit) : 5;

    private void Start()
    {
        stageStartedAt = Time.unscaledTime;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("[Stage3DroneEncounter] No Player tagged object found.");
            return;
        }

        artReferences = FindAnyObjectByType<Stage3ArtReferences>();
        playerInput = player.GetComponent<PlayerInput>();
        companion = StarSpiritCompanion.Attach(player.transform,
            artReferences != null ? artReferences.CompanionMoveFrames : null);
        if (player.GetComponent<Stage2Shooter>() == null)
            player.AddComponent<Stage2Shooter>();
        if (player.GetComponent<Stage2PlayerHealth>() == null)
            player.AddComponent<Stage2PlayerHealth>();

        BuildHud();
        RefreshHud();
        StartCoroutine(SpawnAfterPlayerWalks(player.transform));
        SpawnSlimes();
        SpawnHardBeasts();
    }

    private void Update()
    {
        if (!finished && enemiesDefeated && lootBoxLanded && !lootBoxOpened &&
            playerInput != null && playerInput.FinalPosePressed)
            OpenLootBox();
    }

    private void SpawnSlimes()
    {
        Sprite[] frames = artReferences != null ? artReferences.SlimeWalkFrames : null;
        Sprite attack = artReferences != null ? artReferences.SlimeAttack : null;
        if (frames == null || frames.Length < 4 || System.Array.Exists(frames, frame => frame == null) || attack == null)
        {
            Debug.LogError("[Stage3DroneEncounter] Slime sprites are not assigned on Stage3 Main Camera.");
            return;
        }

        for (int i = 0; i < SlimeGoal; i++)
        {
            float center = i < SlimeCenters.Length
                ? SlimeCenters[i]
                : SlimeCenters[SlimeCenters.Length - 1] + (i - SlimeCenters.Length + 1) * 16f;
            GameObject slime = new GameObject("Ground Slime");
            slime.transform.position = new Vector3(center, -4.25f, 0f);
            GameObject art = new GameObject("Slime Art");
            art.transform.SetParent(slime.transform, false);
            art.transform.localScale = Vector3.one * 0.19f;
            SpriteRenderer renderer = art.AddComponent<SpriteRenderer>();
            renderer.sprite = frames[0];
            renderer.sortingOrder = 20;
            BoxCollider2D collider = slime.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(1.45f, 0.9f);
            slime.AddComponent<Stage3Slime>().Initialize(this, frames, attack,
                center - SlimePatrolHalfWidth, center + SlimePatrolHalfWidth);
        }
    }

    private void SpawnHardBeasts()
    {
        Sprite[] frames = artReferences != null ? artReferences.HardBeastWalkFrames : null;
        Sprite jump = artReferences != null ? artReferences.HardBeastJump : null;
        Sprite stomp = artReferences != null ? artReferences.HardBeastStomp : null;
        if (frames == null || frames.Length < 5 || System.Array.Exists(frames, frame => frame == null) ||
            jump == null || stomp == null)
        {
            Debug.LogError("[Stage3DroneEncounter] Hard Blob Beast sprites are not assigned on Stage3 Main Camera.");
            return;
        }

        GameObject ground = GameObject.Find("Ground");
        Collider2D groundCollider = ground != null ? ground.GetComponent<Collider2D>() : null;
        float groundTop = groundCollider != null ? groundCollider.bounds.max.y : -4.5f;
        for (int i = 0; i < HardBeastGoal; i++)
        {
            float x = GetHardBeastSpawnX(i);
            GameObject beast = new GameObject("Hard Blob Beast");
            beast.transform.position = new Vector3(x, groundTop, 0f);
            GameObject art = new GameObject("Hard Blob Beast Art");
            art.transform.SetParent(beast.transform, false);
            art.transform.localPosition = new Vector3(-1f, 0f, 0f);
            art.transform.localScale = Vector3.one * 0.24f;
            SpriteRenderer renderer = art.AddComponent<SpriteRenderer>();
            renderer.sprite = frames[0];
            renderer.sortingOrder = 20;
            Vector3 artPosition = art.transform.localPosition;
            artPosition.y += groundTop - renderer.bounds.min.y - 0.12f;
            art.transform.localPosition = artPosition;
            BoxCollider2D collider = beast.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(1.9f, 2.2f);
            collider.offset = new Vector2(0f, 1.1f);
            beast.AddComponent<Stage2BlobBeast>().Initialize(
                this, frames, jump, stomp, i % 2 == 1,
                artReferences != null ? artReferences.HardBeastAI : null);
        }
    }

    private static float GetDroneX(int index) => index < DroneXs.Length
        ? DroneXs[index]
        : DroneXs[DroneXs.Length - 1] + (index - DroneXs.Length + 1) * 16f;

    private static float GetSlimeCenter(int index) => index < SlimeCenters.Length
        ? SlimeCenters[index]
        : SlimeCenters[SlimeCenters.Length - 1] + (index - SlimeCenters.Length + 1) * 16f;

    private float GetHardBeastSpawnX(int index)
    {
        float droneRight = GetDroneX(DroneGoal - 1) + DronePatrolRadius + 0.5625f;
        float slimeRight = GetSlimeCenter(SlimeGoal - 1) + SlimePatrolHalfWidth + 0.725f;
        // The hard beasts occupy a later, separate section of the stage. Each gets
        // a full 16-unit lane so its patrol and leap area stays clear of the drones,
        // slimes, and the other hard beasts.
        return Mathf.Max(droneRight, slimeRight) + 8f + index * HardBeastSpacing;
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
        CheckObjectivesComplete();
    }

    public void OnSlimeDestroyed()
    {
        if (finished) return;
        destroyedSlimes++;
        RefreshHud();
        CheckObjectivesComplete();
    }

    public void OnHardBeastDestroyed()
    {
        if (finished) return;
        destroyedHardBeasts++;
        RefreshHud();
        CheckObjectivesComplete();
    }

    private void CheckObjectivesComplete()
    {
        if (enemiesDefeated || destroyedDrones < DroneGoal || destroyedSlimes < SlimeGoal ||
            destroyedHardBeasts < HardBeastGoal) return;
        enemiesDefeated = true;
        StartCoroutine(DropLootBox());
    }

    private IEnumerator DropLootBox()
    {
        if (artReferences == null || artReferences.LootBoxWithShield == null || artReferences.LootBox == null ||
            artReferences.StarSpiritMoveFrames == null || artReferences.StarSpiritMoveFrames.Length < 2 ||
            artReferences.StarSpiritMoveFrames[0] == null || artReferences.StarSpiritMoveFrames[1] == null ||
            artReferences.StarSpiritFinalPose == null)
        {
            Debug.LogError("[Stage3DroneEncounter] Loot box or star spirit sprites are not assigned on Stage3 Main Camera.");
            yield break;
        }

        float lastDroneX = DroneGoal <= DroneXs.Length
            ? DroneXs[Mathf.Max(0, DroneGoal - 1)]
            : DroneXs[DroneXs.Length - 1] + (DroneGoal - DroneXs.Length) * 16f;
        float lastSlimeX = SlimeGoal <= SlimeCenters.Length
            ? SlimeCenters[Mathf.Max(0, SlimeGoal - 1)]
            : SlimeCenters[SlimeCenters.Length - 1] + (SlimeGoal - SlimeCenters.Length) * 16f;
        float lastHardBeastX = GetHardBeastSpawnX(HardBeastGoal - 1);
        float boxX = Mathf.Max(Mathf.Max(lastDroneX, lastSlimeX), lastHardBeastX) + 8f;

        GameObject box = new GameObject("Shielded Star Loot Box");
        lootBoxTransform = box.transform;
        lootBoxRenderer = box.AddComponent<SpriteRenderer>();
        lootBoxRenderer.sprite = artReferences.LootBoxWithShield;
        lootBoxRenderer.sortingOrder = 8;
        lootBoxTransform.localScale = Vector3.one * 0.18f;
        GameObject ground = GameObject.Find("Ground");
        Collider2D groundCollider = ground != null ? ground.GetComponent<Collider2D>() : null;
        float groundTop = groundCollider != null ? groundCollider.bounds.max.y : -4.5f;
        float groundedY = groundTop + lootBoxRenderer.bounds.extents.y - 0.08f;
        lootBoxTransform.position = new Vector3(boxX, groundedY + 6f, 0f);

        while (lootBoxTransform.position.y > groundedY)
        {
            Vector3 position = lootBoxTransform.position;
            position.y = Mathf.MoveTowards(position.y, groundedY, 4f * Time.deltaTime);
            lootBoxTransform.position = position;
            yield return null;
        }
        lootBoxLanded = true;
    }

    private void OpenLootBox()
    {
        lootBoxOpened = true;
        lootBoxRenderer.sprite = artReferences.LootBox;
        StartCoroutine(ReleaseStarSpirit());
    }

    private IEnumerator ReleaseStarSpirit()
    {
        GameObject spirit = new GameObject("Rescued Star Spirit");
        SpriteRenderer renderer = spirit.AddComponent<SpriteRenderer>();
        renderer.sprite = artReferences.StarSpiritMoveFrames[0];
        renderer.sortingOrder = 13;
        spirit.transform.localScale = Vector3.one * 0.15f;
        spirit.transform.position = lootBoxTransform.position + Vector3.up * (lootBoxRenderer.bounds.extents.y + 0.25f);
        Vector3 start = spirit.transform.position;
        Vector3 finish = start + Vector3.up * 2.5f;
        float elapsed = 0f;
        const float floatDuration = 2f;
        while (elapsed < floatDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / floatDuration);
            spirit.transform.position = Vector3.Lerp(start, finish, t);
            renderer.sprite = artReferences.StarSpiritMoveFrames[Mathf.FloorToInt(elapsed / 0.18f) % 2];
            yield return null;
        }

        if (companion != null && artReferences.CompanionReunionPose != null)
            yield return companion.ReuniteWith(spirit.transform,
                artReferences.CompanionReunionPose, artReferences.StarSpiritFinalPose);
        else
            renderer.sprite = artReferences.StarSpiritFinalPose;
        yield return new WaitForSeconds(2f);
        finished = true;
        GameDifficulty.AdjustAfterCombat(playerHits, LifeLimit);
        ShowResult("Star rescued!", "Stages Completed!", false);
        Time.timeScale = 0f;
        resultText.gameObject.SetActive(false);
        transitionText.gameObject.SetActive(false);
        AiCoachInteraction.Show(overlayCanvas.transform, "Stage 3", Time.unscaledTime - stageStartedAt,
            0, 0, $"The player rescued the star after defeating {destroyedDrones} drones, {destroyedSlimes} slimes, and {destroyedHardBeasts} hard beasts, taking {playerHits} hits.",
            playerInput, ReturnToMainMenu);
    }

    public void OnPlayerHit()
    {
        if (finished) return;
        playerHits++;
        RefreshHud();
        if (playerHits < LifeLimit) return;

        finished = true;
        GameDifficulty.AdjustAfterCombat(playerHits, LifeLimit);
        ShowResult("GAME OVER", string.Empty, true);
        Time.timeScale = 0f;
    }

    private void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    private void BuildHud()
    {
        Canvas canvas = RuntimeUiUtility.CreateOverlayCanvas("Stage3 HUD", 100, new Vector2(1920f, 1080f));
        RuntimeUiUtility.EnsureEventSystem();
        lifeIcons = new Image[LifeLimit];
        RectTransform hud = RuntimeUiUtility.CreateRect("Encounter Status", canvas.transform);
        hud.anchorMin = hud.anchorMax = new Vector2(1f, 1f);
        hud.pivot = new Vector2(1f, 1f);
        hud.anchoredPosition = new Vector2(-32f, -24f);
        hud.sizeDelta = new Vector2(720f, 88f);
        Sprite heart = artReferences != null ? artReferences.Heart : null;
        for (int i = 0; i < lifeIcons.Length; i++)
        {
            GameObject icon = new GameObject($"Life {i + 1}", typeof(RectTransform), typeof(Image));
            icon.transform.SetParent(hud, false);
            RectTransform rect = icon.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-i * 92f, 0f);
            rect.sizeDelta = new Vector2(112f, 112f);
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
        resultText = RuntimeUiUtility.CreateText("Result Text", overlay, RuntimeUiUtility.GetFont(), 58,
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
