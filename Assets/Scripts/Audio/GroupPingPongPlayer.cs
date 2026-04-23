using System.Collections;
using UnityEngine;

/// <summary>
/// Plays ping pong in the staggered two-group rhythm:
/// 30s play window with 10s fade-in and 10s fade-out, followed by 20s silence.
/// Group 0 starts with the play window, Group 1 starts with the silence offset.
/// </summary>
public class GroupPingPongPlayer : MonoBehaviour
{
    private const float PlayDuration = 30f;
    private const float FadeDuration = 10f;
    private const float SilenceDuration = 20f;

    [SerializeField] private DoubleFader fader;

    private Coroutine routine;

    public void SetFader(DoubleFader targetFader)
    {
        fader = targetFader;
    }

    public void Play(int groupIndex)
    {
        StopRoutine();

        if (fader == null)
        {
            Debug.LogWarning("[GroupPingPongPlayer] No DoubleFader assigned.");
            return;
        }

        fader.SetGroupVolume(0f);
        routine = StartCoroutine(GroupLoop(groupIndex == 0 ? 0 : 1));
    }

    public void Stop()
    {
        StopRoutine();

        if (fader != null)
        {
            fader.SetGroupVolume(0f);
            fader.Stop();
        }
    }

    private void OnDisable()
    {
        Stop();
    }

    private IEnumerator GroupLoop(int groupIndex)
    {
        if (groupIndex == 1)
            yield return new WaitForSeconds(SilenceDuration);

        while (true)
        {
            fader.Play();
            yield return FadeVolume(0f, 1f, FadeDuration);

            float sustainDuration = Mathf.Max(0f, PlayDuration - (2f * FadeDuration));
            if (sustainDuration > 0f)
                yield return new WaitForSeconds(sustainDuration);

            yield return FadeVolume(1f, 0f, FadeDuration);

            fader.Stop();
            fader.SetGroupVolume(0f);

            yield return new WaitForSeconds(SilenceDuration);
        }
    }

    private IEnumerator FadeVolume(float from, float to, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            fader.SetGroupVolume(Mathf.Lerp(from, to, t));
            yield return null;
        }

        fader.SetGroupVolume(to);
    }

    private void StopRoutine()
    {
        if (routine == null)
            return;

        StopCoroutine(routine);
        routine = null;
    }
}
