using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// Monitor-side UI. Communicates exclusively through NetworkedMonitor -
/// never touches AudioManager directly (AudioManager lives on the client).
///
/// Initialisation is driven by NetworkedMonitor.OnStartClient(), which calls
/// Init() once the NetworkObject is fully spawned and SyncVar values are valid.
/// This guarantees a late-connecting monitor sees the correct UI state immediately
/// without polling via a coroutine.
/// </summary>
public class MonitorUI : MonoBehaviour
{
    [Header("Master Volume")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Button fadeInButton;
    [SerializeField] private Button fadeOutButton;
    [SerializeField] private Button muteButton;
    [SerializeField] private Button resetButton;

    [Header("Audio Mode Toggles")]
    [FormerlySerializedAs("organsOfNutritionToggle")]
    [SerializeField] private Toggle introToggle;
    [SerializeField] private Toggle pingPongToggle;
    [SerializeField] private Toggle organsOfGenerationToggle;
    [SerializeField] private Toggle heartToggle;
    [SerializeField] private Toggle vibrationToggle;
    [SerializeField] private Toggle groupColorToggle;

    [Header("View Toggles")]
    [SerializeField] private Toggle participationToggle;
    [SerializeField] private Toggle completeAnatomyToggle;

    private bool initialised;
    private Text introLabel;
    private Text pingPongLabel;
    private Text organsOfGenerationLabel;
    private Text heartLabel;
    private Text vibrationLabel;
    private Text groupColorLabel;

    private void Start()
    {
        ResolveAudioControls();
        participationToggle = ResolveParticipationToggle();
        completeAnatomyToggle = ResolveCompleteAnatomyToggle();
    }

    private void OnDestroy()
    {
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
        if (fadeInButton != null)
            fadeInButton.onClick.RemoveListener(OnFadeInClicked);
        if (fadeOutButton != null)
            fadeOutButton.onClick.RemoveListener(OnFadeOutClicked);
        if (muteButton != null)
            muteButton.onClick.RemoveListener(OnMuteClicked);
        if (resetButton != null)
            resetButton.onClick.RemoveListener(OnResetClicked);

        if (introToggle != null)
            introToggle.onValueChanged.RemoveListener(OnIntroToggled);
        if (pingPongToggle != null)
            pingPongToggle.onValueChanged.RemoveListener(OnPingPongToggled);
        if (organsOfGenerationToggle != null)
            organsOfGenerationToggle.onValueChanged.RemoveListener(OnOrgansOfGenerationToggled);
        if (heartToggle != null)
            heartToggle.onValueChanged.RemoveListener(OnHeartToggled);
        if (vibrationToggle != null)
            vibrationToggle.onValueChanged.RemoveListener(OnVibrationToggled);
        if (groupColorToggle != null)
            groupColorToggle.onValueChanged.RemoveListener(OnGroupColorToggled);
        if (participationToggle != null)
            participationToggle.onValueChanged.RemoveListener(OnParticipationToggled);
        if (completeAnatomyToggle != null)
            completeAnatomyToggle.onValueChanged.RemoveListener(OnCompleteAnatomyToggled);
    }

    public void Init(NetworkedMonitor nm)
    {
        if (initialised)
            return;

        initialised = true;

        ResolveAudioControls();

        if (participationToggle == null)
            participationToggle = ResolveParticipationToggle();

        if (completeAnatomyToggle == null)
            completeAnatomyToggle = ResolveCompleteAnatomyToggle();

        SyncStateFromServer(nm);
        WireListeners();
    }

    private void SyncStateFromServer(NetworkedMonitor nm)
    {
        if (introToggle != null)
            introToggle.SetIsOnWithoutNotify(nm.ShouldPlayIntro);

        if (pingPongToggle != null)
            pingPongToggle.SetIsOnWithoutNotify(nm.ShouldPlayPingPong);

        if (organsOfGenerationToggle != null)
            organsOfGenerationToggle.SetIsOnWithoutNotify(nm.ShouldPlayOrgansOfGeneration);

        if (heartToggle != null)
            heartToggle.SetIsOnWithoutNotify(nm.ShouldPlayHeart);

        if (vibrationToggle != null)
            vibrationToggle.SetIsOnWithoutNotify(nm.ShouldPlayVibration);

        if (groupColorToggle != null)
            groupColorToggle.SetIsOnWithoutNotify(nm.GroupColorModeActive);

        if (participationToggle != null)
            participationToggle.SetIsOnWithoutNotify(nm.ParticipationMode);

        if (completeAnatomyToggle != null)
            completeAnatomyToggle.SetIsOnWithoutNotify(nm.CompleteAnatomyMode);
    }

    private void WireListeners()
    {
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);

        if (fadeInButton != null)
            fadeInButton.onClick.AddListener(OnFadeInClicked);
        if (fadeOutButton != null)
            fadeOutButton.onClick.AddListener(OnFadeOutClicked);
        if (muteButton != null)
            muteButton.onClick.AddListener(OnMuteClicked);
        if (resetButton != null)
            resetButton.onClick.AddListener(OnResetClicked);

        if (introToggle != null)
            introToggle.onValueChanged.AddListener(OnIntroToggled);
        if (pingPongToggle != null)
            pingPongToggle.onValueChanged.AddListener(OnPingPongToggled);
        if (organsOfGenerationToggle != null)
            organsOfGenerationToggle.onValueChanged.AddListener(OnOrgansOfGenerationToggled);
        if (heartToggle != null)
            heartToggle.onValueChanged.AddListener(OnHeartToggled);
        if (vibrationToggle != null)
            vibrationToggle.onValueChanged.AddListener(OnVibrationToggled);
        if (groupColorToggle != null)
            groupColorToggle.onValueChanged.AddListener(OnGroupColorToggled);
        if (participationToggle != null)
            participationToggle.onValueChanged.AddListener(OnParticipationToggled);
        if (completeAnatomyToggle != null)
            completeAnatomyToggle.onValueChanged.AddListener(OnCompleteAnatomyToggled);
    }

    private void OnMasterVolumeChanged(float value)
    {
        Instances.NetworkedMonitor.SetMasterVolume(value);
    }

    private void OnFadeInClicked()
    {
        Instances.NetworkedMonitor.TriggerMasterFadeIn();
    }

    private void OnFadeOutClicked()
    {
        Instances.NetworkedMonitor.TriggerMasterFadeOut();
    }

    private void OnMuteClicked()
    {
        Instances.NetworkedMonitor.TriggerMasterMute();
    }

    private void OnResetClicked()
    {
        Instances.NetworkedMonitor.TriggerMasterReset();
    }

    private void OnIntroToggled(bool value)
    {
        Instances.NetworkedMonitor.SetShouldPlayIntro(value);
    }

    private void OnPingPongToggled(bool value)
    {
        Instances.NetworkedMonitor.SetShouldPlayPingPong(value);
    }

    private void OnOrgansOfGenerationToggled(bool value)
    {
        Instances.NetworkedMonitor.SetShouldPlayOrgansOfGeneration(value);
    }

    private void OnHeartToggled(bool value)
    {
        Instances.NetworkedMonitor.SetShouldPlayHeart(value);
    }

    private void OnVibrationToggled(bool value)
    {
        Instances.NetworkedMonitor.SetShouldPlayVibration(value);
    }

    private void OnGroupColorToggled(bool value)
    {
        Instances.NetworkedMonitor.SetGroupColorModeActive(value);
    }

    private void OnParticipationToggled(bool value)
    {
        Instances.NetworkedMonitor.SetParticipationMode(value);
    }

    private void OnCompleteAnatomyToggled(bool value)
    {
        Instances.NetworkedMonitor.SetCompleteAnatomyMode(value);
    }

    private void ResolveAudioControls()
    {
        introToggle = ResolveToggle(introToggle, "Intro Toggle", "Organs Of Nutrition Toggle");
        pingPongToggle = ResolveToggle(pingPongToggle, "Ping Pong Toggle");
        organsOfGenerationToggle = ResolveToggle(organsOfGenerationToggle, "Organs Of Generation Toggle");
        heartToggle = ResolveToggle(heartToggle, "Heart Toggle");
        vibrationToggle = ResolveToggle(vibrationToggle, "Vibration Toggle");
        groupColorToggle = ResolveToggle(groupColorToggle, "Group Color Toggle");

        introLabel = ResolveLabel(introLabel, "Intro Label", "Organs Of Nutrition Label");
        pingPongLabel = ResolveLabel(pingPongLabel, "Ping Pong Label");
        organsOfGenerationLabel = ResolveLabel(organsOfGenerationLabel, "Organs Of Generation Label");
        heartLabel = ResolveLabel(heartLabel, "Heart Label");
        vibrationLabel = ResolveLabel(vibrationLabel, "Vibration Label");
        groupColorLabel = ResolveLabel(groupColorLabel, "Group Color Label");

        if (introToggle != null)
            RenameObject(introToggle.gameObject, "Intro Toggle");
        if (introLabel != null)
        {
            RenameObject(introLabel.gameObject, "Intro Label");
            introLabel.text = "Intro";
        }

        if (pingPongToggle != null)
        {
            RenameObject(pingPongToggle.gameObject, "Ping Pong Toggle");
            pingPongToggle.SetIsOnWithoutNotify(false);
        }

        if (pingPongLabel != null)
        {
            RenameObject(pingPongLabel.gameObject, "Ping Pong Label");
            pingPongLabel.text = "Ping Pong";
        }

        if (groupColorToggle != null)
        {
            RenameObject(groupColorToggle.gameObject, "Group Color Toggle");
            groupColorToggle.SetIsOnWithoutNotify(false);
        }

        if (groupColorLabel != null)
        {
            RenameObject(groupColorLabel.gameObject, "Group Color Label");
            groupColorLabel.text = "Go To Your Color";
        }

        if (vibrationToggle != null)
        {
            RenameObject(vibrationToggle.gameObject, "Vibration Toggle");
            vibrationToggle.SetIsOnWithoutNotify(false);
        }

        if (vibrationLabel != null)
        {
            RenameObject(vibrationLabel.gameObject, "Vibration Label");
            vibrationLabel.text = "Vibration";
        }
    }

    private Toggle ResolveToggle(Toggle current, params string[] objectNames)
    {
        if (current != null)
            return current;

        foreach (string objectName in objectNames)
        {
            GameObject existing = GameObject.Find(objectName);
            if (existing != null && existing.TryGetComponent(out Toggle toggle))
                return toggle;
        }

        return null;
    }

    private Text ResolveLabel(Text current, params string[] objectNames)
    {
        if (current != null)
            return current;

        foreach (string objectName in objectNames)
        {
            GameObject existing = GameObject.Find(objectName);
            if (existing != null && existing.TryGetComponent(out Text label))
                return label;
        }

        return null;
    }

    private void RenameObject(GameObject gameObject, string objectName)
    {
        if (gameObject != null)
            gameObject.name = objectName;
    }

    private Toggle ResolveCompleteAnatomyToggle()
    {
        if (completeAnatomyToggle != null)
            return completeAnatomyToggle;

        GameObject existing = GameObject.Find("Complete Anatomy");
        if (existing != null && existing.TryGetComponent(out Toggle existingToggle))
            return existingToggle;

        return null;
    }

    private Toggle ResolveParticipationToggle()
    {
        if (participationToggle != null)
            return participationToggle;

        GameObject existing = GameObject.Find("Participation");
        if (existing != null && existing.TryGetComponent(out Toggle existingToggle))
            return existingToggle;

        return null;
    }
}
