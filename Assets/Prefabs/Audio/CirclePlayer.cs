using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;

/// <summary>
/// Plays three looping AudioSources (Low, Mid, High) simultaneously.
/// Randomly selects a sound type each time StartPlayback() is called.
/// Crossfade between bands is driven directly by source volume (not mixer attenuation).
/// </summary>
public class CirclePlayer : MonoBehaviour
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource lowSource;
    [SerializeField] private AudioSource midSource;
    [SerializeField] private AudioSource highSource;

    [Header("Mixer Groups")]
    [SerializeField] private AudioMixerGroup lowMixerGroup;
    [SerializeField] private AudioMixerGroup midMixerGroup;
    [SerializeField] private AudioMixerGroup highMixerGroup;

    [Header("Clips")]
    [Tooltip("Flat array, groups of 3 in order [Low, Mid, High] per sound type.")]
    [SerializeField] private AudioClip[] clips;

    private const int NumBands   = 3;
    private int       NumTypes   => clips.Length / NumBands;

    // ── Public API ─────────────────────────────────────────────────────────────

    public void StartPlayback()
    {
        if (clips.Length == 0)
        {
            Debug.LogError($"[CirclePlayer] {name}: Clips array is empty.");
            return;
        }

        if (lowSource.isPlaying)
            StopPlayback();

        int type = Random.Range(0, NumTypes);

        lowSource.outputAudioMixerGroup  = lowMixerGroup;
        midSource.outputAudioMixerGroup  = midMixerGroup;
        highSource.outputAudioMixerGroup = highMixerGroup;

        lowSource.clip  = clips[type * NumBands];
        midSource.clip  = clips[type * NumBands + 1];
        highSource.clip = clips[type * NumBands + 2];

        lowSource.loop  = true;
        midSource.loop  = true;
        highSource.loop = true;

        lowSource.Play();
        midSource.Play();
        highSource.Play();

        Debug.Log($"[CirclePlayer] Starting playback, type {type} of {NumTypes}.");
    }

    public void StopPlayback()
    {
        lowSource.Stop();
        midSource.Stop();
        highSource.Stop();
    }

    /// <summary>
    /// Crossfades across Low → Mid → High by adjusting source volumes directly.
    /// 0 = fully Low, 0.5 = fully Mid, 1 = fully High.
    /// </summary>
    public void SetFadeValue(float fadeVal)
    {
        float percentagePerBand    = 1f / (float)(NumBands - 1);
        int   startBand            = Mathf.FloorToInt(fadeVal / percentagePerBand);
        float remainder            = fadeVal - (percentagePerBand * startBand);
        float remainderPercentage  = remainder / percentagePerBand;

        AudioSource[] sources = { lowSource, midSource, highSource };

        for (int i = 0; i < NumBands; i++)
        {
            float volume = 0f;
            if (i == startBand)     volume = 1f - remainderPercentage;
            if (i == startBand + 1) volume = remainderPercentage;
            sources[i].volume = volume;
        }
    }
}
