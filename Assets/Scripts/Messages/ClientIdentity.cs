using System.Collections;
using FishNet;
using FishNet.Transporting;
using UnityEngine;

#if UNITY_EDITOR
using ParrelSync;
#endif

/// <summary>
/// Generates and stores a permanent per-install unique ID in PlayerPrefs.
/// Sends it to the server once the FishNet client connection is established.
/// </summary>
public class ClientIdentity : MonoBehaviour
{
    private const string PrefKey = "ClientUniqueId";
    private const int RegistrationRetryCount = 5;
    private const float RegistrationRetryDelaySeconds = 2f;

    public static string UniqueId { get; private set; }

    private Coroutine registrationRoutine;

    private void Awake()
    {
        string resolvedPrefKey = ResolvePrefKey();

        if (!PlayerPrefs.HasKey(resolvedPrefKey))
        {
            PlayerPrefs.SetString(resolvedPrefKey, System.Guid.NewGuid().ToString());
            PlayerPrefs.Save();
        }

        UniqueId = PlayerPrefs.GetString(resolvedPrefKey);
        Debug.Log($"[ClientIdentity] UniqueId = {UniqueId} (key: {resolvedPrefKey})");
    }

    private void OnEnable()
    {
        if (InstanceFinder.ClientManager != null)
            InstanceFinder.ClientManager.OnClientConnectionState += OnClientConnectionState;
    }

    private void OnDisable()
    {
        StopRegistrationRoutine();

        if (InstanceFinder.ClientManager != null)
            InstanceFinder.ClientManager.OnClientConnectionState -= OnClientConnectionState;
    }

    private void OnClientConnectionState(ClientConnectionStateArgs args)
    {
        if (args.ConnectionState == LocalConnectionState.Started)
        {
            StopRegistrationRoutine();
            registrationRoutine = StartCoroutine(RegisterWhenReady());
        }
        else if (args.ConnectionState == LocalConnectionState.Stopped)
        {
            StopRegistrationRoutine();
        }
    }

    private IEnumerator RegisterWhenReady()
    {
        for (int attempt = 1; attempt <= RegistrationRetryCount; attempt++)
        {
            if (InstanceFinder.ClientManager == null || !InstanceFinder.ClientManager.Started)
                yield break;

            NetworkedMessageSystem nms = null;
            while (nms == null)
            {
                if (InstanceFinder.ClientManager == null || !InstanceFinder.ClientManager.Started)
                    yield break;

                nms = FindObjectOfType<NetworkedMessageSystem>();
                if (nms == null)
                    yield return new WaitForSeconds(0.25f);
            }

            nms.RegisterClient(UniqueId);
            Debug.Log($"[ClientIdentity] Registration sent to server ({attempt}/{RegistrationRetryCount}).");

            if (attempt < RegistrationRetryCount)
                yield return new WaitForSeconds(RegistrationRetryDelaySeconds);
        }

        registrationRoutine = null;
    }

    private void StopRegistrationRoutine()
    {
        if (registrationRoutine == null)
            return;

        StopCoroutine(registrationRoutine);
        registrationRoutine = null;
    }

    private static string ResolvePrefKey()
    {
#if UNITY_EDITOR
        if (ClonesManager.IsClone())
        {
            string cloneArgument = ClonesManager.GetArgument();
            if (!string.IsNullOrWhiteSpace(cloneArgument))
                return $"{PrefKey}_{cloneArgument}";

            return $"{PrefKey}_Clone";
        }
#endif
        return PrefKey;
    }
}
