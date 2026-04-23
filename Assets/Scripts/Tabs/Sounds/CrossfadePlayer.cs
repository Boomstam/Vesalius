using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class CrossfadePlayer : MonoBehaviour
{
    [Header("Clips — matched by index")]
    [SerializeField] private AudioClip[] lowClips;
    [SerializeField] private AudioClip[] highClips;

    [Header("Mixer Groups")]
    [SerializeField] private AudioMixerGroup lowMixerGroup;
    [SerializeField] private AudioMixerGroup highMixerGroup;

    [Header("Mixer Exposed Parameters")]
    [SerializeField] private string lowParamName  = "Low";
    [SerializeField] private string highParamName = "High";

    [Header("UI")]
    [SerializeField] private Slider crossfadeSlider;

    private AudioSource lowSource;
    private AudioSource highSource;
    private int lastIndex = -1;

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        EnsureSources();
        PlayRandomPair();
        crossfadeSlider.onValueChanged.AddListener(OnSliderChanged);
        OnSliderChanged(crossfadeSlider.value);
    }

    private void OnDisable()
    {
        crossfadeSlider.onValueChanged.RemoveListener(OnSliderChanged);
        lowSource.Stop();
        highSource.Stop();
    }

    // ─── Setup ────────────────────────────────────────────────────────────────

    private void EnsureSources()
    {
        if (lowSource  == null) lowSource  = CreateSource("Source_Low",  lowMixerGroup);
        if (highSource == null) highSource = CreateSource("Source_High", highMixerGroup);
    }

    private AudioSource CreateSource(string goName, AudioMixerGroup group)
    {
        var go  = new GameObject(goName);
        go.transform.SetParent(transform);
        var src = go.AddComponent<AudioSource>();
        src.loop        = true;
        src.playOnAwake = false;
        src.volume      = 1f;
        if (group != null) src.outputAudioMixerGroup = group;
        return src;
    }

    // ─── Playback ─────────────────────────────────────────────────────────────

    private void PlayRandomPair()
    {
        int count = Mathf.Min(lowClips.Length, highClips.Length);
        if (count == 0) { Debug.LogError("[BandedCrossfadePlayer] No clips assigned."); return; }

        int index = PickIndex(count);
        lastIndex = index;

        lowSource.clip  = lowClips[index];
        highSource.clip = highClips[index];

        lowSource.Play();
        highSource.Play();
    }

    private int PickIndex(int count)
    {
        if (count == 1) return 0;

        int index;
        do { index = Random.Range(0, count); }
        while (index == lastIndex);
        return index;
    }

    // ─── Crossfade ────────────────────────────────────────────────────────────

    private void OnSliderChanged(float t)
    {
        if (lowMixerGroup == null || highMixerGroup == null)
        {
            Debug.LogWarning($"[CrossfadePlayer] {name}: Mixer group reference is missing.");
            return;
        }

        // t=0 → full low, t=1 → full high
        // Using log scale to match human hearing, clamped to avoid -inf dB
        const float minVol = 0.0001f;
        float lowVol  = Mathf.Clamp(1f - t, minVol, 1f);
        float highVol = Mathf.Clamp(t,       minVol, 1f);

        lowMixerGroup.audioMixer.SetFloat(lowParamName,  Mathf.Log10(lowVol)  * 20f);
        highMixerGroup.audioMixer.SetFloat(highParamName, Mathf.Log10(highVol) * 20f);
    }

    private void OnDestroy()
    {
        if (lowSource  != null) Destroy(lowSource.gameObject);
        if (highSource != null) Destroy(highSource.gameObject);
    }
}
