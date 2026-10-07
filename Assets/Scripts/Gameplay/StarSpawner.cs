using UnityEngine;

/// <summary>
/// Pairs a world spawn location with motion/type settings for that star.
/// </summary>
[System.Serializable]
public class StarSpawnPoint
{
    public Transform point;
    public StarType starType = StarType.Normal;
    [Min(0f)] public float horizontalSpeed = 3f;
    [Min(0f)] public float downwardSpeed = 1.5f;
}

/// <summary>
/// Sequentially spawns stars from an ordered list of world positions.
/// Each spawn point has its own StarType, speed and trajectory.
///
/// Normal stars alternate between the yellow and blue star sprites.
/// Rainbow stars always use the rainbow star sprite.
///
/// NOTE: Currently timer-based. A future iteration should tie spawning
/// to world/camera progression rather than a flat interval.
/// </summary>
public class StarSpawner : MonoBehaviour
{
    [SerializeField] private StarMovement starPrefab;
    [SerializeField] private StarSpawnPoint[] spawnPoints;
    [SerializeField, Min(0f)] private float spawnInterval = 2f;
    [SerializeField] private bool repeatSequence = true;

    [Header("Star Sprites")]
    [Tooltip("Drag Art/Stars/Yellow Star here.")]
    [SerializeField] private Sprite yellowStarSprite;
    [Tooltip("Drag Art/Stars/Blue Star here.")]
    [SerializeField] private Sprite blueStarSprite;
    [Tooltip("Drag Art/Stars/Rainbow Star here.")]
    [SerializeField] private Sprite rainbowStarSprite;

    private int currentSpawnPoint;
    private float spawnTimer;
    private int normalStarCounter;   // toggles 0 / 1 to alternate yellow ↔ blue

    private void Update()
    {
        if (starPrefab == null || spawnPoints == null || spawnPoints.Length == 0) return;
        if (!repeatSequence && currentSpawnPoint >= spawnPoints.Length) return;

        spawnTimer += Time.deltaTime;
        if (spawnTimer < spawnInterval) return;
        spawnTimer = 0f;
        SpawnNext();
    }

    private void SpawnNext()
    {
        StarSpawnPoint settings = spawnPoints[currentSpawnPoint];
        currentSpawnPoint++;
        if (repeatSequence && currentSpawnPoint >= spawnPoints.Length)
            currentSpawnPoint = 0;

        if (settings == null || settings.point == null) return;

        // Choose the correct sprite based on type.
        Sprite visual;
        if (settings.starType == StarType.Rainbow)
        {
            visual = rainbowStarSprite;
        }
        else
        {
            // Alternate between yellow and blue for Normal stars.
            visual = (normalStarCounter % 2 == 0) ? yellowStarSprite : blueStarSprite;
            normalStarCounter++;
        }

        StarMovement star = Instantiate(starPrefab, settings.point.position, Quaternion.identity);
        star.Initialize(settings.starType, settings.horizontalSpeed, settings.downwardSpeed, visual);
    }
}

