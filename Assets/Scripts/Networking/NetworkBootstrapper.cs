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

    private Multipass _multipass;

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

        if (_runLocally)
        {
            switch (SceneLoader.BuildType)
            {
                case BuildType.Monitor:
                    Debug.Log($"[NetworkBootstrapper] Local mode — starting Multipass server " +
                              $"(Tugboat:{_tugboatPort} / Bayou:{_bayouPort})");
                    InstanceFinder.ServerManager.StartConnection();
                    SelectClientTransport();
                    ConnectClient("localhost");
                    break;

                case BuildType.Server:
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

    private void OnDestroy()
    {
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
                Debug.Log($"[NetworkBootstrapper] Connecting via Tugboat → {tugboatHost}:{_tugboatPort}");
                InstanceFinder.ClientManager.StartConnection(tugboatHost, _tugboatPort);
                break;

            case TransportType.Bayou:
                string host = _runLocally ? _bayouLocalAddress : _bayouRemoteAddress;
                ushort port = _runLocally ? _bayouPort : _bayouRemotePort;
                Debug.Log($"[NetworkBootstrapper] Connecting via Bayou → {host}:{port}  WSS={!_runLocally}");
                InstanceFinder.ClientManager.StartConnection(host, port);
                break;
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

    private void OnServerConnectionState(ServerConnectionStateArgs args) =>
        Debug.Log($"[NetworkBootstrapper] Server state → {args.ConnectionState}");

    private void OnClientConnectionState(ClientConnectionStateArgs args)
    {
        Debug.Log($"[NetworkBootstrapper] Client state → {args.ConnectionState}");
        bool connected = args.ConnectionState == LocalConnectionState.Started;

        GameObject overlay = GameObject.Find("Not Connected Overlay Image");
        if (overlay != null)
        {
            overlay.SetActive(connected);
            Debug.Log("overlay active " + connected);
        }
    }
}