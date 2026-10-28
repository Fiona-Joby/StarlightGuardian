using UnityEngine;

/// <summary>
/// Controls a falling star's movement and lifecycle.
///
/// Stars originate from the RIGHT and travel LEFT with a slight downward angle.
/// They NEVER follow the player — all motion is in fixed world space.
///
/// Normal star: lands on ground → stays visible and collectible.
///              Alternates between yellow and blue sprites (set by StarSpawner).
/// Rainbow star: lands on ground → destroyed (not collectible on ground).
///               If caught airborne → activates 2× score multiplier.
///               Uses the dedicated rainbow star sprite.
///
/// Rainbow stars are slightly larger so the player can immediately tell them apart.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class StarMovement : MonoBehaviour
{
    private static readonly ContactFilter2D PlatformOverlapFilter = ContactFilter2D.noFilter;

    [Header("Motion")]
    [SerializeField, Min(0f)] private float speed = 3f;
    [SerializeField, Min(0f)] private float downwardSpeed = 1.5f;

    [Header("Type")]
    [SerializeField] private StarType starType = StarType.Normal;
    [SerializeField, Min(0f)] private float rainbowMultiplierDuration = 5f;
    [SerializeField, Min(0f)] private float landedCollectibleLifetime = 15f;

    [Header("Rainbow Visuals")]
    [SerializeField, Min(0f)] private float rainbowCycleSpeed = 0.5f;
    [SerializeField, Min(1f)] private float rainbowScale = 1.3f;

    private bool landed;
    private bool collected;
    private Rigidbody2D body;
    private SpriteRenderer spriteRenderer;
    private Collider2D landingPlatform;
    private float landedAtTime;
    private Collider2D starCollider;
    private Collider2D[] platformOverlapBuffer = new Collider2D[16];

    public StarType Type => starType;
    public bool CanBeCaughtBy(PlayerMovement player)
    {
        return landingPlatform == null || (player != null && player.IsOnPlatform(landingPlatform));
    }

    // ------------------------------------------------------------------ //
    //  Lifecycle
    // ------------------------------------------------------------------ //

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        spriteRenderer = GetComponent<SpriteRenderer>();
        starCollider = GetComponent<Collider2D>();
    }

    /// <summary>Called by StarSpawner right after Instantiate.</summary>
    public void Initialize(StarType type, float horizontalSpeed, float fallSpeed)
    {
        starType = type;
        speed = horizontalSpeed;
        downwardSpeed = fallSpeed;

        if (starType == StarType.Rainbow)
        {
            transform.localScale *= rainbowScale;
        }
    }

    /// <summary>
    /// Called by StarSpawner right after Instantiate to set the star type,
    /// motion parameters, and the visual sprite (yellow/blue/rainbow).
    /// </summary>
    public void Initialize(StarType type, float horizontalSpeed, float fallSpeed, Sprite visual)
    {
        Initialize(type, horizontalSpeed, fallSpeed);

        if (visual != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = visual;
            // Reset tint so the sprite's own colours come through.
            spriteRenderer.color = Color.white;
        }
    }

    // ------------------------------------------------------------------ //
    //  Movement
    // ------------------------------------------------------------------ //

    private void FixedUpdate()
    {
        if (landed || collected) return;
        // World-space motion: stars never read the player transform.
        Vector2 nextPosition = body.position + new Vector2(-speed, -downwardSpeed) * Time.fixedDeltaTime;
        if (!TryLandOnPlatform(nextPosition))
            body.MovePosition(nextPosition);
    }

    /// <summary>Rainbow stars shimmer with a subtle brightness pulse.</summary>
    private void Update()
    {
        if (landed && landedCollectibleLifetime > 0f && Time.time - landedAtTime >= landedCollectibleLifetime)
        {
            Destroy(gameObject);
            return;
        }

        if (starType == StarType.Rainbow && spriteRenderer != null && !collected)
        {
            // Subtle brightness pulse instead of full HSV cycling,
            // because the rainbow sprite already has its own colours.
            float brightness = 0.85f + Mathf.Sin(Time.time * rainbowCycleSpeed * 4f) * 0.15f;
            spriteRenderer.color = new Color(brightness, brightness, brightness, 1f);
        }
    }


    // ------------------------------------------------------------------ //
    //  Ground landing
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Called by GroundDetector when the star reaches ground level.
    /// Normal stars stop and stay collectible. Rainbow stars are destroyed.
    /// </summary>
    public void ReachGround(Vector2 landingPosition)
    {
        if (landed || collected) return;

        if (starType == StarType.Rainbow)
        {
            collected = true;
            Destroy(gameObject);
            return;
        }

        // Normal star: stop moving, stay visible, keep collider active for pickup.
        landed = true;
        landedAtTime = Time.time;
        body.MovePosition(landingPosition);
    }

    /// <summary>Land on a platform and remember which platform supports this star.</summary>
    public void ReachPlatform(Collider2D platform, Vector2 landingPosition)
    {
        if (platform == null || landed || collected) return;
        landingPlatform = platform;
        ReachGround(landingPosition);
    }

    // ------------------------------------------------------------------ //
    //  Collection
    // ------------------------------------------------------------------ //

    /// <summary>Called when the player's CatchZone picks up this star.</summary>
    public void Caught()
    {
        if (collected) return;
        collected = true;

        if (starType == StarType.Rainbow)
            ScoreMultiplier.ActivateTemporaryDouble(rainbowMultiplierDuration);

        StarEvents.RaiseCaught(this);
        Destroy(gameObject);
    }

    private bool TryLandOnPlatform(Vector2 nextPosition)
    {
        if (starCollider == null || downwardSpeed <= 0f) return false;

        Bounds starBounds = starCollider.bounds;
        float halfWidth = starBounds.extents.x;
        float halfHeight = starBounds.extents.y;
        float currentBottom = body.position.y - halfHeight;
        float nextBottom = nextPosition.y - halfHeight;
        if (nextBottom > currentBottom) return false;

        Vector2 areaMin = new Vector2(
            Mathf.Min(body.position.x, nextPosition.x) - halfWidth,
            nextBottom - 0.02f);
        Vector2 areaMax = new Vector2(
            Mathf.Max(body.position.x, nextPosition.x) + halfWidth,
            currentBottom + 0.02f);
        int candidateCount = Physics2D.OverlapArea(areaMin, areaMax, PlatformOverlapFilter, platformOverlapBuffer);
        while (candidateCount == platformOverlapBuffer.Length)
        {
            System.Array.Resize(ref platformOverlapBuffer, platformOverlapBuffer.Length * 2);
            candidateCount = Physics2D.OverlapArea(areaMin, areaMax, PlatformOverlapFilter, platformOverlapBuffer);
        }

        Collider2D landingSurface = null;
        float landingY = float.NegativeInfinity;
        float landingX = nextPosition.x;
        float earliestCrossing = float.PositiveInfinity;

        for (int i = 0; i < candidateCount; i++)
        {
            Collider2D candidate = platformOverlapBuffer[i];
            if (candidate == null || !candidate.CompareTag("Platform")) continue;

            Bounds platformBounds = candidate.bounds;
            float starMinX = Mathf.Min(body.position.x, nextPosition.x) - halfWidth;
            float starMaxX = Mathf.Max(body.position.x, nextPosition.x) + halfWidth;
            if (starMaxX < platformBounds.min.x || starMinX > platformBounds.max.x) continue;

            float platformTop = platformBounds.max.y;
            if (currentBottom < platformTop - 0.02f || nextBottom > platformTop) continue;

            float crossing = (currentBottom - platformTop) / Mathf.Max(0.0001f, currentBottom - nextBottom);
            float crossingX = Mathf.Lerp(body.position.x, nextPosition.x, crossing);
            if (crossingX + halfWidth < platformBounds.min.x ||
                crossingX - halfWidth > platformBounds.max.x)
                continue;

            if (crossing < earliestCrossing)
            {
                earliestCrossing = crossing;
                landingSurface = candidate;
                landingY = platformTop;
                landingX = crossingX;
            }
        }

        if (landingSurface == null) return false;

        ReachPlatform(landingSurface, new Vector2(landingX, landingY + halfHeight));
        return true;
    }
}
