using System.Collections;
using System.Collections.Generic;
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
    private readonly HashSet<EntityId> creditedHardBeasts = new HashSet<EntityId>();
    private int droneGoalAtStart;
    private int slimeGoalAtStart;
    private int hardBeastGoalAtStart;
    private int playerHits;
    private bool finished;
    private bool enemiesDefeated;
    private bool lootBoxLanded;
    private bool lootBoxOpened;
    private bool restorePromptVisible;
    private bool completionFeedbackShown;
    private bool lootBoxReleaseAuthorized;
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
    private RectTransform restorePrompt;
    private Text restorePromptText;
    private Image whiteFade;
    private GameObject reunitedCompanion;
    private GameObject reunitedFriend;
    private float finalEnemyRightBoundary;
    private float lootBoxGroundY;
    private float stageStartedAt;
    private int playedDifficulty;
    private bool automaticDifficulty;

    public bool IsFinished => finished;
    public bool HasLootBoxPlacement => lootBoxLanded;
    public float FinalEnemyRightBoundary => finalEnemyRightBoundary;
    public float LootBoxX => lootBoxTransform != null ? lootBoxTransform.position.x : 0f;
    public int DroneGoal => droneGoalAtStart > 0 ? droneGoalAtStart : GameDifficulty.Stage3DroneGoal(artReferences != null ? artReferences.DroneGoal : 5);
    public int SlimeGoal => slimeGoalAtStart > 0 ? slimeGoalAtStart : GameDifficulty.Stage3SlimeGoal(artReferences != null ? artReferences.SlimeGoal : 4);
    public int HardBeastGoal => hardBeastGoalAtStart > 0 ? hardBeastGoalAtStart : GameDifficulty.Stage3HardBeastGoal(artReferences != null ? artReferences.HardBeastGoal : 2);
    public int DestroyedDrones => destroyedDrones;
    public int DestroyedSlimes => destroyedSlimes;
    public int DestroyedHardBeasts => destroyedHardBeasts;
    public string ObjectiveProgress =>
        $"Drones: {destroyedDrones}/{DroneGoal}\nSlimes: {destroyedSlimes}/{SlimeGoal}\nHard Beasts: {destroyedHardBeasts}/{HardBeastGoal}";
    public int LifeLimit => artReferences != null ? Mathf.Max(1, artReferences.LifeLimit) : 5;

    private IEnumerator Start()
    {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(0.2f);
        bool skipEntryStory = StageRestartState.ConsumeEntryStorySkip("Stage3");
        if (!skipEntryStory) yield return StoryArc.PlayStage3Entry();
        StageQuitButton pauseUi = FindAnyObjectByType<StageQuitButton>();
        if (pauseUi != null) pauseUi.ShowStage3ObjectivesAfterStory();
        stageStartedAt = Time.unscaledTime;
        playedDifficulty = GameDifficulty.Level;
        automaticDifficulty = GameDifficulty.IsAutomatic;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("[Stage3DroneEncounter] No Player tagged object found.");
            yield break;
        }

        artReferences = FindAnyObjectByType<Stage3ArtReferences>();
        droneGoalAtStart = GameDifficulty.Stage3DroneGoal(artReferences != null ? artReferences.DroneGoal : 5);
        slimeGoalAtStart = GameDifficulty.Stage3SlimeGoal(artReferences != null ? artReferences.SlimeGoal : 4);
        hardBeastGoalAtStart = GameDifficulty.Stage3HardBeastGoal(artReferences != null ? artReferences.HardBeastGoal : 2);
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
        if (finished || playerInput == null) return;
        if (enemiesDefeated && lootBoxLanded && lootBoxRenderer != null)
        {
            lootBoxRenderer.sprite = artReferences.LootBox;
            Vector3 boxPosition = lootBoxTransform.position;
            boxPosition.y = lootBoxGroundY + lootBoxRenderer.bounds.extents.y;
            lootBoxTransform.position = boxPosition;
        }
        if (enemiesDefeated && lootBoxReleaseAuthorized && lootBoxLanded && !lootBoxOpened &&
            Vector2.Distance(playerInput.transform.position, lootBoxTransform.position) < 2f)
            OpenLootBox();
        if (restorePromptVisible && playerInput.FinalPosePressed)
        {
            restorePromptVisible = false;
            restorePrompt.gameObject.SetActive(false);
            StartCoroutine(RestoreConstellation());
        }
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
            renderer.sortingOrder = 20;
            BoxCollider2D collider = drone.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(1.125f, 0.9375f);
            drone.AddComponent<Stage3Drone>().Initialize(this, frames, beam, drone.transform.position);
            if (i == DroneGoal - 1) PlaceLootBox();
        }
    }

    public void OnDroneDestroyed()
    {
        if (finished || destroyedDrones >= DroneGoal) return;
        destroyedDrones++;
        RefreshHud();
        CheckObjectivesComplete();
    }

    public void OnSlimeDestroyed()
    {
        if (finished || destroyedSlimes >= SlimeGoal) return;
        destroyedSlimes++;
        RefreshHud();
        CheckObjectivesComplete();
    }

    public void OnHardBeastDestroyed(EntityId beastEntityId)
    {
        if (finished || destroyedHardBeasts >= HardBeastGoal || !creditedHardBeasts.Add(beastEntityId)) return;
        destroyedHardBeasts++;
        RefreshHud();
        CheckObjectivesComplete();
    }

    private void CheckObjectivesComplete()
    {
        if (enemiesDefeated || destroyedDrones < DroneGoal || destroyedSlimes < SlimeGoal ||
            destroyedHardBeasts < HardBeastGoal) return;
        enemiesDefeated = true;
        if (lootBoxRenderer != null) lootBoxRenderer.sprite = artReferences.LootBox;
        if (completionFeedbackShown) return;
        completionFeedbackShown = true;
        Time.timeScale = 0f;
        AiCoachInteraction.Show(overlayCanvas.transform, "Stage 3", Time.unscaledTime - stageStartedAt,
            0, 0, $"The player defeated {destroyedDrones}/{DroneGoal} drones, {destroyedSlimes}/{SlimeGoal} slimes, and {destroyedHardBeasts}/{HardBeastGoal}, taking {playerHits} hits.",
            playerInput, ContinueAfterCombatFeedback, true, playedDifficulty, automaticDifficulty);
    }

    private void ContinueAfterCombatFeedback()
    {
        StoryArc.ShowStage3ChestLine(() =>
        {
            lootBoxReleaseAuthorized = true;
            Time.timeScale = 1f;
        });
    }

    private void PlaceLootBox()
    {
        if (artReferences == null || artReferences.LootBoxWithShield == null || artReferences.LootBox == null ||
            artReferences.StarSpiritMoveFrames == null || artReferences.StarSpiritMoveFrames.Length < 2 ||
            artReferences.StarSpiritMoveFrames[0] == null || artReferences.StarSpiritMoveFrames[1] == null ||
            artReferences.StarSpiritFinalPose == null)
        {
            Debug.LogError("[Stage3DroneEncounter] Loot box or star spirit sprites are not assigned on Stage3 Main Camera.");
            return;
        }

        float lastDroneX = DroneGoal <= DroneXs.Length
            ? DroneXs[Mathf.Max(0, DroneGoal - 1)]
            : DroneXs[DroneXs.Length - 1] + (DroneGoal - DroneXs.Length) * 16f;
        float lastSlimeX = SlimeGoal <= SlimeCenters.Length
            ? SlimeCenters[Mathf.Max(0, SlimeGoal - 1)]
            : SlimeCenters[SlimeCenters.Length - 1] + (SlimeGoal - SlimeCenters.Length) * 16f;
        float lastHardBeastX = GetHardBeastSpawnX(HardBeastGoal - 1);
        Stage2BlobBeast.AISettings beastAI = artReferences.HardBeastAI;
        float beastRightPatrolOffset = beastAI != null
            ? Mathf.Max(beastAI.patrolLeftOffset, beastAI.patrolRightOffset)
            : 2.5f;
        finalEnemyRightBoundary = Mathf.Max(Mathf.Max(lastDroneX + DronePatrolRadius + 0.5625f,
                lastSlimeX + SlimePatrolHalfWidth + 0.725f),
            lastHardBeastX + beastRightPatrolOffset + 0.95f);

        GameObject box = new GameObject("Shielded Star Loot Box");
        lootBoxTransform = box.transform;
        lootBoxRenderer = box.AddComponent<SpriteRenderer>();
        lootBoxRenderer.sprite = artReferences.LootBoxWithShield;
        lootBoxRenderer.sortingOrder = 8;
        lootBoxTransform.localScale = Vector3.one * 0.24f;
        float boxX = finalEnemyRightBoundary + lootBoxRenderer.bounds.extents.x + 0.12f;
        GameObject ground = GameObject.Find("Ground");
        Collider2D groundCollider = ground != null ? ground.GetComponent<Collider2D>() : null;
        float groundTop = groundCollider != null ? groundCollider.bounds.max.y : -4.5f;
        lootBoxGroundY = groundTop;
        float groundedY = groundTop + lootBoxRenderer.bounds.extents.y;
        lootBoxTransform.position = new Vector3(boxX, groundedY, 0f);
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

        CreateStage3Reunion(spirit.transform);
        GameObject pairedCompanion = GameObject.Find("Reunited Star Spirit Companion");
        GameObject pairedFriend = GameObject.Find("Reunited Freed Star Spirit");
        if (pairedCompanion != null && pairedFriend != null)
            yield return FloatTogether(pairedCompanion.transform, pairedFriend.transform);
        bool storyDone = false;
        StoryArc.ShowStage3Reunion(() => storyDone = true);
        while (!storyDone) yield return null;
        Time.timeScale = 1f;
        ShowRestorePrompt();
        while (!finished && restorePromptVisible) yield return null;
    }

    private void CreateStage3Reunion(Transform freedSpirit)
    {
        if (freedSpirit == null) return;
        Vector3 center = companion != null
            ? (companion.transform.position + freedSpirit.position) * 0.5f
            : freedSpirit.position;
        if (companion != null) Destroy(companion.gameObject);
        SpriteRenderer freedRenderer = freedSpirit.GetComponent<SpriteRenderer>();
        if (freedRenderer != null) freedRenderer.enabled = false;
        CreateStage3Spirit("Reunited Star Spirit Companion", artReferences.CompanionReunionPose,
            center + Vector3.left * 0.95f, false);
        CreateStage3Spirit("Reunited Freed Star Spirit", artReferences.StarSpiritFinalPose,
            center + Vector3.right * 0.95f, true);
    }

    private static void CreateStage3Spirit(string name, Sprite pose, Vector3 position, bool flipX)
    {
        GameObject spirit = new GameObject(name);
        spirit.transform.position = position;
        spirit.transform.localScale = Vector3.one * 0.15f;
        SpriteRenderer renderer = spirit.AddComponent<SpriteRenderer>();
        renderer.sprite = pose;
        renderer.flipX = flipX;
        renderer.sortingOrder = 15;
    }

    private static IEnumerator FloatTogether(Transform first, Transform second)
    {
        Vector3 firstStart = first.position;
        Vector3 secondStart = second.position;
        const float duration = 1.5f;
        float elapsed = 0f;
        while (elapsed < duration && first != null && second != null)
        {
            elapsed += Time.deltaTime;
            Vector3 rise = Vector3.up * (1.25f * Mathf.Clamp01(elapsed / duration));
            first.position = firstStart + rise;
            second.position = secondStart + rise;
            yield return null;
        }
    }

    private void ShowRestorePrompt()
    {
        int language = Mathf.Clamp(PlayerPrefs.GetInt("Language", 0), 0, 3);
        bool gestures = PlayerPrefs.GetInt("GestureControls", 0) == 1;
        string[,] copy =
        {
            { "Hold your palms together to restore the Constellation.", "Type 'STAR' to restore the Constellation." },
            { "നക്ഷത്രരാശി പുനഃസ്ഥാപിക്കാൻ കൈപ്പത്തികൾ ചേർത്തുപിടിക്കുക.", "നക്ഷത്രരാശി പുനഃസ്ഥാപിക്കാൻ 'STAR' എന്ന് ടൈപ്പ് ചെയ്യുക." },
            { "Satukan telapak tangan untuk memulihkan Rasi Bintang.", "Ketik 'STAR' untuk memulihkan Rasi Bintang." },
            { "合起雙掌以修復星座。", "輸入「STAR」以修復星座。" }
        };
        restorePrompt = RuntimeUiUtility.CreateRect("Restore Constellation Prompt", overlayCanvas.transform);
        restorePrompt.sizeDelta = new Vector2(960f, 300f);
        RawImage box = restorePrompt.gameObject.AddComponent<RawImage>();
        box.texture = Resources.Load<Texture2D>("StageUI/UI Box");
        box.uvRect = new Rect(566f / 1920f, 1f - 901f / 1080f,
            (1331f - 566f) / 1920f, (901f - 176f) / 1080f);
        restorePromptText = RuntimeUiUtility.CreateText("Prompt Text", restorePrompt, RuntimeUiUtility.GetFont(),
            language == 1 ? 38 : language == 3 ? 48 : 46, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
        restorePromptText.rectTransform.offsetMin = new Vector2(100f, 45f);
        restorePromptText.rectTransform.offsetMax = new Vector2(-100f, -45f);
        restorePromptText.text = copy[language, gestures ? 0 : 1];
        restorePromptText.font = Font.CreateDynamicFontFromOSFont(
            language == 1 ? "Nirmala UI" : language == 3 ? "Microsoft YaHei" : "Arial", restorePromptText.fontSize);
        restorePromptVisible = true;
    }

    private IEnumerator RestoreConstellation()
    {
        reunitedCompanion = GameObject.Find("Reunited Star Spirit Companion");
        reunitedFriend = GameObject.Find("Reunited Freed Star Spirit");
        if (reunitedCompanion == null || reunitedFriend == null) yield break;
        Time.timeScale = 0f;
        whiteFade = RuntimeUiUtility.CreateRect("Constellation White Fade", overlayCanvas.transform).gameObject.AddComponent<Image>();
        RectTransform fadeRect = whiteFade.rectTransform;
        fadeRect.anchorMin = Vector2.zero;
        fadeRect.anchorMax = Vector2.one;
        fadeRect.offsetMin = Vector2.zero;
        fadeRect.offsetMax = Vector2.zero;
        whiteFade.color = new Color(1f, 1f, 1f, 0f);
        whiteFade.raycastTarget = false;
        whiteFade.transform.SetAsLastSibling();
        Vector3 firstStart = reunitedCompanion.transform.position;
        Vector3 secondStart = reunitedFriend.transform.position;
        Vector3 spiritCenter = (firstStart + secondStart) * 0.5f;
        Vector3 firstEnd = spiritCenter + Vector3.left * 0.4f + Vector3.up * 2.2f;
        Vector3 secondEnd = spiritCenter + Vector3.right * 0.4f + Vector3.up * 2.2f;
        completionFeedbackShown = true;
        StoryArc.ShowEnding(ReturnToMainMenu);
        const float fadeDuration = 3f;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            float eased = t * t * (3f - 2f * t);
            if (reunitedCompanion != null) reunitedCompanion.transform.position = Vector3.Lerp(firstStart, firstEnd, eased);
            if (reunitedFriend != null) reunitedFriend.transform.position = Vector3.Lerp(secondStart, secondEnd, eased);
            whiteFade.color = new Color(1f, 1f, 1f, t);
            yield return null;
        }
        whiteFade.color = Color.white;
        GameObject shieldHud = GameObject.Find("Shield HUD");
        if (shieldHud != null) shieldHud.SetActive(false);
        finished = true;
        GameDifficulty.AdjustAfterCombat(playerHits, LifeLimit);
        StoryArc.SetEndingFadeComplete();
    }

    public void OnPlayerHit()
    {
        if (finished) return;
        playerHits++;
        RefreshHud();
        if (playerHits < LifeLimit) return;

        finished = true;
        GameDifficulty.AdjustAfterCombat(playerHits, LifeLimit);
        ShowResult("GAME OVER", string.Empty, false);
        Time.timeScale = 0f;
        StartCoroutine(ShowGameOverCoach());
    }

    private IEnumerator ShowGameOverCoach()
    {
        yield return new WaitForSecondsRealtime(1.5f);
        AiCoachInteraction.Show(overlayCanvas.transform, "Stage 3", Time.unscaledTime - stageStartedAt,
            0, 0, $"The player lost after defeating {destroyedDrones}/{DroneGoal} drones, {destroyedSlimes}/{SlimeGoal} slimes, and {destroyedHardBeasts}/{HardBeastGoal} hard beasts, taking {playerHits} hits.",
            playerInput, null, false, playedDifficulty, automaticDifficulty);
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
        StageRestartState.MarkCurrentStageRestart();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
