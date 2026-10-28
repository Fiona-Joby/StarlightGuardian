using UnityEngine;
using UnityEngine.UI;

/// <summary>Creates a persistent-in-scene quit button for each playable stage.</summary>
public class StageQuitButton : MonoBehaviour
{
    private void Awake()
    {
        Canvas canvas = RuntimeUiUtility.CreateOverlayCanvas(
            "Quit Button Canvas", 300, new Vector2(1920f, 1080f));

        Button button = RuntimeUiUtility.CreateButton(
            "Quit Button", canvas.transform, "QUIT", 30,
            new Color(0.62f, 0.16f, 0.18f, 0.96f), Color.white,
            new Vector2(170f, 64f));
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-28f, -24f);
        rect.sizeDelta = new Vector2(170f, 64f);

        button.onClick.AddListener(QuitGame);

        RuntimeUiUtility.EnsureEventSystem();
    }

    private static void QuitGame()
    {
        Application.Quit();
    }
}
