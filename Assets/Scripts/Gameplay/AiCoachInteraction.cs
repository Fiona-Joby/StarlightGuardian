using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

/// <summary>Paused, optional AI coach interaction shown at successful stage transitions.</summary>
public sealed class AiCoachInteraction : MonoBehaviour
{
    private const string Endpoint = "https://starlight-guardian.onrender.com/coach";
    private const int MaxFollowUps = 2;
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
        { "AI Movement Coach", "Ask Coach", "How can I improve my movement?", "What did I do well?", "Type a question…", "Ask", "Continue", "Thinking…", "The coach is taking a moment. Try a question, or continue when you’re ready.", "You can ask {0} more question(s).", "Opening tip", "Reply in English." },
        { "AI ചലന പരിശീലകൻ", "പരിശീലകനോട് ചോദിക്കുക", "എന്റെ ചലനം എങ്ങനെ മെച്ചപ്പെടുത്താം?", "ഞാൻ നന്നായി ചെയ്തത് എന്താണ്?", "ഒരു ചോദ്യം ടൈപ്പ് ചെയ്യൂ…", "ചോദിക്കുക", "തുടരുക", "ചിന്തിക്കുന്നു…", "പരിശീലകൻ മറുപടി നൽകാൻ വൈകുന്നു. ഒരു ചോദ്യം ചോദിക്കാം, അല്ലെങ്കിൽ തയ്യാറാകുമ്പോൾ തുടരാം.", "ഇനി {0} ചോദ്യം ചോദിക്കാം.", "ആദ്യ നിർദേശം", "മലയാളത്തിൽ മറുപടി നൽകുക." },
        { "Pelatih Gerakan AI", "Tanya Pelatih", "Bagaimana cara meningkatkan gerakan saya?", "Apa yang saya lakukan dengan baik?", "Ketik pertanyaan…", "Tanya", "Lanjutkan", "Sedang berpikir…", "Pelatih sedang lambat merespons. Coba bertanya atau lanjutkan saat siap.", "Kamu bisa bertanya {0} kali lagi.", "Saran awal", "Balas dalam bahasa Indonesia." },
        { "AI 动作教练", "询问教练", "如何改善我的动作？", "我哪些方面做得好？", "输入问题…", "提问", "继续", "正在思考…", "教练暂时没有回应。你可以再试一次，或准备好后继续。", "你还可以提问 {0} 次。", "初始建议", "请用中文回答。" }
    };

    private readonly List<CoachTurn> history = new List<CoachTurn>();
    private CoachSummary summary;
    private Action continueAction;
    private int followUps;
    private bool requestInProgress;
    private Text titleText, replyText, statusText;
    private Button improveButton, wellButton, askButton, continueButton;
    private InputField questionInput;
    private int language;

    public static AiCoachInteraction Show(Transform parent, string stage, float durationSeconds,
        int score, int starsCollected, string context, Action onContinue)
    {
        RuntimeUiUtility.EnsureEventSystem();
        var host = new GameObject("AI Coach Interaction", typeof(RectTransform));
        host.transform.SetParent(parent, false);
        var hostRect = host.GetComponent<RectTransform>();
        hostRect.anchorMin = Vector2.zero;
        hostRect.anchorMax = Vector2.one;
        hostRect.offsetMin = hostRect.offsetMax = Vector2.zero;
        var coach = host.AddComponent<AiCoachInteraction>();
        coach.summary = new CoachSummary
        {
            stage = stage,
            durationSeconds = Mathf.Clamp(durationSeconds, 0f, 3600f),
            score = Mathf.Max(0, score),
            starsCollected = Mathf.Max(0, starsCollected)
        };
        coach.continueAction = onContinue;
        coach.Build(context ?? string.Empty);
        coach.StartCoroutine(coach.RequestReply(coach.Localized(10) + ". " + coach.Localized(2) + " " + coach.Localized(11), true));
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
        replyText = AddText("Coach Reply", root, Localized(7), 42, new Vector2(0.07f, 0.59f), new Vector2(0.93f, 0.81f), new Color(0.94f, 0.96f, 1f, 1f), FontStyle.Bold);
        statusText = AddText("Coach Status", root, Localized(10), 28, new Vector2(0.05f, 0.54f), new Vector2(0.95f, 0.60f), new Color(0.82f, 0.87f, 1f, 1f), FontStyle.Bold);

        improveButton = AddButton("Improve Prompt", root, Localized(2), new Vector2(0.08f, 0.43f), new Vector2(0.48f, 0.53f));
        wellButton = AddButton("Well Prompt", root, Localized(3), new Vector2(0.52f, 0.43f), new Vector2(0.92f, 0.53f));
        improveButton.onClick.AddListener(() => Ask(Localized(2), context));
        wellButton.onClick.AddListener(() => Ask(Localized(3), context));

        questionInput = AddInput(root, Localized(4));
        askButton = AddButton("Ask Button", root, Localized(5), new Vector2(0.58f, 0.13f), new Vector2(0.92f, 0.22f));
        askButton.onClick.AddListener(() => Ask(questionInput.text, context));
        continueButton = AddButton("Continue Button", root, Localized(6), new Vector2(0.32f, 0.025f), new Vector2(0.68f, 0.12f));
        continueButton.GetComponent<Image>().color = new Color(0.28f, 0.55f, 0.88f, 1f);
        continueButton.onClick.AddListener(Continue);
        statusText.text = string.Format(Localized(9), MaxFollowUps);
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
        rect.anchorMin = new Vector2(0.08f, 0.25f);
        rect.anchorMax = new Vector2(0.92f, 0.38f);
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

    private void Ask(string question, string context)
    {
        if (requestInProgress || followUps >= MaxFollowUps || string.IsNullOrWhiteSpace(question)) return;
        followUps++;
        string contextQuestion = question.Trim() + " " + Localized(11);
        if (!string.IsNullOrWhiteSpace(context)) contextQuestion += "\nStage details: " + context;
        StartCoroutine(RequestReply(contextQuestion, false));
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
                    statusText.text = string.Format(Localized(9), Mathf.Max(0, MaxFollowUps - followUps));
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
        SetAskControls(followUps < MaxFollowUps);
    }

    private void ShowFallback()
    {
        replyText.text = Localized(8);
        statusText.text = string.Format(Localized(9), Mathf.Max(0, MaxFollowUps - followUps));
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
}
