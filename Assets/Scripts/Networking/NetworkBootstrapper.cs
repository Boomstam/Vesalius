using System.Net;
using System.Net.Sockets;
using System.Reflection;
using UnityEngine;
using FishNet;
using FishNet.Transporting;
using FishNet.Transporting.Tugboat;

/// <summary>
/// Starts the correct FishNet role based on BuildType resolved by SceneLoader.
/// Uses Tugboat (UDP) transport.
///
/// TODO: Replace the IP string with your domain once DNS is configured.
/// </summary>
public class NetworkBootstrapper : MonoBehaviour
{
    [Tooltip("Must match the port on your Tugboat transport component.")]
    [SerializeField] private ushort _port = 7777;

    [Tooltip("Public IP or domain of the dedicated server.")]
    [SerializeField] private string _serverAddress = "178.104.196.127";

    [Tooltip("When enabled, MainEditor acts as server+host and all clients connect to localhost. Useful for local testing without Hetzner.")]
    [SerializeField] private bool _runLocally = false;

    private void Start()
    {
        // ── Diagnostic header ────────────────────────────────────────────────
        Debug.Log($"[NetworkBootstrapper] Process ID: {System.Diagnostics.Process.GetCurrentProcess().Id}");
        Debug.Log($"[NetworkBootstrapper] BuildType: {SceneLoader.BuildType}");

        if (SceneLoader.BuildType == BuildType.Server)
        {
            LogPortAvailability(_port);
            LogTugboatSettings();
        }
        // ─────────────────────────────────────────────────────────────────────

        // Subscribe to server state changes so we know exactly what FishNet sees
        InstanceFinder.ServerManager.OnServerConnectionState += OnServerConnectionState;
        
        InstanceFinder.ClientManager.OnClientConnectionState += OnClientConnectionState;

        if (_runLocally)
        {
            switch (SceneLoader.BuildType)
            {
                case BuildType.Monitor:
                    Debug.Log($"[NetworkBootstrapper] Local mode — starting Server + Host on port {_port}");
                    InstanceFinder.ServerManager.StartConnection(_port);
                    InstanceFinder.ClientManager.StartConnection("localhost", _port);
                    break;

                case BuildType.Server:
                case BuildType.Client:
                    Debug.Log($"[NetworkBootstrapper] Local mode — connecting to localhost:{_port}");
                    InstanceFinder.ClientManager.StartConnection("localhost", _port);
                    break;
            }
            return;
        }

        switch (SceneLoader.BuildType)
        {
            case BuildType.Server:
                Debug.Log($"[NetworkBootstrapper] Server build — starting server on port {_port}");
                InstanceFinder.ServerManager.StartConnection(_port);
                break;

            case BuildType.Monitor:
            case BuildType.Client:
                Debug.Log($"[NetworkBootstrapper] Client — connecting to {_serverAddress}:{_port}");
                InstanceFinder.ClientManager.StartConnection(_serverAddress, _port);
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

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Dumps every field on the Tugboat component via reflection so we can see
    /// exactly what IPv6 / dual-mode settings LiteNetLib will receive.
    /// </summary>
    private void LogTugboatSettings()
    {
        var tugboat = InstanceFinder.NetworkManager.GetComponent<Tugboat>();
        if (tugboat == null)
            tugboat = InstanceFinder.NetworkManager.GetComponentInChildren<Tugboat>(true);

        if (tugboat == null)
        {
            Debug.LogWarning("[NetworkBootstrapper] Could not find Tugboat component for diagnostics.");
            return;
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("[NetworkBootstrapper] ── Tugboat component field dump ──");

        foreach (var f in tugboat.GetType()
            .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            sb.AppendLine($"  {f.Name} = {f.GetValue(tugboat)}");
        }

        foreach (var p in tugboat.GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            try   { sb.AppendLine($"  [prop] {p.Name} = {p.GetValue(tugboat)}"); }
            catch { sb.AppendLine($"  [prop] {p.Name} = <error reading>"); }
        }

        sb.AppendLine("[NetworkBootstrapper] ─────────────────────────────────");
        Debug.Log(sb.ToString());
    }

    /// <summary>
    /// Attempts to bind a UDP socket on the target port before FishNet does.
    /// Logs whether the port is free or already occupied, and by what.
    /// </summary>
    private static void LogPortAvailability(ushort port)
    {
        // IPv4 check
        TryBindUdp(IPAddress.Any, port, "IPv4");

        // IPv6 check (LiteNetLib opens both when IPv6 is available)
        TryBindUdp(IPAddress.IPv6Any, port, "IPv6");
    }

    private static void TryBindUdp(IPAddress address, ushort port, string label)
    {
        Socket s = null;
        try
        {
            s = new Socket(
                address.AddressFamily,
                SocketType.Dgram,
                ProtocolType.Udp);

            if (address == IPAddress.IPv6Any)
                s.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, true);

            s.Bind(new IPEndPoint(address, port));
            Debug.Log($"[NetworkBootstrapper] Pre-flight {label} UDP port {port}: FREE ✓");
        }
        catch (SocketException ex)
        {
            // Port is in use — try to identify the holder via active listeners
            Debug.LogWarning(
                $"[NetworkBootstrapper] Pre-flight {label} UDP port {port}: ALREADY IN USE ✗  " +
                $"(SocketError={ex.SocketErrorCode}, HResult=0x{ex.HResult:X})");
        }
        finally
        {
            s?.Close();
        }
    }

    /// <summary>
    /// Logs every transport state transition so failures are visible without
    /// digging through LiteNetLib internals.
    /// </summary>
    private void OnServerConnectionState(ServerConnectionStateArgs args)
    {
        Debug.Log($"[NetworkBootstrapper] Server connection state → {args.ConnectionState}");
    }
    
    private void OnClientConnectionState(ClientConnectionStateArgs args)
    {
        Debug.Log($"[NetworkBootstrapper] Client connection state → {args.ConnectionState}");
        
        bool connected = (args.ConnectionState == LocalConnectionState.Started);

        GameObject notConnectedOverlayImage = GameObject.Find("Not Connected Overlay Image");
        
        if(notConnectedOverlayImage != null)
            notConnectedOverlayImage. SetActive(connected);
    }
}