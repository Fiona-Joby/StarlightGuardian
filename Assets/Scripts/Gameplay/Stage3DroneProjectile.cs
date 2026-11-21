using UnityEngine;

/// <summary>Stage 3 drone laser that hits the visible player and is consumed on contact.</summary>
public sealed class Stage3DroneProjectile : MonoBehaviour
{
    private Stage3DroneEncounter encounter;
    private Vector2 direction;
    private float speed;
    private float lifetime;
    private Collider2D beamCollider;
    private bool hasHit;
    private float impactDisplayTimer;

    public void Initialize(Stage3DroneEncounter owner, Vector2 travelDirection, float projectileSpeed, float duration)
    {
        encounter = owner;
        direction = travelDirection.normalized;
        speed = projectileSpeed;
        lifetime = duration;
        beamCollider = GetComponent<Collider2D>();
    }

    private void Update()
    {
        if (hasHit)
        {
            impactDisplayTimer -= Time.deltaTime;
            if (impactDisplayTimer <= 0f) Destroy(gameObject);
            return;
        }

        if (encounter == null || encounter.IsFinished)
        {
            Destroy(gameObject);
            return;
        }

        Vector2 start = transform.position;
        float distance = speed * Time.deltaTime;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        SpriteRenderer playerRenderer = player != null ? player.GetComponent<SpriteRenderer>() : null;

        if (playerRenderer != null)
        {
            // Stage 3's player collider is intentionally a very thin platform
            // probe. Use the rendered character bounds and the beam's own
            // collider size so contact with any visible part counts.
            Bounds contactBounds = playerRenderer.bounds;
            Vector3 beamExtents = beamCollider != null ? beamCollider.bounds.extents : Vector3.zero;
            contactBounds.Expand(beamExtents * 2f);

            if (contactBounds.IntersectRay(new Ray(start, direction), out float hitDistance) &&
                hitDistance <= distance)
            {
                transform.position = start + direction * hitDistance;
                PlayerShield shield = player.GetComponent<PlayerShield>();
                if (shield == null || !shield.IsShielding)
                    encounter.OnPlayerHit();

                // Destroying immediately in this Update removes the beam
                // before Unity renders its impact frame. Hold the stopped beam
                // briefly so the hit is visible, including point-blank hits.
                hasHit = true;
                impactDisplayTimer = 0.12f;
                if (beamCollider != null) beamCollider.enabled = false;
                return;
            }
        }

        transform.position = start + direction * distance;
        lifetime -= Time.deltaTime;
        if (lifetime <= 0f) Destroy(gameObject);
    }
}
