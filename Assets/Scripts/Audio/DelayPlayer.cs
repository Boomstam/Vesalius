using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;

/// <summary>
/// Fires probabilistic audio events on a random interval.
/// Each event spawns three AudioSources (Low/Mid/High) and repeats them
/// at decreasing volume to simulate a tape-echo decay.
/// Sources are created and destroyed at runtime; only the prefab is assigned
/// in the inspector. Band crossfade is driven via mixer group attenuation.
/// </summary>
public class DelayPlayer : MonoBehaviour
{
    [Header("Clips & Prefab")]
    [Tooltip("Flat array, groups of 3 in order [Low, Mid, High] per sound type.")]
    [SerializeField] private AudioClip[]   delayClips;
    [SerializeField] private AudioSource   delaySamplePrefab;

    [Header("Mixer Groups")]
    [SerializeField] private AudioMixerGroup delayLowMixer;
    [SerializeField] private AudioMixerGroup delayMidMixer;
    [SerializeField] private AudioMixerGroup delayHighMixer;

    [Header("Interval Timing")]
    [Tooltip("Maximum time in seconds between delay events.")]
    [SerializeField] private float delayIntervalLength = 10f;
    [Tooltip("Minimum random wait between events.")]
    [SerializeField] private float minIntervalWait     = 3f;
    [Tooltip("0–1 probability that an event actually plays when triggered.")]
    [SerializeField] [Range(0f, 1f)] private float chanceOfPlaying = 0.85f;

    [Header("Echo Parameters")]
    [Tooltip("0–1. Higher value = more repeats before silence. 0.85 ≈ 6–7 repeats.")]
    [SerializeField] [Range(0f, 1f)] private float feedback  = 0.85f;
    [Tooltip("Time in seconds between echo repeats. Can be set at runtime via SetDelayTime().")]
    [SerializeField] private float delayTime = 1f;

    // ── Private State ──────────────────────────────────────────────────────────

    private const int NumBands = 3;
    private int       NumTypes => delayClips.Length / NumBands;

    private bool  isActive;
    private float lastPlayTime        = -999f;
    private float currentWaitTime;
    private int   randomTypeIndex;

    private float TimeSinceLastPlay => Time.time - lastPlayTime;

    private readonly List<AudioSource> createdSources = new List<AudioSource>();

    // ── Public API ─────────────────────────────────────────────────────────────

    public void StartPlayback()
    {
        if (delayClips.Length == 0)
        {
            Debug.LogError($"[DelayPlayer] {name}: Clips array is empty.");
            return;
        }

        isActive        = true;
        randomTypeIndex = Random.Range(0, NumTypes);
        currentWaitTime = Random.Range(minIntervalWait, delayIntervalLength);
        lastPlayTime    = Time.time;

        Debug.Log($"[DelayPlayer] Started. Type {randomTypeIndex} of {NumTypes}.");
    }

    public void StopAllPlaybackAndRemoveSources()
    {
        isActive = false;
        StopAllCoroutines();

        foreach (AudioSource source in createdSources)
        {
            if (source == null) continue;
            source.Stop();
            Destroy(source.gameObject);
        }
        createdSources.Clear();

        Debug.Log("[DelayPlayer] Stopped all playback.");
    }

    /// <summary>Set the echo repeat interval. Intended for a local slider.</summary>
    public void SetDelayTime(float value)
    {
        delayTime = Mathf.Max(0.05f, value);
    }

    /// <summary>
    /// Crossfades across Low → Mid → High via mixer group attenuation.
    /// 0 = fully Low, 0.5 = fully Mid, 1 = fully High.
    /// </summary>
    public void SetBandFade(float fadeVal)
    {
        float percentagePerBand   = 1f / (float)(NumBands - 1);
        int   startBand           = Mathf.FloorToInt(fadeVal / percentagePerBand);
        float remainder           = fadeVal - (percentagePerBand * startBand);
        float remainderPercentage = remainder / percentagePerBand;

        for (int i = 0; i < NumBands; i++)
        {
            float volume = 0f;
            if (i == startBand)     volume = 1f - remainderPercentage;
            if (i == startBand + 1) volume = remainderPercentage;

            // Clamp away from zero to avoid log(0).
            volume = Mathf.Max(volume, 0.0001f);
            float scaledVolume = Mathf.Log(volume) * 20f;

            MixerGroupForBand(i).audioMixer.SetFloat(ParamNameForBand(i), scaledVolume);
        }
    }

    // ── Update Loop ────────────────────────────────────────────────────────────

    private void Update()
    {
        if (!isActive) return;

        if (TimeSinceLastPlay >= currentWaitTime)
        {
            TryPlayRandomDelay();
            currentWaitTime = Random.Range(minIntervalWait, delayIntervalLength);
            lastPlayTime    = Time.time;
        }
    }

    // ── Internal Logic ─────────────────────────────────────────────────────────

    private void TryPlayRandomDelay()
    {
        if (Random.value > chanceOfPlaying)
        {
            Debug.Log("[DelayPlayer] Skipped event — chance check failed.");
            return;
        }

        AudioClip[] clips =
        {
            delayClips[randomTypeIndex * NumBands],
            delayClips[randomTypeIndex * NumBands + 1],
            delayClips[randomTypeIndex * NumBands + 2],
        };

        StartCoroutine(EchoDecayRoutine(clips, delayTime, feedback));
    }

    private IEnumerator EchoDecayRoutine(AudioClip[] audioClips, float repeatTime, float feedbackVal)
    {
        if (feedbackVal is < 0f or > 1f)
            Debug.LogError($"[DelayPlayer] Feedback {feedbackVal} must be between 0 and 1.");

        float clipLength = audioClips[0].length;
        float volume     = 1f;
        float fallOff    = 1f - feedbackVal;

        // Keep a local list so cleanup at the end only removes this batch.
        List<AudioSource> batchSources = new List<AudioSource>();

        // First echo.
        batchSources.AddRange(SpawnSourcesWithClips(audioClips, volume));

        // Subsequent echoes at decreasing volume.
        while (volume > 0f)
        {
            yield return new WaitForSeconds(repeatTime);

            volume -= fallOff;
            if (volume <= 0f) break;

            batchSources.AddRange(SpawnSourcesWithClips(audioClips, volume));
        }

        // Wait for the last spawned clip to finish.
        yield return new WaitForSeconds(clipLength);

        CleanupBatch(batchSources);
    }

    /// <summary>Instantiates one Low/Mid/High set, schedules play after 3 frames.</summary>
    private AudioSource[] SpawnSourcesWithClips(AudioClip[] audioClips, float volume)
    {
        AudioSource low  = Instantiate(delaySamplePrefab);
        AudioSource mid  = Instantiate(delaySamplePrefab);
        AudioSource high = Instantiate(delaySamplePrefab);

        low.outputAudioMixerGroup  = delayLowMixer;
        mid.outputAudioMixerGroup  = delayMidMixer;
        high.outputAudioMixerGroup = delayHighMixer;

        low.clip  = audioClips[0];
        mid.clip  = audioClips[1];
        high.clip = audioClips[2];

        low.volume  = volume;
        mid.volume  = volume;
        high.volume = volume;

        low.loop  = false;
        mid.loop  = false;
        high.loop = false;

        createdSources.Add(low);
        createdSources.Add(mid);
        createdSources.Add(high);

        // Defer Play() by 3 frames to allow the AudioSource to fully initialise.
        StartCoroutine(PlayAfterFrames(new[] { low, mid, high }, 3));

        return new[] { low, mid, high };
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
            if (source == null) continue;
            source.Stop();
            Destroy(source.gameObject);
            createdSources.Remove(source);
        }
        batch.Clear();
    }

    // ── Mixer Helpers ──────────────────────────────────────────────────────────

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
