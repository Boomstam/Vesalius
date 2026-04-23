using System.Collections;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using TMPro;
using UnityEngine;

/// <summary>
/// Networked state authority for the monitor.
/// Syncs current part number to all clients (drives ViewManager).
/// Syncs audio mode booleans; callbacks on clients drive AudioManager.
/// Syncs color overlay state; callbacks on clients drive ColorOverlay.
/// Master volume, audio fades, and color overlay fades are fire-and-forget ObserversRpcs.
/// </summary>
public class NetworkedMonitor : NetworkBehaviour
{
    private enum AudioMode
    {
        None,
        Intro,
        PingPong,
        OrgansOfGeneration,
        Heart,
    }

    private ViewManager viewManager;
    private TMP_InputField partInputField;

    private readonly SyncVar<int> currentPart = new(-1);
    private readonly SyncVar<bool> completeAnatomyMode = new(false);

    private readonly SyncVar<bool> shouldPlayIntro = new(false);
    private readonly SyncVar<bool> shouldPlayPingPong = new(false);
    private readonly SyncVar<bool> shouldPlayOrgansOfGeneration = new(false);
    private readonly SyncVar<bool> shouldPlayHeart = new(false);

    [Header("Heartbeat Config")]
    [Tooltip("Color at the trough of each heartbeat cycle (rest state).")]
    [SerializeField] private Color heartbeatStartColor = new(0f, 0f, 0f, 0f);
    [Tooltip("Color at the peak of each heartbeat pulse.")]
    [SerializeField] private Color heartbeatEndColor = new(0.8f, 0f, 0f, 1f);
    [Tooltip("Duration of one full heartbeat cycle in seconds.")]
    [SerializeField] private float heartbeatBeatTime = 0.8f;

    private readonly SyncVar<bool> masterOpacityActive = new(false);
    private readonly SyncVar<float> masterOpacityValue = new(1f);
    private readonly SyncVar<bool> heartbeatActive = new(false);
    private readonly SyncVar<Color> heartbeatStartColorSync = new();
    private readonly SyncVar<Color> heartbeatEndColorSync = new();
    private readonly SyncVar<float> heartbeatBeatTimeSync = new(0.8f);

    public bool CompleteAnatomyMode => completeAnatomyMode.Value;
    public bool ShouldPlayIntro => shouldPlayIntro.Value;
    public bool ShouldPlayPingPong => shouldPlayPingPong.Value;
    public bool ShouldPlayOrgansOfGeneration => shouldPlayOrgansOfGeneration.Value;
    public bool ShouldPlayHeart => shouldPlayHeart.Value;
    public bool MasterOpacityActive => masterOpacityActive.Value;
    public float MasterOpacityValue => masterOpacityValue.Value;
    public bool HeartbeatActive => heartbeatActive.Value;

    public override void OnStartClient()
    {
        base.OnStartClient();

        currentPart.OnChange += OnCurrentPartChanged;
        completeAnatomyMode.OnChange += OnCompleteAnatomyModeChanged;

        shouldPlayIntro.OnChange += OnShouldPlayIntroChanged;
        shouldPlayPingPong.OnChange += OnShouldPlayPingPongChanged;
        shouldPlayOrgansOfGeneration.OnChange += OnShouldPlayOrgansOfGenerationChanged;
        shouldPlayHeart.OnChange += OnShouldPlayHeartChanged;

        masterOpacityActive.OnChange += OnMasterOpacityActiveChanged;
        masterOpacityValue.OnChange += OnMasterOpacityValueChanged;
        heartbeatActive.OnChange += OnHeartbeatActiveChanged;

        if (SceneLoader.BuildType == BuildType.Monitor)
        {
            MonitorUI monitorUI = Instances.MonitorUI;
            if (monitorUI != null)
                monitorUI.Init(this);

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

        currentPart.OnChange -= OnCurrentPartChanged;
        completeAnatomyMode.OnChange -= OnCompleteAnatomyModeChanged;

        shouldPlayIntro.OnChange -= OnShouldPlayIntroChanged;
        shouldPlayPingPong.OnChange -= OnShouldPlayPingPongChanged;
        shouldPlayOrgansOfGeneration.OnChange -= OnShouldPlayOrgansOfGenerationChanged;
        shouldPlayHeart.OnChange -= OnShouldPlayHeartChanged;

        masterOpacityActive.OnChange -= OnMasterOpacityActiveChanged;
        masterOpacityValue.OnChange -= OnMasterOpacityValueChanged;
        heartbeatActive.OnChange -= OnHeartbeatActiveChanged;

        UnsubscribeInputField();
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetShouldPlayIntro(bool value)
    {
        if (value)
            SetExclusiveAudioMode(AudioMode.Intro);
        else
            shouldPlayIntro.Value = false;
    }

    private void OnShouldPlayIntroChanged(bool prev, bool next, bool asServer)
    {
        if (SceneLoader.BuildType != BuildType.Client)
            return;

        if (next)
            Instances.AudioManager.PlayIntro();
        else
            Instances.AudioManager.StopIntro();
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetShouldPlayPingPong(bool value)
    {
        if (value)
            SetExclusiveAudioMode(AudioMode.PingPong);
        else
            shouldPlayPingPong.Value = false;
    }

    private void OnShouldPlayPingPongChanged(bool prev, bool next, bool asServer)
    {
        if (SceneLoader.BuildType != BuildType.Client)
            return;

        if (next)
            Instances.AudioManager.PlayPingPong();
        else
            Instances.AudioManager.StopPingPong();
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetShouldPlayOrgansOfGeneration(bool value)
    {
        if (value)
            SetExclusiveAudioMode(AudioMode.OrgansOfGeneration);
        else
            shouldPlayOrgansOfGeneration.Value = false;
    }

    private void OnShouldPlayOrgansOfGenerationChanged(bool prev, bool next, bool asServer)
    {
        if (SceneLoader.BuildType != BuildType.Client)
            return;

        if (next)
            Instances.AudioManager.PlayOrgansOfGeneration();
        else
            Instances.AudioManager.StopOrgansOfGeneration();
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetShouldPlayHeart(bool value)
    {
        if (value)
            SetExclusiveAudioMode(AudioMode.Heart);
        else
            shouldPlayHeart.Value = false;
    }

    private void OnShouldPlayHeartChanged(bool prev, bool next, bool asServer)
    {
        if (SceneLoader.BuildType != BuildType.Client)
            return;

        if (next)
            Instances.AudioManager.PlayHeart();
        else
            Instances.AudioManager.StopHeart();
    }

    private void SetExclusiveAudioMode(AudioMode mode)
    {
        shouldPlayIntro.Value = mode == AudioMode.Intro;
        shouldPlayPingPong.Value = mode == AudioMode.PingPong;
        shouldPlayOrgansOfGeneration.Value = mode == AudioMode.OrgansOfGeneration;
        shouldPlayHeart.Value = mode == AudioMode.Heart;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetMasterVolume(float value)
    {
        RpcSetMasterVolume(value);
    }

    [ObserversRpc]
    private void RpcSetMasterVolume(float value)
    {
        if (SceneLoader.BuildType != BuildType.Client)
            return;

        Instances.AudioManager.SetMasterVolume(value);
    }

    [ServerRpc(RequireOwnership = false)]
    public void TriggerMasterFadeIn()
    {
        RpcTriggerMasterFadeIn();
    }

    [ObserversRpc]
    private void RpcTriggerMasterFadeIn()
    {
        if (SceneLoader.BuildType != BuildType.Client)
            return;

        Instances.AudioManager.FadeIn();
    }

    [ServerRpc(RequireOwnership = false)]
    public void TriggerMasterFadeOut()
    {
        RpcTriggerMasterFadeOut();
    }

    [ObserversRpc]
    private void RpcTriggerMasterFadeOut()
    {
        if (SceneLoader.BuildType != BuildType.Client)
            return;

        Instances.AudioManager.FadeOut();
    }

    [ServerRpc(RequireOwnership = false)]
    public void TriggerMasterMute()
    {
        RpcTriggerMasterMute();
    }

    [ObserversRpc]
    private void RpcTriggerMasterMute()
    {
        if (SceneLoader.BuildType != BuildType.Client)
            return;

        Instances.AudioManager.MuteImmediate();
    }

    [ServerRpc(RequireOwnership = false)]
    public void TriggerMasterReset()
    {
        RpcTriggerMasterReset();
    }

    [ObserversRpc]
    private void RpcTriggerMasterReset()
    {
        if (SceneLoader.BuildType != BuildType.Client)
            return;

        Instances.AudioManager.ResetImmediate();
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetColorMasterOpacityActive(bool value)
    {
        masterOpacityActive.Value = value;
    }

    private void OnMasterOpacityActiveChanged(bool prev, bool next, bool asServer)
    {
        if (SceneLoader.BuildType != BuildType.Client || Instances.ColorOverlay == null)
            return;

        Instances.ColorOverlay.SetMasterOpacityActive(next);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetColorMasterOpacity(float value)
    {
        masterOpacityValue.Value = Mathf.Clamp01(value);
    }

    private void OnMasterOpacityValueChanged(float prev, float next, bool asServer)
    {
        if (SceneLoader.BuildType != BuildType.Client || Instances.ColorOverlay == null)
            return;

        Instances.ColorOverlay.SetMasterOpacity(next);
    }

    [ServerRpc(RequireOwnership = false)]
    public void TriggerColorFadeIn()
    {
        RpcTriggerColorFadeIn();
    }

    [ObserversRpc]
    private void RpcTriggerColorFadeIn()
    {
        if (SceneLoader.BuildType != BuildType.Client || Instances.ColorOverlay == null)
            return;

        Instances.ColorOverlay.TriggerMasterFadeIn();
    }

    [ServerRpc(RequireOwnership = false)]
    public void TriggerColorFadeOut()
    {
        RpcTriggerColorFadeOut();
    }

    [ObserversRpc]
    private void RpcTriggerColorFadeOut()
    {
        if (SceneLoader.BuildType != BuildType.Client || Instances.ColorOverlay == null)
            return;

        Instances.ColorOverlay.TriggerMasterFadeOut();
    }

    [ServerRpc(RequireOwnership = false)]
    public void TriggerColorCutToBlack()
    {
        masterOpacityValue.Value = 0f;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetHeartbeatActive(bool value)
    {
        if (value)
        {
            heartbeatStartColorSync.Value = heartbeatStartColor;
            heartbeatEndColorSync.Value = heartbeatEndColor;
            heartbeatBeatTimeSync.Value = heartbeatBeatTime;
        }

        heartbeatActive.Value = value;
    }

    private void OnHeartbeatActiveChanged(bool prev, bool next, bool asServer)
    {
        if (SceneLoader.BuildType != BuildType.Client || Instances.ColorOverlay == null)
            return;

        if (next)
        {
            Instances.ColorOverlay.StartHeartbeat(
                heartbeatStartColorSync.Value,
                heartbeatEndColorSync.Value,
                heartbeatBeatTimeSync.Value);
        }
        else
        {
            Instances.ColorOverlay.StopHeartbeat();
        }
    }

    private void OnPartInputChanged(string value)
    {
        if (!int.TryParse(value, out int part))
            return;

        RequestSetPartServerRpc(part);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestSetPartServerRpc(int part)
    {
        currentPart.Value = part;
    }

    private void OnCurrentPartChanged(int prev, int next, bool asServer)
    {
        if (viewManager != null)
            viewManager.SetPart(next);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetCompleteAnatomyMode(bool value)
    {
        completeAnatomyMode.Value = value;
    }

    private void OnCompleteAnatomyModeChanged(bool prev, bool next, bool asServer)
    {
        ApplyCompleteAnatomyMode(next);
    }

    private void ApplyCompleteAnatomyMode(bool enabled)
    {
        if (SceneLoader.BuildType == BuildType.Monitor)
            return;

        if (viewManager != null)
            viewManager.SetCompleteAnatomyMode(enabled);
    }

    private IEnumerator FindViewManagerCoroutine()
    {
        while (true)
        {
            viewManager = FindObjectOfType<ViewManager>();
            if (viewManager != null)
            {
                viewManager.SetCompleteAnatomyMode(completeAnatomyMode.Value);
                yield break;
            }

            yield return new WaitForSeconds(1f);
        }
    }

    private IEnumerator FindAndSubscribeInputFieldCoroutine()
    {
        while (true)
        {
            GameObject go = GameObject.Find("Part Number");
            if (go != null)
            {
                partInputField = go.GetComponent<TMP_InputField>();
                if (partInputField != null)
                {
                    partInputField.onValueChanged.AddListener(OnPartInputChanged);
                    yield break;
                }
            }

            yield return new WaitForSeconds(1f);
        }
    }

    private void UnsubscribeInputField()
    {
        if (partInputField == null)
            return;

        partInputField.onValueChanged.RemoveListener(OnPartInputChanged);
        partInputField = null;
    }
}
