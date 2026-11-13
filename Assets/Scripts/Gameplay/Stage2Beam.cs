using UnityEngine;

/// <summary>Moves the player's beam and removes turrets or Stage 3 drones it hits.</summary>
public class Stage2Beam : MonoBehaviour
{
    private float direction;
    private float speed;
    private float lifetime;

    public void Initialize(bool facingRight, float beamSpeed, float beamLifetime)
    {
        direction = facingRight ? 1f : -1f;
        speed = beamSpeed;
        lifetime = beamLifetime;
    }

    private void Update()
    {
        transform.position += Vector3.right * (direction * speed * Time.deltaTime);
        lifetime -= Time.deltaTime;
        if (lifetime <= 0f) Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Stage2Turret turret = other.GetComponentInParent<Stage2Turret>();
        if (turret != null)
        {
            turret.TakeHit();
            Destroy(gameObject);
            return;
        }
        Stage3Drone drone = other.GetComponentInParent<Stage3Drone>();
        if (drone != null)
        {
            drone.TakeHit();
            Destroy(gameObject);
        }
    }
}
