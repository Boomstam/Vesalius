using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using FishNet.Object;
using TMPro;

/// <summary>
/// Attach this to a NetworkObject in the Monitor scene alongside a UI Button.
/// Only registers the button listener when running as MainEditor (Monitor build).
/// </summary>
public class MonitorLogger : NetworkBehaviour
{
    [SerializeField] private string _logMessage;
    [SerializeField] private string _buttonObjectName = "Monitor Log Button";
    [SerializeField] private Button _buttonOverride;

    private Button _triggerButton;

    private void Start()
    {
        if (SceneLoader.BuildType != BuildType.Monitor)
        {
            Debug.Log("[MonitorLogger] Not a Monitor build — skipping button setup.");
            return;
        }
        StartCoroutine(FindButtonAfterDelay());
    }

    private IEnumerator FindButtonAfterDelay()
    {
        yield return new WaitForSeconds(5);

        _triggerButton = ResolveTriggerButton();

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

    private Button ResolveTriggerButton()
    {
        if (_buttonOverride != null)
            return _buttonOverride;

        if (string.IsNullOrWhiteSpace(_buttonObjectName))
            return null;

        GameObject buttonObject = GameObject.Find(_buttonObjectName);
        if (buttonObject == null)
        {
            Debug.LogWarning($"[MonitorLogger] Button '{_buttonObjectName}' not found in scene.");
            return null;
        }

        Button button = buttonObject.GetComponent<Button>();
        if (button == null)
            Debug.LogWarning($"[MonitorLogger] GameObject '{_buttonObjectName}' has no Button component.");

        return button;
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        
        if (_triggerButton != null)
            _triggerButton.onClick.RemoveListener(OnButtonClicked);
    }

    private void OnButtonClicked()
    {
        Debug.Log($"[MonitorLogger] Button clicked locally.");
        
        RpcLogOnClients(_logMessage);
        RpcLogOnServer(_logMessage);
    }

    [ObserversRpc]
    private void RpcLogOnClients(string message)
    {
        Debug.Log("[MonitorLogger] RPC RECEIVED ON CLIENT → " + message);
        
        GameObject notConnectedOverlayImage = GameObject.Find("Debug Text");

        if (notConnectedOverlayImage != null)
            notConnectedOverlayImage.GetComponent<TextMeshProUGUI>().text  = message;
    }

    [ServerRpc(RequireOwnership = false)]
    private void RpcLogOnServer(string message)
    {
        Debug.Log("[MonitorLogger] RPC RECEIVED ON SERVER → " + message);
        
        RpcLogOnClients("THIS MESSAGE IS A ROUND TRIP FROM THE SERVER!");
    }
}
