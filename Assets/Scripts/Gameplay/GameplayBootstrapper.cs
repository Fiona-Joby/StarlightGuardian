using UnityEngine;

/// <summary>
/// Automatically creates the ScoreManager, ScoreProgressBar, StageManager,
/// and ParallaxLayerSetup at game start if they are not already present.
///
/// Uses [RuntimeInitializeOnLoadMethod] so no manual GameObject setup
/// is needed in the Unity Editor.
/// </summary>
public static class GameplayBootstrapper
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        // --- ScoreManager ---
        if (Object.FindAnyObjectByType<ScoreManager>() == null)
        {
            var go = new GameObject("ScoreManager");
            go.AddComponent<ScoreManager>();
            Debug.Log("[GameplayBootstrapper] Created ScoreManager.");
        }

        // --- ScoreProgressBar ---
        if (Object.FindAnyObjectByType<ScoreProgressBar>() == null)
        {
            ScoreManager sm = Object.FindAnyObjectByType<ScoreManager>();
            var host = (sm != null) ? sm.gameObject : new GameObject("ScoreUI");
            host.AddComponent<ScoreProgressBar>();
            Debug.Log("[GameplayBootstrapper] Created ScoreProgressBar.");
        }

        // --- StageManager ---
        if (Object.FindAnyObjectByType<StageManager>() == null)
        {
            ScoreManager sm = Object.FindAnyObjectByType<ScoreManager>();
            var host = (sm != null) ? sm.gameObject : new GameObject("StageManager");
            host.AddComponent<StageManager>();
            Debug.Log("[GameplayBootstrapper] Created StageManager.");
        }

        // --- ParallaxLayerSetup ---
        if (Object.FindAnyObjectByType<ParallaxLayerSetup>() == null)
        {
            var bgGO = new GameObject("ParallaxBackground_Auto");
            bgGO.AddComponent<ParallaxLayerSetup>();
            Debug.Log("[GameplayBootstrapper] Created ParallaxLayerSetup.");
        }
    }
}
