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
    [Header("Motion")]
    [SerializeField, Min(0f)] private float speed = 3f;
    [SerializeField, Min(0f)] private float downwardSpeed = 1.5f;

    [Header("Type")]
    [SerializeField] private StarType starType = StarType.Normal;
    [SerializeField, Min(0f)] private float rainbowMultiplierDuration = 5f;

    [Header("Rainbow Visuals")]
    [SerializeField, Min(0f)] private float rainbowCycleSpeed = 0.5f;
    [SerializeField, Min(1f)] private float rainbowScale = 1.3f;

    private bool landed;
    private bool collected;
    private Rigidbody2D body;
    private SpriteRenderer spriteRenderer;

    public StarType Type => starType;
    public bool IsLanded => landed;

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
        body.MovePosition(body.position + new Vector2(-speed, -downwardSpeed) * Time.fixedDeltaTime);
    }

    /// <summary>Rainbow stars shimmer with a subtle brightness pulse.</summary>
    private void Update()
    {
        if (starType == StarType.Rainbow && spriteRenderer != null && !collected)
        {
            // Subtle brightness pulse instead of full HSV cycling,
            // because the rainbow sprite already has its own colours.
            float brightness = 0.85f + Mathf.Sin(Time.time * rainbowCycleSpeed * 4f) * 0.15f;
            spriteRenderer.color = new Color(brightness, brightness, brightness, 1f);
        }
    }

    // ------------------------------------------------------------------ //
    //  Trigger detection — star detects the CatchZone, not vice versa
    // ------------------------------------------------------------------ //

    private void OnTriggerEnter2D(Collider2D other)
    {
        CatchZone catchZone = other.GetComponent<CatchZone>();
        if (catchZone != null)
        {
            catchZone.TryCatch(this);
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
        body.MovePosition(landingPosition);
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
}
