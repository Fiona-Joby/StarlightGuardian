using UnityEngine;

/// <summary>
/// Centralized scoring system for Starlight Guardian.
///
/// Subscribes to <see cref="StarEvents.StarCaught"/> and awards points based
/// on the star type and the current <see cref="ScoreMultiplier"/>.
///
/// Yellow star → 5 × multiplier points. Blue star → 10 × multiplier points.
/// Rainbow star → no points, but activates the 2× multiplier.
///
/// Fires <see cref="ScoreChanged"/> whenever a star is caught and
/// <see cref="StageGoalReached"/> once when the target is met.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    /// <summary>Current accumulated score.</summary>
    public int Score { get; private set; }

    /// <summary>The score required to complete Stage 1 at this difficulty.</summary>
    public int StageGoal => GameDifficulty.Stage1PointGoal;

    /// <summary>Total stars caught during the current stage, including rainbow stars.</summary>
    public int StarsCollected { get; private set; }

    /// <summary>Fires whenever a star is caught. Args: (currentScore, starGoal).</summary>
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

        StarsCollected++;

        int points = star.Type switch
        {
            StarType.Yellow => 5,
            StarType.Blue => 10,
            StarType.Rainbow => 0,
            _ => 5
        };
        Score += Mathf.RoundToInt(points * ScoreMultiplier.CurrentMultiplier);
        ScoreChanged?.Invoke(Score, StageGoal);

        if (Score >= StageGoal && !goalReached)
        {
            goalReached = true;
            StageGoalReached?.Invoke();
        }
    }
}
