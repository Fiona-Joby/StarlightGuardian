using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Routes scene music and shared gameplay sound effects.</summary>
public sealed class GameAudio : MonoBehaviour
{
    private static GameAudio instance;
    private AudioSource musicSource;
    private AudioSource sfxSource;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (instance != null) return;
        var host = new GameObject("Game Audio");
        instance = host.AddComponent<GameAudio>();
        DontDestroyOnLoad(host);
    }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        GameObject musicObject = new GameObject("Music Audio");
        musicObject.transform.SetParent(transform, false);
        musicSource = musicObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.spatialBlend = 0f;
        GameObject sfxObject = new GameObject("Gameplay SFX Audio");
        sfxObject.transform.SetParent(transform, false);
        sfxSource = sfxObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;
        SceneManager.sceneLoaded += OnSceneLoaded;
        PlaySceneMusic(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        if (instance != this) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => PlaySceneMusic(scene.name);

    private void PlaySceneMusic(string sceneName)
    {
        string clipName = sceneName == "MainMenu" ? "Bgm 1 Calming" :
            sceneName == "MainScene" ? "Bgm 2 Calming" :
            sceneName == "Stage2" ? "Bgm 6" :
            sceneName == "Stage3" ? "Bgm 4" : null;
        AudioClip clip = clipName != null ? Resources.Load<AudioClip>("Audio/" + clipName) : null;
        if (musicSource.clip == clip && musicSource.isPlaying) return;
        musicSource.Stop();
        musicSource.clip = clip;
        if (clip != null)
        {
            musicSource.volume = PlayerPrefs.GetFloat("MusicVolume", 0.8f);
            musicSource.Play();
        }
    }

    public static void PlaySfx(string clipName)
    {
        if (instance == null || instance.sfxSource == null) return;
        AudioClip clip = Resources.Load<AudioClip>("Audio/" + clipName);
        if (clip == null) return;
        instance.sfxSource.volume = PlayerPrefs.GetFloat("SfxVolume", 0.8f);
        instance.sfxSource.PlayOneShot(clip);
    }
}
