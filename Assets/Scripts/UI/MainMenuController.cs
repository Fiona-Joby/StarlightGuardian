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

    [Header("Settings controls")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Text musicVolumePercent;
    [SerializeField] private Text sfxVolumePercent;

    private static readonly string[] SceneNames = { "MainScene", "Stage2", "Stage3" };
    private static readonly string[,] Translations =
    {
        { "NEW GAME", "പുതിയ കളി", "PERMAINAN BARU", "新游戏" },
        { "STAGE", "ഘട്ടം", "TAHAP", "关卡" },
        { "LANGUAGE", "ഭാഷ", "BAHASA", "语言" },
        { "SETTINGS", "ക്രമീകരണങ്ങൾ", "PENGATURAN", "设置" },
        { "BACK", "തിരികെ", "KEMBALI", "返回" },
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
        UpdateVolumePercentages();
        musicSlider.onValueChanged.AddListener(value => { PlayerPrefs.SetFloat("MusicVolume", value); ApplyVolumes(); UpdateVolumePercentages(); });
        sfxSlider.onValueChanged.AddListener(value => { PlayerPrefs.SetFloat("SfxVolume", value); ApplyVolumes(); UpdateVolumePercentages(); });
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
        if (mainLabels != null)
            for (int i = 0; i < mainLabels.Length && i < main.Length; i++)
                if (mainLabels[i] != null) mainLabels[i].text = main[i];
        stageTitle.text = T(1);
        languageTitle.text = T(2);
        if (settingsTitle != null) settingsTitle.text = T(3);
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
        UpdateVolumePercentages();
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
        string mode = PlayerPrefs.GetInt("GestureControls", 0) == 1 ? T(6) : T(5);
        SetButtonLabel(controlsButton, mode);
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

    private void UpdateVolumePercentages()
    {
        if (musicVolumePercent != null && musicSlider != null)
            musicVolumePercent.text = Mathf.RoundToInt(musicSlider.value * 100f) + "%";
        if (sfxVolumePercent != null && sfxSlider != null)
            sfxVolumePercent.text = Mathf.RoundToInt(sfxSlider.value * 100f) + "%";
    }

    private static void LoadStage(int index)
    {
        if (index < 0 || index >= SceneNames.Length) return;
        PlayerPrefs.SetInt("SelectedStage", index + 1);
        if (Application.CanStreamedLevelBeLoaded(SceneNames[index])) SceneManager.LoadScene(SceneNames[index]);
        else Debug.LogError("Stage scene is missing from Build Settings: " + SceneNames[index]);
    }
}
