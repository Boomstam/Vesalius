using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MonitorColorOverlayUI : MonoBehaviour
{
    [Header("Master Opacity")]
    [SerializeField] private Toggle masterOpacityToggle;
    [SerializeField] private Slider masterOpacitySlider;
    [SerializeField] private Button fadeInButton;
    [SerializeField] private Button fadeOutButton;
    [SerializeField] private Button cutToBlackButton;

    [Header("Heartbeat")]
    [SerializeField] private Toggle heartbeatToggle;

    private NetworkedMonitor networkedMonitor;

    private void Start()
    {
        if (SceneLoader.BuildType != BuildType.Monitor)
        {
            gameObject.SetActive(false);
            return;
        }

        SetAllInteractable(false);
        StartCoroutine(FindNetworkedMonitorCoroutine());
    }

    private IEnumerator FindNetworkedMonitorCoroutine()
    {
        while (networkedMonitor == null)
        {
            networkedMonitor = FindObjectOfType<NetworkedMonitor>();
            if (networkedMonitor == null)
                yield return new WaitForSeconds(0.5f);
        }

        Debug.Log("[MonitorColorOverlayUI] NetworkedMonitor found — syncing UI state.");
        SyncStateFromServer(networkedMonitor);
        WireListeners();
        SetAllInteractable(true);
        RefreshOpacityControls(masterOpacityToggle != null && masterOpacityToggle.isOn);
    }

    /// <summary>
    /// Mirrors current SyncVar values into controls without triggering listeners,
    /// so the UI reflects live server state on connect without sending redundant RPCs.
    /// </summary>
    private void SyncStateFromServer(NetworkedMonitor nm)
    {
        if (masterOpacityToggle != null)
            masterOpacityToggle.SetIsOnWithoutNotify(nm.MasterOpacityActive);

        if (masterOpacitySlider != null)
            masterOpacitySlider.SetValueWithoutNotify(nm.MasterOpacityValue);

        if (heartbeatToggle != null)
            heartbeatToggle.SetIsOnWithoutNotify(nm.HeartbeatActive);
    }

    private void WireListeners()
    {
        if (masterOpacityToggle != null) masterOpacityToggle.onValueChanged.AddListener(OnMasterOpacityToggleChanged);
        if (masterOpacitySlider != null) masterOpacitySlider.onValueChanged.AddListener(OnMasterOpacitySliderChanged);
        if (fadeInButton != null)        fadeInButton.onClick.AddListener(OnFadeInClicked);
        if (fadeOutButton != null)       fadeOutButton.onClick.AddListener(OnFadeOutClicked);
        if (cutToBlackButton != null)    cutToBlackButton.onClick.AddListener(OnCutToBlackClicked);
        if (heartbeatToggle != null)     heartbeatToggle.onValueChanged.AddListener(OnHeartbeatToggleChanged);
    }

    private void OnDestroy()
    {
        if (masterOpacityToggle != null) masterOpacityToggle.onValueChanged.RemoveListener(OnMasterOpacityToggleChanged);
        if (masterOpacitySlider != null) masterOpacitySlider.onValueChanged.RemoveListener(OnMasterOpacitySliderChanged);
        if (fadeInButton != null)        fadeInButton.onClick.RemoveListener(OnFadeInClicked);
        if (fadeOutButton != null)       fadeOutButton.onClick.RemoveListener(OnFadeOutClicked);
        if (cutToBlackButton != null)    cutToBlackButton.onClick.RemoveListener(OnCutToBlackClicked);
        if (heartbeatToggle != null)     heartbeatToggle.onValueChanged.RemoveListener(OnHeartbeatToggleChanged);
    }

    private void OnMasterOpacityToggleChanged(bool value)
    {
        if (networkedMonitor == null) return;
        networkedMonitor.SetColorMasterOpacityActive(value);
        RefreshOpacityControls(value);
    }

    private void OnMasterOpacitySliderChanged(float value)
    {
        if (networkedMonitor == null || masterOpacityToggle == null || !masterOpacityToggle.isOn) return;
        networkedMonitor.SetColorMasterOpacity(value);
    }

    private void OnFadeInClicked()
    {
        if (networkedMonitor == null) return;
        networkedMonitor.TriggerColorFadeIn();
        if (masterOpacitySlider != null) masterOpacitySlider.SetValueWithoutNotify(1f);
    }

    private void OnFadeOutClicked()
    {
        if (networkedMonitor == null) return;
        networkedMonitor.TriggerColorFadeOut();
        if (masterOpacitySlider != null) masterOpacitySlider.SetValueWithoutNotify(0f);
    }

    private void OnCutToBlackClicked()
    {
        if (networkedMonitor == null) return;
        networkedMonitor.TriggerColorCutToBlack();
        if (masterOpacitySlider != null) masterOpacitySlider.SetValueWithoutNotify(0f);
    }

    private void OnHeartbeatToggleChanged(bool value)
    {
        if (networkedMonitor == null) return;
        networkedMonitor.SetHeartbeatActive(value);
    }

    private void RefreshOpacityControls(bool masterActive)
    {
        if (masterOpacitySlider != null) masterOpacitySlider.interactable = masterActive;
        if (fadeInButton != null)        fadeInButton.interactable = masterActive;
        if (fadeOutButton != null)       fadeOutButton.interactable = masterActive;
        if (cutToBlackButton != null)    cutToBlackButton.interactable = masterActive;
    }

    private void SetAllInteractable(bool value)
    {
        if (masterOpacityToggle != null) masterOpacityToggle.interactable = value;
        if (heartbeatToggle != null)     heartbeatToggle.interactable = value;
        RefreshOpacityControls(value && masterOpacityToggle != null && masterOpacityToggle.isOn);
    }
}