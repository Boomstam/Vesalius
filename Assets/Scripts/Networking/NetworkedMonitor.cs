using System.Collections;
using TMPro;
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

/// <summary>
/// Networked state authority for the monitor.
/// Syncs current part number to all clients (drives ViewManager).
/// Syncs three audio mode booleans; callbacks on clients drive AudioManager.
/// Master volume and fade commands are fire-and-forget ObserversRpcs.
/// </summary>
public class NetworkedMonitor : NetworkBehaviour
{
    // ── Part Number ────────────────────────────────────────────────────────────

    private ViewManager    _viewManager;
    private TMP_InputField _partInputField;

    private readonly SyncVar<int> _currentPart = new SyncVar<int>(-1);

    // ── Audio State ────────────────────────────────────────────────────────────

    private readonly SyncVar<bool> _shouldPlayOrgansOfNutrition  = new SyncVar<bool>(false);
    private readonly SyncVar<bool> _shouldPlayOrgansOfGeneration = new SyncVar<bool>(false);
    private readonly SyncVar<bool> _shouldPlayHeart              = new SyncVar<bool>(false);

    // ── FishNet Lifecycle ──────────────────────────────────────────────────────

    public override void OnStartClient()
    {
        base.OnStartClient();

        _currentPart.OnChange               += OnCurrentPartChanged;
        _shouldPlayOrgansOfNutrition.OnChange  += OnShouldPlayOrgansOfNutritionChanged;
        _shouldPlayOrgansOfGeneration.OnChange += OnShouldPlayOrgansOfGenerationChanged;
        _shouldPlayHeart.OnChange             += OnShouldPlayHeartChanged;

        if (SceneLoader.BuildType == BuildType.Monitor)
            StartCoroutine(FindAndSubscribeInputFieldCoroutine());
        else
            StartCoroutine(FindViewManagerCoroutine());
    }

    public override void OnStopClient()
    {
        base.OnStopClient();

        _currentPart.OnChange               -= OnCurrentPartChanged;
        _shouldPlayOrgansOfNutrition.OnChange  -= OnShouldPlayOrgansOfNutritionChanged;
        _shouldPlayOrgansOfGeneration.OnChange -= OnShouldPlayOrgansOfGenerationChanged;
        _shouldPlayHeart.OnChange             -= OnShouldPlayHeartChanged;

        UnsubscribeInputField();
    }

    // ── Audio RPCs — Toggles ───────────────────────────────────────────────────

    [ServerRpc(RequireOwnership = false)]
    public void SetShouldPlayOrgansOfNutrition(bool value)
    {
        _shouldPlayOrgansOfNutrition.Value = value;
    }

    private void OnShouldPlayOrgansOfNutritionChanged(bool prev, bool next, bool asServer)
    {
        if (SceneLoader.BuildType != BuildType.Client) return;

        if (next) Instances.AudioManager.PlayOrgansOfNutrition();
        else      Instances.AudioManager.StopOrgansOfNutrition();
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
        else      Instances.AudioManager.StopOrgansOfGeneration();
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
        else      Instances.AudioManager.StopHeart();
    }

    // ── Audio RPCs — Master Volume ─────────────────────────────────────────────

    /// <summary>Drives real-time master volume on all clients from the monitor slider.</summary>
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

    // ── Part Number ────────────────────────────────────────────────────────────

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
        Debug.Log($"[NetworkedMonitor] Part changed {prev} → {next} (asServer={asServer}).");

        if (_viewManager != null)
            _viewManager.SetPart(next);
        else if (SceneLoader.BuildType != BuildType.Monitor)
            Debug.LogWarning("[NetworkedMonitor] ViewManager not yet resolved — part change dropped.");
    }

    // ── Find Coroutines ────────────────────────────────────────────────────────

    private IEnumerator FindViewManagerCoroutine()
    {
        Debug.Log("[NetworkedMonitor] Searching for ViewManager…");
        while (true)
        {
            _viewManager = FindObjectOfType<ViewManager>();
            if (_viewManager != null)
            {
                Debug.Log("[NetworkedMonitor] ViewManager found.");
                yield break;
            }
            Debug.LogWarning("[NetworkedMonitor] ViewManager not found. Retrying in 1 s…");
            yield return new WaitForSeconds(1f);
        }
    }

    private IEnumerator FindAndSubscribeInputFieldCoroutine()
    {
        Debug.Log("[NetworkedMonitor] Searching for 'Part Number' input field…");
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
            Debug.LogWarning("[NetworkedMonitor] 'Part Number' input field not found. Retrying in 1 s…");
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