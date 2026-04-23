using System.Collections;
using TMPro;
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

/// <summary>
/// Networked state authority for the monitor.
/// Syncs current part number to all clients (drives ViewManager).
/// Syncs audio mode booleans; callbacks on clients drive AudioManager.
/// Syncs color overlay state; callbacks on clients drive ColorOverlay.
/// Master volume, audio fades, and color overlay fades are fire-and-forget ObserversRpcs.
///
/// On Monitor builds, OnStartClient() calls Instances.MonitorUI?.Init(this) directly
/// so the UI is synced and wired the moment the NetworkObject is ready —
/// no polling coroutine required.
/// </summary>
public class NetworkedMonitor : NetworkBehaviour
{
    private ViewManager _viewManager;
    private TMP_InputField _partInputField;

    private readonly SyncVar<int> _currentPart = new SyncVar<int>(-1);
    private readonly SyncVar<bool> _completeAnatomyMode = new SyncVar<bool>(false);

    private readonly SyncVar<bool> _shouldPlayOrgansOfNutrition = new SyncVar<bool>(false);
    private readonly SyncVar<bool> _shouldPlayOrgansOfGeneration = new SyncVar<bool>(false);
    private readonly SyncVar<bool> _shouldPlayHeart = new SyncVar<bool>(false);

    [Header("Heartbeat Config")]
    [Tooltip("Color at the trough of each heartbeat cycle (rest state).")]
    [SerializeField] private Color _heartbeatStartColor = new Color(0f, 0f, 0f, 0f);
    [Tooltip("Color at the peak of each heartbeat pulse.")]
    [SerializeField] private Color _heartbeatEndColor = new Color(0.8f, 0f, 0f, 1f);
    [Tooltip("Duration of one full heartbeat cycle in seconds.")]
    [SerializeField] private float _heartbeatBeatTime = 0.8f;

    private readonly SyncVar<bool> _masterOpacityActive = new SyncVar<bool>(false);
    private readonly SyncVar<float> _masterOpacityValue = new SyncVar<float>(1f);
    private readonly SyncVar<bool> _heartbeatActive = new SyncVar<bool>(false);
    private readonly SyncVar<Color> _heartbeatStartColorSync = new SyncVar<Color>();
    private readonly SyncVar<Color> _heartbeatEndColorSync = new SyncVar<Color>();
    private readonly SyncVar<float> _heartbeatBeatTimeSync = new SyncVar<float>(0.8f);

    // ── Public state accessors (for MonitorUI sync-on-connect) ────────────────

    public bool CompleteAnatomyMode          => _completeAnatomyMode.Value;
    public bool ShouldPlayOrgansOfNutrition  => _shouldPlayOrgansOfNutrition.Value;
    public bool ShouldPlayOrgansOfGeneration => _shouldPlayOrgansOfGeneration.Value;
    public bool ShouldPlayHeart              => _shouldPlayHeart.Value;
    public bool MasterOpacityActive          => _masterOpacityActive.Value;
    public float MasterOpacityValue          => _masterOpacityValue.Value;
    public bool HeartbeatActive              => _heartbeatActive.Value;

    public override void OnStartClient()
    {
        base.OnStartClient();

        _currentPart.OnChange += OnCurrentPartChanged;
        _completeAnatomyMode.OnChange += OnCompleteAnatomyModeChanged;

        _shouldPlayOrgansOfNutrition.OnChange += OnShouldPlayOrgansOfNutritionChanged;
        _shouldPlayOrgansOfGeneration.OnChange += OnShouldPlayOrgansOfGenerationChanged;
        _shouldPlayHeart.OnChange += OnShouldPlayHeartChanged;

        _masterOpacityActive.OnChange += OnMasterOpacityActiveChanged;
        _masterOpacityValue.OnChange += OnMasterOpacityValueChanged;
        _heartbeatActive.OnChange += OnHeartbeatActiveChanged;

        if (SceneLoader.BuildType == BuildType.Monitor)
        {
            // Initialise the UI directly — SyncVar values are valid at this point.
            MonitorUI monitorUI = Instances.MonitorUI;
            Debug.Log($"[NetworkedMonitor] OnStartClient (Monitor build) — MonitorUI found: {monitorUI != null}");
            if (monitorUI != null)
                monitorUI.Init(this);
            else
                Debug.LogWarning("[NetworkedMonitor] MonitorUI not found in scene — UI will not be initialised.");
            StartCoroutine(FindAndSubscribeInputFieldCoroutine());
        }
        else
        {
            StartCoroutine(FindViewManagerCoroutine());
        }
    }

    public override void OnStopClient()
    {
        base.OnStopClient();

        _currentPart.OnChange -= OnCurrentPartChanged;
        _completeAnatomyMode.OnChange -= OnCompleteAnatomyModeChanged;

        _shouldPlayOrgansOfNutrition.OnChange -= OnShouldPlayOrgansOfNutritionChanged;
        _shouldPlayOrgansOfGeneration.OnChange -= OnShouldPlayOrgansOfGenerationChanged;
        _shouldPlayHeart.OnChange -= OnShouldPlayHeartChanged;

        _masterOpacityActive.OnChange -= OnMasterOpacityActiveChanged;
        _masterOpacityValue.OnChange -= OnMasterOpacityValueChanged;
        _heartbeatActive.OnChange -= OnHeartbeatActiveChanged;

        UnsubscribeInputField();
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetShouldPlayOrgansOfNutrition(bool value)
    {
        _shouldPlayOrgansOfNutrition.Value = value;
    }

    private void OnShouldPlayOrgansOfNutritionChanged(bool prev, bool next, bool asServer)
    {
        if (SceneLoader.BuildType != BuildType.Client) return;

        if (next) Instances.AudioManager.PlayOrgansOfNutrition();
        else Instances.AudioManager.StopOrgansOfNutrition();
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetShouldPlayOrgansOfGeneration(bool value)
    {
        _shouldPlayOrgansOfGeneration.Value = value;
    }

    private void OnShouldPlayOrgansOfGenerationChanged(bool prev, bool next, bool asServer)
    {
        if (SceneLoader.BuildType != BuildType.Client) return;

        if (next) Instances.AudioManager.PlayOrgansOfGeneration();
        else Instances.AudioManager.StopOrgansOfGeneration();
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetShouldPlayHeart(bool value)
    {
        _shouldPlayHeart.Value = value;
    }

    private void OnShouldPlayHeartChanged(bool prev, bool next, bool asServer)
    {
        if (SceneLoader.BuildType != BuildType.Client) return;

        if (next) Instances.AudioManager.PlayHeart();
        else Instances.AudioManager.StopHeart();
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetMasterVolume(float value) => RpcSetMasterVolume(value);

    [ObserversRpc]
    private void RpcSetMasterVolume(float value)
    {
        if (SceneLoader.BuildType != BuildType.Client) return;
        Instances.AudioManager.SetMasterVolume(value);
    }

    [ServerRpc(RequireOwnership = false)]
    public void TriggerMasterFadeIn() => RpcTriggerMasterFadeIn();

    [ObserversRpc]
    private void RpcTriggerMasterFadeIn()
    {
        if (SceneLoader.BuildType != BuildType.Client) return;
        Instances.AudioManager.FadeIn();
    }

    [ServerRpc(RequireOwnership = false)]
    public void TriggerMasterFadeOut() => RpcTriggerMasterFadeOut();

    [ObserversRpc]
    private void RpcTriggerMasterFadeOut()
    {
        if (SceneLoader.BuildType != BuildType.Client) return;
        Instances.AudioManager.FadeOut();
    }

    [ServerRpc(RequireOwnership = false)]
    public void TriggerMasterMute() => RpcTriggerMasterMute();

    [ObserversRpc]
    private void RpcTriggerMasterMute()
    {
        if (SceneLoader.BuildType != BuildType.Client) return;
        Instances.AudioManager.MuteImmediate();
    }

    [ServerRpc(RequireOwnership = false)]
    public void TriggerMasterReset() => RpcTriggerMasterReset();

    [ObserversRpc]
    private void RpcTriggerMasterReset()
    {
        if (SceneLoader.BuildType != BuildType.Client) return;
        Instances.AudioManager.ResetImmediate();
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetColorMasterOpacityActive(bool value)
    {
        _masterOpacityActive.Value = value;
    }

    private void OnMasterOpacityActiveChanged(bool prev, bool next, bool asServer)
    {
        if (SceneLoader.BuildType != BuildType.Client || Instances.ColorOverlay == null) return;
        Instances.ColorOverlay.SetMasterOpacityActive(next);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetColorMasterOpacity(float value)
    {
        _masterOpacityValue.Value = Mathf.Clamp01(value);
    }

    private void OnMasterOpacityValueChanged(float prev, float next, bool asServer)
    {
        if (SceneLoader.BuildType != BuildType.Client || Instances.ColorOverlay == null) return;
        Instances.ColorOverlay.SetMasterOpacity(next);
    }

    [ServerRpc(RequireOwnership = false)]
    public void TriggerColorFadeIn() => RpcTriggerColorFadeIn();

    [ObserversRpc]
    private void RpcTriggerColorFadeIn()
    {
        if (SceneLoader.BuildType != BuildType.Client || Instances.ColorOverlay == null) return;
        Instances.ColorOverlay.TriggerMasterFadeIn();
    }

    [ServerRpc(RequireOwnership = false)]
    public void TriggerColorFadeOut() => RpcTriggerColorFadeOut();

    [ObserversRpc]
    private void RpcTriggerColorFadeOut()
    {
        if (SceneLoader.BuildType != BuildType.Client || Instances.ColorOverlay == null) return;
        Instances.ColorOverlay.TriggerMasterFadeOut();
    }

    [ServerRpc(RequireOwnership = false)]
    public void TriggerColorCutToBlack()
    {
        _masterOpacityValue.Value = 0f;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetHeartbeatActive(bool value)
    {
        if (value)
        {
            _heartbeatStartColorSync.Value = _heartbeatStartColor;
            _heartbeatEndColorSync.Value = _heartbeatEndColor;
            _heartbeatBeatTimeSync.Value = _heartbeatBeatTime;
        }

        _heartbeatActive.Value = value;
    }

    private void OnHeartbeatActiveChanged(bool prev, bool next, bool asServer)
    {
        if (SceneLoader.BuildType != BuildType.Client || Instances.ColorOverlay == null) return;

        if (next)
        {
            Instances.ColorOverlay.StartHeartbeat(
                _heartbeatStartColorSync.Value,
                _heartbeatEndColorSync.Value,
                _heartbeatBeatTimeSync.Value);
        }
        else
        {
            Instances.ColorOverlay.StopHeartbeat();
        }
    }

    private void OnPartInputChanged(string value)
    {
        if (!int.TryParse(value, out int part)) return;
        Debug.Log($"[NetworkedMonitor] Monitor requesting part {part}.");
        RequestSetPartServerRpc(part);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestSetPartServerRpc(int part)
    {
        Debug.Log($"[NetworkedMonitor] Server setting _currentPart to {part}.");
        _currentPart.Value = part;
    }

    private void OnCurrentPartChanged(int prev, int next, bool asServer)
    {
        Debug.Log($"[NetworkedMonitor] Part changed {prev} -> {next} (asServer={asServer}).");

        if (_viewManager != null)
            _viewManager.SetPart(next);
        else if (SceneLoader.BuildType != BuildType.Monitor)
            Debug.LogWarning("[NetworkedMonitor] ViewManager not yet resolved - part change dropped.");
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetCompleteAnatomyMode(bool value)
    {
        _completeAnatomyMode.Value = value;
    }

    private void OnCompleteAnatomyModeChanged(bool prev, bool next, bool asServer)
    {
        Debug.Log($"[NetworkedMonitor] Complete Anatomy changed {prev} -> {next} (asServer={asServer}).");
        ApplyCompleteAnatomyMode(next);
    }

    private void ApplyCompleteAnatomyMode(bool enabled)
    {
        if (SceneLoader.BuildType == BuildType.Monitor) return;

        if (_viewManager != null)
            _viewManager.SetCompleteAnatomyMode(enabled);
        else
            Debug.LogWarning("[NetworkedMonitor] ViewManager not yet resolved - complete anatomy change dropped.");
    }

    private IEnumerator FindViewManagerCoroutine()
    {
        Debug.Log("[NetworkedMonitor] Searching for ViewManager...");
        while (true)
        {
            _viewManager = FindObjectOfType<ViewManager>();
            if (_viewManager != null)
            {
                Debug.Log("[NetworkedMonitor] ViewManager found.");
                _viewManager.SetCompleteAnatomyMode(_completeAnatomyMode.Value);
                yield break;
            }

            Debug.LogWarning("[NetworkedMonitor] ViewManager not found. Retrying in 1 s...");
            yield return new WaitForSeconds(1f);
        }
    }

    private IEnumerator FindAndSubscribeInputFieldCoroutine()
    {
        Debug.Log("[NetworkedMonitor] Searching for 'Part Number' input field...");
        while (true)
        {
            GameObject go = GameObject.Find("Part Number");
            if (go != null)
            {
                _partInputField = go.GetComponent<TMP_InputField>();
                if (_partInputField != null)
                {
                    _partInputField.onValueChanged.AddListener(OnPartInputChanged);
                    Debug.Log("[NetworkedMonitor] Subscribed to 'Part Number' input field.");
                    yield break;
                }
            }

            Debug.LogWarning("[NetworkedMonitor] 'Part Number' input field not found. Retrying in 1 s...");
            yield return new WaitForSeconds(1f);
        }
    }

    private void UnsubscribeInputField()
    {
        if (_partInputField == null) return;
        _partInputField.onValueChanged.RemoveListener(OnPartInputChanged);
        _partInputField = null;
    }
}