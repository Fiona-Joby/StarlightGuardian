using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using UnityEngine.UI;

/// <summary>Paused, optional AI coach interaction shown at successful stage transitions.</summary>
public sealed class AiCoachInteraction : MonoBehaviour
{
    private const string Endpoint = "https://starlight-guardian.onrender.com/coach";
    // Render's free service can take around a minute to wake after inactivity.
    private const int RequestTimeoutSeconds = 90;
    private static readonly Dictionary<string, Sprite> CoachSpriteCache = new Dictionary<string, Sprite>();

    [Serializable] private sealed class CoachRequest
    {
        public string question;
        public CoachSummary summary;
        public List<CoachTurn> history;
    }
    [Serializable] private sealed class CoachSummary
    {
        public string stage;
        public string controlMode;
        public string performance;
        public float durationSeconds;
        public int score;
        public int starsCollected;
        public int playedDifficulty;
        public bool automaticDifficulty;
        public CoachGestures gestures = new CoachGestures();
    }
    [Serializable] private sealed class CoachGestures
    {
        public int left, right, jump, reach, collect;
    }
    [Serializable] private sealed class CoachTurn
    {
        public string role;
        public string content;
    }
    [Serializable] private sealed class CoachResponse { public string answer; public string error; }

    private static readonly string[,] Copy =
    {
        { "AI Movement Coach", "Ask Coach", "How can I improve my movement?", "What did I do well?", "Type a question…", "Ask", "Continue", "Thinking…", "The coach is taking a moment. Try a question, or continue when you’re ready.", "Ask another question any time.", "Opening tip", "Reply in English.", "Difficulty: {0} ({1})", "Easy", "Normal", "Hard", "Auto", "Manual", "Restart", "Use simple words and short sentences.", "Difficulty" },
        { "AI ചലന പരിശീലകൻ", "പരിശീലകനോട് ചോദിക്കുക", "എന്റെ ചലനം എങ്ങനെ മെച്ചപ്പെടുത്താം?", "ഞാൻ നന്നായി ചെയ്തത് എന്താണ്?", "ഒരു ചോദ്യം ടൈപ്പ് ചെയ്യൂ…", "ചോദിക്കുക", "തുടരുക", "ചിന്തിക്കുന്നു…", "പരിശീലകൻ മറുപടി നൽകാൻ വൈകുന്നു. ഒരു ചോദ്യം ചോദിക്കാം, അല്ലെങ്കിൽ തയ്യാറാകുമ്പോൾ തുടരാം.", "എപ്പോൾ വേണമെങ്കിലും മറ്റൊരു ചോദ്യം ചോദിക്കാം.", "ആദ്യ നിർദേശം", "മലയാളത്തിൽ മറുപടി നൽകുക.", "ബുദ്ധിമുട്ട്: {0} ({1})", "എളുപ്പം", "സാധാരണം", "കഠിനം", "സ്വയം", "മാനുവൽ", "വീണ്ടും തുടങ്ങുക", "ലളിതമായ വാക്കുകളും ചെറിയ വാക്യങ്ങളും ഉപയോഗിക്കുക.", "ബുദ്ധിമുട്ട്" },
        { "Pelatih Gerakan AI", "Tanya Pelatih", "Bagaimana cara meningkatkan gerakan saya?", "Apa yang saya lakukan dengan baik?", "Ketik pertanyaan…", "Tanya", "Lanjutkan", "Sedang berpikir…", "Pelatih sedang lambat merespons. Coba bertanya atau lanjutkan saat siap.", "Kamu bisa bertanya lagi kapan saja.", "Saran awal", "Balas dalam bahasa Indonesia.", "Kesulitan: {0} ({1})", "Mudah", "Normal", "Sulit", "Otomatis", "Manual", "Ulangi tahap", "Gunakan kata sederhana dan kalimat pendek.", "Kesulitan" },
        { "AI 动作教练", "询问教练", "如何改善我的动作？", "我哪些方面做得好？", "输入问题…", "提问", "继续", "正在思考…", "教练暂时没有回应。你可以再试一次，或准备好后继续。", "你可以随时继续提问。", "初始建议", "请用中文回答。", "难度：{0}（{1}）", "简单", "普通", "困难", "自动", "手动", "重新开始", "请使用简单词语和短句。", "难度" }
    };

    private readonly List<CoachTurn> history = new List<CoachTurn>();
    private CoachSummary summary;
    private Action continueAction;
    private bool requestInProgress;
    private Text replyText;
    private Button improveButton, wellButton, askButton, continueButton, restartButton;
    private Button easyButton, normalButton, hardButton, autoButton;
    private InputField questionInput;
    private int language;
    private bool gestureMode;
    private bool showContinue = true;
    private string stageContext;

    public static AiCoachInteraction Show(Transform parent, string stage, float durationSeconds,
        int score, int starsCollected, string context, PlayerInput playerInput, Action onContinue,
        bool allowContinue = true, int playedDifficulty = -1, bool automaticDifficulty = false)
    {
        RuntimeUiUtility.EnsureEventSystem();
        var host = new GameObject("AI Coach Interaction", typeof(RectTransform));
        host.transform.SetParent(parent, false);
        var hostRect = host.GetComponent<RectTransform>();
        hostRect.anchorMin = Vector2.zero;
        hostRect.anchorMax = Vector2.one;
        hostRect.offsetMin = hostRect.offsetMax = Vector2.zero;
        Canvas coachCanvas = host.AddComponent<Canvas>();
        coachCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        coachCanvas.overrideSorting = true;
        coachCanvas.sortingOrder = 500;
        host.AddComponent<GraphicRaycaster>();
        var coach = host.AddComponent<AiCoachInteraction>();
        coach.summary = new CoachSummary
        {
            stage = stage,
            durationSeconds = Mathf.Clamp(durationSeconds, 0f, 3600f),
            score = Mathf.Max(0, score),
            starsCollected = Mathf.Max(0, starsCollected),
            playedDifficulty = Mathf.Clamp(playedDifficulty < 0 ? GameDifficulty.Level : playedDifficulty, 0, 2),
            automaticDifficulty = automaticDifficulty
        };
        coach.continueAction = onContinue;
        coach.showContinue = allowContinue;
        coach.gestureMode = playerInput != null && playerInput.IsUsingGestureControls;
        coach.summary.controlMode = coach.gestureMode ? "gesture" : "keyboard";
        coach.stageContext = context ?? string.Empty;
        coach.summary.performance = coach.stageContext;
        if (playerInput != null && coach.gestureMode)
        {
            coach.summary.gestures.left = playerInput.GestureLeftCount;
            coach.summary.gestures.right = playerInput.GestureRightCount;
            coach.summary.gestures.jump = playerInput.GestureJumpCount;
            coach.summary.gestures.reach = playerInput.GestureReachCount;
            coach.summary.gestures.collect = playerInput.GestureCollectCount;
        }
        coach.Build(coach.stageContext);
        coach.StartCoroutine(coach.RequestReply(coach.CreateQuestion(coach.Localized(10) + ". " + coach.Localized(2)), true));
        return coach;
    }

    private string Localized(int key) => Copy[language, key];

    private void Build(string context)
    {
        language = Mathf.Clamp(PlayerPrefs.GetInt("Language", 0), 0, 3);
        var root = RuntimeUiUtility.CreateRect("Coach Card", transform);
        root.anchorMin = new Vector2(0.02f, 0.02f);
        root.anchorMax = new Vector2(0.98f, 0.98f);
        root.offsetMin = root.offsetMax = Vector2.zero;
        var background = root.gameObject.AddComponent<Image>();
        background.sprite = LoadCoachSprite("AI Tab");
        background.type = Image.Type.Simple;
        background.preserveAspect = false;

        // The supplied panel art already contains the title and question bar.
        AddExpandedFeedbackPanel(root);
        replyText = AddText("Coach Reply", root, Localized(7), 42,
            new Vector2(0.14f, 0.53f), new Vector2(0.86f, 0.78f), new Color(0.94f, 0.96f, 1f, 1f), FontStyle.Bold);

        easyButton = AddImageButton("Easy Difficulty", root, "AI Easy", new Vector2(0.14f, 0.43f), new Vector2(0.31f, 0.50f));
        normalButton = AddImageButton("Normal Difficulty", root, "AI Normal", new Vector2(0.335f, 0.43f), new Vector2(0.50f, 0.50f));
        hardButton = AddImageButton("Hard Difficulty", root, "AI Hard", new Vector2(0.525f, 0.43f), new Vector2(0.69f, 0.50f));
        autoButton = AddImageButton("Automatic Difficulty", root, "AI Auto", new Vector2(0.715f, 0.43f), new Vector2(0.88f, 0.50f));
        easyButton.onClick.AddListener(() => SelectManualDifficulty(0));
        normalButton.onClick.AddListener(() => SelectManualDifficulty(1));
        hardButton.onClick.AddListener(() => SelectManualDifficulty(2));
        autoButton.onClick.AddListener(SelectAutomaticDifficulty);
        RefreshDifficultyControls();

        improveButton = AddImageButton("Improve Prompt", root, "AI Tips", new Vector2(0.26f, 0.31f), new Vector2(0.49f, 0.41f));
        wellButton = AddImageButton("Well Prompt", root, "AI Do Well", new Vector2(0.51f, 0.31f), new Vector2(0.75f, 0.41f));
        improveButton.onClick.AddListener(() => Ask(Localized(2)));
        wellButton.onClick.AddListener(() => Ask(Localized(3)));

        questionInput = AddInput(root);
        askButton = AddImageButton("Ask Button", root, "Common/AI Ask Enter", new Vector2(0.81f, 0.235f), new Vector2(0.86f, 0.305f));
        askButton.onClick.AddListener(() => Ask(questionInput.text));
        questionInput.onEndEdit.AddListener(text =>
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) Ask(text);
        });
        restartButton = AddImageButton("Restart Button", root, "AI Restart", new Vector2(0.12f, 0.12f), new Vector2(0.31f, 0.21f));
        restartButton.onClick.AddListener(RestartCurrentStage);
        continueButton = AddImageButton("Continue Button", root, "AI Continue", new Vector2(0.69f, 0.12f), new Vector2(0.88f, 0.21f));
        continueButton.onClick.AddListener(Continue);
        continueButton.gameObject.SetActive(showContinue);
    }

    private void SelectManualDifficulty(int level)
    {
        GameDifficulty.SetManual(level);
        RefreshDifficultyControls();
    }

    private void SelectAutomaticDifficulty()
    {
        GameDifficulty.EnableAutomatic();
        RefreshDifficultyControls();
    }

    private void RefreshDifficultyControls()
    {
        SetDifficultyButtonState(easyButton, GameDifficulty.Level == 0);
        SetDifficultyButtonState(normalButton, GameDifficulty.Level == 1);
        SetDifficultyButtonState(hardButton, GameDifficulty.Level == 2);
        SetDifficultyButtonState(autoButton, GameDifficulty.IsAutomatic);
    }

    private static void SetDifficultyButtonState(Button button, bool selected)
    {
        if (button != null)
        {
            var outline = button.GetComponent<Outline>();
            if (outline != null) outline.enabled = selected;
        }
    }

    private Text AddText(string name, Transform parent, string value, int size, Vector2 min, Vector2 max, Color color, FontStyle style)
    {
        Text text = RuntimeUiUtility.CreateText(name, parent, RuntimeUiUtility.GetFont(), size, TextAnchor.MiddleCenter, color, style);
        text.text = value;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.rectTransform.anchorMin = min;
        text.rectTransform.anchorMax = max;
        text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
        text.supportRichText = false;
        string fontName = language == 1 ? "Nirmala UI" : language == 3 ? "Microsoft YaHei" : "Arial";
        Font selectedFont = Font.CreateDynamicFontFromOSFont(fontName, size);
        if (selectedFont != null) text.font = selectedFont;
        var outline = text.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.015f, 0.02f, 0.06f, 0.9f);
        outline.effectDistance = new Vector2(1.25f, -1.25f);
        return text;
    }

    private Button AddImageButton(string name, Transform parent, string spriteName, Vector2 min, Vector2 max)
    {
        var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
        buttonObject.transform.SetParent(parent, false);
        var image = buttonObject.GetComponent<Image>();
        image.sprite = LoadCoachSprite(spriteName);
        image.preserveAspect = false;
        var button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;
        var feedback = buttonObject.AddComponent<CoachButtonFeedback>();
        button.onClick.AddListener(feedback.PlayClickFeedback);
        var outline = buttonObject.GetComponent<Outline>();
        outline.effectColor = new Color(0.25f, 1f, 0.5f, 1f);
        outline.effectDistance = new Vector2(3f, -3f);
        outline.enabled = false;
        RectTransform rect = button.GetComponent<RectTransform>();
        float centerY = (min.y + max.y) * 0.5f;
        rect.anchorMin = new Vector2(min.x, centerY);
        rect.anchorMax = new Vector2(max.x, centerY);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        if (image.sprite != null)
        {
            var aspectFitter = buttonObject.AddComponent<AspectRatioFitter>();
            aspectFitter.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
            aspectFitter.aspectRatio = image.sprite.rect.width / image.sprite.rect.height;
        }
        return button;
    }

    private void AddExpandedFeedbackPanel(Transform parent)
    {
        Sprite source = LoadCoachSprite("AI Tab");
        if (source == null) return;

        string cacheKey = "ExpandedFeedbackPanel/" + language;
        if (!CoachSpriteCache.TryGetValue(cacheKey, out Sprite panelSprite))
        {
            // This is the feedback frame from AI Tab, sliced so its rounded corners and border stay intact.
            panelSprite = Sprite.Create(source.texture, new Rect(235f, 636f, 1446f, 202f),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                new Vector4(38f, 38f, 38f, 38f));
            CoachSpriteCache[cacheKey] = panelSprite;
        }

        GameObject panelObject = new GameObject("Expanded Feedback Window", typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(parent, false);
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.105f, 0.515f);
        panelRect.anchorMax = new Vector2(0.895f, 0.805f);
        panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;
        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.sprite = panelSprite;
        panelImage.type = Image.Type.Sliced;
        panelImage.preserveAspect = false;
    }

    private InputField AddInput(Transform parent)
    {
        GameObject fieldObject = new GameObject("Question Input", typeof(RectTransform), typeof(Image), typeof(InputField));
        fieldObject.transform.SetParent(parent, false);
        RectTransform rect = fieldObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.12f, 0.225f);
        rect.anchorMax = new Vector2(0.78f, 0.315f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        Image hitArea = fieldObject.GetComponent<Image>();
        hitArea.color = new Color(1f, 1f, 1f, 0f);

        Text text = RuntimeUiUtility.CreateText("Text", fieldObject.transform, RuntimeUiUtility.GetFont(), 32,
            TextAnchor.MiddleLeft, Color.white, FontStyle.Bold);
        text.rectTransform.offsetMin = new Vector2(16f, 4f);
        text.rectTransform.offsetMax = new Vector2(-16f, -4f);
        text.raycastTarget = false;
        var input = fieldObject.GetComponent<InputField>();
        input.textComponent = text;
        input.targetGraphic = hitArea;
        input.placeholder = null;
        input.characterLimit = 300;
        input.lineType = InputField.LineType.SingleLine;
        input.caretColor = Color.white;
        input.customCaretColor = true;
        input.selectionColor = new Color(0.25f, 1f, 0.5f, 0.35f);
        return input;
    }

    private Sprite LoadCoachSprite(string spriteName)
    {
        string folder = language == 1 ? "Malayalam" : language == 2 ? "Indonesian" : language == 3 ? "Traditional Chinese" : "English";
        if (language == 2 && spriteName == "AI Hard") spriteName = "AI Sulit";
        string resourcePath = spriteName.StartsWith("Common/", StringComparison.Ordinal)
            ? "AICoach/" + spriteName
            : "AICoach/" + folder + "/" + spriteName;
        if (CoachSpriteCache.TryGetValue(resourcePath, out Sprite cachedSprite)) return cachedSprite;
        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        if (texture == null)
        {
            Debug.LogWarning("[AiCoachInteraction] Missing coach UI image at Resources/" + resourcePath + ".png");
            return null;
        }
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        CoachSpriteCache[resourcePath] = sprite;
        return sprite;
    }

    private void Ask(string question)
    {
        if (requestInProgress || string.IsNullOrWhiteSpace(question)) return;
        StartCoroutine(RequestReply(CreateQuestion(question.Trim()), false));
    }

    private string CreateQuestion(string question)
    {
        string stageFocus;
        switch (summary != null ? summary.stage : string.Empty)
        {
            case "Stage 1":
                stageFocus = "Stage 1 is about moving, jumping and reaching the point goal. Yellow stars give 5 points, blue stars give 10, and rainbow stars grant a temporary 2x multiplier.";
                break;
            case "Stage 2":
                stageFocus = "Stage 2 is about protecting the star spirit by defeating the required turrets and blob beasts. It has no collectible-star objective.";
                break;
            case "Stage 3":
                stageFocus = "Stage 3 is about defeating the required drones, slimes and hard beasts, then rescuing the star spirit and restoring the constellation. It has no collectible-star or score objective. Never advise collecting stars.";
                break;
            default:
                stageFocus = "Focus only on the active stage's stated objectives.";
                break;
        }
        string focus = gestureMode
            ? "Control mode: webcam gestures. Give a relevant gesture tip without claiming to see hand motion; joint data was not sent. " + stageFocus
            : "Control mode: keyboard. Avoid gesture advice. " + stageFocus;
        string difficultyName = Localized(13 + summary.playedDifficulty);
        string suffix = " " + Localized(11) + " " + Localized(19) + "\n" + focus;
        suffix += " Played difficulty: " + difficultyName + (summary.automaticDifficulty ? " (auto mode selected this level for that run)." : ".");
        if (!string.IsNullOrWhiteSpace(stageContext))
        {
            int contextLimit = Mathf.Max(0, 350 - suffix.Length - 15);
            string details = stageContext.Length > contextLimit ? stageContext.Substring(0, contextLimit) : stageContext;
            if (!string.IsNullOrWhiteSpace(details)) suffix += "\nStage details: " + details;
        }
        int questionLimit = Mathf.Max(1, 500 - suffix.Length);
        if (question.Length > questionLimit) question = question.Substring(0, questionLimit);
        return question + suffix;
    }

    private IEnumerator RequestReply(string question, bool opening)
    {
        requestInProgress = true;
        SetAskControls(false);
        if (opening) replyText.text = Localized(7);
        var payload = new CoachRequest { question = question, summary = summary, history = new List<CoachTurn>(history) };
        byte[] body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload));
        using (var request = new UnityWebRequest(Endpoint, UnityWebRequest.kHttpVerbPOST))
        {
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = RequestTimeoutSeconds;
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                CoachResponse result = JsonUtility.FromJson<CoachResponse>(request.downloadHandler.text);
                string answer = result != null ? result.answer : null;
                if (!string.IsNullOrWhiteSpace(answer))
                {
                    replyText.text = answer.Trim();
                    history.Add(new CoachTurn { role = "user", content = question });
                    history.Add(new CoachTurn { role = "assistant", content = answer.Trim() });
                    while (history.Count > 4) history.RemoveAt(0);
                }
                else ShowFallback();
            }
            else
            {
                Debug.LogWarning("[AiCoachInteraction] Coach request failed: " + request.error);
                ShowFallback();
            }
        }
        requestInProgress = false;
        SetAskControls(true);
    }

    private void ShowFallback()
    {
        replyText.text = Localized(8);
    }

    private void SetAskControls(bool enabled)
    {
        if (improveButton != null) improveButton.interactable = enabled;
        if (wellButton != null) wellButton.interactable = enabled;
        if (askButton != null) askButton.interactable = enabled;
        if (questionInput != null) questionInput.interactable = enabled;
    }

    private void Continue()
    {
        if (requestInProgress) StopAllCoroutines();
        Action action = continueAction;
        continueAction = null;
        gameObject.SetActive(false);
        action?.Invoke();
    }

    private void RestartCurrentStage()
    {
        if (requestInProgress) StopAllCoroutines();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}

internal sealed class CoachButtonFeedback : MonoBehaviour, UnityEngine.EventSystems.IPointerEnterHandler,
    UnityEngine.EventSystems.IPointerExitHandler
{
    private RectTransform rect;
    private Vector3 baseScale;
    private Coroutine animationRoutine;

    private void Awake()
    {
        rect = transform as RectTransform;
        baseScale = transform.localScale;
    }

    public void PlayClickFeedback()
    {
        GameAudio.PlaySfx("Button Click 2", 0.65f);
        AnimateTo(baseScale * 0.94f, 0.07f, true);
    }

    public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData)
    {
        AnimateTo(baseScale * 1.035f, 0.1f, false);
    }

    public void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData)
    {
        AnimateTo(baseScale, 0.1f, false);
    }

    private void AnimateTo(Vector3 target, float duration, bool pulse)
    {
        if (animationRoutine != null) StopCoroutine(animationRoutine);
        animationRoutine = StartCoroutine(Animate(target, duration, pulse));
    }

    private IEnumerator Animate(Vector3 target, float duration, bool pulse)
    {
        Vector3 start = rect.localScale;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            rect.localScale = Vector3.Lerp(start, target, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        rect.localScale = target;
        if (pulse)
        {
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                rect.localScale = Vector3.Lerp(target, baseScale, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            rect.localScale = baseScale;
        }
        animationRoutine = null;
    }
}
