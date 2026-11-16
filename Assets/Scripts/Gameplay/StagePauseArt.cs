using UnityEngine;

/// <summary>Resources-backed artwork used by the shared stage pause menu.</summary>
[CreateAssetMenu(menuName = "Starlight Guardian/Stage Pause Art")]
public sealed class StagePauseArt : ScriptableObject
{
    [System.Serializable]
    public sealed class LocalizedArt
    {
        public Sprite pauseTab;
        public Sprite settingsTab;
        public Texture2D settingsOption;
        public Texture2D resumeOption;
        public Texture2D restartOption;
        public Texture2D quitOption;
    }

    public Sprite pauseButton;
    public Sprite taskButton;
    public Sprite controlsDropdownArrow;
    public Sprite sliderHandle;
    public Sprite pauseTab;
    public Texture2D settingsOption;
    public Texture2D resumeOption;
    public Texture2D restartOption;
    public Texture2D quitOption;
    // Language indices match MainMenuController: English, Malayalam, Indonesian, Chinese.
    public LocalizedArt[] localizedArt = new LocalizedArt[4];
}
