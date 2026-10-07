using UnityEngine;

/// <summary>
/// Self-bootstrapping score HUD using OnGUI.
/// Creates itself automatically via [RuntimeInitializeOnLoadMethod].
/// Does NOT depend on any scene setup, Canvas, fonts, or prefabs.
/// </summary>
public class ScoreHUD : MonoBehaviour
{
    private static ScoreHUD instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (instance != null) return;
        var go = new GameObject("ScoreHUD");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<ScoreHUD>();
        Debug.Log("[ScoreHUD] Created.");
    }

    private int score;
    private float fill;
    private bool multiplierOn;
    private bool subscribed;
    private Texture2D barBg;
    private Texture2D barFill;
    private GUIStyle textStyle;

    private void Awake()
    {
        barBg = MakeTex(new Color(0.05f, 0.05f, 0.12f, 0.85f));
        barFill = MakeTex(new Color(1f, 0.85f, 0.2f, 1f));
    }

    private void Update()
    {
        if (!subscribed && ScoreManager.Instance != null)
        {
            ScoreManager.Instance.ScoreChanged += (s, g) => score = s;
            subscribed = true;
            Debug.Log("[ScoreHUD] Subscribed to ScoreManager.");
        }
        ScoreMultiplier.MultiplierChanged -= OnMult;
        ScoreMultiplier.MultiplierChanged += OnMult;

        int goal = ScoreManager.Instance != null ? ScoreManager.Instance.StageGoal : 35;
        float target = goal > 0 ? Mathf.Clamp01((float)score / goal) : 0f;
        fill = Mathf.MoveTowards(fill, target, Time.deltaTime * 2f);
    }

    private void OnMult(float m) => multiplierOn = m > 1.01f;

    private void OnGUI()
    {
        if (textStyle == null)
        {
            textStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = Mathf.Max(16, Screen.height / 40)
            };
            textStyle.normal.textColor = Color.white;
        }

        float h = 34f;
        float pad = 20f;
        float w = Screen.width - pad * 2f;
        Rect bg = new Rect(pad, 10f, w, h);

        // Background bar.
        GUI.DrawTexture(bg, barBg);

        // Fill bar — lerps gold → blue.
        Color c = Color.Lerp(new Color(1f, 0.85f, 0.2f), new Color(0.3f, 0.85f, 1f), fill);
        barFill.SetPixel(0, 0, c);
        barFill.Apply();
        GUI.DrawTexture(new Rect(bg.x + 2, bg.y + 2, (bg.width - 4) * fill, bg.height - 4), barFill);

        // Score text.
        int goal = ScoreManager.Instance != null ? ScoreManager.Instance.StageGoal : 35;
        GUI.Label(bg, $"Score: {score} / {goal}", textStyle);

        // Multiplier.
        if (multiplierOn)
        {
            var ms = new GUIStyle(textStyle);
            ms.alignment = TextAnchor.MiddleRight;
            float pulse = 0.7f + Mathf.Sin(Time.time * 5f) * 0.3f;
            ms.normal.textColor = new Color(1f, 0.4f, 0.9f, pulse);
            GUI.Label(new Rect(bg.x, bg.y, bg.width - 10, bg.height), "×2", ms);
        }
    }

    private static Texture2D MakeTex(Color col)
    {
        var t = new Texture2D(1, 1);
        t.SetPixel(0, 0, col);
        t.Apply();
        return t;
    }
}
