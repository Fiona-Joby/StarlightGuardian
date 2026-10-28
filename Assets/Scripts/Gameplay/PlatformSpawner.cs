using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Spawns platforms at random intervals ahead of the player as they move
/// through a stage. Platforms use that stage's "Platform Long" and "Platform Short"
/// sprites and are tagged "Platform" for collision detection.
///
/// Platforms are placed at heights reachable by jumping from the ground.
/// Platforms remain in the scene after they are spawned.
/// </summary>
public class PlatformSpawner : MonoBehaviour
{
    [Header("Platform Sprites (auto-loaded in Editor)")]
    [SerializeField] private Sprite[] platformLongSprites;
    [SerializeField] private Sprite platformShortSprite;

    [Header("Spawn Settings")]
    [Tooltip("Minimum horizontal distance between consecutive platforms.")]
    [SerializeField, Min(1f)] private float minSpacing = 8f;
    [Tooltip("Maximum horizontal distance between consecutive platforms.")]
    [SerializeField, Min(1f)] private float maxSpacing = 14f;
    [Tooltip("Minimum Y position for platforms (relative to ground).")]
    [SerializeField] private float minHeight = 0.5f;
    [Tooltip("Maximum Y position for platforms (player jump apex reachable).")]
    [SerializeField] private float maxHeight = 1.8f;
    [Tooltip("How far ahead of the player to pre-spawn platforms.")]
    [SerializeField, Min(5f)] private float spawnAheadDistance = 25f;
    [Tooltip("Platform visual scale multiplier.")]
    [SerializeField, Min(0.1f)] private float platformScale = 0.35f;

    [Header("Ground Reference")]
    [Tooltip("Y position of the ground. Platforms spawn above this.")]
    [SerializeField] private float groundY = -3f;

    private Transform player;
    private float nextSpawnX;
    private bool nextPlatformIsLong;
    private bool hasStarLandingDetectors;
    private int spawnedPlatformCount;

    // Start spawning platforms a short distance ahead of initial player position.
    private const float INITIAL_OFFSET = 8f;

    private void Start()
    {
        hasStarLandingDetectors = SceneManager.GetActiveScene().name != "Stage2";
        // Find the player.
        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
        {
            player = playerGO.transform;
            groundY = player.position.y;
        }
        nextSpawnX = (player != null ? player.position.x : 0f) + INITIAL_OFFSET;

        LoadSpritesInEditor();
    }

    private void Update()
    {
        if (player == null) return;

        // Spawn platforms ahead of the player.
        while (nextSpawnX < player.position.x + spawnAheadDistance)
        {
            SpawnPlatform(nextSpawnX);
            nextSpawnX += Random.Range(minSpacing, maxSpacing);
        }

    }

    private void SpawnPlatform(float x)
    {
        // Randomly pick long or short.
        bool isLong = nextPlatformIsLong;
        nextPlatformIsLong = !nextPlatformIsLong;
        Sprite sprite = isLong && platformLongSprites != null && platformLongSprites.Length > 0
            ? platformLongSprites[Random.Range(0, platformLongSprites.Length)]
            : platformShortSprite;

        if (sprite == null)
        {
            Debug.LogWarning("[PlatformSpawner] Platform sprite is null — skipping spawn.");
            return;
        }

        // Random height above ground, reachable by jump.
        float y = groundY + Random.Range(minHeight, maxHeight);

        GameObject platformGO = new GameObject(isLong ? "Platform_Long" : "Platform_Short");
        platformGO.tag = "Platform";
        platformGO.transform.position = new Vector3(x, y, 0f);
        platformGO.transform.localScale = new Vector3(platformScale, platformScale, 1f);

        // Make every third platform a gentle side-to-side moving platform.
        if (spawnedPlatformCount % 3 == 1)
            platformGO.AddComponent<PlatformMover>();
        spawnedPlatformCount++;

        // Visual sprite.
        SpriteRenderer sr = platformGO.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 9; // Above scenery and ground, just behind the player (10).
        sr.color = Color.white;

        // Collider sized to the sprite, used for both player landing and star landing.
        // Use a BoxCollider2D that tightly fits the visible platform top surface.
        BoxCollider2D col = platformGO.AddComponent<BoxCollider2D>();
        col.isTrigger = false; // Solid for physics queries.

        // Adjust the collider to just the top portion of the platform sprite
        // so the player can jump through from below.
        // The sprite has the platform bar roughly in the vertical center.
        Vector2 spriteSize = sprite.bounds.size;
        if (hasStarLandingDetectors)
        {
            col.size = new Vector2(spriteSize.x * 0.95f, spriteSize.y * 0.25f);
            col.offset = new Vector2(0f, spriteSize.y * 0.15f);
        }
        else
        {
            // Stage 2's platform art is trimmed to its visible strip and uses
            // a centered pivot. Put the collider's top edge at the sprite top.
            col.size = new Vector2(spriteSize.x * 0.95f, spriteSize.y * 0.15f);
            col.offset = new Vector2(0f, spriteSize.y * 0.425f);
        }

        if (!hasStarLandingDetectors)
            return;

        // Add the platform landing detector (trigger) for stars.
        GameObject detectorGO = new GameObject("PlatformDetector");
        detectorGO.transform.SetParent(platformGO.transform, false);
        detectorGO.transform.localPosition = new Vector3(0f, spriteSize.y * 0.15f + spriteSize.y * 0.125f, 0f);

        BoxCollider2D detectorCol = detectorGO.AddComponent<BoxCollider2D>();
        detectorCol.isTrigger = true;
        detectorCol.size = new Vector2(spriteSize.x * 0.95f, 0.15f);

        Rigidbody2D detectorRB = detectorGO.AddComponent<Rigidbody2D>();
        detectorRB.bodyType = RigidbodyType2D.Kinematic;
        detectorRB.gravityScale = 0f;

        detectorGO.AddComponent<GroundDetector>();
    }

    // ------------------------------------------------------------------ //
    //  Editor-only sprite loading
    // ------------------------------------------------------------------ //

    private void LoadSpritesInEditor()
    {
#if UNITY_EDITOR
        bool isStage2 = SceneManager.GetActiveScene().name == "Stage2";
        string longSpritePath = isStage2
            ? "Assets/Art/Backgrounds/Stage2/PlatformLong.png"
            : "Assets/Art/Backgrounds/Stage1/PlatformLong.png";
        string shortSpritePath = isStage2
            ? "Assets/Art/Backgrounds/Stage2/PlatformShort.png"
            : "Assets/Art/Backgrounds/Stage1/PlatformShort.png";

        if (platformLongSprites == null || platformLongSprites.Length == 0)
        {
            Object[] longAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(
                longSpritePath);
            var loadedLongSprites = new System.Collections.Generic.List<Sprite>();
            foreach (Object asset in longAssets)
                if (asset is Sprite sprite && (isStage2 || sprite.name == "PlatformLong_1"))
                    loadedLongSprites.Add(sprite);
            platformLongSprites = loadedLongSprites.ToArray();
        }
        if (platformShortSprite == null)
        {
            if (isStage2)
                platformShortSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(shortSpritePath);
            else
                foreach (Object asset in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(shortSpritePath))
                    if (asset is Sprite sprite && sprite.name == "PlatformShort_0")
                    {
                        platformShortSprite = sprite;
                        break;
                    }
        }

        Debug.Log($"[PlatformSpawner] Sprites loaded — Long:{platformLongSprites.Length} Short:{platformShortSprite != null}");
#else
        if (platformLongSprites == null || platformLongSprites.Length == 0 || platformShortSprite == null)
            Debug.LogWarning("[PlatformSpawner] Platform sprites must be assigned in the Inspector.");
#endif
    }
}
