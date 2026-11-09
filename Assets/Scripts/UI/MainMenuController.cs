using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Runtime title screen and its stage, language, and settings panels.</summary>
public class MainMenuController : MonoBehaviour
{
    [SerializeField] private Sprite titleBackground;
    private enum Language { English, Malayalam, Indonesian, Chinese }
    private Language language;
    private Canvas canvas;
    private Font font;
    private Transform panel;
    private readonly List<GameObject> panelItems = new List<GameObject>();
    private int panelButtonCount;
    private int sliderCount;
    private static readonly string[] stages = { "MainScene", "Stage2", "Stage3" };

    private void Awake()
    {
        language = (Language)Mathf.Clamp(PlayerPrefs.GetInt("Language", 0), 0, 3);
        RuntimeUiUtility.EnsureEventSystem();
        canvas = RuntimeUiUtility.CreateOverlayCanvas("Main Menu UI", 10, new Vector2(1920, 1080));
        string fontName = language == Language.Malayalam ? "Nirmala UI" :
            language == Language.Chinese ? "Microsoft YaHei" : "Arial";
        font = Font.CreateDynamicFontFromOSFont(fontName, 32);
        if (font == null) font = RuntimeUiUtility.GetFont();
        if (titleBackground != null)
        {
            Image background = NewImage("Title Illustration", canvas.transform, Color.white);
            background.sprite = titleBackground;
            background.preserveAspect = false;
            RectTransform backgroundRect = background.rectTransform;
            backgroundRect.anchorMin = Vector2.zero; backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero; backgroundRect.offsetMax = Vector2.zero;
            background.transform.SetAsFirstSibling();
        }
        BuildMenu();
    }

    private string T(string key)
    {
        string[,] words = {
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
        string[] keys = { "NEW GAME", "STAGE", "LANGUAGE", "SETTINGS", "BACK", "MUSIC", "SFX", "CONTROLS", "KEYBOARD", "GESTURE" };
        for (int i = 0; i < keys.Length; i++) if (key == keys[i]) return words[i, (int)language];
        return key;
    }

    private void BuildMenu()
    {
        ClearPanel();
        string[] labels = { T("NEW GAME"), T("STAGE"), T("LANGUAGE"), T("SETTINGS") };
        System.Action[] actions = { () => LoadStage(0), ShowStages, ShowLanguages, ShowSettings };
        for (int i = 0; i < labels.Length; i++)
        {
            int index = i;
            Button b = AddButton("Menu " + i, canvas.transform, labels[i], new Vector2(390, 78), new Vector2(0.23f, 0.62f - i * 0.14f));
            b.onClick.AddListener(() => actions[index]());
        }
    }

    private void ShowStages()
    {
        BeginPanel(T("STAGE"));
        for (int i = 0; i < stages.Length; i++)
        {
            int stage = i;
            AddPanelButton(T("STAGE") + " " + (i + 1), () => LoadStage(stage));
        }
        AddPanelButton(T("BACK"), BuildMenu);
    }

    private void ShowLanguages()
    {
        BeginPanel(T("LANGUAGE"));
        string[] names = { "English", "മലയാളം", "Bahasa Indonesia", "简体中文" };
        for (int i = 0; i < names.Length; i++)
        {
            int selected = i;
            AddPanelButton(names[i], () => SetLanguage(selected));
        }
        AddPanelButton(T("BACK"), BuildMenu);
    }

    private void SetLanguage(int selected)
    {
        language = (Language)selected;
        PlayerPrefs.SetInt("Language", selected);
        string fontName = language == Language.Malayalam ? "Nirmala UI" :
            language == Language.Chinese ? "Microsoft YaHei" : "Arial";
        font = Font.CreateDynamicFontFromOSFont(fontName, 32);
        if (font == null) font = RuntimeUiUtility.GetFont();
        BuildMenu();
    }

    private void ShowSettings()
    {
        BeginPanel(T("SETTINGS"));
        AddSlider(T("MUSIC"), "MusicVolume", 0.8f);
        AddSlider(T("SFX"), "SfxVolume", 0.8f);
        ApplyVolumes();
        bool gesture = PlayerPrefs.GetInt("GestureControls", 0) == 1;
        AddPanelButtonAt(T("CONTROLS") + ": " + T(gesture ? "GESTURE" : "KEYBOARD"), () =>
        {
            PlayerPrefs.SetInt("GestureControls", gesture ? 0 : 1);
            ShowSettings();
        }, 0.32f);
        AddPanelButtonAt(T("BACK"), BuildMenu, 0.15f);
    }

    private void BeginPanel(string heading)
    {
        ClearPanel();
        panelButtonCount = 0;
        sliderCount = 0;
        panel = new GameObject("Menu Popup", typeof(RectTransform), typeof(Image)).transform;
        panel.SetParent(canvas.transform, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.66f, 0.5f);
        rect.sizeDelta = new Vector2(620, 650);
        panel.GetComponent<Image>().color = new Color(0.025f, 0.045f, 0.13f, 0.92f);
        Text title = MakeText("Popup Heading", panel, heading, 46, new Vector2(0, 0.92f), new Vector2(560, 72));
        title.color = new Color(1f, 0.84f, 0.38f);
    }

    private void AddPanelButton(string label, System.Action action)
    {
        float y = 0.72f - panelButtonCount++ * 0.15f;
        AddPanelButtonAt(label, action, y);
    }

    private void AddPanelButtonAt(string label, System.Action action, float y)
    {
        Button button = AddButton(label, panel, label, new Vector2(480, 66), new Vector2(0.5f, y));
        panelItems.Add(button.gameObject);
        button.onClick.AddListener(() => action());
    }

    private void AddSlider(string label, string pref, float fallback)
    {
        float y = 0.80f - sliderCount++ * 0.24f;
        Text caption = MakeText(label, panel, label, 26, new Vector2(0.5f, y), new Vector2(500, 44));
        panelItems.Add(caption.gameObject);
        GameObject go = new GameObject(label + " Slider", typeof(RectTransform), typeof(Slider));
        go.transform.SetParent(panel, false);
        RectTransform rt = go.GetComponent<RectTransform>(); rt.anchorMin = rt.anchorMax = new Vector2(0.5f, y - 0.075f); rt.sizeDelta = new Vector2(440, 28);
        Image track = NewImage("Track", go.transform, new Color(0.18f, 0.23f, 0.37f));
        RectTransform tr = track.rectTransform; tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = Vector2.zero; tr.offsetMax = Vector2.zero;
        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image)); fill.transform.SetParent(go.transform, false);
        RectTransform fr = fill.GetComponent<RectTransform>(); fr.anchorMin = Vector2.zero; fr.anchorMax = Vector2.one; fr.offsetMin = Vector2.zero; fr.offsetMax = Vector2.zero;
        fill.GetComponent<Image>().color = new Color(0.98f, 0.67f, 0.22f);
        GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(Image)); handle.transform.SetParent(go.transform, false); handle.GetComponent<Image>().color = Color.white;
        Slider slider = go.GetComponent<Slider>(); slider.minValue = 0; slider.maxValue = 1; slider.fillRect = fr; slider.handleRect = handle.GetComponent<RectTransform>(); slider.targetGraphic = handle.GetComponent<Image>(); slider.value = PlayerPrefs.GetFloat(pref, fallback);
        slider.onValueChanged.AddListener(value => { PlayerPrefs.SetFloat(pref, value); ApplyVolumes(); });
        panelItems.Add(go);
    }

    private void ApplyVolumes()
    {
        float music = PlayerPrefs.GetFloat("MusicVolume", 0.8f);
        float sfx = PlayerPrefs.GetFloat("SfxVolume", 0.8f);
        foreach (AudioSource source in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
            source.volume = source.gameObject.name.ToLowerInvariant().Contains("music") ? music : sfx;
    }

    private void LoadStage(int index)
    {
        PlayerPrefs.SetInt("SelectedStage", index + 1);
        if (Application.CanStreamedLevelBeLoaded(stages[index])) SceneManager.LoadScene(stages[index]);
        else Debug.LogError("Stage scene is missing from Build Settings: " + stages[index]);
    }

    private void ClearPanel()
    {
        if (panel != null) Destroy(panel.gameObject);
        panel = null; panelItems.Clear();
        if (canvas == null) return;
        for (int i = canvas.transform.childCount - 1; i >= 0; i--)
            if (canvas.transform.GetChild(i).name.StartsWith("Menu ")) Destroy(canvas.transform.GetChild(i).gameObject);
    }

    private Button AddButton(string name, Transform parent, string label, Vector2 size, Vector2 anchor)
    {
        Button b = RuntimeUiUtility.CreateButton(name, parent, label, 32, new Color(0.035f, 0.075f, 0.19f, 0.94f), Color.white, size);
        RectTransform rt = b.GetComponent<RectTransform>(); rt.anchorMin = rt.anchorMax = anchor; rt.anchoredPosition = Vector2.zero; rt.sizeDelta = size;
        ColorBlock cb = b.colors; cb.highlightedColor = new Color(0.2f, 0.37f, 0.58f); cb.pressedColor = new Color(0.95f, 0.64f, 0.22f); b.colors = cb;
        return b;
    }

    private Text MakeText(string name, Transform parent, string label, int size, Vector2 anchor, Vector2 dimensions)
    {
        Text text = RuntimeUiUtility.CreateText(name, parent, font, size, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
        RectTransform rt = text.rectTransform; rt.anchorMin = rt.anchorMax = anchor; rt.anchoredPosition = Vector2.zero; rt.sizeDelta = dimensions; text.text = label;
        return text;
    }

    private static Image NewImage(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false); Image image = go.GetComponent<Image>(); image.color = color; return image;
    }
}
