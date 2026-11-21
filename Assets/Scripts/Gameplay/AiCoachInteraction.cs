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
        { "AI Movement Coach", "Ask Coach", "How can I improve my movement?", "What did I do well?", "Type a question…", "Ask", "Continue", "Thinking…", "The coach is taking a moment. Try a question, or continue when you’re ready.", "Ask another question any time.", "Opening tip", "Reply in English.", "Difficulty: {0} ({1})", "Easy", "Normal", "Hard", "Auto", "Manual", "Restart", "Use simple words and short sentences." },
        { "AI ചലന പരിശീലകൻ", "പരിശീലകനോട് ചോദിക്കുക", "എന്റെ ചലനം എങ്ങനെ മെച്ചപ്പെടുത്താം?", "ഞാൻ നന്നായി ചെയ്തത് എന്താണ്?", "ഒരു ചോദ്യം ടൈപ്പ് ചെയ്യൂ…", "ചോദിക്കുക", "തുടരുക", "ചിന്തിക്കുന്നു…", "പരിശീലകൻ മറുപടി നൽകാൻ വൈകുന്നു. ഒരു ചോദ്യം ചോദിക്കാം, അല്ലെങ്കിൽ തയ്യാറാകുമ്പോൾ തുടരാം.", "എപ്പോൾ വേണമെങ്കിലും മറ്റൊരു ചോദ്യം ചോദിക്കാം.", "ആദ്യ നിർദേശം", "മലയാളത്തിൽ മറുപടി നൽകുക.", "ബുദ്ധിമുട്ട്: {0} ({1})", "എളുപ്പം", "സാധാരണം", "കഠിനം", "സ്വയം", "മാനുവൽ", "വീണ്ടും തുടങ്ങുക", "ലളിതമായ വാക്കുകളും ചെറിയ വാക്യങ്ങളും ഉപയോഗിക്കുക." },
        { "Pelatih Gerakan AI", "Tanya Pelatih", "Bagaimana cara meningkatkan gerakan saya?", "Apa yang saya lakukan dengan baik?", "Ketik pertanyaan…", "Tanya", "Lanjutkan", "Sedang berpikir…", "Pelatih sedang lambat merespons. Coba bertanya atau lanjutkan saat siap.", "Kamu bisa bertanya lagi kapan saja.", "Saran awal", "Balas dalam bahasa Indonesia.", "Kesulitan: {0} ({1})", "Mudah", "Normal", "Sulit", "Otomatis", "Manual", "Ulangi tahap", "Gunakan kata sederhana dan kalimat pendek." },
        { "AI 动作教练", "询问教练", "如何改善我的动作？", "我哪些方面做得好？", "输入问题…", "提问", "继续", "正在思考…", "教练暂时没有回应。你可以再试一次，或准备好后继续。", "你可以随时继续提问。", "初始建议", "请用中文回答。", "难度：{0}（{1}）", "简单", "普通", "困难", "自动", "手动", "重新开始", "请使用简单词语和短句。" }
    };

    private readonly List<CoachTurn> history = new List<CoachTurn>();
    private CoachSummary summary;
    private Action continueAction;
    private bool requestInProgress;
    private Text titleText, replyText, statusText;
    private Button improveButton, wellButton, askButton, continueButton, restartButton;
    private Button easyButton, normalButton, hardButton, autoButton;
    private Text difficultyText;
    private InputField questionInput;
    private int language;
    private bool gestureMode;
    private bool showContinue = true;
    private string stageContext;

    public static AiCoachInteraction Show(Transform parent, string stage, float durationSeconds,
        int score, int starsCollected, string context, PlayerInput playerInput, Action onContinue,
        bool allowContinue = true)
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
            starsCollected = Mathf.Max(0, starsCollected)
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
        root.anchorMin = new Vector2(0.035f, 0.035f);
        root.anchorMax = new Vector2(0.965f, 0.965f);
        root.offsetMin = root.offsetMax = Vector2.zero;
        var card = root.gameObject.AddComponent<Image>();
        card.color = new Color(0.035f, 0.045f, 0.12f, 0.97f);

        titleText = AddText("Coach Title", root, Localized(0), 58, new Vector2(0.04f, 0.82f), new Vector2(0.96f, 0.98f), Color.white, FontStyle.Bold);
        replyText = AddText("Coach Reply", root, Localized(7), 42, new Vector2(0.07f, 0.63f), new Vector2(0.93f, 0.81f), new Color(0.94f, 0.96f, 1f, 1f), FontStyle.Bold);
        statusText = AddText("Coach Status", root, Localized(10), 28, new Vector2(0.05f, 0.59f), new Vector2(0.95f, 0.63f), new Color(0.82f, 0.87f, 1f, 1f), FontStyle.Bold);

        difficultyText = AddText("Difficulty Selection Label", root, string.Empty, 30,
            new Vector2(0.04f, 0.52f), new Vector2(0.96f, 0.58f), Color.white, FontStyle.Bold);
        easyButton = AddButton("Easy Difficulty", root, Localized(13), new Vector2(0.06f, 0.445f), new Vector2(0.27f, 0.515f));
        normalButton = AddButton("Normal Difficulty", root, Localized(14), new Vector2(0.30f, 0.445f), new Vector2(0.51f, 0.515f));
        hardButton = AddButton("Hard Difficulty", root, Localized(15), new Vector2(0.54f, 0.445f), new Vector2(0.75f, 0.515f));
        autoButton = AddButton("Automatic Difficulty", root, Localized(16), new Vector2(0.78f, 0.445f), new Vector2(0.96f, 0.515f));
        easyButton.onClick.AddListener(() => SelectManualDifficulty(0));
        normalButton.onClick.AddListener(() => SelectManualDifficulty(1));
        hardButton.onClick.AddListener(() => SelectManualDifficulty(2));
        autoButton.onClick.AddListener(SelectAutomaticDifficulty);
        RefreshDifficultyControls();

        improveButton = AddButton("Improve Prompt", root, Localized(2), new Vector2(0.08f, 0.35f), new Vector2(0.48f, 0.425f));
        wellButton = AddButton("Well Prompt", root, Localized(3), new Vector2(0.52f, 0.35f), new Vector2(0.92f, 0.425f));
        improveButton.onClick.AddListener(() => Ask(Localized(2)));
        wellButton.onClick.AddListener(() => Ask(Localized(3)));

        questionInput = AddInput(root, Localized(4));
        askButton = AddButton("Ask Button", root, Localized(5), new Vector2(0.58f, 0.105f), new Vector2(0.92f, 0.19f));
        askButton.onClick.AddListener(() => Ask(questionInput.text));
        restartButton = AddButton("Restart Button", root, Localized(18), new Vector2(0.08f, 0.02f), new Vector2(0.42f, 0.095f));
        restartButton.GetComponent<Image>().color = new Color(0.2f, 0.25f, 0.4f, 1f);
        restartButton.onClick.AddListener(RestartCurrentStage);
        continueButton = AddButton("Continue Button", root, Localized(6), new Vector2(0.58f, 0.02f), new Vector2(0.92f, 0.095f));
        continueButton.GetComponent<Image>().color = new Color(0.28f, 0.55f, 0.88f, 1f);
        continueButton.onClick.AddListener(Continue);
        continueButton.gameObject.SetActive(showContinue);
        statusText.text = Localized(9);
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
        if (difficultyText == null) return;
        string[] levels = { Localized(13), Localized(14), Localized(15) };
        difficultyText.text = string.Format(Localized(12), levels[GameDifficulty.Level],
            Localized(GameDifficulty.IsAutomatic ? 16 : 17));
        SetDifficultyButtonState(easyButton, !GameDifficulty.IsAutomatic && GameDifficulty.Level == 0);
        SetDifficultyButtonState(normalButton, !GameDifficulty.IsAutomatic && GameDifficulty.Level == 1);
        SetDifficultyButtonState(hardButton, !GameDifficulty.IsAutomatic && GameDifficulty.Level == 2);
        SetDifficultyButtonState(autoButton, GameDifficulty.IsAutomatic);
    }

    private static void SetDifficultyButtonState(Button button, bool selected)
    {
        if (button != null)
            button.GetComponent<Image>().color = selected
                ? new Color(0.2f, 0.55f, 0.38f, 1f)
                : new Color(0.16f, 0.28f, 0.53f, 1f);
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

    private Button AddButton(string name, Transform parent, string label, Vector2 min, Vector2 max)
    {
        Button button = RuntimeUiUtility.CreateButton(name, parent, label, 32,
            new Color(0.16f, 0.28f, 0.53f, 1f), Color.white, Vector2.zero);
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return button;
    }

    private InputField AddInput(Transform parent, string placeholder)
    {
        GameObject fieldObject = new GameObject("Question Input", typeof(RectTransform), typeof(Image), typeof(InputField));
        fieldObject.transform.SetParent(parent, false);
        RectTransform rect = fieldObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.08f, 0.22f);
        rect.anchorMax = new Vector2(0.92f, 0.32f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        fieldObject.GetComponent<Image>().color = new Color(0.12f, 0.15f, 0.24f, 1f);

        Text text = RuntimeUiUtility.CreateText("Text", fieldObject.transform, RuntimeUiUtility.GetFont(), 32,
            TextAnchor.MiddleLeft, Color.white, FontStyle.Bold);
        text.rectTransform.offsetMin = new Vector2(16f, 4f);
        text.rectTransform.offsetMax = new Vector2(-16f, -4f);
        Text hint = RuntimeUiUtility.CreateText("Placeholder", fieldObject.transform, text.font, 27,
            TextAnchor.MiddleLeft, new Color(0.78f, 0.81f, 0.88f, 1f), FontStyle.Bold);
        hint.text = placeholder;
        hint.rectTransform.offsetMin = new Vector2(16f, 4f);
        hint.rectTransform.offsetMax = new Vector2(-16f, -4f);
        var input = fieldObject.GetComponent<InputField>();
        input.textComponent = text;
        input.placeholder = hint;
        input.characterLimit = 300;
        input.lineType = InputField.LineType.SingleLine;
        return input;
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
                stageFocus = "Stage 1 is about moving, jumping and collecting the required falling stars.";
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
        string suffix = " " + Localized(11) + " " + Localized(19) + "\n" + focus;
        if (!string.IsNullOrWhiteSpace(stageContext)) suffix += "\nStage details: " + stageContext;
        int questionLimit = Mathf.Max(1, 500 - suffix.Length);
        if (question.Length > questionLimit) question = question.Substring(0, questionLimit);
        return question + suffix;
    }

    private IEnumerator RequestReply(string question, bool opening)
    {
        requestInProgress = true;
        SetAskControls(false);
        if (opening) replyText.text = Localized(7);
        statusText.text = Localized(7);
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
                    statusText.text = Localized(9);
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
        statusText.text = Localized(9);
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
        continueAction?.Invoke();
    }

    private void RestartCurrentStage()
    {
        if (requestInProgress) StopAllCoroutines();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
