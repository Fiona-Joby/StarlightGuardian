using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Builds the shared in-game pause menu for each playable stage.</summary>
public class StageQuitButton : MonoBehaviour
{
    private const string ArtResource = "StagePauseArt";
    private const float DefaultVolume = 0.8f;

    private Canvas canvas;
    private GameObject pausePage;
    private GameObject settingsPage;
    private Slider musicSlider;
    private Slider sfxSlider;
    private Text controlsLabel;

    private void Awake()
    {
        StagePauseArt art = Resources.Load<StagePauseArt>(ArtResource);
        if (art == null)
        {
            Debug.LogError("StagePauseArt is missing from Assets/Resources. The pause menu cannot be created.");
            return;
        }

        canvas = RuntimeUiUtility.CreateOverlayCanvas(
            "Stage Pause Canvas", 500, new Vector2(1920f, 1080f));
        RuntimeUiUtility.EnsureEventSystem();

        Button pauseButton = CreateImageButton(
            "Pause Button", canvas.transform, art.pauseButton, new Vector2(100f, 100f),
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-76f, -76f));
        pauseButton.onClick.AddListener(OpenPause);

        pausePage = CreatePage("Pause Page", canvas.transform);
        CreatePauseArt(pausePage.transform, art);
        CreateSettingsPage(art);
        pausePage.SetActive(false);
        settingsPage.SetActive(false);
    }

    private void Update()
    {
        if (canvas == null || !Input.GetKeyDown(KeyCode.Escape)) return;

        if (settingsPage.activeSelf)
            ShowPause();
        else if (pausePage.activeSelf)
            ResumeGame();
        else
            OpenPause();
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }

    private void OpenPause()
    {
        Time.timeScale = 0f;
        pausePage.SetActive(true);
        settingsPage.SetActive(false);
    }

    private void ShowPause()
    {
        pausePage.SetActive(true);
        settingsPage.SetActive(false);
    }

    private void ResumeGame()
    {
        Time.timeScale = 1f;
        settingsPage.SetActive(false);
        pausePage.SetActive(false);
    }

    private void RestartStage()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void QuitGame()
    {
        Time.timeScale = 1f;
        Application.Quit();
    }

    private void CreatePauseArt(Transform parent, StagePauseArt art)
    {
        GameObject dimmer = new GameObject("Pause Screen Dimmer", typeof(RectTransform), typeof(Image));
        dimmer.transform.SetParent(parent, false);
        Stretch(dimmer.GetComponent<RectTransform>());
        Image dimmerImage = dimmer.GetComponent<Image>();
        dimmerImage.color = new Color(0f, 0f, 0f, 0.52f);
        dimmerImage.raycastTarget = true;

        Image frame = CreateImage("Pause Tab Background", parent, art.pauseTab);
        RectTransform frameRect = frame.rectTransform;
        frameRect.anchorMin = frameRect.anchorMax = new Vector2(0.5f, 0.5f);
        frameRect.sizeDelta = new Vector2(1280f, 762f);
        frame.raycastTarget = false;

        GameObject options = new GameObject("Pause Options", typeof(RectTransform));
        options.transform.SetParent(parent, false);
        Stretch(options.GetComponent<RectTransform>());

        Texture2D[] optionTextures =
        {
            art.settingsOption, art.resumeOption, art.restartOption, art.quitOption
        };
        string[] optionNames = { "Settings", "Resume", "Restart", "Exit" };
        UnityEngine.Events.UnityAction[] actions =
        {
            ShowSettings, ResumeGame, RestartStage, QuitGame
        };
        float[] x = { 0.265f, 0.425f, 0.58f, 0.735f };
        for (int i = 0; i < optionNames.Length; i++)
        {
            Sprite optionSprite = CreateOptionSprite(optionTextures[i]);
            Button button = CreateImageButton(
                optionNames[i] + " Option", options.transform, optionSprite,
                new Vector2(240f, 292f), new Vector2(x[i], 0.49f),
                new Vector2(x[i], 0.49f), Vector2.zero);
            button.onClick.AddListener(actions[i]);
        }
    }

    private void CreateSettingsPage(StagePauseArt art)
    {
        settingsPage = CreatePage("Pause Settings Page", canvas.transform);

        Image dimmer = CreateImage("Settings Screen Dimmer", settingsPage.transform, null);
        Stretch(dimmer.rectTransform);
        dimmer.color = new Color(0f, 0f, 0f, 0.52f);
        dimmer.raycastTarget = true;

        Image frame = CreateImage("Settings Tab Background", settingsPage.transform, art.pauseTab);
        frame.rectTransform.anchorMin = frame.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        frame.rectTransform.sizeDelta = new Vector2(1340f, 798f);
        frame.raycastTarget = false;

        // Cover the pause artwork's center while leaving its decorative frame visible.
        Image panel = CreateImage("Settings Content Panel", settingsPage.transform, null);
        panel.color = new Color(0.31f, 0.02f, 0.93f, 0.98f);
        panel.rectTransform.anchorMin = panel.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        panel.rectTransform.sizeDelta = new Vector2(1200f, 630f);
        panel.raycastTarget = true;

        Text title = CreateLabel("Settings Title", settingsPage.transform, "SETTINGS", 64);
        Place(title.rectTransform, new Vector2(0.5f, 0.79f), new Vector2(620f, 90f));
        title.color = Color.white;

        Text musicLabel = CreateLabel("Music Volume Label", settingsPage.transform, "MUSIC VOLUME", 34);
        Place(musicLabel.rectTransform, new Vector2(0.5f, 0.66f), new Vector2(800f, 52f));
        musicSlider = CreateSlider("Music Volume", settingsPage.transform, new Vector2(0.5f, 0.57f));

        Text sfxLabel = CreateLabel("Sound FX Volume Label", settingsPage.transform, "SOUND FX VOLUME", 34);
        Place(sfxLabel.rectTransform, new Vector2(0.5f, 0.46f), new Vector2(800f, 52f));
        sfxSlider = CreateSlider("Sound FX Volume", settingsPage.transform, new Vector2(0.5f, 0.37f));

        Button controlsButton = RuntimeUiUtility.CreateButton(
            "Control Mode", settingsPage.transform, "", 32,
            new Color(0.08f, 0.01f, 0.38f, 1f), Color.white,
            new Vector2(600f, 76f));
        Place(controlsButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.25f), new Vector2(600f, 76f));
        controlsLabel = controlsButton.GetComponentInChildren<Text>();
        controlsButton.onClick.AddListener(ToggleControls);

        Button back = RuntimeUiUtility.CreateButton(
            "Back to Pause", settingsPage.transform, "BACK", 30,
            new Color(0.08f, 0.01f, 0.38f, 1f), Color.white,
            new Vector2(260f, 64f));
        Place(back.GetComponent<RectTransform>(), new Vector2(0.5f, 0.13f), new Vector2(260f, 64f));
        back.onClick.AddListener(ShowPause);

        musicSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("MusicVolume", DefaultVolume));
        sfxSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("SfxVolume", DefaultVolume));
        musicSlider.onValueChanged.AddListener(value =>
        {
            PlayerPrefs.SetFloat("MusicVolume", value);
            ApplyVolumes();
        });
        sfxSlider.onValueChanged.AddListener(value =>
        {
            PlayerPrefs.SetFloat("SfxVolume", value);
            ApplyVolumes();
        });
        RefreshControlLabel();
        ApplyVolumes();
    }

    private void ShowSettings()
    {
        musicSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("MusicVolume", DefaultVolume));
        sfxSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("SfxVolume", DefaultVolume));
        RefreshControlLabel();
        ApplyVolumes();
        settingsPage.SetActive(true);
        pausePage.SetActive(false);
    }

    private void ToggleControls()
    {
        PlayerPrefs.SetInt("GestureControls", PlayerPrefs.GetInt("GestureControls", 0) == 1 ? 0 : 1);
        RefreshControlLabel();
    }

    private void RefreshControlLabel()
    {
        if (controlsLabel == null) return;
        string mode = PlayerPrefs.GetInt("GestureControls", 0) == 1 ? "GESTURE" : "KEYBOARD";
        controlsLabel.text = "CONTROLS: " + mode;
    }

    private static void ApplyVolumes()
    {
        float music = PlayerPrefs.GetFloat("MusicVolume", DefaultVolume);
        float sfx = PlayerPrefs.GetFloat("SfxVolume", DefaultVolume);
        foreach (AudioSource source in FindObjectsByType<AudioSource>())
            source.volume = source.gameObject.name.ToLowerInvariant().Contains("music") ? music : sfx;
    }

    private static GameObject CreatePage(string name, Transform parent)
    {
        GameObject page = new GameObject(name, typeof(RectTransform));
        page.transform.SetParent(parent, false);
        Stretch(page.GetComponent<RectTransform>());
        return page;
    }

    private static Button CreateImageButton(
        string name, Transform parent, Sprite sprite, Vector2 size,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.color = Color.white;
        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        return button;
    }

    private static Image CreateImage(string name, Transform parent, Sprite sprite)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        return image;
    }

    private static Text CreateLabel(string name, Transform parent, string value, int size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);
        Text text = go.GetComponent<Text>();
        text.font = RuntimeUiUtility.GetFont();
        text.fontSize = size;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.text = value;
        text.raycastTarget = false;
        return text;
    }

    private static Slider CreateSlider(string name, Transform parent, Vector2 anchor)
    {
        GameObject go = new GameObject(name + " Slider", typeof(RectTransform), typeof(Slider));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = anchor;
        rect.sizeDelta = new Vector2(720f, 34f);

        Image track = CreateImage("Track", go.transform, null);
        track.color = new Color(0.1f, 0.06f, 0.28f, 1f);
        Stretch(track.rectTransform);
        Image fill = CreateImage("Fill", go.transform, null);
        fill.color = new Color(0.86f, 1f, 0.02f, 1f);
        RectTransform fillRect = fill.rectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(4f, 4f);
        fillRect.offsetMax = new Vector2(-4f, -4f);

        GameObject handleGO = new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        handleGO.transform.SetParent(go.transform, false);
        Image handleImage = handleGO.GetComponent<Image>();
        handleImage.color = Color.white;
        RectTransform handle = handleGO.GetComponent<RectTransform>();
        handle.sizeDelta = new Vector2(30f, 50f);

        Slider slider = go.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.fillRect = fillRect;
        slider.handleRect = handle;
        slider.targetGraphic = handleImage;
        return slider;
    }

    private static Sprite CreateOptionSprite(Texture2D texture)
    {
        if (texture == null) return null;
        // The supplied English option textures are 1920x1080 sheets. This crop
        // contains the complete circular icon and its label, excluding the empty canvas.
        Rect contentRect = new Rect(600f, 100f, 700f, 850f);
        return Sprite.Create(texture, contentRect,
            new Vector2(0.5f, 0.5f), 100f);
    }

    private static void Place(RectTransform rect, Vector2 anchor, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = anchor;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
