using UnityEngine;

/// <summary>
/// Centralized scoring system for Starlight Guardian.
///
/// Subscribes to <see cref="StarEvents.StarCaught"/> and awards points based
/// on the star type and the current <see cref="ScoreMultiplier"/>.
///
/// Normal star → 1 × multiplier points.
/// Rainbow star → 0 points (activates the 2× multiplier instead).
///
/// Fires <see cref="ScoreChanged"/> whenever the score updates and
/// <see cref="StageGoalReached"/> once when the target is met.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    [Header("Stage 1 Settings")]
    [SerializeField] private int stageGoal = 35;

    /// <summary>Current accumulated score.</summary>
    public int Score { get; private set; }

    /// <summary>The target score needed to complete the current stage.</summary>
    public int StageGoal => stageGoal;

    /// <summary>Fires every time the score changes. Args: (currentScore, stageGoal).</summary>
    public event System.Action<int, int> ScoreChanged;

    /// <summary>Fires once when the player reaches the stage goal.</summary>
    public event System.Action StageGoalReached;

    private bool goalReached;

    // Singleton-lite for easy access from UI scripts.
    public static ScoreManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        StarEvents.StarCaught += OnStarCaught;
    }

    private void OnDisable()
    {
        StarEvents.StarCaught -= OnStarCaught;
    }

    private void OnStarCaught(StarMovement star)
    {
        if (goalReached) return;

        // Rainbow stars don't award points — they activate the multiplier.
        if (star.Type == StarType.Rainbow) return;

        int points = Mathf.RoundToInt(1 * ScoreMultiplier.CurrentMultiplier);
        Score += points;
        ScoreChanged?.Invoke(Score, stageGoal);

        if (Score >= stageGoal && !goalReached)
        {
            goalReached = true;
            StageGoalReached?.Invoke();
        }
    }
}
