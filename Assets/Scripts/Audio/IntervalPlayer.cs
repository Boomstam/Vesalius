using System.Collections;
using UnityEngine;

/// <summary>
/// Wraps a DoubleFader and alternates between audible and silent intervals.
/// The wrapped fader still receives band-fade updates immediately, even while silent.
/// </summary>
public class IntervalPlayer : MonoBehaviour
{
    [Header("Wrapped Player")]
    [SerializeField] private DoubleFader fader;

    [Header("Initial Silence")]
    [Min(0f)]
    [SerializeField] private float initialSilenceMinSeconds = 1f;
    [Min(0f)]
    [SerializeField] private float initialSilenceMaxSeconds = 10f;

    [Header("Play Interval")]
    [Min(0f)]
    [SerializeField] private float playMinSeconds = 5f;
    [Min(0f)]
    [SerializeField] private float playMaxSeconds = 10f;

    [Header("Silence Interval")]
    [Min(0f)]
    [SerializeField] private float silenceMinSeconds = 10f;
    [Min(0f)]
    [SerializeField] private float silenceMaxSeconds = 20f;

    private Coroutine intervalRoutine;

    public void Play()
    {
        StopIntervalRoutine();
        intervalRoutine = StartCoroutine(PlayLoop());
    }

    public void Stop()
    {
        StopIntervalRoutine();

        if (fader != null)
            fader.Stop();
    }

    public void SetBandFade(float fadeValue)
    {
        if (fader != null)
            fader.SetBandFade(fadeValue);
    }

    private void OnDisable()
    {
        Stop();
    }

    private IEnumerator PlayLoop()
    {
        if (fader == null)
        {
            Debug.LogWarning($"[IntervalPlayer] {name}: No DoubleFader assigned.");
            intervalRoutine = null;
            yield break;
        }

        yield return new WaitForSeconds(GetRandomDuration(initialSilenceMinSeconds, initialSilenceMaxSeconds));

        while (true)
        {
            fader.Play();
            yield return new WaitForSeconds(GetRandomDuration(playMinSeconds, playMaxSeconds));

            fader.Stop();
            yield return new WaitForSeconds(GetRandomDuration(silenceMinSeconds, silenceMaxSeconds));
        }
    }

    private void StopIntervalRoutine()
    {
        if (intervalRoutine == null)
            return;

        StopCoroutine(intervalRoutine);
        intervalRoutine = null;
    }

    private static float GetRandomDuration(float minSeconds, float maxSeconds)
    {
        if (maxSeconds < minSeconds)
            (minSeconds, maxSeconds) = (maxSeconds, minSeconds);

        return Mathf.Approximately(minSeconds, maxSeconds)
            ? minSeconds
            : Random.Range(minSeconds, maxSeconds);
    }
}
