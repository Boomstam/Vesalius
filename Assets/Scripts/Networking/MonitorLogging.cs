using UnityEngine;
using UnityEngine.UI;
using FishNet.Object;

/// <summary>
/// Attach this to a NetworkObject in the Monitor scene alongside a UI Button.
/// </summary>
public class MonitorLogger : NetworkBehaviour
{
    [Tooltip("Assign the UI Button in the Monitor scene.")]
    [SerializeField] private Button _triggerButton;

    [Tooltip("Message that will appear in client logs when the button is pressed.")]
    [SerializeField] private string _logMessage = "Button pressed on Monitor!";

    // ------------------------------------------------------------------ //
    //  General lifecycle — fires on both server and client
    // ------------------------------------------------------------------ //

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        Debug.Log($"[MonitorLogger] OnStartNetwork — IsServerInitialized={IsServerInitialized} IsClientInitialized={IsClientInitialized} ObjectId={ObjectId}");
    }

    // ------------------------------------------------------------------ //
    //  Server lifecycle
    // ------------------------------------------------------------------ //

    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log($"[MonitorLogger] OnStartServer fired. Button assigned: {_triggerButton != null}");

        if (_triggerButton != null)
        {
            _triggerButton.onClick.AddListener(OnButtonClicked);
            Debug.Log("[MonitorLogger] Button listener registered.");
        }
        else
        {
            Debug.LogWarning("[MonitorLogger] No Button assigned — wire it up in the Inspector.");
        }
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        if (_triggerButton != null)
            _triggerButton.onClick.RemoveListener(OnButtonClicked);
    }

    // ------------------------------------------------------------------ //
    //  Client lifecycle
    // ------------------------------------------------------------------ //

    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log($"[MonitorLogger] OnStartClient fired. IsOwner={IsOwner} ObjectId={ObjectId}");
    }

    // ------------------------------------------------------------------ //
    //  Button callback (server only)
    // ------------------------------------------------------------------ //

    private void OnButtonClicked()
    {
        Debug.Log($"[MonitorLogger] Button clicked. IsServerInitialized={IsServerInitialized} IsSpawned={IsSpawned}");

        if (!IsServerInitialized)
        {
            Debug.LogWarning("[MonitorLogger] Button clicked but NOT server — RPC not sent.");
            return;
        }

        if (!IsSpawned)
        {
            Debug.LogWarning("[MonitorLogger] Button clicked but NetworkObject is NOT spawned — RPC not sent.");
            return;
        }

        Debug.Log("[MonitorLogger] Sending RPC to all observers...");
        RpcLogOnClients(_logMessage);
    }

    // ------------------------------------------------------------------ //
    //  RPC — runs on every observer (all clients, including host)
    // ------------------------------------------------------------------ //

    [ObserversRpc]
    private void RpcLogOnClients(string message)
    {
        Debug.Log("[MonitorLogger] RPC RECEIVED on client → " + message);
    }
}