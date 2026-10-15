using UnityEngine;

/// <summary>
/// Spawns stars at randomized positions ahead of the player as they move forward.
/// </summary>
public class StarSpawner : MonoBehaviour
{
    [SerializeField] private StarMovement starPrefab;
    [SerializeField] private Transform player;
    [SerializeField, Min(0f)] private float spawnInterval = 2f;
    [SerializeField, Min(0f)] private float minDistanceAhead = 12f;
    [SerializeField, Min(0f)] private float maxDistanceAhead = 20f;
    [SerializeField] private float minHeightAbovePlayer = 2f;
    [SerializeField] private float maxHeightAbovePlayer = 6f;
    [SerializeField] private float horizontalSpeed = 3f;
    [SerializeField] private float downwardSpeed = 1.5f;
    [SerializeField, Range(0f, 1f)] private float rainbowChance = 0.100f;

    [Header("Star Sprites")]
    [SerializeField] private Sprite yellowStarSprite;
    [SerializeField] private Sprite blueStarSprite;
    [SerializeField] private Sprite rainbowStarSprite;

    private float spawnTimer;
    private int normalStarCounter;

    private void Awake()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                player = playerObject.transform;
        }
    }

    private void Update()
    {
        if (starPrefab == null || player == null) return;

        spawnTimer += Time.deltaTime;
        if (spawnTimer < spawnInterval) return;

        spawnTimer -= spawnInterval;
        SpawnNext();
    }

    private void SpawnNext()
    {
        StarType type = Random.value < rainbowChance ? StarType.Rainbow : StarType.Normal;
        float distanceAhead = Random.Range(minDistanceAhead, Mathf.Max(minDistanceAhead, maxDistanceAhead));
        float height = Random.Range(minHeightAbovePlayer, Mathf.Max(minHeightAbovePlayer, maxHeightAbovePlayer));
        Vector3 position = new Vector3(player.position.x + distanceAhead, player.position.y + height, 0f);

        Sprite visual;
        if (type == StarType.Rainbow)
        {
            visual = rainbowStarSprite;
        }
        else
        {
            visual = (normalStarCounter % 2 == 0) ? yellowStarSprite : blueStarSprite;
            normalStarCounter++;
        }

        StarMovement star = Instantiate(starPrefab, position, Quaternion.identity);
        star.Initialize(type, horizontalSpeed, downwardSpeed, visual);
    }
}
