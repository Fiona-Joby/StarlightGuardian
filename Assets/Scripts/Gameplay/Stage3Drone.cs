using UnityEngine;

/// <summary>Patrols a small area and fires a beam when the player is nearby.</summary>
public class Stage3Drone : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float patrolRadius = 2.2f;
    [SerializeField, Min(0.1f)] private float patrolSpeed = 1.4f;
    [SerializeField, Min(0.5f)] private float detectionDistance = 7f;
    [SerializeField, Min(0.2f)] private float shotInterval = 2.2f;
    [SerializeField, Min(0.1f)] private float projectileSpeed = 12f;

    private Stage3DroneEncounter encounter;
    private Sprite[] frames;
    private Sprite beamSprite;
    private Vector3 patrolCenter;
    private float animationTimer;
    private float shotTimer;
    private bool destroyed;
    private SpriteRenderer spriteRenderer;

    public void Initialize(Stage3DroneEncounter owner, Sprite[] animationFrames, Sprite projectileSprite, Vector3 center)
    {
        encounter = owner;
        frames = animationFrames;
        beamSprite = projectileSprite;
        patrolCenter = center;
        shotTimer = 1f;
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Update()
    {
        if (destroyed || encounter == null || encounter.IsFinished) return;
        animationTimer += Time.deltaTime;
        if (frames != null && frames.Length > 0 && spriteRenderer != null)
            spriteRenderer.sprite = frames[Mathf.FloorToInt(animationTimer * 7f) % frames.Length];

        float phase = Time.time * patrolSpeed + patrolCenter.x;
        transform.position = patrolCenter + new Vector3(Mathf.Sin(phase) * patrolRadius, Mathf.Sin(phase * 0.7f) * 0.45f, 0f);

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;
        Vector2 offset = player.transform.position - transform.position;
        if (offset.magnitude > detectionDistance) return;
        shotTimer -= Time.deltaTime;
        if (shotTimer <= 0f)
        {
            Fire(offset.x >= 0f ? Vector2.right : Vector2.left);
            shotTimer = shotInterval;
        }
    }

    private void Fire(Vector2 direction)
    {
        if (beamSprite == null) return;
        GameObject shot = new GameObject("Drone Beam");
        shot.transform.position = transform.position + (Vector3)(direction * 1.2f);
        shot.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        shot.transform.localScale = new Vector3(2.5f, 0.35f, 1f);
        var renderer = shot.AddComponent<SpriteRenderer>();
        renderer.sprite = beamSprite;
        renderer.sortingOrder = 9;
        var hitbox = shot.AddComponent<BoxCollider2D>();
        hitbox.isTrigger = true;
        hitbox.size = new Vector2(1.4f, 0.7f);
        var body = shot.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        shot.AddComponent<TurretProjectile>().Initialize(direction, projectileSpeed, 8f);
    }

    public void TakeHit()
    {
        if (destroyed) return;
        destroyed = true;
        encounter.OnDroneDestroyed();
        Destroy(gameObject);
    }
}
