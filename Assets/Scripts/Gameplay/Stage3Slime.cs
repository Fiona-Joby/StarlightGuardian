using UnityEngine;

/// <summary>Ground patrol enemy that damages the player when it is close and in front.</summary>
public class Stage3Slime : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float patrolSpeed = 1.5f;
    [SerializeField, Min(0.1f)] private float attackRange = 1.8f;
    [SerializeField, Min(0.1f)] private float attackInterval = 1.4f;
    [SerializeField, Min(0.01f)] private float walkFrameDuration = 0.16f;
    [SerializeField, Min(0.1f)] private float attackDuration = 0.9f;

    private Stage3DroneEncounter encounter;
    private Sprite[] walkFrames;
    private Sprite attackSprite;
    private SpriteRenderer spriteRenderer;
    private Transform player;
    private float leftBound;
    private float rightBound;
    private float direction = -1f;
    private float animationTimer;
    private float attackTimer;
    private float attackAnimationTimer;
    private bool attacking;
    private bool destroyed;

    public void Initialize(Stage3DroneEncounter owner, Sprite[] frames, Sprite attack, float left, float right)
    {
        encounter = owner;
        walkFrames = frames;
        attackSprite = attack;
        leftBound = left;
        rightBound = right;
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null) player = playerObject.transform;
    }

    private void Update()
    {
        if (destroyed || encounter == null || encounter.IsFinished) return;
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject == null) return;
            player = playerObject.transform;
        }

        attackTimer = Mathf.Max(0f, attackTimer - Time.deltaTime);
        if (attacking)
        {
            attackAnimationTimer -= Time.deltaTime;
            if (attackAnimationTimer <= 0f) attacking = false;
            else return;
        }

        float deltaX = player.position.x - transform.position.x;
        bool playerNearby = Mathf.Abs(deltaX) <= attackRange;
        if (playerNearby && attackTimer <= 0f)
        {
            direction = deltaX < 0f ? -1f : 1f;
            StartAttack();
            return;
        }

        float nextX = transform.position.x + direction * patrolSpeed * Time.deltaTime;
        if (nextX <= leftBound || nextX >= rightBound)
        {
            direction = nextX <= leftBound ? 1f : -1f;
            nextX = Mathf.Clamp(nextX, leftBound, rightBound);
        }
        transform.position = new Vector3(nextX, transform.position.y, transform.position.z);
        UpdateWalkAnimation();
    }

    private void StartAttack()
    {
        GameAudio.PlaySfx("Slime Attack");
        if (spriteRenderer != null && attackSprite != null) spriteRenderer.sprite = attackSprite;
        if (spriteRenderer != null) spriteRenderer.flipX = direction < 0f;
        attackTimer = attackInterval;
        attackAnimationTimer = attackDuration;
        attacking = true;
        Stage2PlayerHealth health = player.GetComponent<Stage2PlayerHealth>();
        if (health != null) health.TakeHit(1f);
    }

    private void UpdateWalkAnimation()
    {
        if (spriteRenderer == null) return;
        if (walkFrames != null && walkFrames.Length > 0)
        {
            animationTimer += Time.deltaTime;
            int frame = Mathf.FloorToInt(animationTimer / walkFrameDuration) % walkFrames.Length;
            if (walkFrames[frame] != null) spriteRenderer.sprite = walkFrames[frame];
        }
        // Walk artwork faces left; flip it while travelling right.
        spriteRenderer.flipX = direction > 0f;
    }

    public void TakeHit()
    {
        if (destroyed) return;
        destroyed = true;
        if (encounter != null) encounter.OnSlimeDestroyed();
        Destroy(gameObject);
    }
}
