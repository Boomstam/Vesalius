using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Monitor-side UI. Communicates exclusively through NetworkedMonitor —
/// never touches AudioManager directly (AudioManager lives on the client).
///
/// Required UI elements (wire in Inspector):
///   Master section:  masterVolumeSlider, fadeInButton, fadeOutButton,
///                    muteButton, resetButton
///   Audio toggles:   organsOfNutritionToggle, organsOfGenerationToggle, heartToggle
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
    [SerializeField] private Toggle organsOfNutritionToggle;
    [SerializeField] private Toggle organsOfGenerationToggle;
    [SerializeField] private Toggle heartToggle;

    [Header("View Toggles")]
    [SerializeField] private Toggle completeAnatomyToggle;

    // ── Unity Lifecycle ────────────────────────────────────────────────────────

    private void Start()
    {
        completeAnatomyToggle = ResolveCompleteAnatomyToggle();

        masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);

        fadeInButton.onClick.AddListener(OnFadeInClicked);
        fadeOutButton.onClick.AddListener(OnFadeOutClicked);
        muteButton.onClick.AddListener(OnMuteClicked);
        resetButton.onClick.AddListener(OnResetClicked);

        organsOfNutritionToggle.onValueChanged.AddListener(OnOrgansOfNutritionToggled);
        organsOfGenerationToggle.onValueChanged.AddListener(OnOrgansOfGenerationToggled);
        heartToggle.onValueChanged.AddListener(OnHeartToggled);

        if (completeAnatomyToggle != null)
            completeAnatomyToggle.onValueChanged.AddListener(OnCompleteAnatomyToggled);
    }

    // ── Master Volume ──────────────────────────────────────────────────────────

    private void OnMasterVolumeChanged(float value)
    {
        Instances.NetworkedMonitor.SetMasterVolume(value);
    }

    private void OnFadeInClicked()  => Instances.NetworkedMonitor.TriggerMasterFadeIn();
    private void OnFadeOutClicked() => Instances.NetworkedMonitor.TriggerMasterFadeOut();
    private void OnMuteClicked()    => Instances.NetworkedMonitor.TriggerMasterMute();
    private void OnResetClicked()   => Instances.NetworkedMonitor.TriggerMasterReset();

    // ── Audio Mode Toggles ─────────────────────────────────────────────────────

    private void OnOrgansOfNutritionToggled(bool value)
    {
        Instances.NetworkedMonitor.SetShouldPlayOrgansOfNutrition(value);
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
        {
            Debug.LogWarning("[MonitorUI] Could not create Complete Anatomy toggle because Part Number was not found.");
            return null;
        }

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
