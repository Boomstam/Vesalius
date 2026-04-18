using UnityEngine;
using UnityEngine.UI;
using FishNet.Object;

/// <summary>
/// Attach this to a NetworkObject in the Monitor scene alongside a UI Button.
/// </summary>
public class MonitorLogger : NetworkBehaviour
{
    [SerializeField] private string _logMessage = "Button pressed on Monitor!";

    private Button _triggerButton;

    public override void OnStartServer()
    {
        base.OnStartServer();

        // Find the button in the Monitor scene at runtime — it won't exist on clients
        _triggerButton = FindFirstObjectByType<Button>();

        if (_triggerButton != null)
        {
            _triggerButton.onClick.AddListener(OnButtonClicked);
            Debug.Log("[MonitorLogger] Button listener registered.");
        }
        else
        {
            Debug.LogWarning("[MonitorLogger] No Button found in scene.");
        }
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        if (_triggerButton != null)
            _triggerButton.onClick.RemoveListener(OnButtonClicked);
    }

    private void OnButtonClicked()
    {
        if (!IsServerInitialized || !IsSpawned) return;
        RpcLogOnClients(_logMessage);
        RpcLogOnServer(_logMessage);
    }

    [ObserversRpc]
    private void RpcLogOnClients(string message)
    {
        Debug.Log("[MonitorLogger] RPC RECEIVED ON CLIENT → " + message);
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void RpcLogOnServer(string message)
    {
        Debug.Log("[MonitorLogger] RPC RECEIVED ON SERVER→ " + message);
    }
}