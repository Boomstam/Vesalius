using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Serialization;

/// <summary>
/// Central audio orchestrator. Owns references to all sound players and
/// exposes a clean API for play/stop and master volume control.
/// Lives on the Client build. NetworkedMonitor drives it via SyncVar callbacks.
/// Tutorial is driven directly by client UI.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public enum AudioOverlayKind
    {
        None,
        IntroSingle,
        PingPongSingle,
        GroupPingPongSingle,
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
    [FormerlySerializedAs("introFader")]
    [FormerlySerializedAs("organsOfNutritionFader")]
    [SerializeField] private IntervalPlayer introPlayer;
    [SerializeField] private DoubleFader pingPongFader;
    [SerializeField] private GroupPingPongPlayer groupPingPongPlayer;
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
    private bool introActive;
    private bool pingPongActive;
    private bool groupPingPongActive;
    private bool organsOfGenerationActive;
    private bool heartActive;

    private float introFadeValue = 0.5f;
    private float pingPongFadeValue = 0.5f;
    private float generationFadeValue = 0.5f;
    private float heartBandFadeValue = 0.5f;
    private float heartDelayValue = 0.5f;

    public event Action<AudioOverlayState> OverlayStateChanged;

    public AudioOverlayState CurrentOverlayState => BuildOverlayState();

    private void Awake()
    {
        if (tutorialFader != null)
            tutorialFader.SetBandFade(0.5f);

        if (introPlayer != null)
            introPlayer.SetBandFade(introFadeValue);

        if (pingPongFader != null)
            pingPongFader.SetBandFade(pingPongFadeValue);

        EnsureGroupPingPongPlayer();

        if (organsOfGenerationPlayer != null)
            organsOfGenerationPlayer.SetFadeValue(generationFadeValue);

        if (heartPlayer != null)
        {
            heartPlayer.SetBandFade(heartBandFadeValue);
            heartPlayer.SetDelayTimeNormalized(heartDelayValue);
        }
    }

    public void PlayTutorial()
    {
        StopAllSilent();
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

    public void PlayIntro()
    {
        StopAllSilent();
        introActive = true;
        if (introPlayer != null)
        {
            introPlayer.Play();
            introPlayer.SetBandFade(introFadeValue);
        }

        NotifyOverlayStateChanged();
    }

    public void StopIntro()
    {
        introActive = false;
        if (introPlayer != null)
            introPlayer.Stop();

        NotifyOverlayStateChanged();
    }

    public void PlayPingPong()
    {
        StopAllSilent();
        pingPongActive = true;
        pingPongFader.Play();
        pingPongFader.SetBandFade(pingPongFadeValue);
        NotifyOverlayStateChanged();
    }

    public void StopPingPong()
    {
        pingPongActive = false;
        pingPongFader.Stop();
        NotifyOverlayStateChanged();
    }

    public void PlayGroupPingPong(int groupIndex)
    {
        StopAllSilent();
        groupPingPongActive = true;

        EnsureGroupPingPongPlayer();
        if (pingPongFader != null)
            pingPongFader.SetBandFade(pingPongFadeValue);

        if (groupPingPongPlayer != null)
            groupPingPongPlayer.Play(groupIndex);

        NotifyOverlayStateChanged();
    }

    public void StopGroupPingPong()
    {
        groupPingPongActive = false;

        if (groupPingPongPlayer != null)
            groupPingPongPlayer.Stop();

        NotifyOverlayStateChanged();
    }

    public void PlayOrgansOfGeneration()
    {
        StopAllSilent();
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
        StopAllSilent();
        heartActive = true;
        if (heartPlayer != null)
        {
            heartPlayer.StartPlayback();
            heartPlayer.SetBandFade(heartBandFadeValue);
            heartPlayer.SetDelayTimeNormalized(heartDelayValue);
        }
        NotifyOverlayStateChanged();
    }

    public void EnableHeartMode()
    {
        heartActive = true;

        if (heartPlayer != null)
        {
            heartPlayer.SetBandFade(heartBandFadeValue);
            heartPlayer.SetDelayTimeNormalized(heartDelayValue);
        }

        NotifyOverlayStateChanged();
    }

    public void StopHeartPlayback()
    {
        if (heartPlayer != null)
            heartPlayer.StopAllPlaybackAndRemoveSources();

        NotifyOverlayStateChanged();
    }

    public void StopHeart()
    {
        heartActive = false;

        if (heartPlayer != null)
            heartPlayer.StopAllPlaybackAndRemoveSources();

        NotifyOverlayStateChanged();
    }

    public void StopAll()
    {
        StopAllSilent();
        NotifyOverlayStateChanged();
    }

    private void StopAllSilent()
    {
        tutorialActive = false;
        introActive = false;
        pingPongActive = false;
        groupPingPongActive = false;
        organsOfGenerationActive = false;
        heartActive = false;

        if (tutorialFader != null)
            tutorialFader.Stop();
        if (introPlayer != null)
            introPlayer.Stop();
        if (pingPongFader != null)
            pingPongFader.Stop();
        if (groupPingPongPlayer != null)
            groupPingPongPlayer.Stop();
        if (organsOfGenerationPlayer != null)
            organsOfGenerationPlayer.StopPlayback();
        if (heartPlayer != null)
            heartPlayer.StopAllPlaybackAndRemoveSources();
    }

    public void SetIntroFade(float value)
    {
        introFadeValue = Mathf.Clamp01(value);
        if (introPlayer != null)
            introPlayer.SetBandFade(introFadeValue);
        NotifyOverlayStateChanged();
    }

    public void SetPingPongFade(float value)
    {
        pingPongFadeValue = Mathf.Clamp01(value);
        if (pingPongFader != null)
            pingPongFader.SetBandFade(pingPongFadeValue);
        NotifyOverlayStateChanged();
    }

    public void SetOrgansOfGenerationFade(float value)
    {
        generationFadeValue = Mathf.Clamp01(value);
        if (organsOfGenerationPlayer != null)
            organsOfGenerationPlayer.SetFadeValue(generationFadeValue);
        NotifyOverlayStateChanged();
    }

    public void SetHeartBandFade(float value)
    {
        heartBandFadeValue = Mathf.Clamp01(value);
        if (heartPlayer != null)
            heartPlayer.SetBandFade(heartBandFadeValue);
        NotifyOverlayStateChanged();
    }

    public void SetHeartDelay(float normalizedValue)
    {
        heartDelayValue = Mathf.Clamp01(normalizedValue);
        if (heartPlayer != null)
            heartPlayer.SetDelayTimeNormalized(heartDelayValue);
        NotifyOverlayStateChanged();
    }

    public void SetMasterVolume(float volume)
    {
        StopMasterFade();
        ApplyMasterVolume(volume);
    }

    public void FadeIn()
    {
        StartMasterFade(true);
    }

    public void FadeOut()
    {
        StartMasterFade(false);
    }

    public void MuteImmediate()
    {
        StopMasterFade();
        ApplyMasterVolume(MinVolume);
    }

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
        if (masterFadeRoutine == null)
            return;

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

        if (introActive)
        {
            return new AudioOverlayState(
                AudioOverlayKind.IntroSingle,
                introFadeValue,
                0f,
                "LOW",
                "HIGH",
                string.Empty,
                string.Empty);
        }

        if (groupPingPongActive)
        {
            return new AudioOverlayState(
                AudioOverlayKind.GroupPingPongSingle,
                pingPongFadeValue,
                0f,
                "LOW",
                "HIGH",
                string.Empty,
                string.Empty);
        }

        if (pingPongActive)
        {
            return new AudioOverlayState(
                AudioOverlayKind.PingPongSingle,
                pingPongFadeValue,
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

    private void EnsureGroupPingPongPlayer()
    {
        if (pingPongFader == null)
            return;

        if (groupPingPongPlayer == null)
            groupPingPongPlayer = GetComponent<GroupPingPongPlayer>();

        if (groupPingPongPlayer == null)
            groupPingPongPlayer = gameObject.AddComponent<GroupPingPongPlayer>();

        groupPingPongPlayer.SetFader(pingPongFader);
    }
}
