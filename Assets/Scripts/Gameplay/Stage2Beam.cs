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
            if (IsVisibleToPlayer(turret))
            {
                turret.TakeHit();
                Destroy(gameObject);
            }
            return;
        }
        Stage2BlobBeast blob = other.GetComponentInParent<Stage2BlobBeast>();
        if (blob != null)
        {
            if (IsVisibleToPlayer(blob))
            {
                blob.TakeHit();
                Destroy(gameObject);
            }
            return;
        }
        Stage3Drone drone = other.GetComponentInParent<Stage3Drone>();
        if (drone != null)
        {
            if (IsVisibleToPlayer(drone))
            {
                drone.TakeHit();
                Destroy(gameObject);
            }
            return;
        }
        Stage3Slime slime = other.GetComponentInParent<Stage3Slime>();
        if (slime != null)
        {
            if (IsVisibleToPlayer(slime))
            {
                slime.TakeHit();
                Destroy(gameObject);
            }
        }
    }

    private static bool IsVisibleToPlayer(Component enemy)
    {
        Camera playerCamera = Camera.main;
        if (playerCamera == null || enemy == null) return false;
        Plane[] cameraPlanes = GeometryUtility.CalculateFrustumPlanes(playerCamera);
        SpriteRenderer[] renderers = enemy.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer renderer in renderers)
            if (renderer != null && renderer.enabled && renderer.gameObject.activeInHierarchy &&
                GeometryUtility.TestPlanesAABB(cameraPlanes, renderer.bounds))
                return true;
        return false;
    }
}
