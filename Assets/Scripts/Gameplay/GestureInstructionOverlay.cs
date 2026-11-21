using UnityEngine;
using UnityEngine.UI;

/// <summary>On-demand, localized gesture guide for stages 1 and 2.</summary>
public sealed class GestureInstructionOverlay : MonoBehaviour
{
    public System.Action onStartupDismissed;
    private static readonly string[] Languages = { "English", "Malayalam", "Indonesian", "Traditional Chinese" };
    private static readonly string[][] Labels =
    {
        new[] { "PREVIOUS", "NEXT", "EXIT" },
        new[] { "മുമ്പത്തെ", "അടുത്തത്", "പുറത്ത്" },
        new[] { "SEBELUMNYA", "BERIKUTNYA", "KELUAR" },
        new[] { "上一頁", "下一頁", "退出" }
    };

    private GameObject page;
    private RawImage instructionImage;
    private Button previousButton;
    private Button nextButton;
    private Button exitButton;
    private Button startupContinueButton;
    private int stage;
    private int currentPage = 1;
    private int language;
    private float previousTimeScale = 1f;

    public void Configure(int stageNumber, Transform parent)
    {
        stage = stageNumber;
        language = Mathf.Clamp(PlayerPrefs.GetInt("Language", 0), 0, Languages.Length - 1);
        page = new GameObject("Gesture Instructions", typeof(RectTransform));
        page.transform.SetParent(parent, false);
        RectTransform pageRect = page.GetComponent<RectTransform>();
        pageRect.anchorMin = Vector2.zero;
        pageRect.anchorMax = Vector2.one;
        pageRect.offsetMin = pageRect.offsetMax = Vector2.zero;

        Image dimmer = RuntimeUiUtility.CreateRect("Instruction Backdrop", page.transform).gameObject.AddComponent<Image>();
        dimmer.rectTransform.anchorMin = Vector2.zero;
        dimmer.rectTransform.anchorMax = Vector2.one;
        dimmer.rectTransform.offsetMin = dimmer.rectTransform.offsetMax = Vector2.zero;
        dimmer.color = new Color(0f, 0f, 0f, 0.86f);

        Texture2D texture = LoadPageTexture(1);
        instructionImage = RuntimeUiUtility.CreateRect("Instruction Image", page.transform).gameObject.AddComponent<RawImage>();
        instructionImage.texture = texture;
        instructionImage.raycastTarget = false;
        RectTransform imageRect = instructionImage.rectTransform;
        imageRect.anchorMin = imageRect.anchorMax = new Vector2(0.5f, 0.5f);
        imageRect.sizeDelta = FitSize(texture, new Vector2(1500f, 844f));

        if (stage == 2)
        {
            previousButton = CreateControlButton("Previous", imageRect, Labels[language][0], new Vector2(0f, 0f));
            previousButton.onClick.AddListener(ShowPrevious);
        }
        nextButton = CreateControlButton("Next", imageRect, stage == 2 ? Labels[language][1] : Labels[language][2], new Vector2(1f, 0f));
        nextButton.onClick.AddListener(stage == 2 ? (UnityEngine.Events.UnityAction)ShowNext : Exit);
        if (stage == 2)
        {
            exitButton = CreateControlButton("Exit", imageRect, Labels[language][2], new Vector2(1f, 0f));
            exitButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(-224f, 24f);
            exitButton.onClick.AddListener(Exit);
        }
        startupContinueButton = CreateContinueButton(imageRect);
        startupContinueButton.onClick.AddListener(Exit);
        previousButton?.GetComponent<RectTransform>().SetAsLastSibling();
        nextButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(-24f, 24f);
        page.SetActive(false);
    }

    public void Show()
    {
        if (page == null) return;
        currentPage = 1;
        UpdatePage();
        SetReminderControls(true);
        SetButtonLabel(nextButton, stage == 2 ? Labels[language][1] : Labels[language][2]);
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        page.SetActive(true);
    }

    public void ShowAtStageStart()
    {
        if (page == null) return;
        currentPage = stage == 2 ? 2 : 1;
        UpdatePage();
        if (stage == 2)
        {
            SetReminderControls(false);
            startupContinueButton.gameObject.SetActive(true);
        }
        else
        {
            SetReminderControls(false);
            startupContinueButton.gameObject.SetActive(true);
        }
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        page.SetActive(true);
    }

    private void ShowPrevious() { currentPage = 1; UpdatePage(); }
    private void ShowNext() { currentPage = 2; UpdatePage(); }

    private void UpdatePage()
    {
        Texture2D texture = LoadPageTexture(currentPage);
        if (texture == null) return;
        instructionImage.texture = texture;
        instructionImage.rectTransform.sizeDelta = FitSize(texture, new Vector2(1500f, 844f));
        if (previousButton != null) previousButton.interactable = currentPage > 1;
        if (stage == 2) nextButton.interactable = currentPage < 2;
    }

    private Texture2D LoadPageTexture(int number)
    {
        string path = "GestureInstructions/" + Languages[language] + "/Gesture Control " + number;
        Texture2D texture = Resources.Load<Texture2D>(path);
        if (texture == null) Debug.LogError("Gesture instruction image not found at Resources/" + path + ".png");
        return texture;
    }

    private void Exit()
    {
        if (page != null) page.SetActive(false);
        Time.timeScale = previousTimeScale;
        System.Action callback = onStartupDismissed;
        onStartupDismissed = null;
        callback?.Invoke();
    }

    private void SetReminderControls(bool visible)
    {
        if (previousButton != null) previousButton.gameObject.SetActive(visible);
        if (nextButton != null) nextButton.gameObject.SetActive(visible);
        if (exitButton != null) exitButton.gameObject.SetActive(visible);
        if (startupContinueButton != null) startupContinueButton.gameObject.SetActive(false);
    }

    private static void SetButtonLabel(Button button, string label)
    {
        Text text = button != null ? button.GetComponentInChildren<Text>(true) : null;
        if (text != null) text.text = label;
    }

    private static Vector2 FitSize(Texture2D texture, Vector2 bounds)
    {
        if (texture == null) return bounds;
        float aspect = (float)texture.width / texture.height;
        float width = Mathf.Min(bounds.x, bounds.y * aspect);
        return new Vector2(width, width / aspect);
    }

    private static Button CreateControlButton(string name, Transform parent, string label, Vector2 anchor)
    {
        Button button = RuntimeUiUtility.CreateButton(name, parent, label, 20,
            new Color(0.12f, 0.38f, 0.68f, 1f), Color.white, new Vector2(200f, 64f));
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = new Vector2(anchor.x == 0f ? 24f : -24f, 24f);
        return button;
    }

    private Button CreateContinueButton(Transform parent)
    {
        string[] languageFolders = { "English", "Malayalam", "Indonesian", "Traditional Chinese" };
        Texture2D texture = Resources.Load<Texture2D>("AIContinue/" + languageFolders[language]);
        GameObject go = new GameObject("AI Continue", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        // Keep the startup action within the illustrated control panel, slightly left and higher.
        rect.anchoredPosition = new Vector2(-140f, 112f);
        rect.sizeDelta = new Vector2(300f, 80f);
        RawImage image = go.GetComponent<RawImage>();
        image.texture = texture;
        image.color = Color.white;
        image.uvRect = new Rect(0.10f, 0.31f, 0.80f, 0.38f);
        image.raycastTarget = true;
        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        return button;
    }
}
