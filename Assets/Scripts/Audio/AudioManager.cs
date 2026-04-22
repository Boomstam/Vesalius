using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Central audio orchestrator. Owns references to all sound players and
/// exposes a clean API for play/stop and master volume control.
/// Lives on the Client build. NetworkedMonitor drives it via its SyncVar callbacks.
/// Tutorial is driven directly by client UI.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public enum AudioOverlayKind
    {
        None,
        NutritionSingle,
        GenerationSingle,
        HeartDual,
        TutorialDual,
    }

    public readonly struct AudioOverlayState
    {
        public AudioOverlayState(
            AudioOverlayKind kind,
            float primaryValue,
            float secondaryValue,
            string primaryMinLabel,
            string primaryMaxLabel,
            string secondaryMinLabel,
            string secondaryMaxLabel)
        {
            Kind = kind;
            PrimaryValue = primaryValue;
            SecondaryValue = secondaryValue;
            PrimaryMinLabel = primaryMinLabel;
            PrimaryMaxLabel = primaryMaxLabel;
            SecondaryMinLabel = secondaryMinLabel;
            SecondaryMaxLabel = secondaryMaxLabel;
        }

        public AudioOverlayKind Kind { get; }
        public float PrimaryValue { get; }
        public float SecondaryValue { get; }
        public string PrimaryMinLabel { get; }
        public string PrimaryMaxLabel { get; }
        public string SecondaryMinLabel { get; }
        public string SecondaryMaxLabel { get; }
    }

    [Header("Sound Players")]
    [SerializeField] private DoubleFader tutorialFader;
    [SerializeField] private DoubleFader organsOfNutritionFader;
    [SerializeField] private CirclePlayer organsOfGenerationPlayer;
    [SerializeField] private DelayPlayer heartPlayer;

    [Header("Master Volume")]
    [SerializeField] private AudioMixerGroup masterMixerGroup;
    [Tooltip("Duration of the FadeIn() coroutine in seconds.")]
    [SerializeField] private float fadeInTime = 2f;
    [Tooltip("Duration of the FadeOut() coroutine in seconds.")]
    [SerializeField] private float fadeOutTime = 2f;

    private const float MinVolume = 0.01f;

    private Coroutine masterFadeRoutine;

    private bool tutorialActive;
    private bool organsOfNutritionActive;
    private bool organsOfGenerationActive;
    private bool heartActive;

    private float nutritionFadeValue = 0.5f;
    private float generationFadeValue = 0.5f;
    private float heartBandFadeValue = 0.5f;
    private float heartDelayValue = 0.5f;

    public event Action<AudioOverlayState> OverlayStateChanged;

    public AudioOverlayState CurrentOverlayState => BuildOverlayState();

    private void Awake()
    {
        tutorialFader.SetBandFade(0.5f);
        organsOfNutritionFader.SetBandFade(nutritionFadeValue);
        organsOfGenerationPlayer.SetFadeValue(generationFadeValue);
        heartPlayer.SetBandFade(heartBandFadeValue);
        heartPlayer.SetDelayTimeNormalized(heartDelayValue);
    }

    public void PlayTutorial()
    {
        tutorialActive = true;
        tutorialFader.Play();
        NotifyOverlayStateChanged();
    }

    public void StopTutorial()
    {
        tutorialActive = false;
        tutorialFader.Stop();
        NotifyOverlayStateChanged();
    }

    public void PlayOrgansOfNutrition()
    {
        organsOfNutritionActive = true;
        organsOfNutritionFader.Play();
        organsOfNutritionFader.SetBandFade(nutritionFadeValue);
        NotifyOverlayStateChanged();
    }

    public void StopOrgansOfNutrition()
    {
        organsOfNutritionActive = false;
        organsOfNutritionFader.Stop();
        NotifyOverlayStateChanged();
    }

    public void PlayOrgansOfGeneration()
    {
        organsOfGenerationActive = true;
        organsOfGenerationPlayer.StartPlayback();
        organsOfGenerationPlayer.SetFadeValue(generationFadeValue);
        NotifyOverlayStateChanged();
    }

    public void StopOrgansOfGeneration()
    {
        organsOfGenerationActive = false;
        organsOfGenerationPlayer.StopPlayback();
        NotifyOverlayStateChanged();
    }

    public void PlayHeart()
    {
        heartActive = true;
        heartPlayer.StartPlayback();
        heartPlayer.SetBandFade(heartBandFadeValue);
        heartPlayer.SetDelayTimeNormalized(heartDelayValue);
        NotifyOverlayStateChanged();
    }

    public void StopHeart()
    {
        heartActive = false;
        heartPlayer.StopAllPlaybackAndRemoveSources();
        NotifyOverlayStateChanged();
    }

    public void StopAll()
    {
        tutorialActive = false;
        organsOfNutritionActive = false;
        organsOfGenerationActive = false;
        heartActive = false;

        tutorialFader.Stop();
        organsOfNutritionFader.Stop();
        organsOfGenerationPlayer.StopPlayback();
        heartPlayer.StopAllPlaybackAndRemoveSources();

        NotifyOverlayStateChanged();
    }

    public void SetOrgansOfNutritionFade(float value)
    {
        nutritionFadeValue = Mathf.Clamp01(value);
        organsOfNutritionFader.SetBandFade(nutritionFadeValue);
        NotifyOverlayStateChanged();
    }

    public void SetOrgansOfGenerationFade(float value)
    {
        generationFadeValue = Mathf.Clamp01(value);
        organsOfGenerationPlayer.SetFadeValue(generationFadeValue);
        NotifyOverlayStateChanged();
    }

    public void SetHeartBandFade(float value)
    {
        heartBandFadeValue = Mathf.Clamp01(value);
        heartPlayer.SetBandFade(heartBandFadeValue);
        NotifyOverlayStateChanged();
    }

    public void SetHeartDelay(float normalizedValue)
    {
        heartDelayValue = Mathf.Clamp01(normalizedValue);
        heartPlayer.SetDelayTimeNormalized(heartDelayValue);
        NotifyOverlayStateChanged();
    }

    /// <summary>Directly sets master volume. Intended for real-time slider control.</summary>
    public void SetMasterVolume(float volume)
    {
        StopMasterFade();
        ApplyMasterVolume(volume);
    }

    /// <summary>Animates master volume from current level to 1 over fadeInTime.</summary>
    public void FadeIn()
    {
        StartMasterFade(true);
    }

    /// <summary>Animates master volume from current level to silence over fadeOutTime.</summary>
    public void FadeOut()
    {
        StartMasterFade(false);
    }

    /// <summary>Immediately silences master without affecting the running fade coroutine.</summary>
    public void MuteImmediate()
    {
        StopMasterFade();
        ApplyMasterVolume(MinVolume);
    }

    /// <summary>Immediately restores master to full volume.</summary>
    public void ResetImmediate()
    {
        StopMasterFade();
        ApplyMasterVolume(1f);
    }

    private void StartMasterFade(bool fadeIn)
    {
        StopMasterFade();
        masterFadeRoutine = StartCoroutine(DoMasterFade(fadeIn));
    }

    private void StopMasterFade()
    {
        if (masterFadeRoutine == null) return;
        StopCoroutine(masterFadeRoutine);
        masterFadeRoutine = null;
    }

    private IEnumerator DoMasterFade(bool fadeIn)
    {
        float startTime = Time.time;
        float duration = fadeIn ? fadeInTime : fadeOutTime;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float volume = fadeIn ? t : 1f - t;
            ApplyMasterVolume(volume);

            elapsed = Time.time - startTime;
            yield return null;
        }

        ApplyMasterVolume(fadeIn ? 1f : MinVolume);
        masterFadeRoutine = null;
    }

    private void ApplyMasterVolume(float volume)
    {
        volume = Mathf.Clamp(volume, MinVolume, 1f);
        masterMixerGroup.audioMixer.SetFloat("Master", Mathf.Log(volume) * 20f);
    }

    private AudioOverlayState BuildOverlayState()
    {
        if (heartActive)
        {
            return new AudioOverlayState(
                AudioOverlayKind.HeartDual,
                heartBandFadeValue,
                heartDelayValue,
                "LOW",
                "HIGH",
                "SHORT",
                "LONG");
        }

        if (organsOfGenerationActive)
        {
            return new AudioOverlayState(
                AudioOverlayKind.GenerationSingle,
                generationFadeValue,
                0f,
                "LOW",
                "HIGH",
                string.Empty,
                string.Empty);
        }

        if (organsOfNutritionActive)
        {
            return new AudioOverlayState(
                AudioOverlayKind.NutritionSingle,
                nutritionFadeValue,
                0f,
                "LOW",
                "HIGH",
                string.Empty,
                string.Empty);
        }

        if (tutorialActive)
        {
            return new AudioOverlayState(
                AudioOverlayKind.TutorialDual,
                0.5f,
                0.5f,
                "LOW",
                "HIGH",
                "SOFT",
                "INTENSE");
        }

        return new AudioOverlayState(
            AudioOverlayKind.None,
            0f,
            0f,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty);
    }

    private void NotifyOverlayStateChanged()
    {
        OverlayStateChanged?.Invoke(CurrentOverlayState);
    }
}
