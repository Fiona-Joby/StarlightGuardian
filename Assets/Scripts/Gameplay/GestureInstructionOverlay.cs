using UnityEngine;
using UnityEngine.UI;

/// <summary>On-demand, localized gesture guide for stages 1 and 2.</summary>
public sealed class GestureInstructionOverlay : MonoBehaviour
{
    public System.Action onStartupDismissed;
    private static readonly string[] Languages = { "English", "Malayalam", "Indonesian", "Traditional Chinese" };
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
            previousButton = CreateControlButton("Previous", imageRect, "StageUI/AI Ask Enter", new Vector2(0f, 0f), false);
            previousButton.onClick.AddListener(ShowPrevious);
        }
        nextButton = stage == 2
            ? CreateControlButton("Next", imageRect, "StageUI/AI Ask Enter", new Vector2(1f, 0f), true)
            : CreateControlButton("Close", imageRect, "StageUI/Close Tab Cross", new Vector2(1f, 1f), false);
        nextButton.onClick.AddListener(stage == 2 ? (UnityEngine.Events.UnityAction)ShowNext : Exit);
        if (stage == 2)
        {
            exitButton = CreateControlButton("Close", imageRect, "StageUI/Close Tab Cross", new Vector2(1f, 1f), false);
            exitButton.onClick.AddListener(Exit);
        }
        startupContinueButton = CreateContinueButton(imageRect);
        startupContinueButton.onClick.AddListener(Exit);
        previousButton?.GetComponent<RectTransform>().SetAsLastSibling();
        page.SetActive(false);
    }

    public void Show()
    {
        if (page == null) return;
        currentPage = 1;
        UpdatePage();
        SetReminderControls(true);
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

    private static Vector2 FitSize(Texture2D texture, Vector2 bounds)
    {
        if (texture == null) return bounds;
        float aspect = (float)texture.width / texture.height;
        float width = Mathf.Min(bounds.x, bounds.y * aspect);
        return new Vector2(width, width / aspect);
    }

    private static Button CreateControlButton(string name, Transform parent, string spriteResource, Vector2 anchor, bool flipped)
    {
        Texture2D texture = Resources.Load<Texture2D>(spriteResource);
        Sprite sprite = texture != null
            ? Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f)
            : null;
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.color = Color.white;
        image.rectTransform.localScale = new Vector3(flipped ? -1f : 1f, 1f, 1f);
        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = anchor.y == 1f
            ? new Vector2(1f, 1f)
            : new Vector2(anchor.x == 0f ? 1f : 0f, 0f);
        rect.anchoredPosition = anchor.y == 1f
            ? new Vector2(16f, 16f)
            : new Vector2(anchor.x == 0f ? 80f : 32f, 70f);
        float buttonSize = anchor.y == 1f ? 230f : 170f;
        rect.sizeDelta = new Vector2(buttonSize, buttonSize);
        return button;
    }

    private Button CreateContinueButton(Transform parent)
    {
        Texture2D texture = Resources.Load<Texture2D>("StageUI/Pick Arrow");
        GameObject go = new GameObject("AI Continue Arrow", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        // Place the continue arrow just below the instruction panel's lower-right corner.
        rect.anchoredPosition = new Vector2(-4f, -84f);
        rect.sizeDelta = FitSize(texture, new Vector2(340f, 340f));
        RawImage image = go.GetComponent<RawImage>();
        image.texture = texture;
        image.color = Color.white;
        image.uvRect = new Rect(0f, 0f, 1f, 1f);
        image.raycastTarget = true;
        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        return button;
    }
}
