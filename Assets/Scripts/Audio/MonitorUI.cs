using TMPro;
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
    private static readonly Vector2 IntroToggleMin = new(0.525f, 0.79f);
    private static readonly Vector2 IntroToggleMax = new(0.555f, 0.845f);
    private static readonly Vector2 PingPongToggleMin = new(0.645f, 0.79f);
    private static readonly Vector2 PingPongToggleMax = new(0.675f, 0.845f);
    private static readonly Vector2 GenerationToggleMin = new(0.765f, 0.79f);
    private static readonly Vector2 GenerationToggleMax = new(0.795f, 0.845f);
    private static readonly Vector2 HeartToggleMin = new(0.885f, 0.79f);
    private static readonly Vector2 HeartToggleMax = new(0.915f, 0.845f);

    private static readonly Vector2 IntroLabelMin = new(0.47f, 0.855f);
    private static readonly Vector2 IntroLabelMax = new(0.61f, 0.915f);
    private static readonly Vector2 PingPongLabelMin = new(0.59f, 0.855f);
    private static readonly Vector2 PingPongLabelMax = new(0.73f, 0.915f);
    private static readonly Vector2 GenerationLabelMin = new(0.71f, 0.855f);
    private static readonly Vector2 GenerationLabelMax = new(0.87f, 0.915f);
    private static readonly Vector2 HeartLabelMin = new(0.83f, 0.855f);
    private static readonly Vector2 HeartLabelMax = new(0.97f, 0.915f);

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

    [Header("View Toggles")]
    [SerializeField] private Toggle completeAnatomyToggle;

    private bool initialised;
    private Text introLabel;
    private Text pingPongLabel;
    private Text organsOfGenerationLabel;
    private Text heartLabel;

    private void Start()
    {
        EnsureAudioToggleLayout();
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
        if (completeAnatomyToggle != null)
            completeAnatomyToggle.onValueChanged.RemoveListener(OnCompleteAnatomyToggled);
    }

    public void Init(NetworkedMonitor nm)
    {
        if (initialised)
            return;

        initialised = true;

        EnsureAudioToggleLayout();

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

    private void OnCompleteAnatomyToggled(bool value)
    {
        Instances.NetworkedMonitor.SetCompleteAnatomyMode(value);
    }

    private void EnsureAudioToggleLayout()
    {
        introToggle = ResolveToggle(introToggle, "Intro Toggle", "Organs Of Nutrition Toggle");
        organsOfGenerationToggle = ResolveToggle(organsOfGenerationToggle, "Organs Of Generation Toggle");
        heartToggle = ResolveToggle(heartToggle, "Heart Toggle");

        introLabel = ResolveLabel(introLabel, "Intro Label", "Organs Of Nutrition Label");
        organsOfGenerationLabel = ResolveLabel(organsOfGenerationLabel, "Organs Of Generation Label");
        heartLabel = ResolveLabel(heartLabel, "Heart Label");

        if (introToggle != null)
            RenameObject(introToggle.gameObject, "Intro Toggle");
        if (introLabel != null)
        {
            RenameObject(introLabel.gameObject, "Intro Label");
            introLabel.text = "Intro";
        }

        if (pingPongToggle == null && introToggle != null)
            pingPongToggle = DuplicateToggle(introToggle, "Ping Pong Toggle");

        if (pingPongLabel == null && introLabel != null)
            pingPongLabel = DuplicateLabel(introLabel, "Ping Pong Label", "Ping Pong");

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

        ApplyAudioToggleLayout();
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

    private Toggle DuplicateToggle(Toggle template, string objectName)
    {
        Toggle duplicate = Instantiate(template, template.transform.parent);
        duplicate.SetIsOnWithoutNotify(false);
        duplicate.onValueChanged.RemoveAllListeners();
        RenameObject(duplicate.gameObject, objectName);
        return duplicate;
    }

    private Text DuplicateLabel(Text template, string objectName, string text)
    {
        Text duplicate = Instantiate(template, template.transform.parent);
        duplicate.text = text;
        RenameObject(duplicate.gameObject, objectName);
        return duplicate;
    }

    private void ApplyAudioToggleLayout()
    {
        SetAnchors(introToggle, IntroToggleMin, IntroToggleMax);
        SetAnchors(pingPongToggle, PingPongToggleMin, PingPongToggleMax);
        SetAnchors(organsOfGenerationToggle, GenerationToggleMin, GenerationToggleMax);
        SetAnchors(heartToggle, HeartToggleMin, HeartToggleMax);

        SetAnchors(introLabel, IntroLabelMin, IntroLabelMax);
        SetAnchors(pingPongLabel, PingPongLabelMin, PingPongLabelMax);
        SetAnchors(organsOfGenerationLabel, GenerationLabelMin, GenerationLabelMax);
        SetAnchors(heartLabel, HeartLabelMin, HeartLabelMax);
    }

    private void SetAnchors(Component component, Vector2 anchorMin, Vector2 anchorMax)
    {
        if (component == null || component.transform is not RectTransform rectTransform)
            return;

        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;
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

        return CreateCompleteAnatomyToggle();
    }

    private Toggle CreateCompleteAnatomyToggle()
    {
        GameObject partNumber = GameObject.Find("Part Number");
        if (partNumber == null || !partNumber.TryGetComponent(out RectTransform partNumberRect))
            return null;

        Transform parent = partNumberRect.parent;
        GameObject root = new GameObject("Complete Anatomy", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Toggle));
        root.layer = partNumber.layer;
        root.transform.SetParent(parent, false);

        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = partNumberRect.anchorMin;
        rect.anchorMax = partNumberRect.anchorMax;
        rect.pivot = partNumberRect.pivot;
        rect.sizeDelta = new Vector2(partNumberRect.sizeDelta.x, 56f);
        rect.anchoredPosition = partNumberRect.anchoredPosition + new Vector2(0f, 150f);

        Image background = root.GetComponent<Image>();
        background.color = new Color(0.18f, 0.22f, 0.26f, 1f);

        Toggle toggle = root.GetComponent<Toggle>();
        toggle.targetGraphic = background;
        toggle.isOn = false;

        Image checkBackground = CreateImageChild(root.transform, "Check Background", new Color(0.08f, 0.1f, 0.12f, 1f));
        RectTransform checkBackgroundRect = checkBackground.rectTransform;
        checkBackgroundRect.anchorMin = new Vector2(0f, 0.5f);
        checkBackgroundRect.anchorMax = new Vector2(0f, 0.5f);
        checkBackgroundRect.pivot = new Vector2(0.5f, 0.5f);
        checkBackgroundRect.sizeDelta = new Vector2(36f, 36f);
        checkBackgroundRect.anchoredPosition = new Vector2(32f, 0f);

        Image checkmark = CreateImageChild(checkBackground.transform, "Checkmark", new Color(0.75f, 0.92f, 1f, 1f));
        RectTransform checkmarkRect = checkmark.rectTransform;
        checkmarkRect.anchorMin = new Vector2(0.5f, 0.5f);
        checkmarkRect.anchorMax = new Vector2(0.5f, 0.5f);
        checkmarkRect.pivot = new Vector2(0.5f, 0.5f);
        checkmarkRect.sizeDelta = new Vector2(20f, 20f);
        checkmarkRect.anchoredPosition = Vector2.zero;
        toggle.graphic = checkmark;
        toggle.SetIsOnWithoutNotify(false);
        checkmark.enabled = false;

        TextMeshProUGUI label = CreateLabelChild(root.transform);
        label.text = "Complete Anatomy";

        return toggle;
    }

    private Image CreateImageChild(Transform parent, string name, Color color)
    {
        GameObject child = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        child.layer = parent.gameObject.layer;
        child.transform.SetParent(parent, false);

        Image image = child.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private TextMeshProUGUI CreateLabelChild(Transform parent)
    {
        GameObject child = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        child.layer = parent.gameObject.layer;
        child.transform.SetParent(parent, false);

        RectTransform rect = child.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(64f, 0f);
        rect.offsetMax = new Vector2(-16f, 0f);

        TextMeshProUGUI label = child.GetComponent<TextMeshProUGUI>();
        label.color = Color.white;
        label.fontSize = 30f;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.raycastTarget = false;
        return label;
    }
}
