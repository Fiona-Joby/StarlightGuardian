using UnityEngine;

/// <summary>
/// Tracks score changes and displays the multiplier cue. Stage goals are shown
/// from the shared Task button instead of a persistent progress bar.
/// </summary>
public class ScoreProgressBar : MonoBehaviour
{
    private bool multiplierActive;
    private GUIStyle multiplierStyle;

    private void OnEnable()
    {
        ScoreMultiplier.MultiplierChanged += OnMultiplierChanged;
    }

    private void OnDisable()
    {
        ScoreMultiplier.MultiplierChanged -= OnMultiplierChanged;
    }

    private void OnGUI()
    {
        // Objective progress is shown from the Task button; keep the separate multiplier cue.
        if (!multiplierActive) return;
        if (multiplierStyle == null)
        {
            multiplierStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleRight,
                fontStyle = FontStyle.Bold,
                fontSize = 20
            };
        }
        float pulse = 0.7f + Mathf.Sin(Time.time * 5f) * 0.3f;
        multiplierStyle.normal.textColor = new Color(1f, 0.4f, 0.9f, pulse);
        GUI.Label(new Rect(Screen.width - 80f, 10f, 60f, 36f), "×2", multiplierStyle);
    }

    private void OnMultiplierChanged(float m)
    {
        multiplierActive = m > 1.01f;
    }

}
