using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class BandedCrossfadePlayer : MonoBehaviour
{
    [SerializeField] private AudioMixerGroup lowMixerGroup;
    [SerializeField] private AudioMixerGroup highMixerGroup;

    [SerializeField] private AudioClip[] clipTypesLow;
    [SerializeField] private AudioClip[] clipTypesHigh;

    [SerializeField] private Slider samplesFadeSlider;
    [SerializeField] private Slider bandsFadeSlider;

    private CrossfadePlayer lowBand;
    private CrossfadePlayer highBand;

    private const float minVolume = 0.0001f;

    private void Start()
    {
        lowBand = CreateBand("LowBand");
        highBand = CreateBand("HighBand");

        samplesFadeSlider.onValueChanged.AddListener(SetFadeValSamples);
        bandsFadeSlider.onValueChanged.AddListener(SetFadeValBands);
        
        Play();

        Debug.Log("Play");
    }

    private void OnEnable()
    {
        
    }

    private CrossfadePlayer CreateBand(string bandName)
    {
        GameObject go = new GameObject(bandName);
        go.transform.SetParent(transform);
        return go.AddComponent<CrossfadePlayer>();
    }

    public void Play()
    {
        int type = Random.Range(0, clipTypesLow.Length / 2);

        lowBand.SetClips(new[] { clipTypesLow[type * 2], clipTypesLow[type * 2 + 1] }, lowMixerGroup);
        highBand.SetClips(new[] { clipTypesHigh[type * 2], clipTypesHigh[type * 2 + 1] }, highMixerGroup);

        lowBand.Play();
        highBand.Play();

        SetFadeValSamples(samplesFadeSlider.value);
        SetFadeValBands(bandsFadeSlider.value);
    }

    public void Stop()
    {
        lowBand.Stop();
        highBand.Stop();
    }

    public void SetFadeValSamples(float fadeVal)
    {
        lowBand.SetFadeValue(fadeVal);
        highBand.SetFadeValue(fadeVal);
    }

    public void SetFadeValBands(float fadeVal)
    {
        float lowVolume = Mathf.Clamp(1f - fadeVal, minVolume, 1f);
        float highVolume = Mathf.Clamp(fadeVal, minVolume, 1f);

        lowMixerGroup.audioMixer.SetFloat("Low", Mathf.Log(lowVolume) * 20f);
        highMixerGroup.audioMixer.SetFloat("High", Mathf.Log(highVolume) * 20f);
    }

    private void OnDestroy()
    {
        samplesFadeSlider.onValueChanged.RemoveListener(SetFadeValSamples);
        bandsFadeSlider.onValueChanged.RemoveListener(SetFadeValBands);
    }
}