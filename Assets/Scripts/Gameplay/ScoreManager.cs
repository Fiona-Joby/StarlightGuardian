using UnityEngine;

/// <summary>
/// Centralized scoring system for Starlight Guardian.
///
/// Subscribes to <see cref="StarEvents.StarCaught"/> and awards points based
/// on the star type and the current <see cref="ScoreMultiplier"/>.
///
/// Normal star → 1 × multiplier points and one star toward the objective.
/// Rainbow star → no points, but counts toward the objective and activates 2×.
///
/// Fires <see cref="ScoreChanged"/> whenever a star is caught and
/// <see cref="StageGoalReached"/> once when the target is met.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    /// <summary>Current accumulated score.</summary>
    public int Score { get; private set; }

    /// <summary>The number of stars required to complete Stage 1 at this difficulty.</summary>
    public int StageGoal => GameDifficulty.Stage1StarGoal;

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

        // Rainbow stars count toward the stage objective but don't award points.
        if (star.Type != StarType.Rainbow)
            Score += Mathf.RoundToInt(1 * ScoreMultiplier.CurrentMultiplier);
        ScoreChanged?.Invoke(Score, StageGoal);

        if (StarsCollected >= StageGoal && !goalReached)
        {
            goalReached = true;
            StageGoalReached?.Invoke();
        }
    }
}
