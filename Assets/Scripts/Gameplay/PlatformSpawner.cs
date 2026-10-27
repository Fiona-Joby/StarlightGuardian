using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawns platforms at random intervals ahead of the player as they move
/// through Stage 1. Platforms use the "Platform Long" and "Platform Short"
/// sprites and are tagged "Platform" for collision detection.
///
/// Platforms are placed at heights reachable by jumping from the ground.
/// Old platforms that are far behind the camera are recycled.
/// </summary>
public class PlatformSpawner : MonoBehaviour
{
    [Header("Platform Sprites (auto-loaded in Editor)")]
    [SerializeField] private Sprite platformLongSprite;
    [SerializeField] private Sprite platformShortSprite;

    [Header("Spawn Settings")]
    [Tooltip("Minimum horizontal distance between consecutive platforms.")]
    [SerializeField, Min(1f)] private float minSpacing = 6f;
    [Tooltip("Maximum horizontal distance between consecutive platforms.")]
    [SerializeField, Min(1f)] private float maxSpacing = 12f;
    [Tooltip("Minimum Y position for platforms (relative to ground).")]
    [SerializeField] private float minHeight = 0.5f;
    [Tooltip("Maximum Y position for platforms (player jump apex reachable).")]
    [SerializeField] private float maxHeight = 1.8f;
    [Tooltip("How far ahead of the player to pre-spawn platforms.")]
    [SerializeField, Min(5f)] private float spawnAheadDistance = 25f;
    [Tooltip("How far behind the camera before a platform is destroyed.")]
    [SerializeField, Min(5f)] private float despawnBehindDistance = 20f;
    [Tooltip("Platform visual scale multiplier.")]
    [SerializeField, Min(0.1f)] private float platformScale = 0.35f;

    [Header("Ground Reference")]
    [Tooltip("Y position of the ground. Platforms spawn above this.")]
    [SerializeField] private float groundY = -3f;

    private Transform player;
    private float nextSpawnX;
    private readonly List<GameObject> activePlatforms = new List<GameObject>();

    // Start spawning platforms a short distance ahead of initial player position.
    private const float INITIAL_OFFSET = 8f;

    private void Start()
    {
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

        // Despawn platforms far behind.
        for (int i = activePlatforms.Count - 1; i >= 0; i--)
        {
            if (activePlatforms[i] == null)
            {
                activePlatforms.RemoveAt(i);
                continue;
            }
            if (activePlatforms[i].transform.position.x < player.position.x - despawnBehindDistance)
            {
                Destroy(activePlatforms[i]);
                activePlatforms.RemoveAt(i);
            }
        }
    }

    private void SpawnPlatform(float x)
    {
        // Randomly pick long or short.
        bool isLong = Random.value > 0.5f;
        Sprite sprite = isLong ? platformLongSprite : platformShortSprite;

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

        // Visual sprite.
        SpriteRenderer sr = platformGO.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 4; // Above background (-10), below ground (5) and player (10).
        sr.color = Color.white;

        // Collider sized to the sprite, used for both player landing and star landing.
        // Use a BoxCollider2D that tightly fits the visible platform top surface.
        BoxCollider2D col = platformGO.AddComponent<BoxCollider2D>();
        col.isTrigger = false; // Solid for physics queries.

        // Adjust the collider to just the top portion of the platform sprite
        // so the player can jump through from below.
        // The sprite has the platform bar roughly in the vertical center.
        Vector2 spriteSize = sprite.bounds.size;
        col.size = new Vector2(spriteSize.x * 0.95f, spriteSize.y * 0.25f);
        col.offset = new Vector2(0f, spriteSize.y * 0.15f);

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

        activePlatforms.Add(platformGO);
    }

    // ------------------------------------------------------------------ //
    //  Editor-only sprite loading
    // ------------------------------------------------------------------ //

    private void LoadSpritesInEditor()
    {
#if UNITY_EDITOR
        if (platformLongSprite == null)
            platformLongSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Art/Backgrounds/Stage1/PlatformLong.png");
        if (platformShortSprite == null)
            platformShortSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Art/Backgrounds/Stage1/PlatformShort.png");

        Debug.Log($"[PlatformSpawner] Sprites loaded — Long:{platformLongSprite != null} Short:{platformShortSprite != null}");
#else
        Debug.LogWarning("[PlatformSpawner] Not in Editor — sprites must be assigned in Inspector.");
#endif
    }
}
