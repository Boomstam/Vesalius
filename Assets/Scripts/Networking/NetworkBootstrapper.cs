using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using UnityEngine;
using FishNet;
using FishNet.Transporting;
using FishNet.Transporting.Multipass;
using FishNet.Transporting.Tugboat;
using FishNet.Transporting.Bayou;

/// <summary>
/// Starts the correct FishNet role based on BuildType + TransportType resolved by SceneLoader.
///
/// Server  → Multipass starts all child transports simultaneously (Tugboat on _tugboatPort, Bayou on _bayouPort).
/// Monitor → same as server when running locally; otherwise connects as Tugboat client.
/// Client  → selects Tugboat or Bayou via Multipass before connecting.
///
/// Remote Bayou traffic hits Caddy on port 443 (WSS), which reverse-proxies to Bayou on localhost:7778 (plain WS).
/// Local Bayou traffic connects directly to localhost:7778 (plain WS, no cert).
///
/// Reconnection: once a client connection has been attempted, any Stopped state that isn't
/// the result of intentional shutdown triggers an automatic reconnect after _reconnectDelay seconds.
/// Set _maxReconnectAttempts to 0 for infinite retries.
/// </summary>
public class NetworkBootstrapper : MonoBehaviour
{
    [Header("Ports")]
    [Tooltip("UDP port used by Tugboat.")]
    [SerializeField] private ushort _tugboatPort = 7777;

    [Tooltip("Plain WS port Bayou listens on internally. Caddy proxies to this.")]
    [SerializeField] private ushort _bayouPort = 7778;

    [Tooltip("External WSS port clients use to reach Caddy. Caddy then forwards to _bayouPort.")]
    [SerializeField] private ushort _bayouRemotePort = 443;

    [Header("Addresses")]
    [Tooltip("Public IP or domain of the dedicated server (used by Tugboat clients).")]
    [SerializeField] private string _serverAddress = "178.104.196.127";

    [Tooltip("Hostname for local Bayou testing (plain WS, no cert).")]
    [SerializeField] private string _bayouLocalAddress = "localhost";

    [Tooltip("Hostname for production Bayou. Caddy terminates TLS here.")]
    [SerializeField] private string _bayouRemoteAddress = "ws.studiotegenstem.com";

    [Tooltip("When enabled, Monitor acts as server+host and all clients connect to localhost.")]
    [SerializeField] private bool _runLocally = false;

    [Header("Reconnection")]
    [Tooltip("Seconds to wait before each reconnection attempt.")]
    [SerializeField] private float _reconnectDelay = 4f;

    [Tooltip("Maximum number of reconnect attempts. 0 = infinite.")]
    [SerializeField] private int _maxReconnectAttempts = 0;

    private Multipass _multipass;

    // Reconnection state
    private bool      _reconnectEnabled  = false; // armed once ConnectClient is first called
    private bool      _isQuitting        = false;
    private int       _reconnectAttempt  = 0;
    private Coroutine _reconnectCoroutine;

    // Cached so the reconnect coroutine can replay the same call
    private string _activeHost;
    private ushort _activePort;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        Debug.Log($"[NetworkBootstrapper] PID={System.Diagnostics.Process.GetCurrentProcess().Id}  " +
                  $"BuildType={SceneLoader.BuildType}  TransportType={SceneLoader.TransportType}");

        _multipass = InstanceFinder.NetworkManager.GetComponent<Multipass>();
        if (_multipass == null)
            Debug.LogError("[NetworkBootstrapper] Multipass component not found on NetworkManager!");

        if (SceneLoader.BuildType == BuildType.Server)
        {
            LogPortAvailability(_tugboatPort, SocketType.Dgram, ProtocolType.Udp, "Tugboat UDP");
            LogTugboatSettings();
        }

        InstanceFinder.ServerManager.OnServerConnectionState += OnServerConnectionState;
        InstanceFinder.ClientManager.OnClientConnectionState += OnClientConnectionState;

        // In the editor, ParrelSync clones should never start a server —
        // they are pure clients connecting to the main editor's server.
#if UNITY_EDITOR
        bool isClone = ParrelSync.ClonesManager.IsClone();
#else
        bool isClone = false;
#endif

        if (_runLocally)
        {
            switch (SceneLoader.BuildType)
            {
                case BuildType.Monitor:
                    if (!isClone)
                    {
                        Debug.Log($"[NetworkBootstrapper] Local mode — starting Multipass server " +
                                  $"(Tugboat:{_tugboatPort} / Bayou:{_bayouPort})");
                        InstanceFinder.ServerManager.StartConnection();
                    }
                    else
                    {
                        Debug.Log("[NetworkBootstrapper] Local mode — Monitor clone, skipping server start.");
                    }
                    SelectClientTransport();
                    ConnectClient("localhost");
                    break;

                case BuildType.Server:                         // ← split this out
                    Debug.Log($"[NetworkBootstrapper] Local mode — Server clone starting Multipass " +
                              $"(Tugboat:{_tugboatPort} / Bayou:{_bayouPort})");
                    InstanceFinder.ServerManager.StartConnection();
                    break;                                     // ← no client connection

                case BuildType.Client:
                    SelectClientTransport();
                    ConnectClient("localhost");
                    break;
            }
            return;
        }

        switch (SceneLoader.BuildType)
        {
            case BuildType.Server:
                Debug.Log($"[NetworkBootstrapper] Server — starting Multipass " +
                          $"(Tugboat:{_tugboatPort} / Bayou:{_bayouPort})");
                InstanceFinder.ServerManager.StartConnection();
                break;

            case BuildType.Monitor:
            case BuildType.Client:
                SelectClientTransport();
                ConnectClient(_serverAddress);
                break;
        }
    }

    private void OnApplicationQuit()
    {
        // Prevent reconnection logic from firing during Unity shutdown.
        _isQuitting = true;
    }

    private void OnDestroy()
    {
        _isQuitting = true; // also guard against scene unloads

        StopReconnectCoroutine();

        if (InstanceFinder.ServerManager != null)
            InstanceFinder.ServerManager.OnServerConnectionState -= OnServerConnectionState;
        if (InstanceFinder.ClientManager != null)
            InstanceFinder.ClientManager.OnClientConnectionState -= OnClientConnectionState;
    }

    // ── Transport selection ───────────────────────────────────────────────────

    private void SelectClientTransport()
    {
        if (_multipass == null) return;

        switch (SceneLoader.TransportType)
        {
            case TransportType.Tugboat:
                _multipass.SetClientTransport<Tugboat>();
                Debug.Log("[NetworkBootstrapper] Client transport → Tugboat");
                break;

            case TransportType.Bayou:
                bool useWss = !_runLocally;
                var bayou = InstanceFinder.NetworkManager.GetComponentInChildren<Bayou>(true);
                if (bayou != null)
                {
                    bayou.SetUseWSS(useWss);
                    Debug.Log($"[NetworkBootstrapper] Bayou WSS → {useWss}");
                }
                else
                {
                    Debug.LogError("[NetworkBootstrapper] Bayou component not found in children of NetworkManager!");
                }
                _multipass.SetClientTransport<Bayou>();
                Debug.Log("[NetworkBootstrapper] Client transport → Bayou");
                break;
        }
    }

    private void ConnectClient(string tugboatHost)
    {
        switch (SceneLoader.TransportType)
        {
            case TransportType.Tugboat:
                _activeHost = tugboatHost;
                _activePort = _tugboatPort;
                Debug.Log($"[NetworkBootstrapper] Connecting via Tugboat → {_activeHost}:{_activePort}");
                InstanceFinder.ClientManager.StartConnection(_activeHost, _activePort);
                break;

            case TransportType.Bayou:
                _activeHost = _runLocally ? _bayouLocalAddress : _bayouRemoteAddress;
                _activePort = _runLocally ? _bayouPort : _bayouRemotePort;
                Debug.Log($"[NetworkBootstrapper] Connecting via Bayou → {_activeHost}:{_activePort}  WSS={!_runLocally}");
                InstanceFinder.ClientManager.StartConnection(_activeHost, _activePort);
                break;
        }

        // Arm reconnection now that a connection has been attempted.
        _reconnectEnabled = true;
    }

    // ── Reconnection ──────────────────────────────────────────────────────────

    /// <summary>
    /// Waits <see cref="_reconnectDelay"/> seconds, then re-selects the transport and
    /// calls StartConnection with the same host/port used originally.
    /// Respects <see cref="_maxReconnectAttempts"/> (0 = infinite).
    /// </summary>
    private IEnumerator ReconnectCoroutine()
    {
        while (!_isQuitting)
        {
            if (_maxReconnectAttempts > 0 && _reconnectAttempt >= _maxReconnectAttempts)
            {
                Debug.LogWarning($"[NetworkBootstrapper] Reached max reconnect attempts ({_maxReconnectAttempts}). Giving up.");
                yield break;
            }

            _reconnectAttempt++;
            Debug.Log($"[NetworkBootstrapper] Reconnect attempt {_reconnectAttempt}" +
                      (_maxReconnectAttempts > 0 ? $"/{_maxReconnectAttempts}" : "") +
                      $" in {_reconnectDelay}s…");

            yield return new WaitForSeconds(_reconnectDelay);

            if (_isQuitting) yield break;

            // Re-select transport in case Multipass needs it re-confirmed.
            SelectClientTransport();
            InstanceFinder.ClientManager.StartConnection(_activeHost, _activePort);

            // Yield one frame so the connection state has a chance to update before
            // the while condition is re-evaluated.
            yield return null;

            // The coroutine will be stopped externally if the connection succeeds
            // (see OnClientConnectionState). If it remains Stopped, we loop again.
        }
    }

    private void StopReconnectCoroutine()
    {
        if (_reconnectCoroutine != null)
        {
            StopCoroutine(_reconnectCoroutine);
            _reconnectCoroutine = null;
        }
    }

    // ── Connection-state callbacks ────────────────────────────────────────────

    private void OnServerConnectionState(ServerConnectionStateArgs args) =>
        Debug.Log($"[NetworkBootstrapper] Server state → {args.ConnectionState}");

    private void OnClientConnectionState(ClientConnectionStateArgs args)
    {
        Debug.Log($"[NetworkBootstrapper] Client state → {args.ConnectionState}");

        bool connected = args.ConnectionState == LocalConnectionState.Started;

        // ── Overlay ──────────────────────────────────────────────────────────
        GameObject overlay = GameObject.Find("Not Connected Overlay Image");
        if (overlay != null)
        {
            overlay.SetActive(connected);
            Debug.Log("overlay active " + connected);
        }

        // ── Reconnection logic ───────────────────────────────────────────────
        if (connected)
        {
            // Successfully connected — cancel any pending reconnect and reset counter.
            StopReconnectCoroutine();
            _reconnectAttempt = 0;
            Debug.Log("[NetworkBootstrapper] Connected — reconnect loop reset.");
        }
        else if (args.ConnectionState == LocalConnectionState.Stopped
                 && _reconnectEnabled
                 && !_isQuitting
                 && SceneLoader.BuildType != BuildType.Server)
        {
            // Connection dropped or failed to start — begin/continue reconnect loop.
            // Guard against double-starting if a coroutine is somehow already running.
            if (_reconnectCoroutine == null)
            {
                Debug.Log("[NetworkBootstrapper] Connection stopped — starting reconnect loop.");
                _reconnectCoroutine = StartCoroutine(ReconnectCoroutine());
            }
        }
    }

    // ── Diagnostics ───────────────────────────────────────────────────────────

    private void LogTugboatSettings()
    {
        var tugboat = InstanceFinder.NetworkManager.GetComponentInChildren<Tugboat>(true);
        if (tugboat == null)
        {
            Debug.LogWarning("[NetworkBootstrapper] Tugboat child not found inside Multipass for diagnostics.");
            return;
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("[NetworkBootstrapper] ── Tugboat field dump ──");
        foreach (var f in tugboat.GetType()
            .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            sb.AppendLine($"  {f.Name} = {f.GetValue(tugboat)}");
        foreach (var p in tugboat.GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            try   { sb.AppendLine($"  [prop] {p.Name} = {p.GetValue(tugboat)}"); }
            catch { sb.AppendLine($"  [prop] {p.Name} = <error reading>"); }
        }
        sb.AppendLine("[NetworkBootstrapper] ─────────────────────────");
        Debug.Log(sb.ToString());
    }

    private static void LogPortAvailability(ushort port, SocketType socketType, ProtocolType protocol, string label)
    {
        TryBind(IPAddress.Any,     port, socketType, protocol, $"{label} IPv4");
        TryBind(IPAddress.IPv6Any, port, socketType, protocol, $"{label} IPv6");
    }

    private static void TryBind(IPAddress address, ushort port, SocketType socketType, ProtocolType protocol, string label)
    {
        Socket s = null;
        try
        {
            s = new Socket(address.AddressFamily, socketType, protocol);
            if (address == IPAddress.IPv6Any)
                s.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, true);
            s.Bind(new IPEndPoint(address, port));
            Debug.Log($"[NetworkBootstrapper] Pre-flight {label} port {port}: FREE ✓");
        }
        catch (SocketException ex)
        {
            Debug.LogWarning($"[NetworkBootstrapper] Pre-flight {label} port {port}: IN USE ✗  " +
                             $"(SocketError={ex.SocketErrorCode}, HResult=0x{ex.HResult:X})");
        }
        finally { s?.Close(); }
    }
}