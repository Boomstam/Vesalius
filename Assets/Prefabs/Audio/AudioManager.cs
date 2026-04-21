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
    [Header("Sound Players")]
    [SerializeField] private DoubleFader  tutorialFader;
    [SerializeField] private DoubleFader  organsOfNutritionFader;
    [SerializeField] private CirclePlayer organsOfGenerationPlayer;
    [SerializeField] private DelayPlayer  heartPlayer;

    [Header("Master Volume")]
    [SerializeField] private AudioMixerGroup masterMixerGroup;
    [Tooltip("Duration of the FadeIn() coroutine in seconds.")]
    [SerializeField] private float fadeInTime  = 2f;
    [Tooltip("Duration of the FadeOut() coroutine in seconds.")]
    [SerializeField] private float fadeOutTime = 2f;

    private const float MinVolume = 0.01f;
    private Coroutine masterFadeRoutine;

    // ── Tutorial ───────────────────────────────────────────────────────────────

    public void PlayTutorial() => tutorialFader.Play();
    public void StopTutorial() => tutorialFader.Stop();

    // ── Organs of Nutrition ────────────────────────────────────────────────────

    public void PlayOrgansOfNutrition() => organsOfNutritionFader.Play();
    public void StopOrgansOfNutrition() => organsOfNutritionFader.Stop();

    // ── Organs of Generation ───────────────────────────────────────────────────

    public void PlayOrgansOfGeneration() => organsOfGenerationPlayer.StartPlayback();
    public void StopOrgansOfGeneration() => organsOfGenerationPlayer.StopPlayback();

    // ── Heart ──────────────────────────────────────────────────────────────────

    public void PlayHeart() => heartPlayer.StartPlayback();
    public void StopHeart() => heartPlayer.StopAllPlaybackAndRemoveSources();

    // ── Stop All ───────────────────────────────────────────────────────────────

    public void StopAll()
    {
        tutorialFader.Stop();
        organsOfNutritionFader.Stop();
        organsOfGenerationPlayer.StopPlayback();
        heartPlayer.StopAllPlaybackAndRemoveSources();
    }

    // ── Master Volume ──────────────────────────────────────────────────────────

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

    // ── Internal Master Fade ───────────────────────────────────────────────────

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
        float startTime  = Time.time;
        float duration   = fadeIn ? fadeInTime : fadeOutTime;
        float elapsed    = 0f;

        while (elapsed < duration)
        {
            float t      = elapsed / duration;
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
}
