using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shield power for Stage 2: activated by a fist gesture (webcam) or Enter key.
/// 
/// When activated, the player is invulnerable for 6 seconds.
/// A single animation clip plays through all five shielding frames:
///   Shielding 1 (0.008s) → 2 (instant) → 3 (5.976s hold) → 4 (0.008s) → 5 (0.008s)
///
/// Drives the Animator's "isShielding" boolean.
/// A HUD shield icon fades during activation and restores during cooldown.
/// </summary>
public class PlayerShield : MonoBehaviour
{
    [Header("Shield Settings")]
    [Tooltip("Total shield duration in seconds.")]
    [SerializeField, Min(0.1f)] private float shieldDuration = 6f;

    [Tooltip("Cooldown before the shield can be used again (seconds).")]
    [SerializeField, Min(0f)] private float cooldown = 10f;

    [SerializeField] private Sprite[] shieldRunFrames = new Sprite[6];
    [SerializeField] private Sprite shieldJumpSprite;
    [SerializeField] private Sprite shieldHudSprite;

    /// <summary>True while the shield is active.</summary>
    public bool IsShielding { get; private set; }

    private Animator animator;
    private PlayerInput playerInput;
    private PlayerMovement playerMovement;
    private SpriteRenderer spriteRenderer;
    private bool isPlayingShieldRun;
    private float shieldRunFrameTimer;
    private int shieldRunFrameIndex;
    private const float ShieldRunFrameDuration = 0.1f;

    private float shieldTimer;
    private float cooldownTimer;

    // Animator parameter hash.
    private static readonly int IsShieldingParam = Animator.StringToHash("isShielding");

    private Image shieldHudIcon;
    private CanvasGroup shieldHudGroup;
    private Text shieldHudTimer;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        playerInput = GetComponent<PlayerInput>();
        playerMovement = GetComponent<PlayerMovement>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (shieldHudSprite != null)
        {
            Canvas canvas = RuntimeUiUtility.CreateOverlayCanvas("Shield HUD", 100, new Vector2(1920f, 1080f));
            GameObject icon = new GameObject("Shield Status", typeof(RectTransform), typeof(Image));
            icon.transform.SetParent(canvas.transform, false);
            RectTransform rect = icon.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-32f, 32f);
            rect.sizeDelta = new Vector2(140f, 140f);
            shieldHudIcon = icon.GetComponent<Image>();
            shieldHudIcon.sprite = shieldHudSprite;
            shieldHudIcon.preserveAspect = true;
            shieldHudIcon.raycastTarget = false;
            shieldHudGroup = icon.AddComponent<CanvasGroup>();
            shieldHudGroup.alpha = 1f;

            GameObject timer = new GameObject("Shield Time Left", typeof(RectTransform), typeof(Text));
            timer.transform.SetParent(canvas.transform, false);
            RectTransform timerRect = timer.GetComponent<RectTransform>();
            timerRect.anchorMin = timerRect.anchorMax = new Vector2(1f, 0f);
            timerRect.pivot = new Vector2(1f, 0f);
            timerRect.anchoredPosition = new Vector2(-40f, 40f);
            timerRect.sizeDelta = new Vector2(76f, 38f);
            shieldHudTimer = timer.GetComponent<Text>();
            shieldHudTimer.font = RuntimeUiUtility.GetFont();
            shieldHudTimer.fontSize = 28;
            shieldHudTimer.fontStyle = FontStyle.Bold;
            shieldHudTimer.alignment = TextAnchor.MiddleRight;
            shieldHudTimer.color = Color.white;
            shieldHudTimer.raycastTarget = false;
            shieldHudTimer.enabled = false;
        }
    }

    private void Update()
    {
        // Cool-down countdown.
        if (cooldownTimer > 0f)
            cooldownTimer = Mathf.Max(0f, cooldownTimer - Time.deltaTime);

        // --- Activation ---
        bool activatePressed = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
        if (playerInput != null)
            activatePressed = activatePressed || playerInput.ShieldPressed;

        if (activatePressed && !IsShielding && cooldownTimer <= 0f)
            ActivateShield();

        // --- Active shield tick ---
        if (IsShielding)
        {
            shieldTimer -= Time.deltaTime;
            if (shieldTimer <= 0f)
                DeactivateShield();
        }

        UpdateShieldIcon();
    }

    private void LateUpdate()
    {
        bool shouldOverrideShieldPose = IsShielding && playerMovement != null &&
            (playerMovement.IsInAir || (playerMovement.IsHorizontallyMoving && AllRunFramesLoaded())) &&
            spriteRenderer != null && animator != null;

        if (shouldOverrideShieldPose != isPlayingShieldRun)
        {
            isPlayingShieldRun = shouldOverrideShieldPose;
            animator.enabled = !isPlayingShieldRun;
            shieldRunFrameTimer = 0f;
            shieldRunFrameIndex = 0;
        }

        if (!isPlayingShieldRun) return;

        if (playerMovement.IsInAir && shieldJumpSprite != null)
        {
            spriteRenderer.sprite = shieldJumpSprite;
            return;
        }

        if (!AllRunFramesLoaded()) return;

        shieldRunFrameTimer += Time.deltaTime;
        while (shieldRunFrameTimer >= ShieldRunFrameDuration)
        {
            shieldRunFrameTimer -= ShieldRunFrameDuration;
            shieldRunFrameIndex = (shieldRunFrameIndex + 1) % shieldRunFrames.Length;
        }
        spriteRenderer.sprite = shieldRunFrames[shieldRunFrameIndex];
    }

    private bool AllRunFramesLoaded()
    {
        for (int i = 0; i < shieldRunFrames.Length; i++)
            if (shieldRunFrames[i] == null) return false;
        return true;
    }

    private void ActivateShield()
    {
        IsShielding = true;
        shieldTimer = shieldDuration;

        if (animator != null)
            animator.SetBool(IsShieldingParam, true);

        Debug.Log("[PlayerShield] Shield activated!");
    }

    private void DeactivateShield()
    {
        IsShielding = false;
        cooldownTimer = cooldown;

        if (animator != null)
            animator.SetBool(IsShieldingParam, false);

        Debug.Log("[PlayerShield] Shield deactivated.");
    }

    private void UpdateShieldIcon()
    {
        if (shieldHudIcon == null) return;
        if (IsShielding)
        {
            float remaining = shieldDuration > 0f ? Mathf.Clamp01(shieldTimer / shieldDuration) : 0f;
            shieldHudIcon.color = Color.white;
            shieldHudGroup.alpha = remaining;
            shieldHudTimer.text = $"{Mathf.CeilToInt(Mathf.Max(0f, shieldTimer))}s";
            shieldHudTimer.enabled = true;
        }
        else if (cooldownTimer > 0f)
        {
            float restored = cooldown > 0f ? 1f - Mathf.Clamp01(cooldownTimer / cooldown) : 1f;
            shieldHudIcon.color = Color.Lerp(new Color(0.28f, 0.28f, 0.28f, 1f), Color.white, restored);
            shieldHudGroup.alpha = 1f;
            shieldHudTimer.enabled = false;
        }
        else
        {
            shieldHudIcon.color = Color.white;
            shieldHudGroup.alpha = 1f;
            shieldHudTimer.enabled = false;
        }
    }

    // ------------------------------------------------------------------ //
    //  Helpers
    // ------------------------------------------------------------------ //

}
