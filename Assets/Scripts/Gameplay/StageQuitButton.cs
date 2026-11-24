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
    private Button controlsButton;
    private RectTransform controlsArrow;
    private GameObject controlsDropdown;
    private Button keyboardOption;
    private Button gestureOption;
    private Text musicVolumePercent;
    private Text sfxVolumePercent;
    private StagePauseArt.LocalizedArt localizedArt;
    private int language;
    private AudioSource uiAudioSource;
    private AudioClip optionHoverSound;
    private AudioClip optionSelectSound;
    private GameObject taskPage;
    private Text taskGoalText;
    private Text taskHeading;
    private float timeScaleBeforeTaskPopup = 1f;
    private float nextTaskRefreshTime;
    private bool pauseMenuOpen;
    private float timeScaleBeforePause = 1f;
    private GameObject objectivePage;
    private GestureInstructionOverlay gestureInstructions;

    private void Awake()
    {
        StagePauseArt art = Resources.Load<StagePauseArt>(ArtResource);
        if (art == null)
        {
            Debug.LogError("StagePauseArt is missing from Assets/Resources. The pause menu cannot be created.");
            return;
        }

        language = Mathf.Clamp(PlayerPrefs.GetInt("Language", 0), 0, 3);
        if (art.localizedArt != null && language < art.localizedArt.Length)
            localizedArt = art.localizedArt[language];

        canvas = RuntimeUiUtility.CreateOverlayCanvas(
            "Stage Pause Canvas", 500, new Vector2(1920f, 1080f));
        RuntimeUiUtility.EnsureEventSystem();

        Button pauseButton = CreateImageButton(
            "Pause Button", canvas.transform, art.pauseButton, new Vector2(100f, 100f),
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(76f, -76f));
        SetupOptionAudio(pauseButton);
        pauseButton.onClick.AddListener(OpenPause);

        Button taskButton = CreateImageButton(
            "Task Button", canvas.transform, art.taskButton, new Vector2(100f, 100f),
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(196f, -76f));
        SetupOptionAudio(taskButton);
        taskButton.onClick.AddListener(ToggleTaskPage);
        CreateTaskPage(canvas.transform);

        string activeSceneName = SceneManager.GetActiveScene().name;
        if (activeSceneName == "MainScene" || activeSceneName == "Stage2" || activeSceneName == "Stage3")
        {
            Texture2D controlButtonTexture = Resources.Load<Texture2D>("StageUI/Control Button");
            if (controlButtonTexture != null)
            {
                Button controlButton = CreateTextureButton("Control Button", canvas.transform, controlButtonTexture,
                    new Vector2(100f, 100f), new Vector2(0f, 1f), new Vector2(316f, -76f));
                SetupOptionAudio(controlButton);
                gestureInstructions = gameObject.AddComponent<GestureInstructionOverlay>();
                gestureInstructions.Configure(activeSceneName == "MainScene" ? 1 : 2, canvas.transform);
                controlButton.onClick.AddListener(() => gestureInstructions.Show());
            }
            else Debug.LogError("Control Button image is missing from Assets/Resources/StageUI.");
        }

        pausePage = CreatePage("Pause Page", canvas.transform);
        CreatePauseArt(pausePage.transform, art);
        CreateSettingsPage(art);
        pausePage.SetActive(false);
        settingsPage.SetActive(false);
    }

    private void ShowStartupObjectives()
    {
        if (canvas == null) return;
        if (objectivePage == null) CreateObjectivePage();
        timeScaleBeforePause = Time.timeScale;
        Time.timeScale = 0f;
        objectivePage.SetActive(true);
    }

    public void ShowStage1InstructionsAfterStory()
    {
        if (gestureInstructions == null) return;
        gestureInstructions.onStartupDismissed = ShowStartupObjectives;
        gestureInstructions.ShowAtStageStart();
    }

    public void ShowStage2InstructionsAfterStory()
    {
        if (gestureInstructions == null) return;
        gestureInstructions.onStartupDismissed = ShowStartupObjectives;
        gestureInstructions.ShowAtStageStart();
    }

    public void ShowStage3ObjectivesAfterStory() => ShowStartupObjectives();

    private void CreateObjectivePage()
    {
        objectivePage = CreatePage("Stage Objectives", canvas.transform);
        Image dimmer = CreateImage("Objective Backdrop", objectivePage.transform, null);
        Stretch(dimmer.rectTransform);
        dimmer.color = new Color(0f, 0f, 0f, 0.82f);
        dimmer.raycastTarget = true;
        Texture2D tab = Resources.Load<Texture2D>("TaskTabs/" + new[] { "English", "Malayalam", "Indonesian", "Traditional Chinese" }[language]);
        RawImage panel = RuntimeUiUtility.CreateRect("Objective Task Tab", objectivePage.transform).gameObject.AddComponent<RawImage>();
        panel.texture = tab;
        panel.raycastTarget = false;
        panel.rectTransform.sizeDelta = new Vector2(1500f, 1000f);
        Text objective = CreateLabel("Objective Text", objectivePage.transform, GetStartupObjective(), 40);
        objective.font = GetLanguageFont();
        objective.fontSize = language == 1 ? 32 : language == 3 ? 38 : 44;
        objective.alignment = TextAnchor.MiddleCenter;
        objective.horizontalOverflow = HorizontalWrapMode.Wrap;
        objective.verticalOverflow = VerticalWrapMode.Overflow;
        Place(objective.rectTransform, new Vector2(0.5f, 0.55f), new Vector2(800f, 390f));
        Button continueButton = CreateImageButton("Start Stage", objectivePage.transform,
            LoadStartButtonSprite(), new Vector2(240f, 72f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -270f));
        SetupOptionAudio(continueButton);
        continueButton.transform.SetAsLastSibling();
        continueButton.onClick.AddListener(() => { objectivePage.SetActive(false); Time.timeScale = timeScaleBeforePause; });
        objectivePage.SetActive(false);
    }

    private string GetStartupObjective()
    {
        string scene = SceneManager.GetActiveScene().name;
        if (scene == "MainScene")
        {
            int points = GameDifficulty.Stage1PointGoal;
            return language switch
            {
                1 => $"നക്ഷത്രരാശി പുനഃസ്ഥാപിക്കാൻ ആസ്റ്ററിനെ സഹായിക്കുക!\nഘട്ടം പൂർത്തിയാക്കാൻ {points} പോയിന്റുകൾ നേടുക",
                2 => $"Bantu Aster memulihkan rasi bintang!\nRaih {points} poin untuk menyelesaikan tahap",
                3 => $"幫助 Aster 修復星座！\n取得 {points} 分以完成關卡",
                _ => $"Help Aster restore the constellation!\nEarn {points} points to complete the stage"
            };
        }
        if (scene == "Stage2")
        {
            Stage2ArtReferences art = Object.FindAnyObjectByType<Stage2ArtReferences>();
            int turrets = GameDifficulty.Stage2TurretGoal(art != null ? art.TurretGoal : 6);
            int beasts = GameDifficulty.Stage2BeastGoal(art != null ? art.BlobBeastGoal : 2);
            return language switch
            {
                1 => $"നിങ്ങളുടെ നക്ഷത്രാത്മാവിനെ സംരക്ഷിക്കുക!\n{turrets} ടററ്റുകളും {beasts} ബ്ലോബ് മൃഗങ്ങളെയും നശിപ്പിക്കുക.",
                2 => $"Lindungi roh bintangmu!\nHancurkan {turrets} turret dan {beasts} monster blob.",
                3 => $"保護你的星靈！\n摧毀 {turrets} 座砲塔和 {beasts} 隻黏 blob 怪。",
                _ => $"Protect your star spirit!\nDestroy {turrets} turrets and {beasts} blob beasts."
            };
        }
        Stage3ArtReferences stage3 = Object.FindAnyObjectByType<Stage3ArtReferences>();
        int drones = GameDifficulty.Stage3DroneGoal(stage3 != null ? stage3.DroneGoal : 5);
        int slimes = GameDifficulty.Stage3SlimeGoal(stage3 != null ? stage3.SlimeGoal : 4);
        int hard = GameDifficulty.Stage3HardBeastGoal(stage3 != null ? stage3.HardBeastGoal : 2);
        return language switch
        {
            1 => $"ലൂട്ട്ബോക്സ് നേടുക!\n{drones} ഡ്രോണുകളും {slimes} സ്ലൈമുകളും {hard} ഹാർഡ് മൃഗങ്ങളെയും തോൽപ്പിക്കുക.",
            2 => $"Dapatkan kotak jarahan!\nKalahkan {drones} drone, {slimes} slime, dan {hard} monster kuat.",
            3 => $"取得戰利品箱！\n擊敗 {drones} 架無人機、{slimes} 隻史萊姆和 {hard} 隻強力怪獸。",
            _ => $"Get the lootbox!\nDefeat {drones} drones, {slimes} slimes, and {hard} hard beasts."
        };
    }

    private void CreateTaskPage(Transform parent)
    {
        taskPage = CreatePage("Task Page", parent);
        Image blocker = CreateImage("Task Popup Click Blocker", taskPage.transform, null);
        Stretch(blocker.rectTransform);
        blocker.color = new Color(0f, 0f, 0f, 0f);
        blocker.raycastTarget = true;
        Texture2D boxTexture = Resources.Load<Texture2D>("StageUI/UI Box");
        RawImage panel = RuntimeUiUtility.CreateRect("Task Box", taskPage.transform).gameObject.AddComponent<RawImage>();
        panel.texture = boxTexture;
        panel.color = Color.white;
        panel.rectTransform.anchorMin = panel.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        panel.rectTransform.sizeDelta = new Vector2(1080f, 540f);
        const float boxSourceWidth = 1920f;
        const float boxSourceHeight = 1080f;
        const float boxLeft = 566f;
        const float boxTop = 176f;
        const float boxRight = 1331f;
        const float boxBottom = 901f;
        panel.uvRect = new Rect(boxLeft / boxSourceWidth, 1f - boxBottom / boxSourceHeight,
            (boxRight - boxLeft) / boxSourceWidth, (boxBottom - boxTop) / boxSourceHeight);
        taskHeading = CreateLabel("Task Heading", taskPage.transform, "GOAL", 40);
        taskHeading.font = GetLanguageFont();
        Place(taskHeading.rectTransform, new Vector2(0.5f, 0.62f), new Vector2(920f, 76f));
        taskGoalText = CreateLabel("Stage Goal", taskPage.transform, GetStageGoal(), 40);
        taskGoalText.font = GetLanguageFont();
        taskGoalText.fontSize = 48;
        Place(taskGoalText.rectTransform, new Vector2(0.5f, 0.46f), new Vector2(820f, 260f));
        Sprite closeSprite = LoadRuntimeSprite("StageUI/Close Tab Cross");
        Button close = CreateImageButton("Close Task", taskPage.transform, closeSprite, new Vector2(250f, 250f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(430f, 202f));
        SetupOptionAudio(close);
        close.onClick.AddListener(CloseTaskPage);
        taskPage.SetActive(false);
    }

    private void ToggleTaskPage()
    {
        if (taskPage == null) return;
        if (taskPage.activeSelf)
        {
            CloseTaskPage();
            return;
        }

        timeScaleBeforeTaskPopup = Time.timeScale;
        Time.timeScale = 0f;
        taskPage.SetActive(true);
        RefreshTaskPage();
    }

    private void CloseTaskPage()
    {
        if (taskPage == null || !taskPage.activeSelf) return;
        taskPage.SetActive(false);
        Time.timeScale = timeScaleBeforeTaskPopup;
    }

    private string GetStageGoal()
    {
        switch (SceneManager.GetActiveScene().name)
        {
            case "MainScene":
                int pointGoal = ScoreManager.Instance != null ? ScoreManager.Instance.StageGoal : GameDifficulty.Stage1PointGoal;
                int currentScore = ScoreManager.Instance != null ? ScoreManager.Instance.Score : 0;
                return language switch
                {
                    1 => $"{pointGoal} പോയിന്റുകൾ നേടുക.\n{currentScore}/{pointGoal} പോയിന്റുകൾ നേടി",
                    2 => $"Raih {pointGoal} poin.\n{currentScore}/{pointGoal} poin terkumpul",
                    3 => $"取得 {pointGoal} 分。\n已取得 {currentScore}/{pointGoal} 分",
                    _ => $"Earn {pointGoal} points.\n{currentScore}/{pointGoal} points earned"
                };
            case "Stage2":
                Stage2ArtReferences stage2 = Object.FindAnyObjectByType<Stage2ArtReferences>();
                Stage2TurretEncounter encounter2 = Object.FindAnyObjectByType<Stage2TurretEncounter>();
                int turrets = encounter2 != null ? encounter2.TurretGoal : GameDifficulty.Stage2TurretGoal(stage2 != null ? stage2.TurretGoal : 6);
                int beasts = encounter2 != null ? encounter2.BlobBeastGoal : GameDifficulty.Stage2BeastGoal(stage2 != null ? stage2.BlobBeastGoal : 2);
                int turretsDestroyed = encounter2 != null ? encounter2.DestroyedTurrets : 0;
                int beastsDestroyed = encounter2 != null ? encounter2.DestroyedBlobBeasts : 0;
                return language switch
                {
                    1 => $"ടററ്റുകളും ബ്ലോബ് മൃഗങ്ങളെയും നശിപ്പിക്കുക.\n{turretsDestroyed}/{turrets} ടററ്റുകൾ നശിപ്പിച്ചു\n{beastsDestroyed}/{beasts} ബ്ലോബ് മൃഗങ്ങൾ നശിപ്പിച്ചു",
                    2 => $"Hancurkan turret dan monster blob.\n{turretsDestroyed}/{turrets} turret dihancurkan\n{beastsDestroyed}/{beasts} monster blob dihancurkan",
                    3 => $"摧毀砲塔和黏 blob 怪。\n已摧毀 {turretsDestroyed}/{turrets} 座砲塔\n已擊敗 {beastsDestroyed}/{beasts} 隻黏 blob 怪",
                    _ => $"Destroy turrets and blob beasts.\n{turretsDestroyed}/{turrets} Turrets destroyed\n{beastsDestroyed}/{beasts} Blob Beasts destroyed"
                };
            case "Stage3":
                Stage3ArtReferences stage3 = Object.FindAnyObjectByType<Stage3ArtReferences>();
                Stage3DroneEncounter encounter = Object.FindAnyObjectByType<Stage3DroneEncounter>();
                int drones = encounter != null ? encounter.DroneGoal : GameDifficulty.Stage3DroneGoal(stage3 != null ? stage3.DroneGoal : 5);
                int slimes = encounter != null ? encounter.SlimeGoal : GameDifficulty.Stage3SlimeGoal(stage3 != null ? stage3.SlimeGoal : 4);
                int hardBeastGoal = encounter != null ? encounter.HardBeastGoal : GameDifficulty.Stage3HardBeastGoal(stage3 != null ? stage3.HardBeastGoal : 2);
                int dronesDestroyed = encounter != null ? encounter.DestroyedDrones : 0;
                int slimesDestroyed = encounter != null ? encounter.DestroyedSlimes : 0;
                int hardDestroyed = encounter != null ? encounter.DestroyedHardBeasts : 0;
                return language switch
                {
                    1 => $"ഡ്രോണുകൾ, സ്ലൈമുകൾ, ഹാർഡ് മൃഗങ്ങൾ എന്നിവയെ തോൽപ്പിക്കുക.\n{dronesDestroyed}/{drones} ഡ്രോണുകൾ നശിപ്പിച്ചു\n{slimesDestroyed}/{slimes} സ്ലൈമുകൾ നശിപ്പിച്ചു\n{hardDestroyed}/{hardBeastGoal} ഹാർഡ് മൃഗങ്ങൾ നശിപ്പിച്ചു",
                    2 => $"Kalahkan drone, slime, dan monster kuat.\n{dronesDestroyed}/{drones} drone dikalahkan\n{slimesDestroyed}/{slimes} slime dikalahkan\n{hardDestroyed}/{hardBeastGoal} monster kuat dikalahkan",
                    3 => $"擊敗無人機、史萊姆和強力怪獸。\n已擊敗 {dronesDestroyed}/{drones} 架無人機\n已擊敗 {slimesDestroyed}/{slimes} 隻史萊姆\n已擊敗 {hardDestroyed}/{hardBeastGoal} 隻強力怪獸",
                    _ => $"Destroy drones, slimes and hard beasts.\n{dronesDestroyed}/{drones} Drones destroyed\n{slimesDestroyed}/{slimes} Slimes destroyed\n{hardDestroyed}/{hardBeastGoal} Hard Beasts destroyed"
                };
            default: return "STAGE GOAL";
        }
    }

    private void Update()
    {
        if (pauseMenuOpen && ((pausePage != null && pausePage.activeSelf) ||
                              (settingsPage != null && settingsPage.activeSelf)))
            Time.timeScale = 0f;

        if (taskPage != null && taskPage.activeSelf && Time.unscaledTime >= nextTaskRefreshTime)
        {
            RefreshTaskPage();
            nextTaskRefreshTime = Time.unscaledTime + 0.25f;
        }

        if (canvas == null || !Input.GetKeyDown(KeyCode.Escape)) return;

        if (settingsPage.activeSelf)
            ShowPause();
        else if (pausePage.activeSelf)
            ResumeGame();
        else
            OpenPause();
    }

    private void RefreshTaskPage()
    {
        if (taskGoalText != null) taskGoalText.text = GetStageGoal();
        if (taskHeading != null) taskHeading.text = language == 3 ? "目標" : language == 1 ? "ലക്ഷ്യം" : language == 2 ? "TUJUAN" : "GOAL";
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }

    private void OpenPause()
    {
        timeScaleBeforePause = Time.timeScale;
        pauseMenuOpen = true;
        Time.timeScale = 0f;
        pausePage.SetActive(true);
        settingsPage.SetActive(false);
    }

    private void ShowPause()
    {
        pauseMenuOpen = true;
        Time.timeScale = 0f;
        pausePage.SetActive(true);
        settingsPage.SetActive(false);
    }

    private void ResumeGame()
    {
        Time.timeScale = timeScaleBeforePause;
        pauseMenuOpen = false;
        settingsPage.SetActive(false);
        pausePage.SetActive(false);
    }

    private void RestartStage()
    {
        pauseMenuOpen = false;
        StageRestartState.MarkCurrentStageRestart();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void ReturnToMainMenu()
    {
        pauseMenuOpen = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    private void CreatePauseArt(Transform parent, StagePauseArt art)
    {
        GameObject dimmer = new GameObject("Pause Screen Dimmer", typeof(RectTransform), typeof(Image));
        dimmer.transform.SetParent(parent, false);
        Stretch(dimmer.GetComponent<RectTransform>());
        Image dimmerImage = dimmer.GetComponent<Image>();
        dimmerImage.color = new Color(0f, 0f, 0f, 0.52f);
        dimmerImage.raycastTarget = true;

        Sprite pauseTab = localizedArt != null && localizedArt.pauseTab != null
            ? localizedArt.pauseTab : art.pauseTab;
        Image frame = CreateImage("Pause Tab Background", parent, pauseTab);
        RectTransform frameRect = frame.rectTransform;
        frameRect.anchorMin = frameRect.anchorMax = new Vector2(0.5f, 0.5f);
        frameRect.sizeDelta = new Vector2(1280f, 762f);
        frame.raycastTarget = false;

        GameObject options = new GameObject("Pause Options", typeof(RectTransform));
        options.transform.SetParent(parent, false);
        Stretch(options.GetComponent<RectTransform>());

        Texture2D[] optionTextures = localizedArt != null
            ? new[] { localizedArt.settingsOption, localizedArt.resumeOption, localizedArt.restartOption, localizedArt.quitOption }
            : new[] { art.settingsOption, art.resumeOption, art.restartOption, art.quitOption };
        string[] optionNames = { "Settings", "Resume", "Restart", "Exit" };
        UnityEngine.Events.UnityAction[] actions =
        {
            ShowSettings, ResumeGame, RestartStage, ReturnToMainMenu
        };
        float[] x = { 0.265f, 0.425f, 0.58f, 0.735f };
        for (int i = 0; i < optionNames.Length; i++)
        {
            Sprite optionSprite = CreateOptionSprite(optionTextures[i], language == 2);
            Button button = CreateImageButton(
                optionNames[i] + " Option", options.transform, optionSprite,
                new Vector2(240f, 292f), new Vector2(x[i], 0.49f),
                new Vector2(x[i], 0.49f), Vector2.zero);
            SetupOptionAudio(button);
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

        Sprite settingsTab = localizedArt != null && localizedArt.settingsTab != null
            ? localizedArt.settingsTab : art.pauseTab;
        Image frame = CreateImage("Settings Tab Background", settingsPage.transform, settingsTab);
        frame.rectTransform.anchorMin = frame.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        frame.rectTransform.sizeDelta = new Vector2(1388.48f, 818.29f);
        frame.preserveAspect = false;
        frame.raycastTarget = false;

        Font font = GetLanguageFont();
        musicSlider = CreateSlider("Music Volume", settingsPage.transform, new Vector2(0.57900006f, 0.5707784f), art.sliderHandle);
        musicSlider.GetComponent<RectTransform>().anchoredPosition = new Vector2(-2.03f, 0.056f);
        musicSlider.GetComponent<RectTransform>().sizeDelta = new Vector2(487.93f, 34f);
        sfxSlider = CreateSlider("Sound FX Volume", settingsPage.transform, new Vector2(0.57900006f, 0.5f), art.sliderHandle);
        sfxSlider.GetComponent<RectTransform>().anchoredPosition = new Vector2(-2.02f, 0.887f);
        sfxSlider.GetComponent<RectTransform>().sizeDelta = new Vector2(487.9f, 34f);

        musicVolumePercent = CreateLabel("Music Volume Percentage", settingsPage.transform, "", 32);
        Place(musicVolumePercent.rectTransform, new Vector2(0.782f, 0.5708f), new Vector2(130f, 54f));
        sfxVolumePercent = CreateLabel("Sound FX Volume Percentage", settingsPage.transform, "", 32);
        Place(sfxVolumePercent.rectTransform, new Vector2(0.782f, 0.5f), new Vector2(130f, 54f));

        controlsButton = RuntimeUiUtility.CreateButton(
            "Control Mode", settingsPage.transform, "", 32,
            new Color(0.54f, 0.35f, 0.97f, 0.93f), Color.white,
            new Vector2(632.65f, 86f));
        Place(controlsButton.GetComponent<RectTransform>(), new Vector2(0.61600006f, 0.404f), new Vector2(632.65f, 86f));
        controlsButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(1.36f, 2f);
        controlsLabel = controlsButton.GetComponentInChildren<Text>();
        controlsLabel.font = font;
        controlsLabel.alignment = TextAnchor.MiddleLeft;
        controlsLabel.rectTransform.offsetMin = new Vector2(38f, 0f);
        controlsLabel.rectTransform.offsetMax = new Vector2(-38f, 0f);
        controlsLabel.fontSize = 40;
        SetupOptionAudio(controlsButton);
        controlsButton.onClick.AddListener(ToggleControls);

        Image arrow = CreateImage("Control Dropdown Arrow", settingsPage.transform, art.controlsDropdownArrow);
        controlsArrow = arrow.rectTransform;
        controlsArrow.anchorMin = controlsArrow.anchorMax = new Vector2(0.57900006f, 0.404f);
        controlsArrow.anchoredPosition = new Vector2(342f, -3f);
        controlsArrow.sizeDelta = new Vector2(142.63f, 78.75f);
        arrow.color = new Color(0.78f, 1f, 0f, 1f);
        arrow.raycastTarget = false;
        controlsArrow.localEulerAngles = new Vector3(0f, 0f, -90f);

        CreateControlsDropdown(settingsPage.transform, font);

        Button pauseSettingsBack = RuntimeUiUtility.CreateButton(
            "Close Settings", settingsPage.transform, "", 30,
            new Color(0f, 0f, 0f, 0f), Color.white, new Vector2(106f, 106f));
        Place(pauseSettingsBack.GetComponent<RectTransform>(), new Vector2(0.82f, 0.79f), new Vector2(106f, 106f));
        SetupOptionAudio(pauseSettingsBack);
        pauseSettingsBack.onClick.AddListener(ShowPause);

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
        musicSlider.onValueChanged.AddListener(_ => UpdateVolumePercentages());
        sfxSlider.onValueChanged.AddListener(_ => UpdateVolumePercentages());
        RefreshControlLabel();
        UpdateVolumePercentages();
        ApplyVolumes();
    }

    private void CreateControlsDropdown(Transform parent, Font font)
    {
        controlsDropdown = new GameObject("Controls Dropdown Panel", typeof(RectTransform), typeof(Image));
        controlsDropdown.transform.SetParent(parent, false);
        RectTransform panelRect = controlsDropdown.GetComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.57900006f, 0.404f);
        panelRect.anchoredPosition = new Vector2(72.377f, -85.1f);
        panelRect.sizeDelta = new Vector2(632.68f, 88.21f);
        controlsDropdown.GetComponent<Image>().color = new Color(0.39f, 0.2f, 0.86f, 1f);

        keyboardOption = CreateControlOption("Keyboard Option", controlsDropdown.transform, font);
        gestureOption = CreateControlOption("Gesture Option", controlsDropdown.transform, font);
        controlsDropdown.SetActive(false);
    }

    private Button CreateControlOption(string name, Transform parent, Font font)
    {
        Button option = RuntimeUiUtility.CreateButton(
            name, parent, "", 34, new Color(0.36f, 0.18f, 0.76f, 1f), Color.white,
            new Vector2(632.68f, 88.21f));
        RectTransform rect = option.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Text label = option.GetComponentInChildren<Text>();
        label.font = font;
        label.fontSize = 38;
        SetupOptionAudio(option);
        option.onClick.AddListener(() => SetControlMode(name == "Gesture Option"));
        return option;
    }

    private void SetupOptionAudio(Button button)
    {
        if (button == null) return;
        if (optionHoverSound == null)
            optionHoverSound = Resources.Load<AudioClip>("Audio/litupsubway-ui-hover-sfx-513360");
        if (optionSelectSound == null)
            optionSelectSound = Resources.Load<AudioClip>("Audio/litupsubway-ui-move-518617");

        if (uiAudioSource == null)
        {
            GameObject audioObject = new GameObject("Pause UI SFX");
            audioObject.transform.SetParent(transform, false);
            uiAudioSource = audioObject.AddComponent<AudioSource>();
            uiAudioSource.playOnAwake = false;
            uiAudioSource.spatialBlend = 0f;
            uiAudioSource.volume = PlayerPrefs.GetFloat("SfxVolume", DefaultVolume);
        }

        MenuOptionAudio optionAudio = button.GetComponent<MenuOptionAudio>();
        if (optionAudio == null) optionAudio = button.gameObject.AddComponent<MenuOptionAudio>();
        optionAudio.Initialize(uiAudioSource, optionHoverSound, optionSelectSound);
        button.onClick.AddListener(optionAudio.PlaySelection);
    }

    private void ShowSettings()
    {
        musicSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("MusicVolume", DefaultVolume));
        sfxSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("SfxVolume", DefaultVolume));
        RefreshControlLabel();
        UpdateVolumePercentages();
        ApplyVolumes();
        settingsPage.SetActive(true);
        pausePage.SetActive(false);
    }

    private void ToggleControls()
    {
        if (controlsDropdown == null) return;
        SetControlsDropdownVisible(!controlsDropdown.activeSelf);
    }

    private void SetControlMode(bool useGestureControls)
    {
        PlayerPrefs.SetInt("GestureControls", useGestureControls ? 1 : 0);
        RefreshControlLabel();
        SetControlsDropdownVisible(false);
    }

    private void SetControlsDropdownVisible(bool visible)
    {
        if (controlsDropdown != null) controlsDropdown.SetActive(visible);
        if (controlsArrow != null)
            controlsArrow.localEulerAngles = new Vector3(0f, 0f, visible ? 90f : -90f);
    }

    private void RefreshControlLabel()
    {
        bool gestureSelected = PlayerPrefs.GetInt("GestureControls", 0) == 1;
        if (controlsLabel != null) controlsLabel.text = gestureSelected ? GetText(6) : GetText(5);
        if (keyboardOption != null)
        {
            keyboardOption.GetComponentInChildren<Text>().text = GetText(5);
            keyboardOption.gameObject.SetActive(gestureSelected);
        }
        if (gestureOption != null)
        {
            gestureOption.GetComponentInChildren<Text>().text = GetText(6);
            gestureOption.gameObject.SetActive(!gestureSelected);
        }
        SetControlsDropdownVisible(false);
    }

    private string GetText(int key)
    {
        string[,] translations =
        {
            { "KEYBOARD", "കീബോർഡ്", "KEYBOARD", "键盘" },
            { "GESTURE", "ആംഗ്യം", "GESTUR", "手势" }
        };
        int row = key == 6 ? 1 : 0;
        return translations[row, language];
    }

    private Font GetLanguageFont()
    {
        string fontName = language == 1 ? "Nirmala UI" : language == 3 ? "Microsoft YaHei" : "Arial";
        Font font = Font.CreateDynamicFontFromOSFont(fontName, 40);
        return font != null ? font : RuntimeUiUtility.GetFont();
    }

    private void UpdateVolumePercentages()
    {
        if (musicVolumePercent != null && musicSlider != null)
            musicVolumePercent.text = Mathf.RoundToInt(musicSlider.value * 100f) + "%";
        if (sfxVolumePercent != null && sfxSlider != null)
            sfxVolumePercent.text = Mathf.RoundToInt(sfxSlider.value * 100f) + "%";
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

    private static Sprite LoadRuntimeSprite(string resourcePath)
    {
        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        return texture != null
            ? Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f)
            : null;
    }

    private Sprite LoadStartButtonSprite()
    {
        string folder = new[] { "English", "Malayalam", "Indonesian", "Traditional Chinese" }[language];
        string resourcePath = "Start/" + folder + "/Start";
        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        if (texture == null)
        {
            Debug.LogWarning("Missing objective start button image at Resources/" + resourcePath + ".png");
            return null;
        }

        // Trim the transparent canvas so preserveAspect fits the visible artwork into the old 240x72 button area.
        Rect sourceCrop = language switch
        {
            0 => new Rect(289f, 483f, 2174f, 570f),
            1 => new Rect(324f, 663f, 1736f, 466f),
            2 => new Rect(325f, 733f, 1751f, 461f),
            _ => new Rect(340f, 712f, 1735f, 463f)
        };
        Vector2 sourceSize = language == 0
            ? new Vector2(2752f, 1536f)
            : new Vector2(2400f, 1792f);
        Vector2 importScale = new Vector2(texture.width / sourceSize.x, texture.height / sourceSize.y);
        Rect crop = new Rect(sourceCrop.x * importScale.x, sourceCrop.y * importScale.y,
            sourceCrop.width * importScale.x, sourceCrop.height * importScale.y);
        return Sprite.Create(texture, crop, new Vector2(0.5f, 0.5f), 100f);
    }

    private static Button CreateTextureButton(
        string name, Transform parent, Texture2D texture, Vector2 size,
        Vector2 anchor, Vector2 anchoredPosition)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        RawImage image = go.GetComponent<RawImage>();
        image.texture = texture;
        image.color = Color.white;
        // Match the visible circular button artwork. The source PNG has transparent margins;
        // crop those margins in UV space so the 100x100 hit target renders at the same visual size.
        const float sourceWidth = 1920f;
        const float sourceHeight = 1080f;
        const float left = 562f;
        const float top = 142f;
        const float right = 1355f;
        const float bottom = 934f;
        image.uvRect = new Rect(left / sourceWidth, 1f - bottom / sourceHeight,
            (right - left) / sourceWidth, (bottom - top) / sourceHeight);
        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        return button;
    }

    private static Button CreateContinueButton(
        string name, Transform parent, Texture2D texture, Vector2 size,
        Vector2 anchor, Vector2 anchoredPosition)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        RawImage image = go.GetComponent<RawImage>();
        image.texture = texture;
        image.color = Color.white;
        // The continue artwork fills the central 80% of the source canvas.
        image.uvRect = new Rect(0.10f, 0.31f, 0.80f, 0.38f);
        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        return button;
    }

    private static Vector2 FitTexture(Texture2D texture, Vector2 bounds)
    {
        if (texture == null) return bounds;
        float aspect = (float)texture.width / texture.height;
        float width = Mathf.Min(bounds.x, bounds.y * aspect);
        return new Vector2(width, width / aspect);
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

    private static Slider CreateSlider(string name, Transform parent, Vector2 anchor, Sprite handleSprite)
    {
        GameObject go = new GameObject(name + " Slider", typeof(RectTransform), typeof(Slider));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = anchor;
        rect.sizeDelta = new Vector2(720f, 34f);

        Image track = CreateImage("Track", go.transform, null);
        track.color = new Color(0.16f, 0.21f, 0.34f, 0f);
        Stretch(track.rectTransform);
        Image fill = CreateImage("Fill", go.transform, null);
        fill.color = new Color(0.80784315f, 1f, 0.023529412f, 1f);
        RectTransform fillRect = fill.rectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(4f, 4f);
        fillRect.offsetMax = new Vector2(-4f, -4f);

        GameObject handleGO = new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        handleGO.transform.SetParent(go.transform, false);
        Image handleImage = handleGO.GetComponent<Image>();
        handleImage.sprite = handleSprite;
        handleImage.preserveAspect = true;
        handleImage.color = Color.white;
        RectTransform handle = handleGO.GetComponent<RectTransform>();
        handle.sizeDelta = new Vector2(24f, 46f);

        Slider slider = go.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.fillRect = fillRect;
        slider.handleRect = handle;
        slider.targetGraphic = handleImage;
        return slider;
    }

    private static Sprite CreateOptionSprite(Texture2D texture, bool indonesian)
    {
        if (texture == null) return null;
        // Indonesian option art places the icon higher on its sheet, so its crop
        // starts higher while retaining the same dimensions and displayed aspect ratio.
        Rect contentRect = new Rect(600f, indonesian ? 170f : 100f, 700f, 850f);
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
