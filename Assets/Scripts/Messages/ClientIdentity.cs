using System.Collections;
using FishNet;
using FishNet.Transporting;
using UnityEngine;

/// <summary>
/// Generates and stores a permanent per-install unique ID in PlayerPrefs.
/// Sends it to the server once the FishNet client connection is established.
/// </summary>
public class ClientIdentity : MonoBehaviour
{
    private const string PrefKey = "ClientUniqueId";

    public static string UniqueId { get; private set; }

    private void Awake()
    {
        if (!PlayerPrefs.HasKey(PrefKey))
        {
            PlayerPrefs.SetString(PrefKey, System.Guid.NewGuid().ToString());
            PlayerPrefs.Save();
        }

        UniqueId = PlayerPrefs.GetString(PrefKey);
        Debug.Log($"[ClientIdentity] UniqueId = {UniqueId}");
    }

    private void OnEnable()
    {
        if (InstanceFinder.ClientManager != null)
            InstanceFinder.ClientManager.OnClientConnectionState += OnClientConnectionState;
    }

    private void OnDisable()
    {
        if (InstanceFinder.ClientManager != null)
            InstanceFinder.ClientManager.OnClientConnectionState -= OnClientConnectionState;
    }

    private void OnClientConnectionState(ClientConnectionStateArgs args)
    {
        if (args.ConnectionState == LocalConnectionState.Started)
            StartCoroutine(RegisterWhenReady());
    }

    private IEnumerator RegisterWhenReady()
    {
        NetworkedMessageSystem nms = null;

        while (nms == null)
        {
            nms = FindObjectOfType<NetworkedMessageSystem>();

            if (nms == null)
                yield return new WaitForSeconds(0.5f);
        }

        nms.RegisterClient(UniqueId);
        Debug.Log("[ClientIdentity] Registration sent to server.");
    }
}
