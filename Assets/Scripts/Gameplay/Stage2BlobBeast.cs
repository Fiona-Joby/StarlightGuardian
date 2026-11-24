using UnityEngine;

/// <summary>Grounded Stage 2 enemy that leaps at the player and stomps on landing.</summary>
public class Stage2BlobBeast : MonoBehaviour
{
    public enum AIState { Patrol, Detect, Prepare, Attack, Recover }

    [System.Serializable]
    public class AISettings
    {
        [Header("Patrol boundaries, relative to spawn")]
        public float patrolLeftOffset = -2.5f;
        public float patrolRightOffset = 2.5f;
        [Min(0.1f)] public float patrolSpeed = 1.25f;
        [Header("Vision")]
        [Min(0.1f)] public float detectionRange = 8f;
        [Range(1f, 180f)] public float visionAngle = 100f;
        public LayerMask visionObstacleLayers = ~0;
        [Min(0f)] public float spottingTime = 0.12f;
        [Header("Jump attack")]
        [Min(0f)] public float preparationTime = 0.45f;
        [Min(0f)] public float landingOffsetTowardFacing = 0.25f;
        [Min(0.1f)] public float playerAimHalfWidth = 0.85f;
        [Min(0.1f)] public float jumpHeight = 2.8f;
        [Min(0.1f)] public float attackDuration = 1.5f;
        [Min(0f)] public float damage = 0.5f;
        [Header("Recovery")]
        [Min(0f)] public float recoveryTime = 1.2f;
        [Min(0.1f)] public float recoverySideDistance = 1.5f;
    }

    private Stage2TurretEncounter stage2Encounter;
    private Stage3DroneEncounter stage3Encounter;
    private Sprite[] walkFrames;
    private Sprite jumpFrame;
    private Sprite stompFrame;
    private SpriteRenderer spriteRenderer;
    private Transform artTransform;
    private AISettings settings;
    private Stage2PlayerHealth playerHealth;
    private PlayerMovement playerMovement;
    private Collider2D playerCollider;
    private SpriteRenderer playerSpriteRenderer;
    private Rigidbody2D body;
    private Collider2D beastCollider;
    private AIState state;
    private float walkTimer;
    private float stateTimer;
    private float facingDirection = 1f;
    private float patrolLeft;
    private float patrolRight;
    private float patrolStartX;
    private float patrolTargetX;
    private float patrolLegElapsed;
    private float patrolLegDuration;
    private float attackStartX;
    private float attackStartY;
    private float attackTargetX;
    private float attackJumpHeight;
    private float attackApproachSide;
    private float attackElapsed;
    private float recoveryTargetX;
    private bool attackPassedAbovePlayer;
    private bool attackSoundPlayed;
    private bool damageApplied;
    private bool destroyed;

    public AIState State => state;

    public void Initialize(Stage2TurretEncounter owner, Sprite[] frames, Sprite jump, Sprite stomp,
        bool startWalkingLeft, AISettings aiSettings)
    {
        stage2Encounter = owner;
        Configure(frames, jump, stomp, startWalkingLeft, aiSettings);
    }

    public void Initialize(Stage3DroneEncounter owner, Sprite[] frames, Sprite jump, Sprite stomp,
        bool startWalkingLeft, AISettings aiSettings)
    {
        stage3Encounter = owner;
        Configure(frames, jump, stomp, startWalkingLeft, aiSettings);
    }

    private void Configure(Sprite[] frames, Sprite jump, Sprite stomp, bool startWalkingLeft, AISettings aiSettings)
    {
        walkFrames = frames;
        jumpFrame = jump;
        stompFrame = stomp;
        settings = aiSettings ?? new AISettings();
        body = GetComponent<Rigidbody2D>();
        if (body == null) body = gameObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.constraints |= RigidbodyConstraints2D.FreezeRotation;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        beastCollider = GetComponent<Collider2D>();
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        playerCollider = player != null ? player.GetComponent<Collider2D>() : null;
        playerSpriteRenderer = player != null ? player.GetComponent<SpriteRenderer>() : null;
        playerHealth = player != null ? player.GetComponent<Stage2PlayerHealth>() : null;
        playerMovement = player != null ? player.GetComponent<PlayerMovement>() : null;
        float spawnX = body.position.x;
        patrolLeft = spawnX + Mathf.Min(settings.patrolLeftOffset, settings.patrolRightOffset);
        patrolRight = spawnX + Mathf.Max(settings.patrolLeftOffset, settings.patrolRightOffset);
        facingDirection = startWalkingLeft ? -1f : 1f;
        BeginPatrolLeg(facingDirection);
        state = AIState.Patrol;
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null) artTransform = spriteRenderer.transform;
        }
    }

    private void FixedUpdate()
    {
        if (destroyed || IsEncounterFinished) return;
        if (playerCollider == null)
        {
            state = AIState.Patrol;
            UpdatePatrol();
            return;
        }

        switch (state)
        {
            case AIState.Patrol: UpdatePatrol(); break;
            case AIState.Detect: UpdateDetection(); break;
            case AIState.Prepare: UpdatePreparation(); break;
            case AIState.Attack: UpdateAttack(); break;
            case AIState.Recover: UpdateRecovery(); break;
        }
    }

    private void UpdatePatrol()
    {
        patrolLegElapsed += Time.fixedDeltaTime;
        float t = Mathf.Clamp01(patrolLegElapsed / patrolLegDuration);
        float easedT = t * t * (3f - 2f * t);
        float x = Mathf.Lerp(patrolStartX, patrolTargetX, easedT);
        MoveTo(new Vector2(x, body.position.y));
        facingDirection = Mathf.Sign(patrolTargetX - patrolStartX);
        SetFacing(facingDirection);
        UpdateWalkingAnimation();

        if (t >= 1f)
        {
            if (body.position.x < patrolLeft) BeginPatrolLeg(1f);
            else if (body.position.x > patrolRight) BeginPatrolLeg(-1f);
            else BeginPatrolLeg(-facingDirection);
        }

        if (CanSeePlayer(true))
        {
            state = AIState.Detect;
            stateTimer = 0f;
            FacePlayer();
        }
    }

    private void BeginPatrolLeg(float direction)
    {
        patrolStartX = body.position.x;
        if (patrolStartX < patrolLeft) patrolTargetX = patrolLeft;
        else if (patrolStartX > patrolRight) patrolTargetX = patrolRight;
        else patrolTargetX = direction >= 0f ? patrolRight : patrolLeft;
        patrolLegElapsed = 0f;
        float distance = Mathf.Abs(patrolTargetX - patrolStartX);
        patrolLegDuration = Mathf.Max(0.05f, distance / Mathf.Max(0.1f, settings.patrolSpeed));
        if (distance > 0.001f) facingDirection = Mathf.Sign(patrolTargetX - patrolStartX);
    }

    private void UpdateDetection()
    {
        // Once spotted, finish the windup even if the player briefly leaves
        // the beast's vision or moves off-screen.
        FacePlayer();
        stateTimer += Time.fixedDeltaTime;
        if (stateTimer >= settings.spottingTime)
        {
            state = AIState.Prepare;
            stateTimer = 0f;
            SetSprite(jumpFrame);
        }
    }

    private void UpdatePreparation()
    {
        FacePlayer();
        stateTimer += Time.fixedDeltaTime;
        if (stateTimer >= settings.preparationTime)
            BeginAttack();
    }

    private void BeginAttack()
    {
        state = AIState.Attack;
        SetAttackSorting(true);
        attackStartX = body.position.x;
        attackStartY = body.position.y;
        attackElapsed = 0f;
        damageApplied = false;
        attackSoundPlayed = false;
        attackPassedAbovePlayer = false;
        Bounds playerBounds = GetPlayerAimBounds();
        // Lock the target at takeoff. The player can avoid the stomp by moving
        // after the beast commits to its jump.
        attackTargetX = playerBounds.center.x;
        attackJumpHeight = Mathf.Max(
            settings.jumpHeight,
            playerBounds.max.y - attackStartY + 0.5f);
        attackApproachSide = Mathf.Sign(attackStartX - playerBounds.center.x);
        if (Mathf.Abs(attackApproachSide) < 0.01f) attackApproachSide = -facingDirection;
        float attackDirection = attackTargetX - attackStartX;
        if (Mathf.Abs(attackDirection) > 0.01f)
            facingDirection = Mathf.Sign(attackDirection);
        SetFacing(facingDirection);
    }

    private void UpdateAttack()
    {
        attackElapsed += Time.fixedDeltaTime;
        float t = Mathf.Clamp01(attackElapsed / Mathf.Max(0.1f, settings.attackDuration));
        float easedT = t * t * (3f - 2f * t);
        float x = Mathf.Lerp(attackStartX, attackTargetX, easedT);
        float y = attackStartY + Mathf.Sin(t * Mathf.PI) * attackJumpHeight;
        float previousY = body.position.y;
        MoveTo(new Vector2(x, y));
        Bounds playerBounds = GetPlayerAimBounds();
        if (y > playerBounds.max.y) attackPassedAbovePlayer = true;

        if (t >= 0.72f)
        {
            SetSprite(stompFrame);
            if (!attackSoundPlayed && t >= 0.6f)
            {
                // Start the impact cue with the visible stomp frame so its
                // onset is heard at the landing, including any clip latency.
                GameAudio.PlaySfx("Blob Beast Stomp", 1.8f);
                attackSoundPlayed = true;
            }
        }
        if (!damageApplied && t >= 0.5f && previousY > playerBounds.max.y &&
            y <= playerBounds.max.y && HorizontalStompOverlap(x, playerBounds) &&
            !PlayerIsGroundedOnPlatform())
        {
            if (playerHealth != null) playerHealth.TakeHit(settings.damage);
            damageApplied = true;
        }

        if (t < 1f) return;
        playerBounds = GetPlayerAimBounds();
        float landingX = attackTargetX;
        MoveTo(new Vector2(landingX, attackStartY));
        if (!damageApplied && attackPassedAbovePlayer && HorizontalStompOverlap(landingX, playerBounds) &&
            playerCollider.bounds.min.y <= attackStartY + 0.6f && !PlayerIsGroundedOnPlatform())
        {
            if (playerHealth != null) playerHealth.TakeHit(settings.damage);
            damageApplied = true;
        }
        SetSprite(stompFrame);
        float playerX = playerCollider != null ? GetPlayerAimBounds().center.x : attackTargetX;
        recoveryTargetX = playerX - attackApproachSide * settings.recoverySideDistance;
        state = AIState.Recover;
        stateTimer = 0f;
    }

    private bool PlayerIsGroundedOnPlatform() =>
        playerMovement != null && playerMovement.IsGroundedOnPlatform;

    private bool HorizontalStompOverlap(float beastX, Bounds playerBounds)
    {
        if (beastCollider == null) return false;
        float halfWidth = beastCollider.bounds.extents.x;
        return beastX + halfWidth >= playerBounds.min.x &&
               beastX - halfWidth <= playerBounds.max.x;
    }

    private void UpdateRecovery()
    {
        SetAttackSorting(false);
        stateTimer += Time.fixedDeltaTime;
        float currentX = body.position.x;
        float direction = Mathf.Sign(recoveryTargetX - currentX);
        if (Mathf.Abs(recoveryTargetX - currentX) > 0.05f)
        {
            facingDirection = direction;
            SetFacing(direction);
            UpdateWalkingAnimation();
            float nextX = Mathf.MoveTowards(currentX, recoveryTargetX,
                Mathf.Max(0.1f, settings.patrolSpeed) * Time.fixedDeltaTime);
            MoveTo(new Vector2(nextX, body.position.y));
            return;
        }

        if (stateTimer < settings.recoveryTime) return;
        ResumePatrol();
    }

    private void ResumePatrol()
    {
        state = AIState.Patrol;
        BeginPatrolLeg(facingDirection);
        SetSprite(walkFrames != null && walkFrames.Length > 0 ? walkFrames[0] : null);
    }

    private bool CanSeePlayer(bool requireForwardCone)
    {
        if (playerCollider == null) return false;
        Vector2 eye = body.position + Vector2.up * 1f;
        Vector2 target = GetPlayerAimBounds().center;
        Vector2 toPlayer = target - eye;
        float distance = toPlayer.magnitude;
        if (distance > Mathf.Max(0.1f, settings.detectionRange)) return false;
        if (requireForwardCone)
        {
            Vector2 forward = new Vector2(facingDirection, 0f);
            float minimumDot = Mathf.Cos(Mathf.Clamp(settings.visionAngle, 1f, 180f) * 0.5f * Mathf.Deg2Rad);
            if (Vector2.Dot(forward, toPlayer.normalized) < minimumDot) return false;
        }

        Camera camera = Camera.main;
        if (camera != null)
        {
            Vector3 beastView = camera.WorldToViewportPoint(body.position);
            Vector3 playerView = camera.WorldToViewportPoint(target);
            if (beastView.z <= 0f || playerView.z <= 0f ||
                beastView.x < 0f || beastView.x > 1f || beastView.y < 0f || beastView.y > 1f ||
                playerView.x < 0f || playerView.x > 1f || playerView.y < 0f || playerView.y > 1f)
                return false;
        }

        RaycastHit2D[] hits = Physics2D.RaycastAll(eye, toPlayer.normalized, distance, settings.visionObstacleLayers);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i].collider;
            if (hit == null || hit == beastCollider || hit.transform == transform || hit.transform.IsChildOf(transform)) continue;
            if (hit == playerCollider || hit.transform == playerCollider.transform ||
                hit.transform.IsChildOf(playerCollider.transform)) continue;
            return false;
        }
        return true;
    }

    private void FacePlayer()
    {
        if (playerCollider == null) return;
        float deltaX = GetPlayerAimBounds().center.x - body.position.x;
        if (Mathf.Abs(deltaX) > 0.01f) facingDirection = Mathf.Sign(deltaX);
        SetFacing(facingDirection);
    }

    private void SetFacing(float direction)
    {
        if (spriteRenderer == null) return;
        spriteRenderer.flipX = direction < 0f;
        AlignArtToCenter();
    }

    private void SetSprite(Sprite sprite)
    {
        if (spriteRenderer == null || sprite == null) return;
        spriteRenderer.sprite = sprite;
        AlignArtToCenter();
    }

    private void SetAttackSorting(bool attacking)
    {
        if (spriteRenderer == null) return;
        int playerOrder = playerSpriteRenderer != null ? playerSpriteRenderer.sortingOrder : 10;
        spriteRenderer.sortingOrder = attacking ? playerOrder + 1 : 8;
    }

    private Bounds GetPlayerAimBounds()
    {
        if (playerSpriteRenderer != null && playerSpriteRenderer.sprite != null)
        {
            // Sprite frames have bottom-left pivots, so their rendered bounds
            // center shifts with each frame. Aim from the stable player root
            // and apply only the small offset toward the player's facing side.
            Bounds renderedBounds = playerSpriteRenderer.bounds;
            Vector3 center = renderedBounds.center;
            float facing = playerSpriteRenderer.flipX ? -1f : 1f;
            center.x = playerSpriteRenderer.transform.position.x +
                       facing * settings.landingOffsetTowardFacing;
            return new Bounds(center,
                new Vector3(settings.playerAimHalfWidth * 2f, renderedBounds.size.y, renderedBounds.size.z));
        }
        return playerCollider != null ? playerCollider.bounds : new Bounds(transform.position, Vector3.one);
    }

    private void MoveTo(Vector2 position)
    {
        body.MovePosition(position);
    }

    private void UpdateWalkingAnimation()
    {
        if (spriteRenderer == null || walkFrames == null || walkFrames.Length == 0) return;
        walkTimer += Time.fixedDeltaTime;
        SetSprite(walkFrames[Mathf.FloorToInt(walkTimer * 7f) % walkFrames.Length]);
    }

    public void TakeHit()
    {
        if (destroyed) return;
        destroyed = true;
        StopAllCoroutines();
        if (stage2Encounter != null) stage2Encounter.OnBlobBeastDestroyed();
        else if (stage3Encounter != null) stage3Encounter.OnHardBeastDestroyed(GetEntityId());
        Destroy(gameObject);
    }

    private bool IsEncounterFinished => stage2Encounter != null
        ? stage2Encounter.IsFinished
        : stage3Encounter == null || stage3Encounter.IsFinished;

    private void AlignArtToCenter()
    {
        if (spriteRenderer == null || artTransform == null) return;
        Vector3 artPosition = artTransform.localPosition;
        float halfWidth = spriteRenderer.sprite != null
            ? spriteRenderer.sprite.bounds.size.x * artTransform.localScale.x * 0.5f
            : 1.15f;
        artPosition.x = spriteRenderer.flipX ? halfWidth : -halfWidth;
        artTransform.localPosition = artPosition;
    }
}
