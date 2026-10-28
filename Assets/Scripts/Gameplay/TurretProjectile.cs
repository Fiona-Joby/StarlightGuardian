using UnityEngine;

/// <summary>Travels left from a turret muzzle and registers a player hit.</summary>
public class TurretProjectile : MonoBehaviour
{
    private Vector2 direction;
    private float speed;
    private float lifetime;

    public void Initialize(Vector2 travelDirection, float projectileSpeed, float duration)
    {
        direction = travelDirection.normalized;
        speed = projectileSpeed;
        lifetime = duration;
    }

    private void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
        lifetime -= Time.deltaTime;
        if (lifetime <= 0f) Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerShield shield = other.GetComponentInParent<PlayerShield>();
        if (shield != null && shield.IsShielding)
        {
            Destroy(gameObject);
            return;
        }

        Stage2PlayerHealth health = other.GetComponentInParent<Stage2PlayerHealth>();
        if (health == null) return;
        health.TakeHit();
        Destroy(gameObject);
    }
}
