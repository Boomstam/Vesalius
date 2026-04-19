using System.Net;
using System.Net.Sockets;
using System.Reflection;
using UnityEngine;
using FishNet;
using FishNet.Transporting;
using FishNet.Transporting.Multipass;
using FishNet.Transporting.Tugboat;
// using FishNet.Transporting.Bayou; // Uncomment in Step 2

/// <summary>
/// Starts the correct FishNet role based on BuildType + TransportType resolved by SceneLoader.
///
/// Server  → Multipass starts all child transports simultaneously (Tugboat on _tugboatPort, Bayou on _bayouPort).
/// Monitor → same as server when running locally; otherwise connects as Tugboat client.
/// Client  → selects Tugboat or Bayou via Multipass before connecting.
///
/// Inspector setup required (Step 1):
///   • Remove the standalone Tugboat component from NetworkManager.
///   • Add a Multipass component.
///   • Add Tugboat as a child of Multipass (or as a sibling — whichever Multipass requires).
///   • Add Bayou as a second child (already present but unused until Step 2).
///   • Set ports on each child transport in the inspector to match _tugboatPort / _bayouPort below,
///     OR leave them at defaults and rely on the programmatic port helpers (see Step 2 TODO).
///
/// TODO (Step 2): Uncomment Bayou using directive and SetClientTransport<Bayou>() in SelectClientTransport().
/// TODO (Step 4): Replace _bayouRemoteAddress with your actual wss:// domain.
/// </summary>
public class NetworkBootstrapper : MonoBehaviour
{
    [Header("Ports")]
    [Tooltip("UDP port used by Tugboat. Must match the Tugboat child transport in Multipass.")]
    [SerializeField] private ushort _tugboatPort = 7777;

    [Tooltip("TCP/WS port used by Bayou. Must match the Bayou child transport in Multipass.")]
    [SerializeField] private ushort _bayouPort = 7778;

    [Header("Addresses")]
    [Tooltip("Public IP or domain of the dedicated server (used by Tugboat clients).")]
    [SerializeField] private string _serverAddress = "178.104.196.127";

    [Tooltip("WebSocket URL for local Bayou testing (Step 2). Plain WS, no cert needed.")]
    [SerializeField] private string _bayouLocalAddress = "ws://localhost";

    [Tooltip("WebSocket URL for production Bayou (Step 4). Caddy terminates TLS.")]
    [SerializeField] private string _bayouRemoteAddress = "wss://ws.yourdomain.com"; // TODO: replace in Step 4

    [Tooltip("When enabled, Monitor acts as server+host and all clients connect to localhost.")]
    [SerializeField] private bool _runLocally = false;

    private Multipass _multipass;

    private void Start()
    {
        Debug.Log($"[NetworkBootstrapper] PID={System.Diagnostics.Process.GetCurrentProcess().Id}  " +
                  $"BuildType={SceneLoader.BuildType}  TransportType={SceneLoader.TransportType}");

        _multipass = InstanceFinder.NetworkManager.GetComponent<Multipass>();
        if (_multipass == null)
            Debug.LogError("[NetworkBootstrapper] Multipass component not found on NetworkManager! " +
                           "Add Multipass and configure Tugboat + Bayou as child transports.");

        if (SceneLoader.BuildType == BuildType.Server)
        {
            LogPortAvailability(_tugboatPort, SocketType.Dgram,   ProtocolType.Udp, "Tugboat UDP");
            // Bayou uses TCP/WS — UDP pre-flight does not apply to it.
            // Add a TCP pre-flight here in Step 2 if desired.
            LogTugboatSettings();
        }

        InstanceFinder.ServerManager.OnServerConnectionState += OnServerConnectionState;
        InstanceFinder.ClientManager.OnClientConnectionState += OnClientConnectionState;

        if (_runLocally)
        {
            switch (SceneLoader.BuildType)
            {
                case BuildType.Monitor:
                    // Monitor is server + host locally. Multipass starts both child transports.
                    Debug.Log($"[NetworkBootstrapper] Local mode — starting Multipass server " +
                              $"(Tugboat:{_tugboatPort} / Bayou:{_bayouPort})");
                    InstanceFinder.ServerManager.StartConnection();
                    SelectClientTransport();
                    InstanceFinder.ClientManager.StartConnection("localhost", _tugboatPort);
                    break;

                case BuildType.Server: // Shouldn't occur locally, but guard anyway
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
                // Multipass.StartConnection() starts all registered child transports.
                // Each child transport uses the port configured on its own component in the inspector.
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

    /// <summary>
    /// Points Multipass at the correct child transport before StartConnection is called.
    /// </summary>
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
                // TODO Step 2: Uncomment the line below and remove the fallback warning.
                // _multipass.SetClientTransport<Bayou>();
                Debug.LogWarning("[NetworkBootstrapper] Bayou not yet wired — falling back to Tugboat. (Fix in Step 2)");
                _multipass.SetClientTransport<Tugboat>();
                break;
        }
    }

    /// <summary>
    /// Calls StartConnection with the correct address and port for the active transport.
    /// Bayou expects a ws:// or wss:// URL; Tugboat expects a plain IP/hostname.
    /// </summary>
    private void ConnectClient(string tugboatHost)
    {
        switch (SceneLoader.TransportType)
        {
            case TransportType.Tugboat:
                Debug.Log($"[NetworkBootstrapper] Connecting via Tugboat → {tugboatHost}:{_tugboatPort}");
                InstanceFinder.ClientManager.StartConnection(tugboatHost, _tugboatPort);
                break;

            case TransportType.Bayou:
                // Bayou takes a WebSocket URL. Port is embedded in the URL or defaults to 80/443.
                // In Step 2 this will connect to _bayouLocalAddress; in Step 5 to _bayouRemoteAddress.
                // For now this path is unreachable (Bayou falls back to Tugboat in SelectClientTransport).
                string wsUrl = _runLocally ? _bayouLocalAddress : _bayouRemoteAddress;
                Debug.Log($"[NetworkBootstrapper] Connecting via Bayou → {wsUrl}:{_bayouPort}");
                InstanceFinder.ClientManager.StartConnection(wsUrl, _bayouPort);
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
        if (overlay != null) overlay.SetActive(connected);
    }
}