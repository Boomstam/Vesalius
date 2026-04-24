using System.Collections;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

/// <summary>
/// Server-authoritative staggered start controller for the heart mode.
/// The server snapshots connected clients when heart is enabled, assigns each
/// one a delay within the 0-40 second lead-in window, and each client then
/// keeps its own local 80-second playback lifetime.
/// </summary>
public class HeartStaggerController : NetworkBehaviour
{
    private const float StaggerLeadInDurationSeconds = 40f;
    private const float LocalPlaybackDurationSeconds = 80f;
    private const float PendingTriggerRetryDelaySeconds = 0.5f;

    private Coroutine localLifecycleRoutine;
    private Coroutine pendingTriggerRoutine;
    private uint localSequence;
    private bool pendingTrigger;

    public void TriggerStagger(NetworkedMessageSystem messageSystem)
    {
        if (!IsServerInitialized || messageSystem == null)
            return;

        List<NetworkConnection> connections = new(messageSystem.GetAllConnections());
        int count = connections.Count;

        if (count == 0)
        {
            pendingTrigger = true;
            return;
        }

        pendingTrigger = false;
        StopPendingTriggerRetry();

        if (count == 1)
        {
            RpcStartWithDelay(connections[0], 0f);
            return;
        }

        for (int index = 0; index < count; index++)
        {
            float delaySeconds = Mathf.Lerp(
                0f,
                StaggerLeadInDurationSeconds,
                index / (float)(count - 1));
            RpcStartWithDelay(connections[index], delaySeconds);
        }
    }

    public void CancelAll()
    {
        if (!IsServerInitialized)
            return;

        pendingTrigger = false;
        StopPendingTriggerRetry();
        RpcCancelAll();
    }

    public void RetryPendingTrigger(NetworkedMessageSystem messageSystem)
    {
        if (!IsServerInitialized || !pendingTrigger || pendingTriggerRoutine != null)
            return;

        pendingTriggerRoutine = StartCoroutine(RetryPendingTriggerRoutine(messageSystem));
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        CancelLocalLifecycle(stopAudio: true);
    }

    [TargetRpc]
    private void RpcStartWithDelay(NetworkConnection connection, float delaySeconds)
    {
        if (SceneLoader.BuildType != BuildType.Client)
            return;

        CancelLocalLifecycle(stopAudio: false);
        localLifecycleRoutine = StartCoroutine(LocalHeartLifecycle(++localSequence, delaySeconds));
    }

    [ObserversRpc]
    private void RpcCancelAll()
    {
        if (SceneLoader.BuildType != BuildType.Client)
            return;

        CancelLocalLifecycle(stopAudio: true);
    }

    private IEnumerator LocalHeartLifecycle(uint sequence, float delaySeconds)
    {
        if (delaySeconds > 0f)
            yield return new WaitForSeconds(delaySeconds);

        if (sequence != localSequence)
            yield break;

        AudioManager audioManager = GetAudioManager();
        if (audioManager == null)
        {
            localLifecycleRoutine = null;
            yield break;
        }

        audioManager.PlayHeart();

        yield return new WaitForSeconds(LocalPlaybackDurationSeconds);

        if (sequence != localSequence)
            yield break;

        audioManager.StopHeartPlayback();
        localLifecycleRoutine = null;
    }

    private void CancelLocalLifecycle(bool stopAudio)
    {
        localSequence++;

        if (localLifecycleRoutine != null)
        {
            StopCoroutine(localLifecycleRoutine);
            localLifecycleRoutine = null;
        }

        if (!stopAudio)
            return;

        AudioManager audioManager = GetAudioManager();
        if (audioManager != null)
            audioManager.StopHeart();
    }

    private IEnumerator RetryPendingTriggerRoutine(NetworkedMessageSystem messageSystem)
    {
        yield return new WaitForSeconds(PendingTriggerRetryDelaySeconds);
        pendingTriggerRoutine = null;

        if (pendingTrigger)
            TriggerStagger(messageSystem);
    }

    private void StopPendingTriggerRetry()
    {
        if (pendingTriggerRoutine == null)
            return;

        StopCoroutine(pendingTriggerRoutine);
        pendingTriggerRoutine = null;
    }

    private static AudioManager GetAudioManager()
    {
        return Instances.AudioManager != null
            ? Instances.AudioManager
            : FindObjectOfType<AudioManager>();
    }
}
