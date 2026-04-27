using System.Collections;
using System.Collections.Generic;
using FishNet.Connection;
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
    private const int GroupA = 0;
    private const int GroupB = 1;
    private const int UnassignedGroup = -1;
    private const int MinimumPartNumber = 0;
    private const int MaximumPartNumber = 10;
    private const int TutorialPartNumber = 0;
    private const int WordsOfVesaliusPartNumber = 5;
    private const float DefaultMasterVolume = 1f;
    private const float MinMasterVolume = 0.01f;

    private enum AudioMode
    {
        None,
        Intro,
        PingPong,
        OrgansOfGeneration,
        Heart,
        Vibration,
    }

    private ViewManager viewManager;
    private TMP_InputField partInputField;

    private readonly SyncVar<int> currentPart = new(MinimumPartNumber);
    private readonly SyncVar<bool> participationMode = new(false);
    private readonly SyncVar<bool> completeAnatomyMode = new(false);

    private readonly SyncVar<bool> shouldPlayIntro = new(false);
    private readonly SyncVar<bool> shouldPlayPingPong = new(false);
    private readonly SyncVar<bool> shouldPlayOrgansOfGeneration = new(false);
    private readonly SyncVar<bool> shouldPlayHeart = new(false);
    private readonly SyncVar<bool> shouldPlayVibration = new(false);
    private readonly SyncVar<float> masterVolume = new(DefaultMasterVolume);

    [Header("Master Volume")]
    [Tooltip("Duration of the Fade In button action in seconds.")]
    [SerializeField] private float masterFadeInTime = 4f;
    [Tooltip("Duration of the Fade Out button action in seconds.")]
    [SerializeField] private float masterFadeOutTime = 4f;

    [Header("Heartbeat Config")]
    [Tooltip("Color at the trough of each heartbeat cycle (rest state).")]
    [SerializeField] private Color heartbeatStartColor = new(0f, 0f, 0f, 0f);
    [Tooltip("Color at the peak of each heartbeat pulse.")]
    [SerializeField] private Color heartbeatEndColor = new(0.8f, 0f, 0f, 1f);
    [Tooltip("Minimum random heartbeat speed picked when heartbeat mode is enabled.")]
    [SerializeField] private int heartbeatMinBpm = 40;
    [Tooltip("Maximum random heartbeat speed picked when heartbeat mode is enabled.")]
    [SerializeField] private int heartbeatMaxBpm = 80;

    [Header("Group Color Mode")]
    [Tooltip("Exactly two colors: index 0 is Group A, index 1 is Group B.")]
    [SerializeField] private Color[] groupColors = { new(0.93f, 0.26f, 0.24f, 1f), new(0.13f, 0.59f, 0.95f, 1f) };

    private readonly SyncVar<bool> masterOpacityActive = new(false);
    private readonly SyncVar<float> masterOpacityValue = new(1f);
    private readonly SyncVar<bool> heartbeatActive = new(false);
    private readonly SyncVar<Color> heartbeatStartColorSync = new();
    private readonly SyncVar<Color> heartbeatEndColorSync = new();
    private readonly SyncVar<float> heartbeatBeatTimeSync = new(0.8f);
    private readonly SyncVar<bool> groupColorModeActive = new(false);

    private readonly Dictionary<string, int> groupAssignmentsByUniqueId = new();

    [Header("Concert Reset")]
    [SerializeField] private int concertReadyPartNumber = TutorialPartNumber;
    [SerializeField] private bool concertReadyParticipationMode = false;

    private NetworkedMessageSystem networkedMessageSystem;
    private int myGroupIndex = UnassignedGroup;
    private Color myGroupColor = Color.clear;
    private Coroutine masterVolumeFadeRoutine;

    public bool CompleteAnatomyMode => completeAnatomyMode.Value;
    public bool ParticipationMode => participationMode.Value;
    public bool ShouldPlayIntro => shouldPlayIntro.Value;
    public bool ShouldPlayPingPong => shouldPlayPingPong.Value;
    public bool ShouldPlayOrgansOfGeneration => shouldPlayOrgansOfGeneration.Value;
    public bool ShouldPlayHeart => shouldPlayHeart.Value;
    public bool ShouldPlayVibration => shouldPlayVibration.Value;
    public float MasterVolume => masterVolume.Value;
    public bool MasterOpacityActive => masterOpacityActive.Value;
    public float MasterOpacityValue => masterOpacityValue.Value;
    public bool HeartbeatActive => heartbeatActive.Value;
    public bool GroupColorModeActive => groupColorModeActive.Value;
    public int CurrentPart => ClampPart(currentPart.Value);

    public override void OnStartServer()
    {
        base.OnStartServer();
        StartCoroutine(FindMessageSystemCoroutine());
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        currentPart.OnChange += OnCurrentPartChanged;
        participationMode.OnChange += OnParticipationModeChanged;
        completeAnatomyMode.OnChange += OnCompleteAnatomyModeChanged;

        shouldPlayIntro.OnChange += OnShouldPlayIntroChanged;
        shouldPlayPingPong.OnChange += OnShouldPlayPingPongChanged;
        shouldPlayOrgansOfGeneration.OnChange += OnShouldPlayOrgansOfGenerationChanged;
        shouldPlayHeart.OnChange += OnShouldPlayHeartChanged;
        shouldPlayVibration.OnChange += OnShouldPlayVibrationChanged;
        masterVolume.OnChange += OnMasterVolumeChanged;

        masterOpacityActive.OnChange += OnMasterOpacityActiveChanged;
        masterOpacityValue.OnChange += OnMasterOpacityValueChanged;
        heartbeatActive.OnChange += OnHeartbeatActiveChanged;
        groupColorModeActive.OnChange += OnGroupColorModeActiveChanged;

        if (SceneLoader.BuildType == BuildType.Monitor)
        {
            MonitorUI monitorUI = Instances.MonitorUI;
            if (monitorUI != null)
                monitorUI.Init(this);

            StartCoroutine(FindAndSubscribeInputFieldCoroutine());
        }
        else
        {
            GroupColorOverlay.EnsureExistsInScene();
            StartCoroutine(FindViewManagerCoroutine());
            StartCoroutine(FindColorOverlayCoroutine());
            ApplyGroupColorVisibility(groupColorModeActive.Value);
        }
    }

    public override void OnStopClient()
    {
        base.OnStopClient();

        currentPart.OnChange -= OnCurrentPartChanged;
        participationMode.OnChange -= OnParticipationModeChanged;
        completeAnatomyMode.OnChange -= OnCompleteAnatomyModeChanged;

        shouldPlayIntro.OnChange -= OnShouldPlayIntroChanged;
        shouldPlayPingPong.OnChange -= OnShouldPlayPingPongChanged;
        shouldPlayOrgansOfGeneration.OnChange -= OnShouldPlayOrgansOfGenerationChanged;
        shouldPlayHeart.OnChange -= OnShouldPlayHeartChanged;
        shouldPlayVibration.OnChange -= OnShouldPlayVibrationChanged;
        masterVolume.OnChange -= OnMasterVolumeChanged;

        masterOpacityActive.OnChange -= OnMasterOpacityActiveChanged;
        masterOpacityValue.OnChange -= OnMasterOpacityValueChanged;
        heartbeatActive.OnChange -= OnHeartbeatActiveChanged;
        groupColorModeActive.OnChange -= OnGroupColorModeActiveChanged;

        UnsubscribeInputField();
    }

    public override void OnStopServer()
    {
        base.OnStopServer();

        if (networkedMessageSystem != null)
            networkedMessageSystem.ClientRegistered -= OnClientRegistered;

        groupAssignmentsByUniqueId.Clear();
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
        if (SceneLoader.BuildType == BuildType.Monitor)
        {
            Instances.MonitorUI?.SetIntroState(next);
            return;
        }

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
        if (SceneLoader.BuildType == BuildType.Monitor)
        {
            Instances.MonitorUI?.SetPingPongState(next);
            return;
        }

        if (SceneLoader.BuildType != BuildType.Client)
            return;

        if (next)
        {
            if (HasLocalGroupAssignment())
                Instances.AudioManager.PlayGroupPingPong(myGroupIndex);
            else
                Instances.AudioManager.PlayPingPong();
        }
        else
        {
            Instances.AudioManager.StopPingPong();
            Instances.AudioManager.StopGroupPingPong();
        }
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
        if (SceneLoader.BuildType == BuildType.Monitor)
        {
            Instances.MonitorUI?.SetOrgansOfGenerationState(next);
            return;
        }

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
        if (!asServer && SceneLoader.BuildType == BuildType.Monitor)
        {
            Instances.MonitorUI?.SetHeartState(next);
            return;
        }

        if (SceneLoader.BuildType != BuildType.Client)
            return;

        if (next)
            Instances.AudioManager.PlayHeart();
        else
            Instances.AudioManager.StopHeart();
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetShouldPlayVibration(bool value)
    {
        if (value)
            SetExclusiveAudioMode(AudioMode.Vibration);
        else
            shouldPlayVibration.Value = false;
    }

    private void OnShouldPlayVibrationChanged(bool prev, bool next, bool asServer)
    {
        if (SceneLoader.BuildType == BuildType.Monitor)
        {
            Instances.MonitorUI?.SetVibrationState(next);
            return;
        }

        if (SceneLoader.BuildType != BuildType.Client)
            return;

        if (next)
            Instances.AudioManager.PlayVibrationMode();
        else
            Instances.AudioManager.StopVibrationMode();
    }

    private void SetExclusiveAudioMode(AudioMode mode)
    {
        shouldPlayIntro.Value = mode == AudioMode.Intro;
        shouldPlayPingPong.Value = mode == AudioMode.PingPong;
        shouldPlayOrgansOfGeneration.Value = mode == AudioMode.OrgansOfGeneration;
        shouldPlayHeart.Value = mode == AudioMode.Heart;
        shouldPlayVibration.Value = mode == AudioMode.Vibration;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetMasterVolume(float value)
    {
        StopMasterVolumeFade();
        SetMasterVolumeState(value);
    }

    [ServerRpc(RequireOwnership = false)]
    public void TriggerMasterFadeIn()
    {
        StartMasterVolumeFade(true);
    }

    [ServerRpc(RequireOwnership = false)]
    public void TriggerMasterFadeOut()
    {
        StartMasterVolumeFade(false);
    }

    [ServerRpc(RequireOwnership = false)]
    public void TriggerMasterMute()
    {
        StopMasterVolumeFade();
        SetMasterVolumeState(MinMasterVolume);
    }

    [ServerRpc(RequireOwnership = false)]
    public void TriggerMasterReset()
    {
        StopMasterVolumeFade();
        SetMasterVolumeState(DefaultMasterVolume);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ResetAllForConcert()
    {
        ApplyConcertReset();
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetColorMasterOpacityActive(bool value)
    {
        masterOpacityActive.Value = value;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetGroupColorModeActive(bool value)
    {
        if (value)
        {
            ReassignGroupsForCurrentConnections();
            groupColorModeActive.Value = true;
            PushAssignmentsToCurrentConnections();
            return;
        }

        groupColorModeActive.Value = false;
        ClearGroupAssignments();
    }

    [ServerRpc(RequireOwnership = false)]
    public void TriggerGroupMessageMode()
    {
        if (groupAssignmentsByUniqueId.Count == 0)
            ReassignGroupsForCurrentConnections();
        else
            EnsureAssignmentsForCurrentConnections();

        if (networkedMessageSystem == null)
            networkedMessageSystem = FindObjectOfType<NetworkedMessageSystem>();

        networkedMessageSystem?.BroadcastGroupMessage();
    }

    private void OnMasterOpacityActiveChanged(bool prev, bool next, bool asServer)
    {
        SyncMonitorColorOverlayUi();

        if (SceneLoader.BuildType != BuildType.Client)
            return;

        ApplyColorOverlayState();
    }

    private void OnMasterVolumeChanged(float prev, float next, bool asServer)
    {
        if (SceneLoader.BuildType == BuildType.Monitor)
            Instances.MonitorUI?.SetMasterVolumeState(next);

        if (SceneLoader.BuildType == BuildType.Client)
            Instances.AudioManager?.SetMasterVolume(next);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetColorMasterOpacity(float value)
    {
        masterOpacityValue.Value = Mathf.Clamp01(value);
    }

    private void OnMasterOpacityValueChanged(float prev, float next, bool asServer)
    {
        SyncMonitorColorOverlayUi();

        if (SceneLoader.BuildType != BuildType.Client)
            return;

        ApplyColorOverlayState();
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
            heartbeatBeatTimeSync.Value = GetRandomHeartbeatBeatTime();
        }

        heartbeatActive.Value = value;
    }

    private void OnHeartbeatActiveChanged(bool prev, bool next, bool asServer)
    {
        SyncMonitorColorOverlayUi();

        if (SceneLoader.BuildType != BuildType.Client)
            return;

        ApplyColorOverlayState();
    }

    private void OnGroupColorModeActiveChanged(bool prev, bool next, bool asServer)
    {
        if (SceneLoader.BuildType == BuildType.Monitor)
        {
            Instances.MonitorUI?.SetGroupColorState(next);
            return;
        }

        if (SceneLoader.BuildType != BuildType.Client)
            return;

        ApplyGroupColorVisibility(next);
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
        ApplyPartChange(part);
    }

    public void IncrementPart()
    {
        RequestAdjustPartServerRpc(1);
    }

    public void DecrementPart()
    {
        RequestAdjustPartServerRpc(-1);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestAdjustPartServerRpc(int delta)
    {
        ApplyPartChange(currentPart.Value + delta);
    }

    private void ApplyPartChange(int requestedPart)
    {
        int clampedPart = ClampPart(requestedPart);
        completeAnatomyMode.Value = ShouldAutoEnableCompleteAnatomy(clampedPart);

        currentPart.Value = clampedPart;
    }

    private static int ClampPart(int part)
    {
        return Mathf.Clamp(part, MinimumPartNumber, MaximumPartNumber);
    }

    private static bool ShouldAutoEnableCompleteAnatomy(int part)
    {
        return part != TutorialPartNumber && part != WordsOfVesaliusPartNumber;
    }

    private void OnCurrentPartChanged(int prev, int next, bool asServer)
    {
        if (partInputField != null)
            partInputField.SetTextWithoutNotify(ClampPart(next).ToString());

        if (SceneLoader.BuildType == BuildType.Monitor)
            Instances.MonitorUI?.SetCurrentPartState(next);

        if (viewManager != null)
            viewManager.SetPart(next);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetParticipationMode(bool value)
    {
        participationMode.Value = value;
    }

    private void OnParticipationModeChanged(bool prev, bool next, bool asServer)
    {
        if (SceneLoader.BuildType == BuildType.Monitor)
            Instances.MonitorUI?.SetParticipationModeState(next);

        ApplyParticipationMode(next);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetCompleteAnatomyMode(bool value)
    {
        completeAnatomyMode.Value = value;
    }

    private void OnCompleteAnatomyModeChanged(bool prev, bool next, bool asServer)
    {
        if (SceneLoader.BuildType == BuildType.Monitor)
            Instances.MonitorUI?.SetCompleteAnatomyModeState(next);

        ApplyCompleteAnatomyMode(next);
    }

    private void ApplyCompleteAnatomyMode(bool enabled)
    {
        if (SceneLoader.BuildType == BuildType.Monitor)
            return;

        if (viewManager != null)
            viewManager.SetCompleteAnatomyMode(enabled);
    }

    private void ApplyParticipationMode(bool enabled)
    {
        if (SceneLoader.BuildType != BuildType.Client)
            return;

        if (viewManager != null)
            viewManager.SetParticipationMode(enabled);
    }

    private IEnumerator FindViewManagerCoroutine()
    {
        while (true)
        {
            viewManager = FindObjectOfType<ViewManager>();
            if (viewManager != null)
            {
                viewManager.SetCompleteAnatomyMode(completeAnatomyMode.Value);
                viewManager.SetParticipationMode(participationMode.Value);
                yield break;
            }

            yield return new WaitForSeconds(1f);
        }
    }

    private IEnumerator FindColorOverlayCoroutine()
    {
        while (SceneLoader.BuildType == BuildType.Client)
        {
            ColorOverlay overlay = FindSharedColorOverlay();
            if (overlay != null)
            {
                Instances.ColorOverlay = overlay;
                ApplyColorOverlayState();
                yield break;
            }

            yield return new WaitForSeconds(0.25f);
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
                    partInputField.SetTextWithoutNotify(CurrentPart.ToString());
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

    private IEnumerator FindMessageSystemCoroutine()
    {
        while (networkedMessageSystem == null)
        {
            networkedMessageSystem = FindObjectOfType<NetworkedMessageSystem>();
            if (networkedMessageSystem == null)
            {
                yield return new WaitForSeconds(0.5f);
                continue;
            }
        }

        networkedMessageSystem.ClientRegistered -= OnClientRegistered;
        networkedMessageSystem.ClientRegistered += OnClientRegistered;
    }

    private void OnClientRegistered(NetworkConnection connection, string uniqueId)
    {
        if (string.IsNullOrWhiteSpace(uniqueId))
            return;

        if (!groupAssignmentsByUniqueId.TryGetValue(uniqueId, out int groupIndex))
        {
            if (groupAssignmentsByUniqueId.Count == 0)
                return;

            groupIndex = GetSmallerActiveGroupIndex();
            groupAssignmentsByUniqueId[uniqueId] = groupIndex;
        }

        SendGroupAssignment(connection, groupIndex);
    }

    private void ReassignGroupsForCurrentConnections()
    {
        if (networkedMessageSystem == null)
            networkedMessageSystem = FindObjectOfType<NetworkedMessageSystem>();

        if (networkedMessageSystem == null)
            return;

        List<(NetworkConnection connection, string uniqueId)> connectedClients = GetConnectedClients();
        Shuffle(connectedClients);

        groupAssignmentsByUniqueId.Clear();

        int firstGroupSize = connectedClients.Count / 2;
        for (int i = 0; i < connectedClients.Count; i++)
        {
            int groupIndex = i < firstGroupSize ? GroupA : GroupB;
            groupAssignmentsByUniqueId[connectedClients[i].uniqueId] = groupIndex;
        }
    }

    private void EnsureAssignmentsForCurrentConnections()
    {
        if (networkedMessageSystem == null)
            networkedMessageSystem = FindObjectOfType<NetworkedMessageSystem>();

        if (networkedMessageSystem == null)
            return;

        foreach ((NetworkConnection _, string uniqueId) in GetConnectedClients())
        {
            if (!groupAssignmentsByUniqueId.ContainsKey(uniqueId))
                groupAssignmentsByUniqueId[uniqueId] = GetSmallerActiveGroupIndex();
        }

        PushAssignmentsToCurrentConnections();
    }

    private void PushAssignmentsToCurrentConnections()
    {
        foreach ((NetworkConnection connection, string uniqueId) in GetConnectedClients())
        {
            if (groupAssignmentsByUniqueId.TryGetValue(uniqueId, out int groupIndex))
                SendGroupAssignment(connection, groupIndex);
        }
    }

    private List<(NetworkConnection connection, string uniqueId)> GetConnectedClients()
    {
        List<(NetworkConnection connection, string uniqueId)> connectedClients = new();
        if (networkedMessageSystem == null)
            return connectedClients;

        foreach (NetworkConnection connection in networkedMessageSystem.GetAllConnections())
        {
            if (networkedMessageSystem.TryGetUniqueId(connection, out string uniqueId) && !string.IsNullOrWhiteSpace(uniqueId))
                connectedClients.Add((connection, uniqueId));
        }

        return connectedClients;
    }

    private int GetSmallerActiveGroupIndex()
    {
        int groupACount = 0;
        int groupBCount = 0;

        foreach ((NetworkConnection _, string uniqueId) in GetConnectedClients())
        {
            if (!groupAssignmentsByUniqueId.TryGetValue(uniqueId, out int groupIndex))
                continue;

            if (groupIndex == GroupA)
                groupACount++;
            else
                groupBCount++;
        }

        return groupACount <= groupBCount ? GroupA : GroupB;
    }

    private void SendGroupAssignment(NetworkConnection connection, int groupIndex)
    {
        RpcReceiveGroupAssignment(connection, groupIndex, ResolveGroupColor(groupIndex));
    }

    [TargetRpc]
    private void RpcReceiveGroupAssignment(NetworkConnection connection, int groupIndex, Color color)
    {
        if (SceneLoader.BuildType != BuildType.Client)
            return;

        myGroupIndex = groupIndex;
        myGroupColor = color;
        ApplyGroupColorVisibility(groupColorModeActive.Value);

        if (shouldPlayPingPong.Value)
        {
            Instances.AudioManager.StopPingPong();
            Instances.AudioManager.PlayGroupPingPong(myGroupIndex);
        }
    }

    [ObserversRpc]
    private void RpcClearGroupAssignments()
    {
        if (SceneLoader.BuildType != BuildType.Client)
            return;

        ClearLocalGroupAssignment();
    }

    [ObserversRpc]
    private void RpcResetClientsForConcert()
    {
        if (SceneLoader.BuildType != BuildType.Client)
            return;

        ClearLocalGroupAssignment();
        MessageOverlay.Instance?.HideMessage();
        Instances.AudioManager.ResetForConcert();
    }

    private void ApplyGroupColorVisibility(bool visible)
    {
        GroupColorOverlay overlay = GroupColorOverlay.EnsureExistsInScene();
        if (overlay == null)
            return;

        if (visible && HasLocalGroupAssignment())
            overlay.Show(myGroupColor);
        else
            overlay.Hide();
    }

    private bool HasLocalGroupAssignment()
    {
        return myGroupIndex == GroupA || myGroupIndex == GroupB;
    }

    private Color ResolveGroupColor(int groupIndex)
    {
        if (groupColors == null || groupColors.Length < 2)
            return groupIndex == GroupA ? Color.red : Color.blue;

        return groupColors[groupIndex == GroupA ? GroupA : GroupB];
    }

    private float GetRandomHeartbeatBeatTime()
    {
        int minBpm = Mathf.Max(1, Mathf.Min(heartbeatMinBpm, heartbeatMaxBpm));
        int maxBpm = Mathf.Max(minBpm, Mathf.Max(heartbeatMinBpm, heartbeatMaxBpm));
        int bpm = Random.Range(minBpm, maxBpm + 1);
        return 60f / bpm;
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private void ApplyConcertReset()
    {
        int resetPart = ClampPart(concertReadyPartNumber);

        SetExclusiveAudioMode(AudioMode.None);

        currentPart.Value = resetPart;
        participationMode.Value = concertReadyParticipationMode;
        completeAnatomyMode.Value = ShouldAutoEnableCompleteAnatomy(resetPart);
        StopMasterVolumeFade();
        SetMasterVolumeState(DefaultMasterVolume);

        masterOpacityActive.Value = false;
        masterOpacityValue.Value = 1f;
        heartbeatActive.Value = false;
        heartbeatStartColorSync.Value = heartbeatStartColor;
        heartbeatEndColorSync.Value = heartbeatEndColor;
        heartbeatBeatTimeSync.Value = GetRandomHeartbeatBeatTime();

        groupColorModeActive.Value = false;
        ClearGroupAssignments();

        if (networkedMessageSystem == null)
            networkedMessageSystem = FindObjectOfType<NetworkedMessageSystem>();

        networkedMessageSystem?.ResetDeckServer();
        networkedMessageSystem?.HardCutAllServer();

        RpcResetClientsForConcert();
    }

    private void SetMasterVolumeState(float value)
    {
        masterVolume.Value = Mathf.Clamp(value, MinMasterVolume, DefaultMasterVolume);

        if (SceneLoader.BuildType == BuildType.Monitor)
            Instances.MonitorUI?.SetMasterVolumeState(masterVolume.Value);
    }

    private void StartMasterVolumeFade(bool fadeIn)
    {
        StopMasterVolumeFade();
        masterVolumeFadeRoutine = StartCoroutine(FadeMasterVolumeCoroutine(fadeIn));
    }

    private void StopMasterVolumeFade()
    {
        if (masterVolumeFadeRoutine == null)
            return;

        StopCoroutine(masterVolumeFadeRoutine);
        masterVolumeFadeRoutine = null;
    }

    private IEnumerator FadeMasterVolumeCoroutine(bool fadeIn)
    {
        float startVolume = masterVolume.Value;
        float targetVolume = fadeIn ? DefaultMasterVolume : MinMasterVolume;
        float duration = Mathf.Max(fadeIn ? masterFadeInTime : masterFadeOutTime, 0.01f);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            SetMasterVolumeState(Mathf.Lerp(startVolume, targetVolume, t));

            elapsed += Time.deltaTime;
            yield return null;
        }

        SetMasterVolumeState(targetVolume);
        masterVolumeFadeRoutine = null;
    }

    private void ClearGroupAssignments()
    {
        groupAssignmentsByUniqueId.Clear();
        RpcClearGroupAssignments();
    }

    private void ClearLocalGroupAssignment()
    {
        myGroupIndex = UnassignedGroup;
        myGroupColor = Color.clear;
        ApplyGroupColorVisibility(false);
    }

    private void SyncMonitorColorOverlayUi()
    {
        if (SceneLoader.BuildType != BuildType.Monitor)
            return;

        MonitorColorOverlayUI overlayUi = FindObjectOfType<MonitorColorOverlayUI>();
        overlayUi?.SetState(masterOpacityActive.Value, masterOpacityValue.Value, heartbeatActive.Value);
    }

    private void ApplyColorOverlayState()
    {
        if (SceneLoader.BuildType != BuildType.Client)
            return;

        ColorOverlay overlay = FindSharedColorOverlay();
        if (overlay == null)
            return;

        Instances.ColorOverlay = overlay;
        bool shouldBeActive = heartbeatActive.Value || masterOpacityActive.Value;
        if (overlay.gameObject.activeSelf != shouldBeActive)
            overlay.gameObject.SetActive(shouldBeActive);

        if (!shouldBeActive)
        {
            if (overlay.HeartbeatActive)
                overlay.StopHeartbeat();
            overlay.SetMasterOpacityActive(false);
            return;
        }

        overlay.SetMasterOpacityActive(masterOpacityActive.Value);
        overlay.SetMasterOpacity(masterOpacityValue.Value);

        if (heartbeatActive.Value && !overlay.HeartbeatActive)
        {
            overlay.StartHeartbeat(
                heartbeatStartColorSync.Value,
                heartbeatEndColorSync.Value,
                heartbeatBeatTimeSync.Value);
        }
        else if (!heartbeatActive.Value && overlay.HeartbeatActive)
        {
            overlay.StopHeartbeat();
        }
    }

    private ColorOverlay FindSharedColorOverlay()
    {
        if (Instances.ColorOverlay != null)
            return Instances.ColorOverlay;

        foreach (ColorOverlay overlay in Resources.FindObjectsOfTypeAll<ColorOverlay>())
        {
            if (overlay == null)
                continue;

            if (!overlay.gameObject.scene.IsValid())
                continue;

            if ((overlay.hideFlags & HideFlags.HideAndDontSave) != 0)
                continue;

            if (!overlay.RegisterAsSharedInstance)
                continue;

            return overlay;
        }

        return null;
    }
}