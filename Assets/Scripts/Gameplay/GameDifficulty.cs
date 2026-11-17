using UnityEngine;

/// <summary>Local rule-based difficulty selection and bounded stage-to-stage adjustment.</summary>
public static class GameDifficulty
{
    private const string LevelKey = "StarlightDifficulty.Level";
    private const string AutomaticKey = "StarlightDifficulty.Automatic";

    public static int Level => Mathf.Clamp(PlayerPrefs.GetInt(LevelKey, 0), 0, 2);
    public static bool IsAutomatic => PlayerPrefs.GetInt(AutomaticKey, 1) == 1;

    public static int Stage1StarGoal => Level == 0 ? 10 : Level == 1 ? 25 : 35;

    public static int Stage2TurretGoal(int normalGoal) => Level == 0 ? 2 :
        Level == 1 ? Mathf.Max(2, normalGoal) : Mathf.Max(4, normalGoal + 2);
    public static int Stage2BeastGoal(int normalGoal) => Level == 0 ? 2 :
        Level == 1 ? Mathf.Max(2, normalGoal) : Mathf.Max(4, normalGoal + 2);

    public static int Stage3DroneGoal(int normalGoal) => Level == 0 ? 2 :
        Level == 1 ? Mathf.Max(2, normalGoal) : Mathf.Max(4, normalGoal + 2);
    public static int Stage3SlimeGoal(int normalGoal) => Level == 0 ? 2 :
        Level == 1 ? Mathf.Max(2, normalGoal) : Mathf.Max(4, normalGoal + 2);
    public static int Stage3HardBeastGoal(int normalGoal) => Level == 0 ? 2 :
        Level == 1 ? Mathf.Max(2, normalGoal) : Mathf.Max(4, normalGoal + 2);

    public static void SetManual(int level)
    {
        PlayerPrefs.SetInt(LevelKey, Mathf.Clamp(level, 0, 2));
        PlayerPrefs.SetInt(AutomaticKey, 0);
        PlayerPrefs.Save();
    }

    public static void EnableAutomatic()
    {
        PlayerPrefs.SetInt(AutomaticKey, 1);
        PlayerPrefs.Save();
    }

    public static void AdjustAfterStage1(float durationSeconds, int starsCollected)
    {
        if (starsCollected <= 0) return;
        float secondsPerStar = durationSeconds / starsCollected;
        if (secondsPerStar <= 5f) Adjust(1);
        else if (secondsPerStar >= 12f) Adjust(-1);
    }

    public static void AdjustAfterCombat(float damageTaken, int lifeLimit)
    {
        if (lifeLimit <= 0) return;
        float damageRatio = damageTaken / lifeLimit;
        if (damageRatio <= 0.2f) Adjust(1);
        else if (damageRatio >= 0.6f) Adjust(-1);
    }

    private static void Adjust(int delta)
    {
        if (!IsAutomatic) return;
        PlayerPrefs.SetInt(LevelKey, Mathf.Clamp(Level + delta, 0, 2));
        PlayerPrefs.Save();
    }
}
