using System.Collections;
using UnityEngine;

/// <summary>
/// Global score multiplier. Rainbow star catch activates a temporary 2× multiplier.
/// Uses a version counter so overlapping activations don't corrupt the reset.
/// </summary>
public static class ScoreMultiplier
{
    public static float CurrentMultiplier { get; private set; } = 1f;
    public static event System.Action<float> MultiplierChanged;
    private static int activationVersion;

    public static void ActivateTemporaryDouble(float durationSeconds)
    {
        activationVersion++;
        int version = activationVersion;
        CurrentMultiplier = 2f;
        MultiplierChanged?.Invoke(CurrentMultiplier);
        ScoreMultiplierTimer.Run(ResetAfter(durationSeconds, version));
    }

    private static IEnumerator ResetAfter(float duration, int version)
    {
        yield return new WaitForSeconds(duration);
        if (version != activationVersion) yield break;
        CurrentMultiplier = 1f;
        MultiplierChanged?.Invoke(CurrentMultiplier);
    }
}
