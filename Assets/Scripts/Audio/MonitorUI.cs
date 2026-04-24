using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using TMPro;

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
    private const string ResetAllButtonObjectName = "Reset All Button";
    private const string ResetAllButtonLabel = "Reset All";
    private const string PartShortNameObjectName = "Part Short Name";
    private static readonly Color ResetAllButtonColor = new(0.8f, 0.17f, 0.17f, 1f);
    private static readonly Color ResetAllButtonHighlightColor = new(0.9f, 0.3f, 0.3f, 1f);
    private static readonly Color ResetAllButtonPressedColor = new(0.62f, 0.1f, 0.1f, 1f);
    private static readonly Vector2 ResetAllButtonOffset = new(250f, 0f);

    [Header("Master Volume")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Button fadeInButton;
    [SerializeField] private Button fadeOutButton;
    [SerializeField] private Button muteButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button resetAllButton;

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

    [Header("Part Controls")]
    [SerializeField] private Button decrementPartButton;
    [SerializeField] private Button incrementPartButton;

    [Header("Part Display")]
    [SerializeField] private TMP_Text partShortNameText;
    [SerializeField] private string[] partShortNames =
    {
        "Tutorial",
        "Nutrition",
        "Veins",
        "Generation",
        "Muscles",
        "Words",
        "Nerves",
        "Bones",
        "Brain",
        "Senses",
        "Heart",
    };

    private bool initialised;
    private Text introLabel;
    private Text pingPongLabel;
    private Text organsOfGenerationLabel;
    private Text heartLabel;
    private Text vibrationLabel;
    private Text groupColorLabel;

    private void Start()
    {
        resetButton = ResolveButton(resetButton, "Reset Button");
        resetAllButton = ResolveButton(resetAllButton, ResetAllButtonObjectName);
        ResolveAudioControls();
        participationToggle = ResolveParticipationToggle();
        completeAnatomyToggle = ResolveCompleteAnatomyToggle();
        EnsureRuntimeUi();
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
        if (resetAllButton != null)
            resetAllButton.onClick.RemoveListener(OnResetAllClicked);

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
        if (decrementPartButton != null)
            decrementPartButton.onClick.RemoveListener(OnDecrementPartClicked);
        if (incrementPartButton != null)
            incrementPartButton.onClick.RemoveListener(OnIncrementPartClicked);
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

        if (resetButton == null)
            resetButton = ResolveButton(resetButton, "Reset Button");

        if (resetAllButton == null)
            resetAllButton = ResolveButton(resetAllButton, ResetAllButtonObjectName);

        if (decrementPartButton == null)
            decrementPartButton = ResolveButton(decrementPartButton, "Part Number Decrement Button");

        if (incrementPartButton == null)
            incrementPartButton = ResolveButton(incrementPartButton, "Part Number Increment Button");

        EnsureRuntimeUi();
        SyncStateFromServer(nm);
        SetCurrentPartState(nm.CurrentPart);
        WireListeners();
    }

    private void SyncStateFromServer(NetworkedMonitor nm)
    {
        SetMasterVolumeState(nm.MasterVolume);

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
        if (resetAllButton != null)
            resetAllButton.onClick.AddListener(OnResetAllClicked);

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
        if (decrementPartButton != null)
            decrementPartButton.onClick.AddListener(OnDecrementPartClicked);
        if (incrementPartButton != null)
            incrementPartButton.onClick.AddListener(OnIncrementPartClicked);
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

    private void OnResetAllClicked()
    {
        Instances.NetworkedMonitor.ResetAllForConcert();
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

    private void OnDecrementPartClicked()
    {
        Instances.NetworkedMonitor.DecrementPart();
    }

    private void OnIncrementPartClicked()
    {
        Instances.NetworkedMonitor.IncrementPart();
    }

    public void SetCompleteAnatomyModeState(bool value)
    {
        if (completeAnatomyToggle != null)
            completeAnatomyToggle.SetIsOnWithoutNotify(value);
    }

    public void SetIntroState(bool value)
    {
        if (introToggle != null)
            introToggle.SetIsOnWithoutNotify(value);
    }

    public void SetPingPongState(bool value)
    {
        if (pingPongToggle != null)
            pingPongToggle.SetIsOnWithoutNotify(value);
    }

    public void SetOrgansOfGenerationState(bool value)
    {
        if (organsOfGenerationToggle != null)
            organsOfGenerationToggle.SetIsOnWithoutNotify(value);
    }

    public void SetHeartState(bool value)
    {
        if (heartToggle != null)
            heartToggle.SetIsOnWithoutNotify(value);
    }

    public void SetVibrationState(bool value)
    {
        if (vibrationToggle != null)
            vibrationToggle.SetIsOnWithoutNotify(value);
    }

    public void SetGroupColorState(bool value)
    {
        if (groupColorToggle != null)
            groupColorToggle.SetIsOnWithoutNotify(value);
    }

    public void SetParticipationModeState(bool value)
    {
        if (participationToggle != null)
            participationToggle.SetIsOnWithoutNotify(value);
    }

    public void SetCurrentPartState(int part)
    {
        EnsureRuntimeUi();

        if (partShortNameText != null)
            partShortNameText.text = ResolvePartShortName(part);
    }

    public void SetMasterVolumeState(float value)
    {
        SetMasterVolumeSliderValue(value);
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
            vibrationLabel.text = "Vibrations";
        }
    }

    private void EnsureRuntimeUi()
    {
        EnsureResetAllButton();
        EnsurePartShortNameText();
    }

    private void EnsureResetAllButton()
    {
        if (resetButton == null)
            resetButton = ResolveButton(resetButton, "Reset Button");

        if (resetAllButton == null)
            resetAllButton = ResolveButton(resetAllButton, ResetAllButtonObjectName);

        if (resetAllButton == null)
            resetAllButton = CreateResetAllButton();

        if (resetAllButton == null)
            return;

        if (resetAllButton.targetGraphic is Graphic graphic)
            graphic.color = ResetAllButtonColor;

        ColorBlock colors = resetAllButton.colors;
        colors.normalColor = ResetAllButtonColor;
        colors.highlightedColor = ResetAllButtonHighlightColor;
        colors.pressedColor = ResetAllButtonPressedColor;
        colors.selectedColor = ResetAllButtonHighlightColor;
        resetAllButton.colors = colors;

        TMP_Text tmpText = resetAllButton.GetComponentInChildren<TMP_Text>(true);
        if (tmpText != null)
        {
            tmpText.text = ResetAllButtonLabel;
            tmpText.color = Color.white;
            return;
        }

        Text text = resetAllButton.GetComponentInChildren<Text>(true);
        if (text != null)
        {
            text.text = ResetAllButtonLabel;
            text.color = Color.white;
        }
    }

    private Button CreateResetAllButton()
    {
        if (resetButton == null)
            return null;

        GameObject buttonObject = Instantiate(resetButton.gameObject, resetButton.transform.parent);
        buttonObject.name = ResetAllButtonObjectName;
        buttonObject.transform.SetSiblingIndex(resetButton.transform.GetSiblingIndex() + 1);

        RectTransform sourceRect = resetButton.GetComponent<RectTransform>();
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        if (sourceRect != null && buttonRect != null)
            buttonRect.anchoredPosition = sourceRect.anchoredPosition + ResetAllButtonOffset;

        return buttonObject.GetComponent<Button>();
    }

    private void EnsurePartShortNameText()
    {
        if (partShortNameText != null)
            return;

        GameObject existing = GameObject.Find(PartShortNameObjectName);
        if (existing != null && existing.TryGetComponent(out TMP_Text existingText))
        {
            partShortNameText = existingText;
            return;
        }

        GameObject partNumberObject = GameObject.Find("Part Number");
        if (partNumberObject == null)
            return;

        RectTransform partNumberRect = partNumberObject.GetComponent<RectTransform>();
        RectTransform parentRect = partNumberRect != null ? partNumberRect.parent as RectTransform : null;
        if (partNumberRect == null || parentRect == null)
            return;

        GameObject labelObject = new(PartShortNameObjectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(parentRect, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = partNumberRect.anchorMin;
        labelRect.anchorMax = partNumberRect.anchorMax;
        labelRect.pivot = new Vector2(0f, 0.5f);
        labelRect.anchoredPosition = partNumberRect.anchoredPosition + new Vector2(105f, 0f);
        labelRect.sizeDelta = new Vector2(230f, Mathf.Max(48f, partNumberRect.rect.height));

        partShortNameText = labelObject.GetComponent<TMP_Text>();
        ConfigurePartShortNameTextStyle(partShortNameText);
    }

    private void ConfigurePartShortNameTextStyle(TMP_Text target)
    {
        if (target == null)
            return;

        TMP_Text styleSource = null;
        GameObject titleObject = GameObject.Find("Part Number Title");
        if (titleObject != null)
            styleSource = titleObject.GetComponent<TMP_Text>();

        if (styleSource != null)
        {
            target.font = styleSource.font;
            target.fontSharedMaterial = styleSource.fontSharedMaterial;
            target.color = styleSource.color;
            target.fontSize = styleSource.fontSize;
        }
        else
        {
            target.color = Color.white;
            target.fontSize = 36f;
        }

        target.enableAutoSizing = true;
        target.fontSizeMin = 20f;
        target.fontSizeMax = Mathf.Max(target.fontSize, 36f);
        target.alignment = TextAlignmentOptions.Left;
        target.text = ResolvePartShortName(0);
        target.raycastTarget = false;
    }

    private void SetMasterVolumeSliderValue(float value)
    {
        if (masterVolumeSlider != null)
            masterVolumeSlider.SetValueWithoutNotify(value);
    }

    private string ResolvePartShortName(int part)
    {
        if (partShortNames != null &&
            partShortNames.Length > 0)
        {
            int index = Mathf.Clamp(part, 0, partShortNames.Length - 1);
            if (index >= 0 &&
                index < partShortNames.Length &&
                !string.IsNullOrWhiteSpace(partShortNames[index]))
            {
                return partShortNames[index];
            }
        }

        return $"Part {Mathf.Max(0, part)}";
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

    private Button ResolveButton(Button current, params string[] objectNames)
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
