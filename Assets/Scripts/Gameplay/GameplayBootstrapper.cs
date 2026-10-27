using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Automatically creates the ScoreManager, ScoreProgressBar, StageManager,
/// and ParallaxLayerSetup at game start if they are not already present.
///
/// Uses [RuntimeInitializeOnLoadMethod] to register a sceneLoaded callback
/// so bootstrapping runs on every scene load (initial + transitions).
///
/// On Stage 2, the ScoreProgressBar (points bar) is NOT created, and the
/// Stage2ParallaxSetup is used instead of ParallaxLayerSetup.
/// The Player is already embedded in the Stage2 scene file.
/// </summary>
public static class GameplayBootstrapper
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        // Subscribe once; this survives scene loads because it's a static handler.
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Bootstrap(scene.name);
    }

    private static void Bootstrap(string sceneName)
    {
        bool isStage2 = sceneName == "Stage2";

        // --- ScoreManager ---
        if (Object.FindAnyObjectByType<ScoreManager>() == null)
        {
            var go = new GameObject("ScoreManager");
            go.AddComponent<ScoreManager>();
            Debug.Log("[GameplayBootstrapper] Created ScoreManager.");
        }

        // --- ScoreProgressBar (skip on Stage 2) ---
        if (!isStage2 && Object.FindAnyObjectByType<ScoreProgressBar>() == null)
        {
            ScoreManager sm = Object.FindAnyObjectByType<ScoreManager>();
            var host = (sm != null) ? sm.gameObject : new GameObject("ScoreUI");
            host.AddComponent<ScoreProgressBar>();
            Debug.Log("[GameplayBootstrapper] Created ScoreProgressBar.");
        }

        // --- StageManager (only needed on Stage 1) ---
        if (!isStage2 && Object.FindAnyObjectByType<StageManager>() == null)
        {
            ScoreManager sm = Object.FindAnyObjectByType<ScoreManager>();
            var host = (sm != null) ? sm.gameObject : new GameObject("StageManager");
            host.AddComponent<StageManager>();
            Debug.Log("[GameplayBootstrapper] Created StageManager.");
        }

        // --- Parallax background ---
        if (isStage2)
        {
            if (Object.FindAnyObjectByType<Stage2ParallaxSetup>() == null)
            {
                var bgGO = new GameObject("Stage2Parallax_Auto");
                bgGO.AddComponent<Stage2ParallaxSetup>();
                Debug.Log("[GameplayBootstrapper] Created Stage2ParallaxSetup.");
            }
        }
        else
        {
            if (Object.FindAnyObjectByType<ParallaxLayerSetup>() == null)
            {
                var bgGO = new GameObject("ParallaxBackground_Auto");
                bgGO.AddComponent<ParallaxLayerSetup>();
                Debug.Log("[GameplayBootstrapper] Created ParallaxLayerSetup.");
            }
        }

        // --- Platforms ---
        if (Object.FindAnyObjectByType<PlatformSpawner>() == null)
        {
            var platGO = new GameObject("PlatformSpawner_Auto");
            platGO.AddComponent<PlatformSpawner>();
            Debug.Log("[GameplayBootstrapper] Created PlatformSpawner.");
        }
    }
}
