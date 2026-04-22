using System.Collections;
using System.Collections.Generic;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Transporting;
using UnityEngine;

/// <summary>
/// Server-authoritative targeted message system.
/// Keeps a per-round shuffle bag of connected clients and tops up from a
/// fresh shuffled deck when the current deck is exhausted.
/// </summary>
public class NetworkedMessageSystem : NetworkBehaviour
{
    [Header("Settings")]
    [Tooltip("How many clients receive each message press.")]
    [SerializeField] private int _targetsPerMessage = 3;

    [Tooltip("Seconds the message stays visible on each client.")]
    [SerializeField] private float _messageDuration = 60f;

    private readonly Dictionary<NetworkConnection, string> _connectedClients = new();
    private readonly List<NetworkConnection> _deckRemaining = new();

    private MessageOverlay _messageOverlay;

    public override void OnStartServer()
    {
        base.OnStartServer();

        if (InstanceFinder.ServerManager != null)
            InstanceFinder.ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;

        Debug.Log("[NetworkedMessageSystem] Server started.");
    }

    public override void OnStopServer()
    {
        base.OnStopServer();

        if (InstanceFinder.ServerManager != null)
            InstanceFinder.ServerManager.OnRemoteConnectionState -= OnRemoteConnectionState;

        _connectedClients.Clear();
        _deckRemaining.Clear();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (SceneLoader.BuildType == BuildType.Client)
            StartCoroutine(FindMessageOverlayCoroutine());
    }

    private void OnRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs args)
    {
        if (args.ConnectionState != RemoteConnectionState.Stopped)
            return;

        if (!_connectedClients.ContainsKey(conn))
            return;

        Debug.Log($"[NetworkedMessageSystem] Client '{_connectedClients[conn]}' disconnected.");
        _connectedClients.Remove(conn);
        _deckRemaining.Remove(conn);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RegisterClient(string uniqueId, NetworkConnection sender = null)
    {
        if (sender == null)
            return;

        _connectedClients[sender] = uniqueId;

        if (!_deckRemaining.Contains(sender))
            _deckRemaining.Add(sender);

        Debug.Log($"[NetworkedMessageSystem] Registered '{uniqueId}' (conn {sender.ClientId}). Total: {_connectedClients.Count} Deck: {_deckRemaining.Count}");
    }

    [ServerRpc(RequireOwnership = false)]
    public void SendMessageToTargets(string word)
    {
        var targets = PickTargets(_targetsPerMessage);
        Debug.Log($"[NetworkedMessageSystem] '{word}' -> {targets.Count} client(s).");

        foreach (NetworkConnection conn in targets)
            RpcReceiveMessage(conn, word, _messageDuration);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ResetDeck()
    {
        _deckRemaining.Clear();
        _deckRemaining.AddRange(_connectedClients.Keys);
        Shuffle(_deckRemaining);

        Debug.Log($"[NetworkedMessageSystem] Deck reset - {_deckRemaining.Count} clients.");
    }

    [ServerRpc(RequireOwnership = false)]
    public void HardCutAll()
    {
        RpcHardCut();
    }

    [TargetRpc]
    private void RpcReceiveMessage(NetworkConnection conn, string word, float duration)
    {
        if (_messageOverlay == null)
            _messageOverlay = FindObjectOfType<MessageOverlay>();

        if (_messageOverlay != null)
            _messageOverlay.ShowMessage(word, duration);
        else
            Debug.LogWarning("[NetworkedMessageSystem] MessageOverlay not found on client.");
    }

    [ObserversRpc]
    private void RpcHardCut()
    {
        if (SceneLoader.BuildType != BuildType.Client)
            return;

        if (_messageOverlay == null)
            _messageOverlay = FindObjectOfType<MessageOverlay>();

        _messageOverlay?.HideMessage();
    }

    private List<NetworkConnection> PickTargets(int count)
    {
        var connected = new List<NetworkConnection>(_connectedClients.Keys);
        if (connected.Count == 0)
            return new List<NetworkConnection>();

        int needed = Mathf.Min(Mathf.Max(0, count), connected.Count);
        var selected = new List<NetworkConnection>();

        while (selected.Count < needed)
        {
            _deckRemaining.RemoveAll(c => !_connectedClients.ContainsKey(c) || selected.Contains(c));

            if (_deckRemaining.Count == 0)
            {
                _deckRemaining.AddRange(connected);
                Shuffle(_deckRemaining);
                _deckRemaining.RemoveAll(c => selected.Contains(c));
            }

            if (_deckRemaining.Count == 0)
                break;

            selected.Add(_deckRemaining[0]);
            _deckRemaining.RemoveAt(0);
        }

        return selected;
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private IEnumerator FindMessageOverlayCoroutine()
    {
        while (_messageOverlay == null)
        {
            _messageOverlay = FindObjectOfType<MessageOverlay>();

            if (_messageOverlay == null)
                yield return new WaitForSeconds(0.5f);
        }

        Debug.Log("[NetworkedMessageSystem] MessageOverlay cached.");
    }
}
