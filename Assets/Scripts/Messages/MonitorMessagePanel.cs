using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Wires monitor message buttons to NetworkedMessageSystem after the networked object exists.
/// </summary>
public class MonitorMessagePanel : MonoBehaviour
{
    [Header("Sound Buttons")]
    [SerializeField] private Button _chimesButton;
    [SerializeField] private Button _clavesButton;
    [SerializeField] private Button _anvilButton;
    [SerializeField] private Button _waterphoneButton;
    [SerializeField] private Button _crotalesButton;
    [SerializeField] private Button _cymbalButton;
    [SerializeField] private Button _waterStationButton;

    [Header("Control Buttons")]
    [SerializeField] private Button _resetDeckButton;
    [SerializeField] private Button _hardCutButton;
    [SerializeField] private Button _groupMessageButton;

    private NetworkedMessageSystem _nms;
    private NetworkedMonitor _networkedMonitor;
    private UnityAction _sendChimes;
    private UnityAction _sendClaves;
    private UnityAction _sendAnvil;
    private UnityAction _sendWaterphone;
    private UnityAction _sendCrotales;
    private UnityAction _sendCymbal;
    private UnityAction _sendWaterStation;
    private UnityAction _resetDeck;
    private UnityAction _hardCut;
    private UnityAction _groupMessage;

    private void Start()
    {
        if (SceneLoader.BuildType == BuildType.Monitor)
            StartCoroutine(FindAndWireCoroutine());
    }

    private void OnDestroy()
    {
        RemoveListeners();
    }

    private IEnumerator FindAndWireCoroutine()
    {
        ResolveSceneButtons();

        while (_nms == null || _networkedMonitor == null)
        {
            if (_nms == null)
                _nms = FindObjectOfType<NetworkedMessageSystem>();

            if (_networkedMonitor == null)
                _networkedMonitor = FindObjectOfType<NetworkedMonitor>();

            if (_nms == null || _networkedMonitor == null)
                yield return new WaitForSeconds(0.5f);
        }

        Debug.Log("[MonitorMessagePanel] Wiring buttons.");

        _sendChimes = () => _nms.SendMessageToTargets("chimes");
        _sendClaves = () => _nms.SendMessageToTargets("claves");
        _sendAnvil = () => _nms.SendMessageToTargets("anvil");
        _sendWaterphone = () => _nms.SendMessageToTargets("waterphone");
        _sendCrotales = () => _nms.SendMessageToTargets("crotales");
        _sendCymbal = () => _nms.SendMessageToTargets("cymbal");
        _sendWaterStation = () => _nms.SendMessageToTargets("water station");
        _resetDeck = () => _nms.ResetDeck();
        _hardCut = () => _nms.HardCutAll();
        _groupMessage = () => _networkedMonitor.TriggerGroupMessageMode();

        AddListener(_chimesButton, _sendChimes);
        AddListener(_clavesButton, _sendClaves);
        AddListener(_anvilButton, _sendAnvil);
        AddListener(_waterphoneButton, _sendWaterphone);
        AddListener(_crotalesButton, _sendCrotales);
        AddListener(_cymbalButton, _sendCymbal);
        AddListener(_waterStationButton, _sendWaterStation);
        AddListener(_resetDeckButton, _resetDeck);
        AddListener(_hardCutButton, _hardCut);
        AddListener(_groupMessageButton, _groupMessage);
    }

    private static void AddListener(Button button, UnityAction action)
    {
        if (button != null)
            button.onClick.AddListener(action);
    }

    private void RemoveListeners()
    {
        RemoveListener(_chimesButton, _sendChimes);
        RemoveListener(_clavesButton, _sendClaves);
        RemoveListener(_anvilButton, _sendAnvil);
        RemoveListener(_waterphoneButton, _sendWaterphone);
        RemoveListener(_crotalesButton, _sendCrotales);
        RemoveListener(_cymbalButton, _sendCymbal);
        RemoveListener(_waterStationButton, _sendWaterStation);
        RemoveListener(_resetDeckButton, _resetDeck);
        RemoveListener(_hardCutButton, _hardCut);
        RemoveListener(_groupMessageButton, _groupMessage);
    }

    private static void RemoveListener(Button button, UnityAction action)
    {
        if (button != null && action != null)
            button.onClick.RemoveListener(action);
    }

    private void ResolveSceneButtons()
    {
        _groupMessageButton = ResolveButton(_groupMessageButton, "Go To Your Color Button", "Group Message Button");
    }

    private static Button ResolveButton(Button current, params string[] objectNames)
    {
        if (current != null)
            return current;

        foreach (string objectName in objectNames)
        {
            GameObject existing = GameObject.Find(objectName);
            if (existing != null && existing.TryGetComponent(out Button button))
                return button;
        }

        return null;
    }
}
