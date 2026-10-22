using UnityEngine;

/// <summary>
/// Shield power for Stage 2: activated by a fist gesture (webcam) or Enter key.
/// 
/// When activated, the player is invulnerable for 6 seconds.
/// A single animation clip plays through all five shielding frames:
///   Shielding 1 (0.008s) → 2 (instant) → 3 (5.976s hold) → 4 (0.008s) → 5 (0.008s)
///
/// Drives the Animator's "isShielding" boolean.
/// While the shield is active, a countdown timer is drawn on screen.
/// </summary>
public class PlayerShield : MonoBehaviour
{
    [Header("Shield Settings")]
    [Tooltip("Total shield duration in seconds.")]
    [SerializeField, Min(0.1f)] private float shieldDuration = 6f;

    [Tooltip("Cooldown before the shield can be used again (seconds).")]
    [SerializeField, Min(0f)] private float cooldown = 10f;

    /// <summary>True while the shield is active.</summary>
    public bool IsShielding { get; private set; }

    private Animator animator;
    private PlayerInput playerInput;

    private float shieldTimer;
    private float cooldownTimer;

    // Animator parameter hash.
    private static readonly int IsShieldingParam = Animator.StringToHash("isShielding");

    // ---- GUI ----
    private Texture2D timerBgTex;
    private Texture2D timerFillTex;
    private GUIStyle timerStyle;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        playerInput = GetComponent<PlayerInput>();
        timerBgTex = MakeTex(new Color(0.05f, 0.05f, 0.15f, 0.7f));
        timerFillTex = MakeTex(new Color(0.3f, 0.75f, 1f, 0.9f));
    }

    private void Update()
    {
        // Cool-down countdown.
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        // --- Activation ---
        bool activatePressed = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
        if (playerInput != null)
            activatePressed = activatePressed || playerInput.ShieldPressed;

        if (activatePressed && !IsShielding && cooldownTimer <= 0f)
            ActivateShield();

        // --- Active shield tick ---
        if (!IsShielding) return;

        shieldTimer -= Time.deltaTime;

        // Shield expired.
        if (shieldTimer <= 0f)
            DeactivateShield();
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

    // ------------------------------------------------------------------ //
    //  On-screen timer HUD
    // ------------------------------------------------------------------ //

    private void OnGUI()
    {
        if (!IsShielding) return;

        // Lazily create style.
        if (timerStyle == null)
        {
            timerStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 16
            };
            timerStyle.normal.textColor = Color.white;
        }

        float barW = 180f;
        float barH = 24f;
        float x = (Screen.width - barW) * 0.5f;
        float y = Screen.height - 50f;
        Rect bgRect = new Rect(x, y, barW, barH);

        // Background.
        GUI.DrawTexture(bgRect, timerBgTex);

        // Fill (shrinks as timer runs out).
        float fill = Mathf.Clamp01(shieldTimer / shieldDuration);
        Color fillCol = Color.Lerp(
            new Color(1f, 0.3f, 0.3f, 0.9f),   // red when nearly out
            new Color(0.3f, 0.75f, 1f, 0.9f),   // cyan when full
            fill);
        SetTexColor(timerFillTex, fillCol);
        Rect fillRect = new Rect(bgRect.x + 2, bgRect.y + 2, (bgRect.width - 4) * fill, bgRect.height - 4);
        GUI.DrawTexture(fillRect, timerFillTex);

        // Label.
        GUI.Label(bgRect, $"Shield: {shieldTimer:F1}s", timerStyle);
    }

    // ------------------------------------------------------------------ //
    //  Helpers
    // ------------------------------------------------------------------ //

    private static Texture2D MakeTex(Color col)
    {
        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, col);
        tex.Apply();
        return tex;
    }

    private static void SetTexColor(Texture2D tex, Color col)
    {
        tex.SetPixel(0, 0, col);
        tex.Apply();
    }
}
