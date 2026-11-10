using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Connects the scene-authored MainMenu hierarchy to game actions.</summary>
public class MainMenuController : MonoBehaviour
{
    private enum Language { English, Malayalam, Indonesian, Chinese }

    [Header("Pages")]
    [SerializeField] private GameObject mainPage;
    [SerializeField] private GameObject stagePage;
    [SerializeField] private GameObject languagePage;
    [SerializeField] private GameObject settingsPage;

    [Header("Navigation buttons")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button stageMenuButton;
    [SerializeField] private Button languageMenuButton;
    [SerializeField] private Button settingsMenuButton;
    [SerializeField] private Button[] stageButtons;
    [SerializeField] private Button[] languageButtons;
    [SerializeField] private Button[] backButtons;
    [SerializeField] private Button controlsButton;

    [Header("Localized labels")]
    [SerializeField] private Text[] mainLabels;
    [SerializeField] private Text stageTitle;
    [SerializeField] private Text languageTitle;
    [SerializeField] private Text settingsTitle;
    [SerializeField] private Text musicLabel;
    [SerializeField] private Text sfxLabel;

    [Header("Settings controls")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    private static readonly string[] SceneNames = { "MainScene", "Stage2", "Stage3" };
    private static readonly string[,] Translations =
    {
        { "NEW GAME", "പുതിയ കളി", "PERMAINAN BARU", "新游戏" },
        { "STAGE", "ഘട്ടം", "TAHAP", "关卡" },
        { "LANGUAGE", "ഭാഷ", "BAHASA", "语言" },
        { "SETTINGS", "ക്രമീകരണങ്ങൾ", "PENGATURAN", "设置" },
        { "BACK", "തിരികെ", "KEMBALI", "返回" },
        { "MUSIC VOLUME", "സംഗീത ശബ്ദം", "VOLUME MUSIK", "音乐音量" },
        { "SOUND FX VOLUME", "ശബ്ദ പ്രഭാവം", "VOLUME EFEK SUARA", "音效音量" },
        { "CONTROLS", "നിയന്ത്രണം", "KONTROL", "控制" },
        { "KEYBOARD", "കീബോർഡ്", "KEYBOARD", "键盘" },
        { "GESTURE", "ആംഗ്യം", "GESTUR", "手势" }
    };

    private int SelectedLanguage
    {
        get { return Mathf.Clamp(PlayerPrefs.GetInt("Language", 0), 0, 3); }
    }

    private void Awake()
    {
        newGameButton.onClick.AddListener(() => LoadStage(0));
        stageMenuButton.onClick.AddListener(() => ShowPage(stagePage));
        languageMenuButton.onClick.AddListener(() => ShowPage(languagePage));
        settingsMenuButton.onClick.AddListener(() => ShowSettings());

        for (int i = 0; i < stageButtons.Length; i++)
        {
            int stage = i;
            stageButtons[i].onClick.AddListener(() => LoadStage(stage));
        }
        for (int i = 0; i < languageButtons.Length; i++)
        {
            int selected = i;
            languageButtons[i].onClick.AddListener(() => SetLanguage(selected));
        }
        foreach (Button back in backButtons)
            back.onClick.AddListener(() => ShowPage(mainPage));

        controlsButton.onClick.AddListener(ToggleControls);
        musicSlider.minValue = sfxSlider.minValue = 0f;
        musicSlider.maxValue = sfxSlider.maxValue = 1f;
        musicSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("MusicVolume", 0.8f));
        sfxSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("SfxVolume", 0.8f));
        musicSlider.onValueChanged.AddListener(value => { PlayerPrefs.SetFloat("MusicVolume", value); ApplyVolumes(); });
        sfxSlider.onValueChanged.AddListener(value => { PlayerPrefs.SetFloat("SfxVolume", value); ApplyVolumes(); });
        ApplyLanguage();
        ApplyVolumes();
        ShowPage(mainPage);
    }

    private string T(int index) => Translations[index, SelectedLanguage];

    private void ApplyLanguage()
    {
        string fontName = SelectedLanguage == (int)Language.Malayalam ? "Nirmala UI" :
            SelectedLanguage == (int)Language.Chinese ? "Microsoft YaHei" : "Arial";
        Font languageFont = Font.CreateDynamicFontFromOSFont(fontName, 32);
        if (languageFont != null)
            foreach (Text text in GetComponentsInChildren<Text>(true)) text.font = languageFont;

        string[] main = { T(0), T(1), T(2), T(3) };
        for (int i = 0; i < mainLabels.Length; i++) mainLabels[i].text = main[i];
        stageTitle.text = T(1);
        languageTitle.text = T(2);
        settingsTitle.text = T(3);
        musicLabel.text = T(5);
        sfxLabel.text = T(6);
        for (int i = 0; i < stageButtons.Length; i++) SetButtonLabel(stageButtons[i], T(1) + " " + (i + 1));
        for (int i = 0; i < backButtons.Length; i++) SetButtonLabel(backButtons[i], T(4));
        SetControlLabel();
    }

    private void SetLanguage(int selected)
    {
        PlayerPrefs.SetInt("Language", selected);
        ApplyLanguage();
        ShowPage(mainPage);
    }

    private void ShowSettings()
    {
        musicSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("MusicVolume", 0.8f));
        sfxSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("SfxVolume", 0.8f));
        ApplyVolumes();
        ShowPage(settingsPage);
    }

    private void ToggleControls()
    {
        int value = PlayerPrefs.GetInt("GestureControls", 0) == 1 ? 0 : 1;
        PlayerPrefs.SetInt("GestureControls", value);
        SetControlLabel();
    }

    private void SetControlLabel()
    {
        string mode = PlayerPrefs.GetInt("GestureControls", 0) == 1 ? T(9) : T(8);
        SetButtonLabel(controlsButton, T(7) + ": " + mode);
    }

    private void ShowPage(GameObject page)
    {
        mainPage.SetActive(page == mainPage);
        stagePage.SetActive(page == stagePage);
        languagePage.SetActive(page == languagePage);
        settingsPage.SetActive(page == settingsPage);
    }

    private static void SetButtonLabel(Button button, string label)
    {
        Text text = button.GetComponentInChildren<Text>();
        if (text != null) text.text = label;
    }

    private void ApplyVolumes()
    {
        float music = PlayerPrefs.GetFloat("MusicVolume", 0.8f);
        float sfx = PlayerPrefs.GetFloat("SfxVolume", 0.8f);
        foreach (AudioSource source in FindObjectsByType<AudioSource>())
            source.volume = source.gameObject.name.ToLowerInvariant().Contains("music") ? music : sfx;
    }

    private static void LoadStage(int index)
    {
        if (index < 0 || index >= SceneNames.Length) return;
        PlayerPrefs.SetInt("SelectedStage", index + 1);
        if (Application.CanStreamedLevelBeLoaded(SceneNames[index])) SceneManager.LoadScene(SceneNames[index]);
        else Debug.LogError("Stage scene is missing from Build Settings: " + SceneNames[index]);
    }
}
