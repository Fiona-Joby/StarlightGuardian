using UnityEngine;

/// <summary>
/// Displays a score progress bar using OnGUI — the most reliable Unity
/// rendering method. No Canvas, no Font loading, no prefabs needed.
///
/// Shows:
///   • Dark background bar at the top of the screen
///   • Coloured fill bar that grows from left to right
///   • "Score: X / 35" text centred on the bar
///   • "×2" indicator when the multiplier is active
/// </summary>
public class ScoreProgressBar : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField] private float barHeight = 32f;
    [SerializeField] private float horizontalMargin = 20f;
    [SerializeField] private float topMargin = 10f;

    private int displayedScore;
    private float animatedFill;
    private bool multiplierActive;

    // Textures created once at runtime for GUI drawing.
    private Texture2D bgTex;
    private Texture2D fillTex;
    private Texture2D multiplierTex;
    private GUIStyle labelStyle;
    private GUIStyle multiplierStyle;
    private bool subscribedToScore;

    private void Awake()
    {
        // Create solid-colour textures for the bar.
        bgTex = MakeTex(1, 1, new Color(0.06f, 0.06f, 0.15f, 0.8f));
        fillTex = MakeTex(1, 1, new Color(1f, 0.85f, 0.2f, 1f));
        multiplierTex = MakeTex(1, 1, new Color(1f, 0.4f, 0.9f, 0.9f));
    }

    private void OnEnable()
    {
        ScoreMultiplier.MultiplierChanged += OnMultiplierChanged;
        TrySubscribe();
    }

    private void OnDisable()
    {
        ScoreMultiplier.MultiplierChanged -= OnMultiplierChanged;
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.ScoreChanged -= OnScoreChanged;
    }

    private void Update()
    {
        TrySubscribe();

        // Smooth fill animation.
        int goal = ScoreManager.Instance != null ? ScoreManager.Instance.StageGoal : 35;
        float target = goal > 0 ? Mathf.Clamp01((float)displayedScore / goal) : 0f;
        animatedFill = Mathf.MoveTowards(animatedFill, target, Time.deltaTime * 2f);
    }

    private void OnGUI()
    {
        // Lazily create styles (must happen during OnGUI).
        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 18
            };
            labelStyle.normal.textColor = Color.white;
        }
        if (multiplierStyle == null)
        {
            multiplierStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleRight,
                fontStyle = FontStyle.Bold,
                fontSize = 20
            };
            multiplierStyle.normal.textColor = new Color(1f, 0.4f, 0.9f, 1f);
        }

        float barW = Screen.width - horizontalMargin * 2f;
        Rect bgRect = new Rect(horizontalMargin, topMargin, barW, barHeight);

        // Background.
        GUI.DrawTexture(bgRect, bgTex);

        // Fill bar — lerp colour from gold to blue as it fills.
        Color fillCol = Color.Lerp(
            new Color(1f, 0.85f, 0.2f, 1f),
            new Color(0.3f, 0.85f, 1f, 1f),
            animatedFill
        );
        SetTexColor(fillTex, fillCol);
        Rect fillRect = new Rect(bgRect.x + 2, bgRect.y + 2, (bgRect.width - 4) * animatedFill, bgRect.height - 4);
        GUI.DrawTexture(fillRect, fillTex);

        // Score label.
        int goal = ScoreManager.Instance != null ? ScoreManager.Instance.StageGoal : 35;
        GUI.Label(bgRect, $"Score: {displayedScore} / {goal}", labelStyle);

        // Multiplier indicator.
        if (multiplierActive)
        {
            Rect multRect = new Rect(bgRect.xMax - 60, bgRect.y, 55, bgRect.height);
            float pulse = 0.7f + Mathf.Sin(Time.time * 5f) * 0.3f;
            multiplierStyle.normal.textColor = new Color(1f, 0.4f, 0.9f, pulse);
            GUI.Label(multRect, "×2", multiplierStyle);
        }
    }

    // ------------------------------------------------------------------ //

    private void TrySubscribe()
    {
        if (subscribedToScore) return;
        if (ScoreManager.Instance == null) return;
        ScoreManager.Instance.ScoreChanged += OnScoreChanged;
        subscribedToScore = true;
    }

    private void OnScoreChanged(int score, int goal)
    {
        displayedScore = score;
    }

    private void OnMultiplierChanged(float m)
    {
        multiplierActive = m > 1.01f;
    }

    // ------------------------------------------------------------------ //
    //  Helpers
    // ------------------------------------------------------------------ //

    private static Texture2D MakeTex(int w, int h, Color col)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var pixels = new Color[w * h];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = col;
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private static void SetTexColor(Texture2D tex, Color col)
    {
        tex.SetPixel(0, 0, col);
        tex.Apply();
    }
}
