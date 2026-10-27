using UnityEngine;

/// <summary>Stage 2 shooting sequence and beam launcher.</summary>
public class Stage2Shooter : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float attackFrameDuration = 0.12f;
    [SerializeField, Min(0f)] private float cooldown = 0.35f;
    [SerializeField, Min(0.1f)] private float beamSpeed = 18f;
    [SerializeField, Min(0.1f)] private float beamLifetime = 2f;

    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private Sprite idleSprite;
    private Sprite[] attackFrames;
    private Sprite beamSprite;
    private float attackAnimationTimer;
    private float cooldownTimer;
    private bool isShooting;
    private bool beamFired;
    private bool facingRight;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        idleSprite = spriteRenderer != null ? spriteRenderer.sprite : null;
        attackFrames = new[]
        {
            Resources.Load<Sprite>("Attacking/Attack 1"),
            Resources.Load<Sprite>("Attacking/Attack 2"),
            Resources.Load<Sprite>("Attacking/Attack 3"),
            Resources.Load<Sprite>("Attacking/Attack 4")
        };
        beamSprite = Resources.Load<Sprite>("Attacking/Attack Beam");
        if (beamSprite == null || System.Array.Exists(attackFrames, frame => frame == null))
            Debug.LogError("[Stage2Shooter] An attack frame or Attack Beam is missing from Resources/Attacking.");
    }

    private void Update()
    {
        cooldownTimer = Mathf.Max(0f, cooldownTimer - Time.deltaTime);
        if (isShooting)
        {
            attackAnimationTimer += Time.deltaTime;
            int frame = Mathf.Min(attackFrames.Length - 1,
                Mathf.FloorToInt(attackAnimationTimer / attackFrameDuration));
            if (spriteRenderer != null) spriteRenderer.sprite = attackFrames[frame];
            if (!beamFired && frame >= 3)
            {
                FireBeam();
                beamFired = true;
            }
            if (attackAnimationTimer >= attackFrameDuration * attackFrames.Length)
                EndShootingAnimation();
        }

        PlayerInput input = GetComponent<PlayerInput>();
        if (input != null && input.ShootPressed && cooldownTimer <= 0f && !isShooting)
            Shoot(input.FacingRight);
    }

    private void Shoot(bool faceRight)
    {
        if (beamSprite == null || System.Array.Exists(attackFrames, frame => frame == null)) return;
        facingRight = faceRight;
        if (spriteRenderer != null)
        {
            idleSprite = spriteRenderer.sprite;
            if (animator != null) animator.enabled = false;
            spriteRenderer.flipX = !facingRight;
            spriteRenderer.sprite = attackFrames[0];
        }
        isShooting = true;
        beamFired = false;
        attackAnimationTimer = 0f;
        cooldownTimer = cooldown;
    }

    private void EndShootingAnimation()
    {
        isShooting = false;
        if (spriteRenderer != null) spriteRenderer.sprite = idleSprite;
        if (animator != null) animator.enabled = true;
    }

    private void FireBeam()
    {
        GameObject beam = new GameObject("Attack Beam");
        // Start close to the forward edge of the Attack 4 pose.
        beam.transform.position = transform.position + new Vector3(facingRight ? 1.8f : -1.8f, 0.05f, 0f);
        beam.transform.localScale = new Vector3(facingRight ? 1.2f : -1.2f, 0.25f, 1f);
        SpriteRenderer beamRenderer = beam.AddComponent<SpriteRenderer>();
        beamRenderer.sprite = beamSprite;
        beamRenderer.color = Color.white;
        beamRenderer.sortingOrder = spriteRenderer != null ? spriteRenderer.sortingOrder + 1 : 11;
        BoxCollider2D hitbox = beam.AddComponent<BoxCollider2D>();
        hitbox.isTrigger = true;
        hitbox.size = new Vector2(2f, 1f);
        Rigidbody2D body = beam.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        beam.AddComponent<Stage2Beam>().Initialize(facingRight, beamSpeed, beamLifetime);
    }
}
