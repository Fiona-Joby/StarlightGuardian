using UnityEngine;

/// <summary>Resources-backed artwork used by the shared stage pause menu.</summary>
[CreateAssetMenu(menuName = "Starlight Guardian/Stage Pause Art")]
public sealed class StagePauseArt : ScriptableObject
{
    public Sprite pauseButton;
    public Sprite pauseTab;
    public Texture2D settingsOption;
    public Texture2D resumeOption;
    public Texture2D restartOption;
    public Texture2D quitOption;
}
