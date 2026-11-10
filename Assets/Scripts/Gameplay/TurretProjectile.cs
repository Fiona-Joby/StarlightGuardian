using UnityEngine;

/// <summary>Travels left from a turret muzzle and registers a player hit.</summary>
public class TurretProjectile : MonoBehaviour
{
    private Vector2 direction;
    private float speed;
    private float lifetime;
    private bool requiresPlatform;

    public void Initialize(Vector2 travelDirection, float projectileSpeed, float duration, bool onlyHitsOnPlatform = false)
    {
        direction = travelDirection.normalized;
        speed = projectileSpeed;
        lifetime = duration;
        requiresPlatform = onlyHitsOnPlatform;
    }

    private void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
        lifetime -= Time.deltaTime;
        if (lifetime <= 0f) Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Stage2PlayerHealth health = other.GetComponentInParent<Stage2PlayerHealth>();
        if (health == null) return;

        // The player's CatchZone is a movable child trigger used for star
        // collection. Beam overlap with that zone must not count as a body hit.
        Collider2D playerBody = health.GetComponent<Collider2D>();
        if (playerBody == null || other != playerBody) return;

        PlayerShield shield = other.GetComponentInParent<PlayerShield>();
        if (shield != null && shield.IsShielding)
        {
            Destroy(gameObject);
            return;
        }

        if (requiresPlatform)
        {
            PlayerMovement movement = other.GetComponentInParent<PlayerMovement>();
            if (movement == null || !movement.IsGroundedOnPlatform) return;
        }
        health.TakeHit();
        Destroy(gameObject);
    }
}
