using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Builds a camera-rendered MainMenu whose complete UI is editable in the scene hierarchy.</summary>
public static class MainMenuSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/MainMenu.unity";
    private const string BackgroundPath = "Assets/Art/Title Screen/Title Screen Illustration.png";
    private const string LogoPath = "Assets/Art/Title Screen/Starlight Guardian Logo.png";

    [MenuItem("Tools/Starlight Guardian/Build Editable Main Menu")]
    public static void Build()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject root = new GameObject("Main Menu");

        Camera camera = CreateCamera(root.transform);
        Canvas canvas = CreateCanvas(root.transform, camera);
        CreateEventSystem(root.transform);
        CreateBackground(canvas.transform);

        MainMenuController controller = root.AddComponent<MainMenuController>();
        GameObject main = CreatePage("Main Options", canvas.transform, true, false);
        GameObject stages = CreatePage("Stage Select Popup", canvas.transform, false, true);
        GameObject languages = CreatePage("Language Select Popup", canvas.transform, false, true);
        GameObject settings = CreatePage("Settings Page", canvas.transform, false, true);

        CreateLogo(main.transform);
        Button[] mainButtons =
        {
            CreateButton("New Game", main.transform, "NEW GAME", new Vector2(0.22f, 0.66f)),
            CreateButton("Stage", main.transform, "STAGE", new Vector2(0.22f, 0.52f)),
            CreateButton("Language", main.transform, "LANGUAGE", new Vector2(0.22f, 0.38f)),
            CreateButton("Settings", main.transform, "SETTINGS", new Vector2(0.22f, 0.24f))
        };

        Text stageTitle = CreateTitle("Stage Popup Title", stages.transform, "STAGE");
        Button[] stageButtons = new Button[3];
        for (int i = 0; i < stageButtons.Length; i++)
            stageButtons[i] = CreateButton("Stage " + (i + 1), stages.transform, "STAGE " + (i + 1), new Vector2(0.68f, 0.68f - i * 0.14f));
        Button stageBack = CreateButton("Back", stages.transform, "BACK", new Vector2(0.68f, 0.17f));

        Text languageTitle = CreateTitle("Language Popup Title", languages.transform, "LANGUAGE");
        string[] languageNames = { "English", "മലയാളം", "Bahasa Indonesia", "简体中文" };
        Button[] languageButtons = new Button[languageNames.Length];
        for (int i = 0; i < languageButtons.Length; i++)
            languageButtons[i] = CreateButton(languageNames[i], languages.transform, languageNames[i], new Vector2(0.68f, 0.70f - i * 0.13f));
        Button languageBack = CreateButton("Back", languages.transform, "BACK", new Vector2(0.68f, 0.13f));

        Text settingsTitle = CreateTitle("Settings Title", settings.transform, "SETTINGS");
        Text musicLabel = CreateText("Music Volume Label", settings.transform, "MUSIC VOLUME", 30, new Vector2(0.68f, 0.72f), new Vector2(500, 50));
        Slider musicSlider = CreateSlider("Music Volume Slider", settings.transform, new Vector2(0.68f, 0.63f));
        Text sfxLabel = CreateText("Sound FX Volume Label", settings.transform, "SOUND FX VOLUME", 30, new Vector2(0.68f, 0.50f), new Vector2(500, 50));
        Slider sfxSlider = CreateSlider("Sound FX Volume Slider", settings.transform, new Vector2(0.68f, 0.41f));
        Button controlsButton = CreateButton("Control Mode", settings.transform, "CONTROLS: KEYBOARD", new Vector2(0.68f, 0.28f));
        Button settingsBack = CreateButton("Back", settings.transform, "BACK", new Vector2(0.68f, 0.14f));

        Set(controller, "mainPage", main);
        Set(controller, "stagePage", stages);
        Set(controller, "languagePage", languages);
        Set(controller, "settingsPage", settings);
        Set(controller, "newGameButton", mainButtons[0]);
        Set(controller, "stageMenuButton", mainButtons[1]);
        Set(controller, "languageMenuButton", mainButtons[2]);
        Set(controller, "settingsMenuButton", mainButtons[3]);
        Set(controller, "stageButtons", stageButtons);
        Set(controller, "languageButtons", languageButtons);
        Set(controller, "backButtons", new[] { stageBack, languageBack, settingsBack });
        Set(controller, "controlsButton", controlsButton);
        Set(controller, "mainLabels", new[] { LabelOf(mainButtons[0]), LabelOf(mainButtons[1]), LabelOf(mainButtons[2]), LabelOf(mainButtons[3]) });
        Set(controller, "stageTitle", stageTitle);
        Set(controller, "languageTitle", languageTitle);
        Set(controller, "settingsTitle", settingsTitle);
        Set(controller, "musicLabel", musicLabel);
        Set(controller, "sfxLabel", sfxLabel);
        Set(controller, "musicSlider", musicSlider);
        Set(controller, "sfxSlider", sfxSlider);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = root;
        Debug.Log("MainMenu rebuilt as editable camera-rendered UI.");
    }

    private static Camera CreateCamera(Transform parent)
    {
        GameObject go = new GameObject("Main Camera");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0, 0, -10);
        go.tag = "MainCamera";
        Camera camera = go.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.015f, 0.025f, 0.08f, 1f);
        camera.orthographic = true;
        camera.orthographicSize = 5.4f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100f;
        go.AddComponent<AudioListener>();
        return camera;
    }

    private static Canvas CreateCanvas(Transform parent, Camera camera)
    {
        GameObject go = new GameObject("Title Screen Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.transform.SetParent(parent, false);
        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 1f;
        canvas.sortingOrder = 0;
        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private static void CreateEventSystem(Transform parent)
    {
        GameObject go = new GameObject("EventSystem");
        go.transform.SetParent(parent, false);
        go.AddComponent<EventSystem>();
        System.Type inputModule = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (inputModule != null) go.AddComponent(inputModule);
        else go.AddComponent<StandaloneInputModule>();
    }

    private static void CreateBackground(Transform parent)
    {
        GameObject go = new GameObject("Title Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        Image image = go.GetComponent<Image>();
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
        image.preserveAspect = false;
        image.raycastTarget = false;
        go.transform.SetAsFirstSibling();
    }

    private static void CreateLogo(Transform parent)
    {
        GameObject go = new GameObject("Starlight Guardian Logo", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.22f, 0.85f);
        rect.sizeDelta = new Vector2(500, 282);
        Image image = go.GetComponent<Image>();
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(LogoPath);
        image.preserveAspect = true;
        image.raycastTarget = false;
    }

    private static GameObject CreatePage(string name, Transform parent, bool active, bool withCard)
    {
        GameObject page = new GameObject(name, typeof(RectTransform));
        page.transform.SetParent(parent, false);
        RectTransform rect = page.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        if (withCard)
        {
            GameObject card = new GameObject("Popup Card", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            card.transform.SetParent(page.transform, false);
            RectTransform cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.68f, 0.5f);
            cardRect.sizeDelta = new Vector2(650, 850);
            card.GetComponent<Image>().color = new Color(0.025f, 0.045f, 0.13f, 0.92f);
        }
        page.SetActive(active);
        return page;
    }

    private static Text CreateTitle(string name, Transform parent, string label)
    {
        Text text = CreateText(name, parent, label, 48, new Vector2(0.68f, 0.88f), new Vector2(600, 70));
        text.color = new Color(1f, 0.84f, 0.38f);
        return text;
    }

    private static Button CreateButton(string name, Transform parent, string label, Vector2 anchor)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = anchor;
        rect.sizeDelta = new Vector2(450, 72);
        Image image = go.GetComponent<Image>();
        image.color = new Color(0.025f, 0.065f, 0.16f, 0.93f);
        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.20f, 0.36f, 0.55f, 1f);
        colors.pressedColor = new Color(0.95f, 0.64f, 0.22f, 1f);
        button.colors = colors;
        Text text = CreateText("Label", go.transform, label, 32, new Vector2(0.5f, 0.5f), new Vector2(430, 62));
        text.fontStyle = FontStyle.Bold;
        return button;
    }

    private static Text CreateText(string name, Transform parent, string label, int size, Vector2 anchor, Vector2 dimensions)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = anchor;
        rect.sizeDelta = dimensions;
        Text text = go.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.text = label;
        text.raycastTarget = false;
        return text;
    }

    private static Slider CreateSlider(string name, Transform parent, Vector2 anchor)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Slider));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = anchor;
        rect.sizeDelta = new Vector2(440, 34);

        Image track = AddImage("Track", go.transform, new Color(0.16f, 0.21f, 0.34f));
        Stretch(track.rectTransform);
        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(go.transform, false);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        Stretch(fillAreaRect); fillAreaRect.offsetMin = new Vector2(4, 5); fillAreaRect.offsetMax = new Vector2(-4, -5);
        Image fill = AddImage("Fill", fillArea.transform, new Color(0.98f, 0.67f, 0.22f));
        Stretch(fill.rectTransform);
        GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(go.transform, false);
        RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
        Stretch(handleAreaRect); handleAreaRect.offsetMin = new Vector2(8, 0); handleAreaRect.offsetMax = new Vector2(-8, 0);
        Image handle = AddImage("Handle", handleArea.transform, Color.white);
        RectTransform handleRect = handle.rectTransform;
        handleRect.sizeDelta = new Vector2(22, 38);
        Slider slider = go.GetComponent<Slider>();
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handleRect;
        slider.targetGraphic = handle;
        return slider;
    }

    private static Image AddImage(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>(); image.color = color; return image;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
    }

    private static Text LabelOf(Button button) => button.GetComponentInChildren<Text>();

    private static void Set(MainMenuController controller, string fieldName, object value)
    {
        FieldInfo field = typeof(MainMenuController).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        field.SetValue(controller, value);
    }
}
