using System.Collections;
using UnityEngine;

/// <summary>
/// Persistent singleton that hosts coroutines for the static ScoreMultiplier class.
/// DontDestroyOnLoad so the timed reset survives without a scene manager.
/// </summary>
public class ScoreMultiplierTimer : MonoBehaviour
{
    private static ScoreMultiplierTimer instance;

    public static void Run(IEnumerator routine)
    {
        if (instance == null)
        {
            var host = new GameObject(nameof(ScoreMultiplierTimer));
            DontDestroyOnLoad(host);
            instance = host.AddComponent<ScoreMultiplierTimer>();
        }
        instance.StartCoroutine(routine);
    }
}
