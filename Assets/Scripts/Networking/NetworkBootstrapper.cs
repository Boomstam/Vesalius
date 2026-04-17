using UnityEngine;
using FishNet;

/// <summary>
/// Place on any GameObject in the Monitor scene.
/// Reads BuildType from SceneLoader to decide whether to start as Server+Host or Client-only.
/// Ensure SceneLoader runs before this script (set Script Execution Order if needed).
/// </summary>
public class NetworkBootstrapper : MonoBehaviour
{
    [Tooltip("Must match the port configured on your Bayou transport component.")]
    [SerializeField] private ushort _port = 7777;

    private void Start()
    {
        if (SceneLoader.BuildType == BuildType.Client)
        {
            Debug.Log("[FishNet] Client build — connecting to localhost:" + _port);
            InstanceFinder.ClientManager.StartConnection("localhost", _port);
        }
        else
        {
            Debug.Log("[FishNet] Monitor build — starting Server + Host on port " + _port);
            InstanceFinder.ServerManager.StartConnection(_port);
            // Host: also connect a local client so the main editor is a full participant
            InstanceFinder.ClientManager.StartConnection("localhost", _port);
        }
    }
}