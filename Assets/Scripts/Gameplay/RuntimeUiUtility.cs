using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Shared setup helpers for runtime screen-space UI.</summary>
public static class RuntimeUiUtility
{
    public static Canvas CreateOverlayCanvas(string name, int sortingOrder, Vector2 referenceResolution)
    {
        GameObject canvasGO = new GameObject(name);
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = referenceResolution;
        canvasGO.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    public static RectTransform CreateRect(string name, Transform parent)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
        return rect;
    }

    public static Text CreateText(
        string name, Transform parent, Font font, int fontSize,
        TextAnchor alignment, Color color, FontStyle fontStyle = FontStyle.Normal)
    {
        RectTransform rect = CreateRect(name, parent);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Text text = rect.gameObject.AddComponent<Text>();
        text.font = font != null ? font : GetFont();
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = color;
        return text;
    }

    public static Button CreateButton(
        string name, Transform parent, string label, int fontSize,
        Color backgroundColor, Color textColor, Vector2 size)
    {
        GameObject buttonGO = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform rect = buttonGO.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.sizeDelta = size;

        Image image = buttonGO.GetComponent<Image>();
        image.color = backgroundColor;
        Button button = buttonGO.GetComponent<Button>();
        button.targetGraphic = image;

        Text buttonLabel = CreateText(
            "Label", buttonGO.transform, GetFont(), fontSize,
            TextAnchor.MiddleCenter, textColor, FontStyle.Bold);
        buttonLabel.text = label;
        return button;
    }

    public static Font GetFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 14);
        return font;
    }

    public static void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;

        GameObject eventSystemGO = new GameObject("EventSystem");
        eventSystemGO.AddComponent<EventSystem>();
        eventSystemGO.AddComponent<StandaloneInputModule>();
    }
}
