using UnityEngine;

/// <summary>A one-hit turret that fires left from its muzzle at the player.</summary>
public class Stage2Turret : MonoBehaviour
{
    [SerializeField, Min(0.2f)] private float shotInterval = 5f;
    [SerializeField, Min(0.1f)] private float projectileSpeed = 12f;
    [SerializeField, Min(0.1f)] private float projectileLifetime = 15f;

    private Stage2TurretEncounter encounter;
    private Transform muzzle;
    private Sprite beamSprite;
    private bool destroyed;

    public void Initialize(Stage2TurretEncounter owner, Transform muzzleTransform, Sprite projectileSprite, float firstShotDelay)
    {
        encounter = owner;
        muzzle = muzzleTransform;
        beamSprite = projectileSprite;
        InvokeRepeating(nameof(Fire), firstShotDelay, shotInterval);
    }

    public void TakeHit()
    {
        if (destroyed) return;
        destroyed = true;
        CancelInvoke(nameof(Fire));
        encounter.OnTurretDestroyed();
        Destroy(gameObject);
    }

    private void Fire()
    {
        if (destroyed || encounter == null || encounter.IsFinished || muzzle == null || beamSprite == null) return;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null || player.transform.position.x >= muzzle.position.x) return;

        Camera camera = Camera.main;
        if (camera == null) return;
        Vector3 viewportPosition = camera.WorldToViewportPoint(transform.position);
        if (viewportPosition.z <= 0f || viewportPosition.x < 0f || viewportPosition.x > 1f ||
            viewportPosition.y < 0f || viewportPosition.y > 1f) return;

        Vector2 direction = Vector2.left;
        GameObject shot = new GameObject("Turret Beam");
        // The beam sprite is centered on its transform; advance half its visible
        // length so the trailing end begins at the muzzle and the beam extends left.
        shot.transform.position = muzzle.position + (Vector3)(direction * 1.25f);
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        shot.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        shot.transform.localScale = new Vector3(2.5f, 0.35f, 1f);
        SpriteRenderer renderer = shot.AddComponent<SpriteRenderer>();
        renderer.sprite = beamSprite;
        renderer.sortingOrder = 9;
        BoxCollider2D hitbox = shot.AddComponent<BoxCollider2D>();
        hitbox.isTrigger = true;
        hitbox.size = new Vector2(1.4f, 0.7f);
        Rigidbody2D body = shot.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        shot.AddComponent<TurretProjectile>().Initialize(direction, projectileSpeed, projectileLifetime);
    }
}
