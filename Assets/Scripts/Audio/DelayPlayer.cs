using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;

/// <summary>
/// Plays one continuous three-band layer plus a delayed secondary layer.
/// Sources are created and destroyed at runtime; only the prefab is assigned
/// in the inspector. Band crossfade is driven via mixer group attenuation.
/// </summary>
public class DelayPlayer : MonoBehaviour
{
    [Header("Clips & Prefab")]
    [Tooltip("Flat array, groups of 3 in order [Low, Mid, High] per sound type.")]
    [SerializeField] private AudioClip[] delayClips;
    [SerializeField] private AudioSource delaySamplePrefab;

    [Header("Mixer Groups")]
    [SerializeField] private AudioMixerGroup delayLowMixer;
    [SerializeField] private AudioMixerGroup delayMidMixer;
    [SerializeField] private AudioMixerGroup delayHighMixer;

    [Header("Continuous Delay")]
    [Tooltip("0-1. Volume of the delayed continuous layer.")]
    [SerializeField] [Range(0f, 1f)] private float feedback = 0.85f;
    [Tooltip("Time in seconds before the delayed continuous layer starts.")]
    [SerializeField] private float delayTime = 1f;
    [Tooltip("Minimum delay time exposed through the UI slider.")]
    [SerializeField] private float minDelayTime = 0.2f;
    [Tooltip("Maximum delay time exposed through the UI slider.")]
    [SerializeField] private float maxDelayTime = 1.8f;

    private const int NumBands = 3;
    private int NumTypes => delayClips.Length / NumBands;

    private bool isActive;
    private int randomTypeIndex;
    private Coroutine delayedSecondaryLoopRoutine;

    private readonly List<AudioSource> createdSources = new();
    private readonly List<AudioSource> continuousPrimarySources = new();
    private readonly List<AudioSource> continuousSecondarySources = new();

    public bool HasAudiblePlayback
    {
        get
        {
            foreach (AudioSource source in createdSources)
            {
                if (source != null && source.isPlaying && source.volume > 0.001f)
                    return true;
            }

            return false;
        }
    }

    public void StartPlayback()
    {
        if (delayClips.Length == 0)
        {
            Debug.LogError($"[DelayPlayer] {name}: Clips array is empty.");
            return;
        }

        isActive = true;
        randomTypeIndex = Random.Range(0, NumTypes);
        StartContinuousPlayback();

        Debug.Log($"[DelayPlayer] Started continuous playback. Type {randomTypeIndex} of {NumTypes}.");
    }

    public void StopAllPlaybackAndRemoveSources()
    {
        isActive = false;
        StopAllCoroutines();
        delayedSecondaryLoopRoutine = null;

        foreach (AudioSource source in createdSources)
        {
            if (source == null)
                continue;

            source.Stop();
            Destroy(source.gameObject);
        }

        createdSources.Clear();
        continuousPrimarySources.Clear();
        continuousSecondarySources.Clear();

        Debug.Log("[DelayPlayer] Stopped all playback.");
    }

    /// <summary>Set the delay before the secondary continuous layer starts.</summary>
    public void SetDelayTime(float value)
    {
        delayTime = Mathf.Max(0.05f, value);

        if (isActive)
            RefreshContinuousDelay();
    }

    /// <summary>Maps a 0-1 UI slider to the configured delay-time range.</summary>
    public void SetDelayTimeNormalized(float normalizedValue)
    {
        float clampedValue = Mathf.Clamp01(normalizedValue);
        SetDelayTime(Mathf.Lerp(minDelayTime, maxDelayTime, clampedValue));
    }

    /// <summary>
    /// Crossfades across Low -> Mid -> High via mixer group attenuation.
    /// 0 = fully Low, 0.5 = fully Mid, 1 = fully High.
    /// </summary>
    public void SetBandFade(float fadeVal)
    {
        float percentagePerBand = 1f / (float)(NumBands - 1);
        int startBand = Mathf.FloorToInt(fadeVal / percentagePerBand);
        float remainder = fadeVal - (percentagePerBand * startBand);
        float remainderPercentage = remainder / percentagePerBand;

        for (int i = 0; i < NumBands; i++)
        {
            float volume = 0f;
            if (i == startBand)
                volume = 1f - remainderPercentage;
            if (i == startBand + 1)
                volume = remainderPercentage;

            volume = Mathf.Max(volume, 0.0001f);
            float scaledVolume = Mathf.Log(volume) * 20f;

            MixerGroupForBand(i).audioMixer.SetFloat(ParamNameForBand(i), scaledVolume);
        }
    }

    private void StartContinuousPlayback()
    {
        AudioClip[] clips = GetCurrentClips();
        ReplaceContinuousSet(continuousPrimarySources, SpawnSourcesWithClips(clips, 1f, loop: true));
        RefreshContinuousDelay();
    }

    private void RefreshContinuousDelay()
    {
        if (!isActive)
            return;

        StopSecondaryLoopRoutine();
        CleanupBatch(continuousSecondarySources);

        AudioClip[] clips = GetCurrentClips();
        delayedSecondaryLoopRoutine = StartCoroutine(StartDelayedContinuousLoop(clips, delayTime));
    }

    private IEnumerator StartDelayedContinuousLoop(AudioClip[] audioClips, float delaySeconds)
    {
        if (delaySeconds > 0f)
            yield return new WaitForSeconds(delaySeconds);

        delayedSecondaryLoopRoutine = null;

        if (!isActive)
            yield break;

        ReplaceContinuousSet(continuousSecondarySources, SpawnSourcesWithClips(audioClips, feedback, loop: true));
    }

    private void StopSecondaryLoopRoutine()
    {
        if (delayedSecondaryLoopRoutine == null)
            return;

        StopCoroutine(delayedSecondaryLoopRoutine);
        delayedSecondaryLoopRoutine = null;
    }

    private AudioSource[] SpawnSourcesWithClips(AudioClip[] audioClips, float volume, bool loop)
    {
        AudioSource low = Instantiate(delaySamplePrefab);
        AudioSource mid = Instantiate(delaySamplePrefab);
        AudioSource high = Instantiate(delaySamplePrefab);

        low.outputAudioMixerGroup = delayLowMixer;
        mid.outputAudioMixerGroup = delayMidMixer;
        high.outputAudioMixerGroup = delayHighMixer;

        low.clip = audioClips[0];
        mid.clip = audioClips[1];
        high.clip = audioClips[2];

        low.volume = volume;
        mid.volume = volume;
        high.volume = volume;

        low.loop = loop;
        mid.loop = loop;
        high.loop = loop;

        createdSources.Add(low);
        createdSources.Add(mid);
        createdSources.Add(high);

        StartCoroutine(PlayAfterFrames(new[] { low, mid, high }, 3));

        return new[] { low, mid, high };
    }

    private AudioClip[] GetCurrentClips()
    {
        return new[]
        {
            delayClips[randomTypeIndex * NumBands],
            delayClips[randomTypeIndex * NumBands + 1],
            delayClips[randomTypeIndex * NumBands + 2],
        };
    }

    private void ReplaceContinuousSet(List<AudioSource> targetSet, AudioSource[] replacement)
    {
        CleanupBatch(targetSet);
        targetSet.AddRange(replacement);
    }

    private IEnumerator PlayAfterFrames(AudioSource[] sources, int frames)
    {
        for (int i = 0; i < frames; i++)
            yield return null;

        foreach (AudioSource source in sources)
        {
            if (source != null)
                source.Play();
        }
    }

    private void CleanupBatch(List<AudioSource> batch)
    {
        foreach (AudioSource source in batch)
        {
            if (source == null)
                continue;

            source.Stop();
            Destroy(source.gameObject);
            createdSources.Remove(source);
        }

        batch.Clear();
    }

    private AudioMixerGroup MixerGroupForBand(int index) => index switch
    {
        0 => delayLowMixer,
        1 => delayMidMixer,
        2 => delayHighMixer,
        _ => throw new System.ArgumentOutOfRangeException(nameof(index))
    };

    private string ParamNameForBand(int index) => index switch
    {
        0 => "DelayLow",
        1 => "DelayMid",
        2 => "DelayHigh",
        _ => throw new System.ArgumentOutOfRangeException(nameof(index))
    };
}
