using UnityEngine;
using UnityEngine.UI;
using FishNet.Object;
using TMPro;
using System.Collections; // Required for Coroutines

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
        // Start the delayed initialization process
        StartCoroutine(DelayedButtonSetup());
    }

    private IEnumerator DelayedButtonSetup()
    {
        // Wait for 5 seconds
        yield return new WaitForSeconds(5f);

        Debug.Log("[MonitorLogger] 5 seconds elapsed. Initializing button listener...");

        // Find the button in the Monitor scene at runtime
        _triggerButton = GameObject.Find("Debug Button").GetComponent<Button>();

        if (_triggerButton != null)
        {
            _triggerButton.onClick.AddListener(OnButtonClicked);
            Debug.Log("[MonitorLogger] Button listener registered.");
        }
        else
        {
            Debug.LogWarning("[MonitorLogger] No Button found in scene after delay.");
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
        // Safety check to ensure we are still server and object is spawned
        if (!IsServerStarted || !IsSpawned) return;
        RpcLogOnClients(_logMessage);
    }

    [ObserversRpc]
    private void RpcLogOnClients(string message)
    {
        Debug.Log("[MonitorLogger] RPC RECEIVED → " + message);
        
        GameObject debugObj = GameObject.Find("Debug Text");
        if (debugObj != null)
        {
            TextMeshProUGUI comp = debugObj.GetComponent<TextMeshProUGUI>();
            if (comp != null) comp.text = message;
        }
    }
}