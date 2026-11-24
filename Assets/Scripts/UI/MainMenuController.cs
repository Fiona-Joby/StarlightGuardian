using System.Collections.Generic;
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
    [SerializeField] private GameObject controlsDropdownPanel;
    [SerializeField] private Button keyboardOptionButton;
    [SerializeField] private Button gestureOptionButton;
    [SerializeField] private RectTransform controlsDropdownArrow;
    [SerializeField] private Sprite selectionArrowSprite;

    [Header("Menu option sounds")]
    [SerializeField] private AudioClip optionHoverSound;
    [SerializeField] private AudioClip optionSelectSound;

    [Header("Localized visual GameObjects (language-major)")]
    [SerializeField] private GameObject[] localizedMainOptionVisuals;
    [SerializeField] private GameObject[] localizedStageOptionVisuals;
    [SerializeField] private GameObject[] localizedSettingsTabVisuals;
    [SerializeField] private Image settingsPopupCard;
    private Image[] englishMainOptionImages;
    private Image[] englishStageOptionImages;
    private Color[] englishStageOptionImageColors;
    private AudioSource menuAudioSource;

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
        if (newGameButton != null) newGameButton.onClick.AddListener(() => LoadStage(0));
        if (stageMenuButton != null) stageMenuButton.onClick.AddListener(() => ShowPage(stagePage));
        if (languageMenuButton != null) languageMenuButton.onClick.AddListener(() => ShowPage(languagePage));
        if (settingsMenuButton != null) settingsMenuButton.onClick.AddListener(() => ShowSettings());
        for (int i = 0; stageButtons != null && i < stageButtons.Length; i++)
        {
            if (stageButtons[i] == null) continue;
            DisableColorTint(stageButtons[i]);
            int stage = i;
            stageButtons[i].onClick.AddListener(() => LoadStage(stage));
        }
        for (int i = 0; languageButtons != null && i < languageButtons.Length; i++)
        {
            if (languageButtons[i] == null) continue;
            DisableColorTint(languageButtons[i]);
            int selected = i;
            languageButtons[i].onClick.AddListener(() => SetLanguage(selected));
        }
        if (backButtons != null)
            foreach (Button back in backButtons)
                if (back != null) back.onClick.AddListener(() => ShowPage(mainPage));

        if (controlsButton != null) controlsButton.onClick.AddListener(ToggleControls);
        if (keyboardOptionButton != null) keyboardOptionButton.onClick.AddListener(() => SetControlMode(false));
        if (gestureOptionButton != null) gestureOptionButton.onClick.AddListener(() => SetControlMode(true));
        SetupOptionAudio();
        SetControlsDropdownVisible(false);
        SetupSelectionMarkers();
        if (musicSlider != null)
        {
            musicSlider.minValue = 0f;
            musicSlider.maxValue = 1f;
            musicSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("MusicVolume", 0.8f));
            musicSlider.onValueChanged.AddListener(value => { PlayerPrefs.SetFloat("MusicVolume", value); ApplyVolumes(); UpdateVolumePercentages(); });
        }
        if (sfxSlider != null)
        {
            sfxSlider.minValue = 0f;
            sfxSlider.maxValue = 1f;
            sfxSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("SfxVolume", 0.8f));
            sfxSlider.onValueChanged.AddListener(value => { PlayerPrefs.SetFloat("SfxVolume", value); ApplyVolumes(); UpdateVolumePercentages(); });
        }
        UpdateVolumePercentages();
        ApplyVolumes();
        ShowPage(mainPage);
    }

    private void Start()
    {
        Canvas.ForceUpdateCanvases();
        englishMainOptionImages = CollectEnglishOptionImages(new[]
            { newGameButton, stageMenuButton, languageMenuButton, settingsMenuButton, GetExitButton() });
        englishStageOptionImages = CollectEnglishOptionImages(stageButtons);
        englishStageOptionImageColors = CaptureImageColors(englishStageOptionImages);
        ApplyLanguage();
    }

    private string T(int index) => Translations[index, SelectedLanguage];

    private void ApplyLanguage()
    {
        ApplyLocalizedVisualObjects();
        string fontName = SelectedLanguage == (int)Language.Malayalam ? "Nirmala UI" :
            SelectedLanguage == (int)Language.Chinese ? "Microsoft YaHei" : "Arial";
        Font languageFont = Font.CreateDynamicFontFromOSFont(fontName, 32);
        if (languageFont != null)
            foreach (Text text in GetComponentsInChildren<Text>(true)) text.font = languageFont;

        string[] main = { T(0), T(1), T(2), T(3) };
        if (mainLabels != null)
            for (int i = 0; i < mainLabels.Length && i < main.Length; i++)
                if (mainLabels[i] != null) mainLabels[i].text = main[i];
        if (stageTitle != null) stageTitle.text = T(1);
        if (languageTitle != null) languageTitle.text = T(2);
        if (settingsTitle != null) settingsTitle.text = T(3);
        if (stageButtons != null)
            for (int i = 0; i < stageButtons.Length; i++) SetButtonLabel(stageButtons[i], T(1) + " " + (i + 1));
        if (backButtons != null)
            for (int i = 0; i < backButtons.Length; i++) SetButtonLabel(backButtons[i], T(4));
        SetButtonLabel(keyboardOptionButton, T(5));
        SetButtonLabel(gestureOptionButton, T(6));
        SetControlLabel();
    }

    private void ApplyLocalizedVisualObjects()
    {
        bool englishSelected = SelectedLanguage == (int)Language.English;
        SetImagesEnabled(englishMainOptionImages, englishSelected);
        SetEnglishStageOptionImagesVisible(englishSelected);
        SetVisualObjects(localizedMainOptionVisuals, SelectedLanguage - 1, 5);
        SetVisualObjects(localizedStageOptionVisuals, SelectedLanguage - 1, 3);
        MakeLocalizedStageVisualsIgnorePointerInput();
        RefreshStageSelectionMarkers();
        SetVisualObjects(localizedSettingsTabVisuals, SelectedLanguage - 1, 1);
        if (settingsPopupCard != null) settingsPopupCard.enabled = englishSelected;
    }

    private void SetEnglishStageOptionImagesVisible(bool visible)
    {
        if (englishStageOptionImages == null) return;
        for (int i = 0; i < englishStageOptionImages.Length; i++)
        {
            Image image = englishStageOptionImages[i];
            if (image == null) continue;

            // The Image is also each Button's raycast target. Keep it enabled
            // in every language so the button receives hover and click events.
            image.enabled = true;
            image.raycastTarget = true;
            Color color = englishStageOptionImageColors != null && i < englishStageOptionImageColors.Length
                ? englishStageOptionImageColors[i]
                : image.color;
            color.a = visible ? color.a : 0f;
            image.color = color;
        }
    }

    private static Color[] CaptureImageColors(Image[] images)
    {
        if (images == null) return new Color[0];
        Color[] colors = new Color[images.Length];
        for (int i = 0; i < images.Length; i++)
            if (images[i] != null) colors[i] = images[i].color;
        return colors;
    }

    private void MakeLocalizedStageVisualsIgnorePointerInput()
    {
        if (localizedStageOptionVisuals == null) return;

        // These translated images sit over the actual stage Buttons. If they
        // participate in raycasts, they swallow hover and click events, which
        // also prevents the button's arrow and audio handlers from running.
        foreach (GameObject visual in localizedStageOptionVisuals)
        {
            if (visual == null) continue;
            foreach (Graphic graphic in visual.GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = false;
        }
    }

    private void RefreshStageSelectionMarkers()
    {
        if (selectionArrowSprite == null || stageButtons == null) return;
        foreach (Button button in stageButtons)
        {
            if (button == null) continue;
            MenuButtonSelectionMarker marker = button.GetComponent<MenuButtonSelectionMarker>();
            if (marker != null)
                marker.Initialize(selectionArrowSprite);
        }
    }

    private static Image[] CollectEnglishOptionImages(Button[] buttons)
    {
        HashSet<Image> images = new HashSet<Image>();
        if (buttons == null) return new Image[0];
        foreach (Button button in buttons)
        {
            if (button == null) continue;
            Transform parent = button.transform.parent;
            GameObject visualRoot = parent != null && IsLocalizedOptionName(parent.name)
                ? parent.gameObject : button.gameObject;
            foreach (Image image in visualRoot.GetComponents<Image>())
                if (image != null) images.Add(image);
        }
        Image[] result = new Image[images.Count];
        images.CopyTo(result);
        return result;
    }

    private static void SetImagesEnabled(Image[] images, bool enabled)
    {
        if (images == null) return;
        foreach (Image image in images)
            if (image != null) image.enabled = enabled;
    }

    private static void SetVisualObjects(GameObject[] visuals, int languageIndex, int visualsPerLanguage)
    {
        if (visuals == null) return;
        int selectedIndex = languageIndex >= 0 ? languageIndex * visualsPerLanguage : -1;
        for (int i = 0; i < visuals.Length; i++)
            if (visuals[i] != null)
            {
                bool selected = languageIndex >= 0 && i >= selectedIndex && i < selectedIndex + visualsPerLanguage;
                visuals[i].SetActive(selected);
                if (selected) visuals[i].transform.SetAsFirstSibling();
            }
    }

    private Button GetExitButton()
    {
        if (mainPage == null) return null;
        Button[] buttons = mainPage.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
            if (button != null && button.gameObject.name == "Exit") return button;
        return null;
    }

    private static bool IsLocalizedOptionName(string name)
    {
        return name == "New Game" || name == "Stage" || name == "Language" || name == "Settings" ||
            name == "Exit" || name == "Stage 1" || name == "Stage 2" || name == "Stage 3";
    }

    private void SetLanguage(int selected)
    {
        PlayerPrefs.SetInt("Language", selected);
        ApplyLanguage();
        ShowPage(mainPage);
    }

    private void ShowSettings()
    {
        if (musicSlider != null) musicSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("MusicVolume", 0.8f));
        if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("SfxVolume", 0.8f));
        UpdateVolumePercentages();
        ApplyVolumes();
        ShowPage(settingsPage);
    }

    private void ToggleControls()
    {
        if (controlsDropdownPanel != null)
            SetControlsDropdownVisible(!controlsDropdownPanel.activeSelf);
    }

    private void SetControlMode(bool useGestureControls)
    {
        PlayerPrefs.SetInt("GestureControls", useGestureControls ? 1 : 0);
        SetControlLabel();
        SetControlsDropdownVisible(false);
    }

    private void SetControlsDropdownVisible(bool visible)
    {
        if (controlsDropdownPanel != null) controlsDropdownPanel.SetActive(visible);
        if (controlsDropdownArrow != null)
            controlsDropdownArrow.localEulerAngles = new Vector3(0f, 0f, visible ? 90f : -90f);
    }

    private void SetControlLabel()
    {
        bool gestureSelected = PlayerPrefs.GetInt("GestureControls", 0) == 1;
        string mode = gestureSelected ? T(6) : T(5);
        SetButtonLabel(controlsButton, mode);
        SetButtonLabel(keyboardOptionButton, T(5));
        SetButtonLabel(gestureOptionButton, T(6));
        SetControlOptionVisible(keyboardOptionButton, gestureSelected);
        SetControlOptionVisible(gestureOptionButton, !gestureSelected);
    }

    private static void SetControlOptionVisible(Button option, bool visible)
    {
        if (option == null) return;
        option.gameObject.SetActive(visible);
        foreach (Graphic graphic in option.GetComponentsInChildren<Graphic>(true))
            graphic.enabled = visible;
    }

    private void ShowPage(GameObject page)
    {
        if (page != settingsPage) SetControlsDropdownVisible(false);
        bool keepMainOptionsVisible = page == mainPage || page == stagePage || page == languagePage;
        if (mainPage != null) mainPage.SetActive(keepMainOptionsVisible);
        if (stagePage != null) stagePage.SetActive(page == stagePage);
        if (languagePage != null) languagePage.SetActive(page == languagePage);
        if (settingsPage != null) settingsPage.SetActive(page == settingsPage);
    }

    private static void SetButtonLabel(Button button, string label)
    {
        if (button == null) return;
        foreach (Text text in button.GetComponentsInChildren<Text>(true))
            if (text != null) text.text = label;
    }

    private static void DisableColorTint(Button button)
    {
        if (button != null && button.transition == Selectable.Transition.ColorTint)
            button.transition = Selectable.Transition.None;
    }

    private void SetupSelectionMarkers()
    {
        if (selectionArrowSprite == null) return;

        HashSet<Button> buttons = new HashSet<Button>();
        AddSelectionMarker(buttons, newGameButton);
        AddSelectionMarker(buttons, stageMenuButton);
        AddSelectionMarker(buttons, languageMenuButton);
        AddSelectionMarker(buttons, settingsMenuButton);
        Button exitButton = GetExitButton();
        AddSelectionMarker(buttons, exitButton, 12f);

        if (stageButtons != null)
            foreach (Button button in stageButtons) AddSelectionMarker(buttons, button);
        if (languageButtons != null)
            foreach (Button button in languageButtons) AddSelectionMarker(buttons, button);
    }

    private void AddSelectionMarker(HashSet<Button> buttons, Button button, float additionalLeftOffset = 0f)
    {
        if (button == null || !buttons.Add(button)) return;

        MenuButtonSelectionMarker marker = button.GetComponent<MenuButtonSelectionMarker>();
        if (marker == null) marker = button.gameObject.AddComponent<MenuButtonSelectionMarker>();
        marker.Initialize(selectionArrowSprite, additionalLeftOffset);
    }

    private void SetupOptionAudio()
    {
        if (optionHoverSound == null)
            optionHoverSound = Resources.Load<AudioClip>("Audio/litupsubway-ui-hover-sfx-513360");
        if (optionSelectSound == null)
            optionSelectSound = Resources.Load<AudioClip>("Audio/litupsubway-ui-move-518617");

        GameObject audioObject = new GameObject("UI SFX");
        audioObject.transform.SetParent(transform, false);
        menuAudioSource = audioObject.AddComponent<AudioSource>();
        menuAudioSource.playOnAwake = false;
        menuAudioSource.spatialBlend = 0f;
        menuAudioSource.volume = PlayerPrefs.GetFloat("SfxVolume", 0.8f);

        HashSet<Button> buttons = new HashSet<Button>();
        AddOptionAudio(buttons, newGameButton);
        AddOptionAudio(buttons, stageMenuButton);
        AddOptionAudio(buttons, languageMenuButton);
        AddOptionAudio(buttons, settingsMenuButton);
        AddOptionAudio(buttons, GetExitButton());
        AddOptionAudio(buttons, controlsButton);
        AddOptionAudio(buttons, keyboardOptionButton);
        AddOptionAudio(buttons, gestureOptionButton);
        AddOptionAudio(buttons, stageButtons);
        AddOptionAudio(buttons, languageButtons);
        AddOptionAudio(buttons, backButtons);
    }

    private void AddOptionAudio(HashSet<Button> buttons, Button button)
    {
        if (button == null || !buttons.Add(button)) return;

        MenuOptionAudio optionAudio = button.GetComponent<MenuOptionAudio>();
        if (optionAudio == null) optionAudio = button.gameObject.AddComponent<MenuOptionAudio>();
        optionAudio.Initialize(menuAudioSource, optionHoverSound, optionSelectSound);
        button.onClick.AddListener(optionAudio.PlaySelection);
    }

    private void AddOptionAudio(HashSet<Button> buttons, Button[] buttonArray)
    {
        if (buttonArray == null) return;
        foreach (Button button in buttonArray) AddOptionAudio(buttons, button);
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
