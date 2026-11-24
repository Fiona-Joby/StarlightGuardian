using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Plays the menu hover cue and the option activation cue.</summary>
public sealed class MenuOptionAudio : MonoBehaviour, IPointerEnterHandler
{
    private const float ButtonSoundVolume = 0.5f;

    private AudioSource audioSource;
    private AudioClip hoverClip;
    private AudioClip selectClip;

    public void Initialize(AudioSource source, AudioClip hover, AudioClip select)
    {
        audioSource = source;
        hoverClip = hover;
        selectClip = select;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Play(hoverClip);
    }

    public void PlaySelection()
    {
        Play(selectClip);
    }

    private void Play(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip, ButtonSoundVolume);
    }
}
