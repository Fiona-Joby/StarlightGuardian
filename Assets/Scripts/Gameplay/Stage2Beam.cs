using UnityEngine;

/// <summary>Moves the Stage 2 beam and removes enemies it hits.</summary>
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
        if (turret == null) return;
        turret.TakeHit();
        Destroy(gameObject);
    }
}
