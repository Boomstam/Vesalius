using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using Random = UnityEngine.Random;

/// <summary>
/// Plays two looping AudioSources (Low and High frequency bands) and crossfades
/// between them via mixer group attenuation. Randomly selects a sound type on Play().
/// Both TutorialFader and OrgansOfNutritionFader are instances of this class,
/// sharing the same "Low" and "High" mixer parameters (they never play simultaneously).
/// </summary>
public class DoubleFader : MonoBehaviour
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource lowSource;
    [SerializeField] private AudioSource highSource;

    [Header("Mixer Groups")]
    [SerializeField] private AudioMixerGroup lowMixerGroup;
    [SerializeField] private AudioMixerGroup highMixerGroup;
    [SerializeField] private string lowMixerParam  = "Low";
    [SerializeField] private string highMixerParam = "High";

    [Header("Clips")]
    [Tooltip("One clip per sound type. Indexed by type.")]
    [SerializeField] private AudioClip[] lowClips;
    [SerializeField] private AudioClip[] highClips;

    [Header("Labels")]
    [Tooltip("One label per sound type, matching clip array order.")]
    [SerializeField] private string[] clipTypeLabels;

    /// <summary>Fired on Play() with the label of the selected clip type.</summary>
    public UnityEvent<string> onClipTypeSelected;

    private const float MinVolume = 0.01f;

    // ── Public API ─────────────────────────────────────────────────────────────

    public void Play()
    {
        if (lowClips.Length == 0 || highClips.Length == 0)
        {
            Debug.LogError($"[DoubleFader] {name}: Clip arrays are empty.");
            return;
        }

        int type = Random.Range(0, lowClips.Length);

        lowSource.outputAudioMixerGroup  = lowMixerGroup;
        highSource.outputAudioMixerGroup = highMixerGroup;

        lowSource.clip  = lowClips[type];
        highSource.clip = highClips[type];

        lowSource.loop  = true;
        highSource.loop = true;

        lowSource.Play();
        highSource.Play();

        if (clipTypeLabels != null && type < clipTypeLabels.Length)
            onClipTypeSelected?.Invoke(clipTypeLabels[type]);
    }

    public void Stop()
    {
        lowSource.Stop();
        highSource.Stop();
    }

    /// <summary>
    /// Crossfades between Low and High mixer groups.
    /// 0 = fully Low, 1 = fully High.
    /// </summary>
    public void SetBandFade(float fadeVal)
    {
        float lowVolume  = Mathf.Clamp(1f - fadeVal, MinVolume, 1f);
        float highVolume = Mathf.Clamp(fadeVal,       MinVolume, 1f);

        lowMixerGroup.audioMixer.SetFloat(lowMixerParam,  Mathf.Log(lowVolume)  * 20f);
        highMixerGroup.audioMixer.SetFloat(highMixerParam, Mathf.Log(highVolume) * 20f);
    }
}
