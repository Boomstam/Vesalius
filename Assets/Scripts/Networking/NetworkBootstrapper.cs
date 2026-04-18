using UnityEngine;
using FishNet;

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
        if (_runLocally)
        {
            switch (SceneLoader.BuildType)
            {
                case BuildType.MainEditor:
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

            case BuildType.MainEditor:
            case BuildType.Client:
                Debug.Log($"[NetworkBootstrapper] Client — connecting to {_serverAddress}:{_port}");
                InstanceFinder.ClientManager.StartConnection(_serverAddress, _port);
                break;
        }
    }
}