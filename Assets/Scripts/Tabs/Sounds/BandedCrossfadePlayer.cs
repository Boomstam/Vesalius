using UnityEngine;
using UnityEngine.Audio;

public class BandedCrossfadePlayer : MonoBehaviour
{
    [SerializeField] private CrossfadePlayer lowBand;
    [SerializeField] private CrossfadePlayer highBand;
    [SerializeField] private AudioMixerGroup lowMixerGroup;
    [SerializeField] private AudioMixerGroup highMixerGroup;

    [SerializeField] private AudioClip[] clipTypesLow;
    [SerializeField] private AudioClip[] clipTypesHigh;

    private const float minVolume = 0.0001f;

    public void Play()
    {
        int type = Random.Range(0, clipTypesLow.Length / 2);

        lowBand.SetClips(new[] { clipTypesLow[type * 2], clipTypesLow[type * 2 + 1] });
        highBand.SetClips(new[] { clipTypesHigh[type * 2], clipTypesHigh[type * 2 + 1] });

        lowBand.Play();
        highBand.Play();
    }

    public void Stop()
    {
        lowBand.Stop();
        highBand.Stop();
    }

    // Slider 1: crossfade between clips within each band
    public void SetFadeValSamples(float fadeVal)
    {
        lowBand.SetFadeValue(fadeVal);
        highBand.SetFadeValue(fadeVal);
    }

    // Slider 2: crossfade between low and high bands
    public void SetFadeValBands(float fadeVal)
    {
        fadeVal = Mathf.Clamp(fadeVal, minVolume, 1f);

        float lowVolume = Mathf.Clamp(1f - fadeVal, minVolume, 1f);
        float highVolume = Mathf.Clamp(fadeVal, minVolume, 1f);

        lowMixerGroup.audioMixer.SetFloat("Low", Mathf.Log(lowVolume) * 20f);
        highMixerGroup.audioMixer.SetFloat("High", Mathf.Log(highVolume) * 20f);
    }
}