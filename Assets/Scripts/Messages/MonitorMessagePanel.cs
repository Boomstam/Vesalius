using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Wires monitor message buttons to NetworkedMessageSystem after the networked object exists.
/// </summary>
public class MonitorMessagePanel : MonoBehaviour
{
    [Header("Sound Buttons")]
    [SerializeField] private Button _chimesButton;
    [SerializeField] private Button _anvilButton;
    [SerializeField] private Button _waterphoneButton;
    [SerializeField] private Button _crotalesButton;
    [SerializeField] private Button _cymbalButton;
    [SerializeField] private Button _waterStationButton;

    [Header("Control Buttons")]
    [SerializeField] private Button _resetDeckButton;
    [SerializeField] private Button _hardCutButton;

    private NetworkedMessageSystem _nms;
    private UnityEngine.Events.UnityAction _sendChimes;
    private UnityEngine.Events.UnityAction _sendAnvil;
    private UnityEngine.Events.UnityAction _sendWaterphone;
    private UnityEngine.Events.UnityAction _sendCrotales;
    private UnityEngine.Events.UnityAction _sendCymbal;
    private UnityEngine.Events.UnityAction _sendWaterStation;
    private UnityEngine.Events.UnityAction _resetDeck;
    private UnityEngine.Events.UnityAction _hardCut;

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
        while (_nms == null)
        {
            _nms = FindObjectOfType<NetworkedMessageSystem>();

            if (_nms == null)
                yield return new WaitForSeconds(0.5f);
        }

        Debug.Log("[MonitorMessagePanel] Wiring buttons.");

        _sendChimes = () => _nms.SendMessageToTargets("chimes");
        _sendAnvil = () => _nms.SendMessageToTargets("anvil");
        _sendWaterphone = () => _nms.SendMessageToTargets("waterphone");
        _sendCrotales = () => _nms.SendMessageToTargets("crotales");
        _sendCymbal = () => _nms.SendMessageToTargets("cymbal");
        _sendWaterStation = () => _nms.SendMessageToTargets("water station");
        _resetDeck = () => _nms.ResetDeck();
        _hardCut = () => _nms.HardCutAll();

        AddListener(_chimesButton, _sendChimes);
        AddListener(_anvilButton, _sendAnvil);
        AddListener(_waterphoneButton, _sendWaterphone);
        AddListener(_crotalesButton, _sendCrotales);
        AddListener(_cymbalButton, _sendCymbal);
        AddListener(_waterStationButton, _sendWaterStation);
        AddListener(_resetDeckButton, _resetDeck);
        AddListener(_hardCutButton, _hardCut);
    }

    private static void AddListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
            button.onClick.AddListener(action);
    }

    private void RemoveListeners()
    {
        RemoveListener(_chimesButton, _sendChimes);
        RemoveListener(_anvilButton, _sendAnvil);
        RemoveListener(_waterphoneButton, _sendWaterphone);
        RemoveListener(_crotalesButton, _sendCrotales);
        RemoveListener(_cymbalButton, _sendCymbal);
        RemoveListener(_waterStationButton, _sendWaterStation);
        RemoveListener(_resetDeckButton, _resetDeck);
        RemoveListener(_hardCutButton, _hardCut);
    }

    private static void RemoveListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null && action != null)
            button.onClick.RemoveListener(action);
    }
}
