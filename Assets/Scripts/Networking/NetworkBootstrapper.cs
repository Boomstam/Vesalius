using UnityEngine;
using FishNet;

/// <summary>
/// Reads BuildType from SceneLoader and starts the correct network role.
///
///   Monitor      → starts Server, then connects a local Host client to localhost
///   Client       → connects to localhost (ParrelSync clone in Editor)
///   MobileClient → connects to _serverAddress over LAN (Android / iOS)
///
/// </summary>
public class NetworkBootstrapper : MonoBehaviour
{
    [Header("Transport")]
    [Tooltip("Must match the port configured on your Bayou transport component.")]
    [SerializeField] private ushort _port = 7777;

    [Header("LAN Server Address")]
    [Tooltip("The static LAN IP of the Monitor PC assigned via DHCP reservation on the router.\n" +
             "Only used by MobileClient builds — Editor clients always use localhost.")]
    [SerializeField] private string _serverAddress = "192.168.0.10"; // ← update to match your DHCP reservation

    private void Start()
    {
        switch (SceneLoader.BuildType)
        {
            // ── Monitor: start server, then join as host client ──────────────
            case BuildType.Monitor:
                Debug.Log($"[NetworkBootstrapper] Monitor — starting Server + Host on port {_port}");
                InstanceFinder.ServerManager.StartConnection(_port);
                // Connect a local host client so the Monitor is a full participant.
                // "localhost" is correct here regardless of LAN IP.
                InstanceFinder.ClientManager.StartConnection("localhost", _port);
                break;

            // ── Editor clone: connect to localhost (ParrelSync) ───────────────
            case BuildType.Client:
                Debug.Log($"[NetworkBootstrapper] Editor Client — connecting to localhost:{_port}");
                InstanceFinder.ClientManager.StartConnection("localhost", _port);
                break;

            // ── Mobile device: connect to the Monitor's LAN IP ────────────────
            case BuildType.MobileClient:
                Debug.Log($"[NetworkBootstrapper] Mobile Client — connecting to {_serverAddress}:{_port}");
                // Bayou will construct ws://<_serverAddress>:<_port> internally.
                // Ensure the Android manifest allows cleartext WS traffic to this IP
                // (see network_security_config.xml).
                InstanceFinder.ClientManager.StartConnection(_serverAddress, _port);
                break;

            default:
                Debug.LogError($"[NetworkBootstrapper] Unhandled BuildType: {SceneLoader.BuildType}. " +
                               "No network connection was started.");
                break;
        }
    }
}