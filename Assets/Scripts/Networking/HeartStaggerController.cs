using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

/// <summary>
/// Legacy compatibility component. Heart audio now uses the same simple
/// toggle-driven looping flow as the other continuous sound modes.
/// </summary>
public class HeartStaggerController : NetworkBehaviour
{
    public void TriggerStagger(NetworkedMessageSystem messageSystem)
    {
        if (!IsServerInitialized)
            return;

        RpcSetHeartEnabled(true);
    }

    public void CancelAll()
    {
        if (!IsServerInitialized)
            return;

        RpcSetHeartEnabled(false);
    }

    public void RetryPendingTrigger(NetworkedMessageSystem messageSystem)
    {
        // Intentionally empty. Staggered retries are no longer used.
    }

    public override void OnStopClient()
    {
        base.OnStopClient();

        if (SceneLoader.BuildType != BuildType.Client)
            return;

        AudioManager audioManager = GetAudioManager();
        if (audioManager != null)
            audioManager.StopHeart();
    }

    [ObserversRpc]
    private void RpcSetHeartEnabled(bool enabled)
    {
        if (SceneLoader.BuildType != BuildType.Client)
            return;

        AudioManager audioManager = GetAudioManager();
        if (audioManager == null)
            return;

        if (enabled)
            audioManager.PlayHeart();
        else
            audioManager.StopHeart();
    }

    private static AudioManager GetAudioManager()
    {
        return Instances.AudioManager != null
            ? Instances.AudioManager
            : FindObjectOfType<AudioManager>();
    }
}
