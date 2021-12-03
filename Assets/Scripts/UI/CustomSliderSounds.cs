//
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
//
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.UI
{
    /// <summary>
    /// Component that plays sounds to communicate the state of a pinch slider.
    /// Customized by adding properties to update the fields at runtime.
    /// </summary>
    [RequireComponent(typeof(PinchSlider))]
    [AddComponentMenu("Scripts/MRTK/SDK/CustomSliderSounds")]
    public class CustomSliderSounds : MonoBehaviour
    {
        #region Serializable fields
        [Header("Audio Clips")]
        [SerializeField]
        [Tooltip("Sound to play when interaction with slider starts")]
        private AudioClip interactionStartSound = null;
        [SerializeField]
        [Tooltip("Sound to play when interaction with slider ends")]
        private AudioClip interactionEndSound = null;

        [Header("Tick Notch Sounds")]

        [SerializeField]
        [Tooltip("Whether to play 'tick tick' sounds as the slider passes notches")]
        private bool playTickSounds = true;

        [SerializeField]
        [Tooltip("Sound to play when slider passes a notch")]
        private AudioClip passNotchSound = null;

        [Range(0, 1)]
        [SerializeField]
        private float tickEvery = 0.1f;

        [SerializeField]
        private float startPitch = 0.75f;

        [SerializeField]
        private float endPitch = 1.25f;

        [SerializeField]
        private float minSecondsBetweenTicks = 0.01f;
        #endregion

        #region Properties
        public float MinSecondsBetweenTicks
        {
            get => minSecondsBetweenTicks;
            set => minSecondsBetweenTicks = value;
        }
        public float EndPitch
        {
            get => endPitch;
            set => endPitch = value;
        }
        public float StartPitch
        {
            get => startPitch;
            set => startPitch = value;
        }
        public float TickEvery
        {
            get => tickEvery;
            set => tickEvery = value;
        }
        public AudioClip PassNotchSound
        {
            get => passNotchSound;
            set => passNotchSound = value;
        }
        public AudioClip InteractionEndSound
        {
            get => interactionEndSound;
            set => interactionEndSound = value;
        }
        public AudioClip InteractionStartSound
        {
            get => interactionStartSound;
            set => interactionStartSound = value;
        }
        public bool PlayTickSounds
        {
            get => playTickSounds;
            set => playTickSounds = value;
        }

        #endregion

        #region Private members
        private PinchSlider slider;

        // Play sound when passing through slider notches
        private float accumulatedDeltaSliderValue = 0;
        private float lastSoundPlayTime;

        private AudioSource grabReleaseAudioSource = null;
        private AudioSource passNotchAudioSource = null;

        #endregion

        private void Start()
        {
            if (grabReleaseAudioSource == null)
            {
                grabReleaseAudioSource = gameObject.AddComponent<AudioSource>();
            }
            if (passNotchAudioSource == null)
            {
                passNotchAudioSource = gameObject.AddComponent<AudioSource>();
            }
            slider = GetComponent<PinchSlider>();
            slider.OnInteractionStarted.AddListener(OnInteractionStarted);
            slider.OnInteractionEnded.AddListener(OnInteractionEnded);
            slider.OnValueUpdated.AddListener(OnValueUpdated);
        }

        private void OnValueUpdated(SliderEventData eventData)
        {
            if (PlayTickSounds && passNotchAudioSource != null && PassNotchSound != null)
            {
                float delta = eventData.NewValue - eventData.OldValue;
                if (Mathf.Approximately(0.0f, delta))
                {
                    return;
                }
                accumulatedDeltaSliderValue += Mathf.Abs((float)delta);
                var now = Time.timeSinceLevelLoad;
                if (accumulatedDeltaSliderValue > TickEvery && now - lastSoundPlayTime > MinSecondsBetweenTicks)
                {
                    passNotchAudioSource.pitch = Mathf.Lerp(StartPitch, EndPitch, eventData.NewValue);
                    if (passNotchAudioSource.isActiveAndEnabled)
                    {
                        passNotchAudioSource.PlayOneShot(PassNotchSound);
                    }

                    accumulatedDeltaSliderValue = 0;
                    lastSoundPlayTime = now;
                }
            }
        }

        private void OnInteractionEnded(SliderEventData arg0)
        {
            if (InteractionEndSound != null && grabReleaseAudioSource != null && grabReleaseAudioSource.isActiveAndEnabled)
            {
                grabReleaseAudioSource.PlayOneShot(InteractionEndSound);
            }
        }

        private void OnInteractionStarted(SliderEventData arg0)
        {
            if (InteractionStartSound != null && grabReleaseAudioSource != null && grabReleaseAudioSource.isActiveAndEnabled)
            {
                grabReleaseAudioSource.PlayOneShot(InteractionStartSound);
            }
        }
    }


}