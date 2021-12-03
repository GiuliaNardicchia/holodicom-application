using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SliceSlider))]
[RequireComponent(typeof(AudioSource))]
public class SliceSliderSoundManager : MonoBehaviour
{

    #region Serializable Fields

    [Header("Audio Clips")]
    [SerializeField]
    [Tooltip("Sound to play when interaction with slider starts")]
    private AudioClip interactionStartSound = null;

    [SerializeField]
    [Tooltip("Sound to play when interaction with slider ends")]
    private AudioClip interactionEndSound = null;

    [SerializeField]
    [Tooltip("Sound to play when slider passes a notch")]
    private AudioClip passNotchSound = null;

    #endregion

    #region Private Fields

    private SliceSlider slider;
    private AudioSource audioSource;

    #endregion

    #region Event Handlers

    private void OnIndexUpdate(SliceSlider.EventData eventData)
    {
        PlayAudioClip(audioSource, passNotchSound);
    }

    private void OnInteractionStart(SliceSlider.EventData eventData)
    {
        PlayAudioClip(audioSource, interactionStartSound);
    }

    private void OnInteractionEnd(SliceSlider.EventData eventData)
    {
        PlayAudioClip(audioSource, interactionEndSound);
    }

    private void PlayAudioClip(AudioSource audioSource, AudioClip clip)
    {
        if (audioSource.isActiveAndEnabled && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    #endregion

    void Start()
    {
        slider = GetComponent<SliceSlider>();
        audioSource = GetComponent<AudioSource>();
        slider.OnIndexChanged.AddListener(OnIndexUpdate);
        slider.OnInteractionStarted.AddListener(OnInteractionStart);
        slider.OnInteractionEnded.AddListener(OnInteractionEnd);
    }

}
