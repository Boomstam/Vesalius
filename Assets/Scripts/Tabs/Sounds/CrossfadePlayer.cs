using UnityEngine;
using UnityEngine.Audio;

public class CrossfadePlayer : MonoBehaviour
{
    [SerializeField] private AudioClip[] clips;
    [SerializeField] private AudioMixerGroup[] mixerGroups; // parallel to clips
    [SerializeField] private bool loop = true;

    private AudioSource[] sources;

    public void Play()
    {
        if (clips == null || clips.Length == 0) { Debug.LogError("No clips assigned"); return; }
        if (mixerGroups != null && mixerGroups.Length != clips.Length)
            Debug.LogWarning("mixerGroups length doesn't match clips — some sources won't have a mixer group");

        Stop();

        sources = new AudioSource[clips.Length];
        for (int i = 0; i < clips.Length; i++)
        {
            GameObject child = new GameObject($"Source_{i}");
            child.transform.SetParent(transform);

            AudioSource src = child.AddComponent<AudioSource>();
            src.clip = clips[i];
            src.loop = loop;
            src.volume = 0f;
            src.playOnAwake = false;

            if (mixerGroups != null && i < mixerGroups.Length)
                src.outputAudioMixerGroup = mixerGroups[i];

            src.Play();
            sources[i] = src;
        }
    }

    public void Stop()
    {
        if (sources == null) return;
        foreach (var src in sources)
            if (src != null) Destroy(src.gameObject);
        sources = null;
    }

    // fadeVal: 0 = first clip, 1 = last clip
    public void SetFadeValue(float fadeVal)
    {
        if (sources == null || sources.Length == 0) return;
        if (sources.Length == 1) { sources[0].volume = 1f; return; }

        fadeVal = Mathf.Clamp01(fadeVal);
        float percentagePerSource = 1f / (sources.Length - 1);
        int startSample = Mathf.Min(Mathf.FloorToInt(fadeVal / percentagePerSource), sources.Length - 2);
        float remainderPercentage = (fadeVal - percentagePerSource * startSample) / percentagePerSource;

        for (int i = 0; i < sources.Length; i++)
        {
            float volume = 0f;
            if (i == startSample)     volume = 1f - remainderPercentage;
            if (i == startSample + 1) volume = remainderPercentage;
            sources[i].volume = volume;
        }
    }

    private void OnDestroy() => Stop();
}