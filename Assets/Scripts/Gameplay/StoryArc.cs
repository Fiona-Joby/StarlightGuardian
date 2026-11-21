using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Localized between-stage story panels and their scene transitions.</summary>
public sealed class StoryArc : MonoBehaviour
{
    private static readonly string[][] Lines =
    {
        new[] {
            "Once, a great constellation sustained the city, powering its systems and protecting its people.",
            "But a mysterious Black Storm swept across the skies, shattering the constellation. Darkness spread, the city's systems fell into ruin, and its guardians turned hostile.",
            "With the city slowly losing its light, one little guardian sets out to restore the stars.",
            "The constellation... It's broken.", "I have to restore it. There must still be some stars left.",
            "The gathered stars merge into a tiny Star Spirit, its light shining faintly.", "A little star... Are you coming with me?",
            "The spirit follows the guardian, but its light alone is not enough to restore the constellation. They must venture deeper into the city.",
            "The residential district has fallen silent. The Black Storm has corrupted its defense turrets, while strange creatures roam the streets.",
            "The guardian must protect the Star Spirit and clear the path to the city core.", "Stay close. I'll protect you.",
            "With the path cleared, the guardian ventures deeper into the city, where the storm's corruption grows stronger.",
            "At the city core, the Black Storm's influence is strongest. The Star Spirit senses another source of light nearby.", "I can feel it... We're close.",
            "The barrier surrounding a chest disappears. Inside lies another Star Spirit, its light surviving the catastrophe.",
            "The two spirits draw together, resonating with one another.", "Another Star Spirit...!",
            "The two Star Spirits unite, releasing a wave of energy that reconnects the shattered constellation.",
            "The city's systems awaken. Its lights return, and the Black Storm slowly fades.", "The homeland is saved.", "From the smallest light, a new beginning is born."
        },
        new[] {
            "ഒരിക്കൽ, ഒരു മഹത്തായ നക്ഷത്രരാശി നഗരത്തെ നിലനിർത്തി; അതിന്റെ സംവിധാനങ്ങൾക്ക് ഊർജം നൽകി ജനങ്ങളെ സംരക്ഷിച്ചു.",
            "എന്നാൽ ഒരു നിഗൂഢമായ കറുത്ത കൊടുങ്കാറ്റ് ആകാശം കീറി നക്ഷത്രരാശിയെ തകർത്തു. ഇരുട്ട് പടർന്നു, നഗരത്തിന്റെ സംവിധാനങ്ങൾ തകർന്നു, കാവൽക്കാർ ശത്രുക്കളായി.",
            "നഗരത്തിന്റെ വെളിച്ചം മങ്ങുമ്പോൾ, നക്ഷത്രങ്ങളെ വീണ്ടെടുക്കാൻ ഒരു ചെറിയ കാവൽക്കാരൻ പുറപ്പെടുന്നു.",
            "നക്ഷത്രരാശി... തകർന്നിരിക്കുന്നു.", "ഞാൻ അത് പുനഃസ്ഥാപിക്കണം. കുറച്ച് നക്ഷത്രങ്ങളെങ്കിലും ബാക്കിയുണ്ടാകണം.",
            "ശേഖരിച്ച നക്ഷത്രങ്ങൾ മങ്ങിയ വെളിച്ചമുള്ള ഒരു ചെറിയ നക്ഷത്രാത്മാവായി ഒന്നിക്കുന്നു.", "ഒരു കുഞ്ഞു നക്ഷത്രം... നീ എന്റെ കൂടെ വരുന്നോ?",
            "ആത്മാവ് കാവൽക്കാരനെ പിന്തുടരുന്നു. എന്നാൽ അതിന്റെ വെളിച്ചം മാത്രം നക്ഷത്രരാശി വീണ്ടെടുക്കാൻ പോരാ. അവർ നഗരത്തിന്റെ ഉള്ളിലേക്ക് പോകണം.",
            "വാസമേഖല നിശ്ശബ്ദമാണ്. കറുത്ത കൊടുങ്കാറ്റ് പ്രതിരോധ ടററ്റുകളെ മലിനമാക്കി; വിചിത്ര ജീവികൾ തെരുവുകളിൽ അലഞ്ഞുനടക്കുന്നു.",
            "നക്ഷത്രാത്മാവിനെ സംരക്ഷിച്ച് നഗരകേന്ദ്രത്തിലേക്കുള്ള വഴി തെളിക്കണം.", "അടുത്തുതന്നെ നിൽക്കൂ. ഞാൻ നിന്നെ സംരക്ഷിക്കാം.",
            "വഴി തെളിഞ്ഞതോടെ കാവൽക്കാരൻ നഗരത്തിന്റെ ഉള്ളിലേക്ക് നീങ്ങുന്നു; കൊടുങ്കാറ്റിന്റെ മലിനീകരണം കൂടുതൽ ശക്തമാകുന്നു.",
            "നഗരകേന്ദ്രത്തിൽ കറുത്ത കൊടുങ്കാറ്റിന്റെ സ്വാധീനം ഏറ്റവും ശക്തമാണ്. സമീപത്ത് മറ്റൊരു പ്രകാശസ്രോതസ്സ് നക്ഷത്രാത്മാവ് തിരിച്ചറിയുന്നു.", "എനിക്ക് അനുഭവപ്പെടുന്നു... നമ്മൾ അടുത്തെത്തി.",
            "പെട്ടിയെ ചുറ്റിയിരുന്ന തടസ്സം അപ്രത്യക്ഷമാകുന്നു. അകത്ത് ദുരന്തത്തെ അതിജീവിച്ച മറ്റൊരു നക്ഷത്രാത്മാവുണ്ട്.",
            "രണ്ട് ആത്മാക്കളും അടുത്തേക്ക് വന്ന് പരസ്പരം പ്രകമ്പനം കൊള്ളുന്നു.", "മറ്റൊരു നക്ഷത്രാത്മാവ്...!",
            "രണ്ട് നക്ഷത്രാത്മാക്കൾ ഒന്നിച്ച് ഊർജതരംഗം പുറപ്പെടുവിച്ച് തകർന്ന നക്ഷത്രരാശിയെ വീണ്ടും ബന്ധിപ്പിക്കുന്നു.",
            "നഗരത്തിന്റെ സംവിധാനങ്ങൾ ഉണരുന്നു. വിളക്കുകൾ തെളിയുന്നു, കറുത്ത കൊടുങ്കാറ്റ് പതുക്കെ മാഞ്ഞുപോകുന്നു.", "നമ്മുടെ നാട് രക്ഷപ്പെട്ടു.", "ഏറ്റവും ചെറിയ വെളിച്ചത്തിൽ നിന്ന് പുതിയ തുടക്കം പിറക്കുന്നു."
        },
        new[] {
            "Dahulu, sebuah rasi bintang agung menopang kota, memberi daya pada sistemnya dan melindungi penduduknya.",
            "Namun Badai Hitam misterius menyapu langit dan menghancurkan rasi itu. Kegelapan menyebar, sistem kota rusak, dan para penjaganya menjadi ganas.",
            "Saat cahaya kota perlahan padam, seorang penjaga kecil berangkat untuk memulihkan bintang-bintang.",
            "Rasi bintangnya... hancur.", "Aku harus memulihkannya. Pasti masih ada bintang yang tersisa.",
            "Bintang-bintang yang terkumpul menyatu menjadi Roh Bintang kecil dengan cahaya redup.", "Bintang kecil... mau ikut denganku?",
            "Roh itu mengikuti sang penjaga, tetapi cahayanya saja tak cukup untuk memulihkan rasi bintang. Mereka harus menjelajah lebih jauh ke dalam kota.",
            "Distrik permukiman telah sunyi. Badai Hitam merusak menara pertahanan, sementara makhluk aneh berkeliaran di jalanan.",
            "Penjaga harus melindungi Roh Bintang dan membuka jalan menuju pusat kota.", "Tetap dekat. Aku akan melindungimu.",
            "Setelah jalan terbuka, penjaga melangkah lebih jauh ke kota, tempat korupsi badai semakin kuat.",
            "Di pusat kota, pengaruh Badai Hitam paling kuat. Roh Bintang merasakan sumber cahaya lain di dekat sini.", "Aku bisa merasakannya... Kita sudah dekat.",
            "Penghalang di sekitar peti menghilang. Di dalamnya ada Roh Bintang lain yang selamat dari bencana.",
            "Kedua roh mendekat dan beresonansi satu sama lain.", "Roh Bintang lainnya...!",
            "Kedua Roh Bintang bersatu, melepaskan energi yang menghubungkan kembali rasi bintang yang hancur.",
            "Sistem kota menyala kembali. Lampu-lampunya hidup dan Badai Hitam perlahan menghilang.", "Tanah air terselamatkan.", "Dari cahaya terkecil, awal yang baru terlahir."
        },
        new[] {
            "從前，一個偉大的星座支撐著城市，為城市系統提供能量並保護居民。",
            "然而，神秘的黑色風暴席捲天空，粉碎了星座。黑暗蔓延，城市系統陷入廢墟，守護者也變得充滿敵意。",
            "城市的光芒逐漸消失，一位小小守護者踏上旅程，決心恢復星辰。",
            "星座……破碎了。", "我一定要修復它。一定還有星星留下。",
            "收集到的星星合而為一，成為一個光芒微弱的小星靈。", "一顆小星星……你願意跟我走嗎？",
            "星靈跟著守護者，但僅憑它的光芒不足以恢復星座。他們必須深入城市。",
            "住宅區一片死寂。黑色風暴腐化了防禦砲塔，奇怪的生物在街道上遊蕩。",
            "守護者必須保護星靈，清出通往城市核心的道路。", "跟緊我。我會保護你。",
            "道路清除後，守護者繼續深入城市，風暴的腐化也越發強烈。",
            "黑色風暴在城市核心的影響最強。星靈感應到附近有另一道光源。", "我感覺到了……我們快到了。",
            "包圍寶箱的屏障消失了。裡面有另一個星靈，牠在災難中倖存下來。",
            "兩個星靈彼此靠近，互相共鳴。", "又一個星靈……！",
            "兩個星靈合而為一，釋放能量波，重新連結破碎的星座。",
            "城市系統重新啟動。燈火再次亮起，黑色風暴也逐漸消散。", "我們的家園得救了。", "最微小的光芒，也能孕育新的開始。"
        }
    };

    private static readonly int[] Stage2 = { 8, 9, 10 };
    private static readonly int[] Stage3 = { 12, 13 };
    private static StoryArc instance;
    private Canvas canvas;
    private Image backdrop, panel;
    private RawImage stageBackground;
    private Text narration, dialogue;
    private Button next;
    private bool advance;
    private int language;
    private Sprite spirit1, spirit2;
    private Sprite storyBoxSprite, dialogueBoxSprite;
    private Sprite continueArrowSprite;
    private Texture2D stage1Background, stage2Background, stage3Background;

    public static StoryArc Ensure()
    {
        if (instance != null) return instance;
        var go = new GameObject("Story Arc");
        instance = go.AddComponent<StoryArc>();
        DontDestroyOnLoad(go);
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        language = Mathf.Clamp(PlayerPrefs.GetInt("Language", 0), 0, 3);
        spirit1 = LoadSprite("StorySpirit1");
        spirit2 = LoadSprite("StorySpirit2");
        storyBoxSprite = LoadBoxSprite("StoryBox", .05f, .065f, .90f, .335f);
        dialogueBoxSprite = LoadBoxSprite("DialogueBox", .06f, .065f, .88f, .335f);
        continueArrowSprite = LoadBoxSprite("StoryContinueArrow", 764f / 1920f, (1080f - 677f) / 1080f, 372f / 1920f, 356f / 1080f);
        stage1Background = Resources.Load<Texture2D>("StoryBackgroundStage1");
        stage2Background = Resources.Load<Texture2D>("StoryBackgroundStage2");
        stage3Background = Resources.Load<Texture2D>("StoryBackgroundStage3");
    }

    public static void BeginStage(string scene)
    {
        var arc = Ensure();
        if (scene == "MainScene") arc.StartCoroutine(arc.PlayIntro());
    }

    public static IEnumerator PlayStage2Entry() { var arc = Ensure(); yield return arc.PlayStage2EntryRoutine(); }
    public static IEnumerator PlayStage3Entry() { var arc = Ensure(); yield return arc.PlayStage3EntryRoutine(); }

    public static void AfterStage1(Action done) => Ensure().StartCoroutine(Ensure().Stage1Transition(done));
    public static void AfterStage2(Action done) => Ensure().StartCoroutine(Ensure().Stage2Transition(done));
    public static void ShowStage3FoundSpirit(Action done) => Ensure().StartCoroutine(Ensure().FoundSpirit(done));
    public static void ShowEnding(Action done) => Ensure().StartCoroutine(Ensure().Ending(done));

    private static Sprite LoadSprite(string resource) { Texture2D texture = Resources.Load<Texture2D>(resource); return texture != null ? Sprite.Create(texture, new Rect(0,0,texture.width,texture.height), new Vector2(.5f,.5f), 100f) : null; }
    private static Sprite LoadBoxSprite(string resource, float x, float y, float width, float height)
    {
        Texture2D texture = Resources.Load<Texture2D>(resource);
        if (texture == null) return null;
        return Sprite.Create(texture, new Rect(x * texture.width, y * texture.height,
            width * texture.width, height * texture.height), new Vector2(.5f,.5f), 100f);
    }

    private IEnumerator PlayIntro()
    {
        Time.timeScale = 0f;
        var saved = HideActorsAndPlatforms();
        yield return Present(new[] { 0, 1, 2 }, false, true, false, null);
        yield return Present(new[] { 3, 4 }, true, false, false, null);
        Restore(saved);
        Time.timeScale = 1f;
        StageQuitButton pauseUi = FindAnyObjectByType<StageQuitButton>();
        if (pauseUi != null) pauseUi.ShowStage1InstructionsAfterStory();
    }

    private IEnumerator PlayStage2EntryRoutine() { yield return new WaitForSecondsRealtime(0.1f); Time.timeScale = 0f; var saved = HideActorsAndPlatforms(); yield return Present(new[] { 8, 9 }, false, false, false, null); yield return Present(new[] { 10 }, true, false, false, null); Restore(saved); Time.timeScale = 1f; }
    private IEnumerator PlayStage3EntryRoutine() { yield return new WaitForSecondsRealtime(0.1f); Time.timeScale = 0f; var saved = HideActorsAndPlatforms(); yield return Present(new[] { 12 }, false, false, false, null); yield return Present(new[] { 13 }, true, false, false, null); Restore(saved); Time.timeScale = 1f; }
    private IEnumerator Stage1Transition(Action done)
    {
        Time.timeScale = 0f;
        yield return Present(new[] { 5 }, false, true, true, null);
        yield return Present(new[] { 6 }, true, false, true, null);
        yield return SpiritFlight();
        yield return Present(new[] { 7 }, false, true, false, null);
        Time.timeScale = 1f;
        done?.Invoke();
    }
    private IEnumerator Stage2Transition(Action done) { Time.timeScale = 0f; yield return Present(new[] { 11 }, false, true, false, null); Time.timeScale = 1f; done?.Invoke(); }
    private IEnumerator FoundSpirit(Action done) { Time.timeScale = 0f; yield return Present(new[] { 14, 15 }, false, false, true, null); yield return Present(new[] { 16 }, true, false, true, null); Time.timeScale = 1f; done?.Invoke(); }
    private IEnumerator Ending(Action done)
    {
        yield return Present(new[] { 17, 18, 19, 20 }, false, false, true, null);
        done?.Invoke();
    }

    private IEnumerator Present(int[] keys, bool isDialogue, bool black, bool spirits, Action completed)
    {
        Build();
        backdrop.color = black ? Color.black : new Color(0, 0, 0, 0);
        Texture2D background = black ? null : GetStageBackground();
        stageBackground.texture = background;
        stageBackground.uvRect = GetCoverUv(background);
        stageBackground.gameObject.SetActive(background != null);
        panel.sprite = isDialogue ? dialogueBoxSprite : storyBoxSprite;
        RectTransform panelRect = panel.rectTransform;
        panelRect.anchorMin = isDialogue ? new Vector2(.06f, .04f) : new Vector2(.04f, .04f);
        panelRect.anchorMax = isDialogue ? new Vector2(.94f, .37f) : new Vector2(.96f, .37f);
        panel.gameObject.SetActive(true); narration.gameObject.SetActive(!isDialogue); dialogue.gameObject.SetActive(isDialogue);
        for (int i = 0; i < keys.Length; i++)
        {
            string value = Lines[language][keys[i]];
            Text target = isDialogue ? dialogue : narration;
            target.text = value;
            next.gameObject.SetActive(true); advance = false;
            if (spirits && spirit1 != null) ShowSpirit(spirit1); else HideSpirits();
            Vector2 spiritAnchor = spiritImage != null ? spiritImage.rectTransform.anchorMin : Vector2.zero;
            while (!advance)
            {
                if (spirits && spiritImage != null && spiritImage.gameObject.activeSelf)
                {
                    float bob = Mathf.Sin(Time.unscaledTime * 2.2f) * 0.018f;
                    spiritImage.rectTransform.anchorMin = spiritImage.rectTransform.anchorMax = spiritAnchor + Vector2.up * bob;
                    if (spirit2 != null) spiritImage.sprite = Mathf.Sin(Time.unscaledTime * 4f) > 0f ? spirit1 : spirit2;
                }
                yield return null;
            }
        }
        panel.gameObject.SetActive(false);
        narration.gameObject.SetActive(false);
        dialogue.gameObject.SetActive(false);
        next.gameObject.SetActive(false);
        stageBackground.gameObject.SetActive(false);
        backdrop.color = new Color(0, 0, 0, 0);
        HideSpirits(); completed?.Invoke();
    }

    private Texture2D GetStageBackground()
    {
        switch (SceneManager.GetActiveScene().name)
        {
            case "Stage2": return stage2Background;
            case "Stage3": return stage3Background;
            default: return stage1Background;
        }
    }

    private void Build()
    {
        if (canvas != null) return;
        language = Mathf.Clamp(PlayerPrefs.GetInt("Language", 0), 0, 3);
        canvas = RuntimeUiUtility.CreateOverlayCanvas("Story Overlay", 900, new Vector2(1920, 1080));
        canvas.transform.SetParent(transform, false);
        backdrop = RuntimeUiUtility.CreateRect("Story Backdrop", canvas.transform).gameObject.AddComponent<Image>();
        RectTransform br = backdrop.rectTransform; br.anchorMin = Vector2.zero; br.anchorMax = Vector2.one; br.offsetMin = br.offsetMax = Vector2.zero; backdrop.color = Color.black;
        backdrop.raycastTarget = false;
        stageBackground = RuntimeUiUtility.CreateRect("Story Stage Background", canvas.transform).gameObject.AddComponent<RawImage>();
        RectTransform sbr = stageBackground.rectTransform; sbr.anchorMin = Vector2.zero; sbr.anchorMax = Vector2.one; sbr.offsetMin = sbr.offsetMax = Vector2.zero;
        stageBackground.raycastTarget = false;
        panel = RuntimeUiUtility.CreateRect("Story Box", canvas.transform).gameObject.AddComponent<Image>();
        RectTransform pr = panel.rectTransform; pr.anchorMin = new Vector2(.04f, .04f); pr.anchorMax = new Vector2(.96f, .37f); pr.offsetMin = pr.offsetMax = Vector2.zero; panel.preserveAspect = true; panel.raycastTarget = false;
        narration = MakeText("Narration", new Vector2(.09f, .10f), new Vector2(.81f, .31f), 48, TextAnchor.MiddleCenter);
        // DialogueBox.png includes the guardian portrait, name, and divider on the left.
        // Keep the spoken line in the clear area beneath the divider on its right.
        dialogue = MakeText("Guardian Dialogue", new Vector2(.34f, .09f), new Vector2(.81f, .25f), 50, TextAnchor.MiddleLeft);
        GameObject arrowObject = new GameObject("Continue Story Arrow", typeof(RectTransform), typeof(Image), typeof(Button));
        arrowObject.transform.SetParent(canvas.transform, false);
        Image arrowImage = arrowObject.GetComponent<Image>();
        arrowImage.sprite = continueArrowSprite;
        arrowImage.preserveAspect = true;
        arrowImage.color = Color.white;
        next = arrowObject.GetComponent<Button>();
        next.targetGraphic = arrowImage;
        next.transition = Selectable.Transition.None;
        RectTransform nr = next.GetComponent<RectTransform>();
        nr.anchorMin = nr.anchorMax = new Vector2(.93f,.07f); nr.pivot = new Vector2(1,0); nr.sizeDelta = new Vector2(76,76);
        next.onClick.AddListener(() => advance = true);
    }

    private Text MakeText(string name, Vector2 min, Vector2 max, int size, TextAnchor align)
    {
        Text t = RuntimeUiUtility.CreateText(name, canvas.transform, RuntimeUiUtility.GetFont(), language == 1 ? size - 6 : language == 3 ? size + 2 : size, align, Color.white, FontStyle.Bold);
        t.supportRichText = false; t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Truncate;
        t.rectTransform.anchorMin = min; t.rectTransform.anchorMax = max; t.rectTransform.offsetMin = t.rectTransform.offsetMax = Vector2.zero;
        string font = language == 1 ? "Nirmala UI" : language == 3 ? "Microsoft YaHei" : "Arial";
        Font f = Font.CreateDynamicFontFromOSFont(font, t.fontSize); if (f != null) t.font = f;
        Outline outline = t.gameObject.AddComponent<Outline>(); outline.effectColor = new Color(.02f,.02f,.08f,.95f); outline.effectDistance = new Vector2(2,-2); return t;
    }

    private Image spiritImage;
    private void ShowSpirit(Sprite sprite)
    {
        if (spiritImage == null)
        {
            spiritImage = RuntimeUiUtility.CreateRect("Story Star Spirit", canvas.transform).gameObject.AddComponent<Image>();
            RectTransform r = spiritImage.rectTransform; r.anchorMin = r.anchorMax = new Vector2(.5f,.67f); r.sizeDelta = new Vector2(220,220); spiritImage.preserveAspect = true;
        }
        spiritImage.rectTransform.anchorMin = spiritImage.rectTransform.anchorMax = new Vector2(.5f,.67f);
        spiritImage.sprite = sprite; spiritImage.gameObject.SetActive(true);
    }
    private void HideSpirits() { if (spiritImage != null) spiritImage.gameObject.SetActive(false); }
    private System.Collections.Generic.List<SpriteRenderer> HideActorsAndPlatforms()
    {
        var hidden = new System.Collections.Generic.List<SpriteRenderer>();
        foreach (SpriteRenderer renderer in FindObjectsByType<SpriteRenderer>())
        {
            string n = renderer.transform.root.name.ToLowerInvariant();
            string own = renderer.gameObject.name.ToLowerInvariant();
            if (renderer.GetComponentInParent<PlayerMovement>() != null || n.Contains("platform") || n.Contains("ground") || own.Contains("platform"))
            { renderer.enabled = false; hidden.Add(renderer); }
        }
        return hidden;
    }
    private static void Restore(System.Collections.Generic.List<SpriteRenderer> renderers) { foreach (var renderer in renderers) if (renderer != null) renderer.enabled = true; }

    private static Rect GetCoverUv(Texture2D texture)
    {
        if (texture == null || Screen.width <= 0 || Screen.height <= 0) return new Rect(0,0,1,1);
        float imageAspect = (float)texture.width / texture.height;
        float screenAspect = (float)Screen.width / Screen.height;
        if (screenAspect > imageAspect)
        {
            float visibleHeight = imageAspect / screenAspect;
            return new Rect(0f, (1f - visibleHeight) * 0.5f, 1f, visibleHeight);
        }
        float visibleWidth = screenAspect / imageAspect;
        return new Rect((1f - visibleWidth) * 0.5f, 0f, visibleWidth, 1f);
    }
    private IEnumerator SpiritFlight()
    {
        if (spiritImage == null) yield break;
        RectTransform r = spiritImage.rectTransform; Vector2 start = new Vector2(.5f,.67f), end = new Vector2(.16f,.15f); float elapsed = 0;
        while (elapsed < 1.5f) { elapsed += Time.unscaledDeltaTime; r.anchorMin = r.anchorMax = Vector2.Lerp(start,end,Mathf.Clamp01(elapsed/1.5f)); yield return null; }
        HideSpirits();
    }
}
