using System;
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
    private static readonly Dictionary<string, string> InstrumentDisplayLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["chimes"] = "1\n(chimes)",
        ["claves"] = "2\n(claves)",
        ["anvil"] = "3\n(anvil)",
        ["waterphone"] = "4\n(waterphone)",
        ["crotales"] = "5\n(crotales)",
        ["cymbal"] = "6\n(cymbal)",
        ["water station"] = "7\n(water station)"
    };

    [Header("Settings")]
    [Tooltip("How many clients receive each message press.")]
    [SerializeField] private int _targetsPerMessage = 3;

    [Tooltip("Seconds the message stays visible on each client.")]
    [SerializeField] private float _messageDuration = 60f;

    private readonly Dictionary<NetworkConnection, string> _connectedClients = new();
    private readonly Dictionary<string, NetworkConnection> _connectionsByUniqueId = new();
    private readonly List<NetworkConnection> _deckRemaining = new();

    private MessageOverlay _messageOverlay;

    /// <summary>
    /// Fired on the server whenever a client registers or reconnects.
    /// </summary>
    public event Action<NetworkConnection, string> ClientRegistered;

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
        _connectionsByUniqueId.Clear();
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

        if (!_connectedClients.TryGetValue(conn, out string uniqueId))
            return;

        Debug.Log($"[NetworkedMessageSystem] Client '{uniqueId}' disconnected.");
        _connectedClients.Remove(conn);
        _deckRemaining.Remove(conn);

        if (_connectionsByUniqueId.TryGetValue(uniqueId, out NetworkConnection mappedConn) && mappedConn == conn)
            _connectionsByUniqueId.Remove(uniqueId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RegisterClient(string uniqueId, NetworkConnection sender = null)
    {
        if (sender == null || string.IsNullOrWhiteSpace(uniqueId))
            return;

        if (_connectionsByUniqueId.TryGetValue(uniqueId, out NetworkConnection previousConn) && previousConn != sender)
        {
            _connectedClients.Remove(previousConn);
            _deckRemaining.Remove(previousConn);
        }

        _connectedClients[sender] = uniqueId;
        _connectionsByUniqueId[uniqueId] = sender;

        if (!_deckRemaining.Contains(sender))
            _deckRemaining.Add(sender);

        Debug.Log($"[NetworkedMessageSystem] Registered '{uniqueId}' (conn {sender.ClientId}). Total: {_connectedClients.Count} Deck: {_deckRemaining.Count}");
        ClientRegistered?.Invoke(sender, uniqueId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SendMessageToTargets(string word)
    {
        string displayWord = GetDisplayLabel(word);
        List<NetworkConnection> targets = PickTargets(_targetsPerMessage);
        Debug.Log($"[NetworkedMessageSystem] '{displayWord}' -> {targets.Count} client(s).");

        foreach (NetworkConnection conn in targets)
            RpcReceiveMessage(conn, displayWord, _messageDuration);
    }

    [ServerRpc(RequireOwnership = false)]
    public void TriggerGroupMessage()
    {
        BroadcastGroupMessage();
    }

    [ServerRpc(RequireOwnership = false)]
    public void ResetDeck()
    {
        ResetDeckServer();
    }

    public void ResetDeckServer()
    {
        if (!IsServerInitialized)
            return;

        _deckRemaining.Clear();
        _deckRemaining.AddRange(_connectedClients.Keys);
        Shuffle(_deckRemaining);

        Debug.Log($"[NetworkedMessageSystem] Deck reset - {_deckRemaining.Count} clients.");
    }

    [ServerRpc(RequireOwnership = false)]
    public void HardCutAll()
    {
        HardCutAllServer();
    }

    public void HardCutAllServer()
    {
        if (!IsServerInitialized)
            return;

        RpcHardCut();
    }

    public IReadOnlyCollection<NetworkConnection> GetAllConnections()
    {
        return _connectedClients.Keys;
    }

    public bool TryGetUniqueId(NetworkConnection connection, out string uniqueId)
    {
        return _connectedClients.TryGetValue(connection, out uniqueId);
    }

    public void BroadcastGroupMessage()
    {
        Debug.Log("[NetworkedMessageSystem] Broadcasting group message to all clients.");
        RpcReceiveGroupMessage(_messageDuration);
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
    private void RpcReceiveGroupMessage(float duration)
    {
        if (SceneLoader.BuildType != BuildType.Client)
            return;

        if (_messageOverlay == null)
            _messageOverlay = FindObjectOfType<MessageOverlay>();

        if (_messageOverlay != null)
            _messageOverlay.ShowMessage("Go to your color", duration, showBackdrop: false);
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
        List<NetworkConnection> connected = new(_connectedClients.Keys);
        if (connected.Count == 0)
            return new List<NetworkConnection>();

        int needed = Mathf.Min(Mathf.Max(0, count), connected.Count);
        List<NetworkConnection> selected = new();

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
            int j = UnityEngine.Random.Range(0, i + 1);
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

    private static string GetDisplayLabel(string instrumentName)
    {
        if (string.IsNullOrWhiteSpace(instrumentName))
            return string.Empty;

        return InstrumentDisplayLabels.TryGetValue(instrumentName, out string displayLabel)
            ? displayLabel
            : instrumentName;
    }
}
