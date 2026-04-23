using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Monitor-side UI. Communicates exclusively through NetworkedMonitor —
/// never touches AudioManager directly (AudioManager lives on the client).
///
/// Waits for NetworkedMonitor to be available, then mirrors current server
/// SyncVar state into all controls before wiring listeners, so a late-connecting
/// Monitor sees the correct UI state immediately.
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
        StartCoroutine(InitAfterNetworkedMonitorCoroutine());
    }

    private void OnDestroy()
    {
        if (masterVolumeSlider != null) masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
        if (fadeInButton != null)       fadeInButton.onClick.RemoveListener(OnFadeInClicked);
        if (fadeOutButton != null)      fadeOutButton.onClick.RemoveListener(OnFadeOutClicked);
        if (muteButton != null)         muteButton.onClick.RemoveListener(OnMuteClicked);
        if (resetButton != null)        resetButton.onClick.RemoveListener(OnResetClicked);

        if (organsOfNutritionToggle != null)  organsOfNutritionToggle.onValueChanged.RemoveListener(OnOrgansOfNutritionToggled);
        if (organsOfGenerationToggle != null) organsOfGenerationToggle.onValueChanged.RemoveListener(OnOrgansOfGenerationToggled);
        if (heartToggle != null)              heartToggle.onValueChanged.RemoveListener(OnHeartToggled);
        if (completeAnatomyToggle != null)    completeAnatomyToggle.onValueChanged.RemoveListener(OnCompleteAnatomyToggled);
    }

    // ── Initialisation ─────────────────────────────────────────────────────────

    /// <summary>
    /// Waits until Instances.NetworkedMonitor is available (the NetworkObject must
    /// be spawned before we can read SyncVar values), then syncs all controls to the
    /// current server state and wires the change listeners.
    /// </summary>
    private IEnumerator InitAfterNetworkedMonitorCoroutine()
    {
        while (Instances.NetworkedMonitor == null)
            yield return new WaitForSeconds(0.5f);

        Debug.Log("[MonitorUI] NetworkedMonitor found — syncing UI state.");
        SyncStateFromServer(Instances.NetworkedMonitor);
        WireListeners();
    }

    /// <summary>
    /// Pushes current SyncVar values into controls without notifying listeners,
    /// so the UI reflects server state without sending redundant RPCs.
    /// </summary>
    private void SyncStateFromServer(NetworkedMonitor nm)
    {
        if (organsOfNutritionToggle != null)
            organsOfNutritionToggle.SetIsOnWithoutNotify(nm.ShouldPlayOrgansOfNutrition);

        if (organsOfGenerationToggle != null)
            organsOfGenerationToggle.SetIsOnWithoutNotify(nm.ShouldPlayOrgansOfGeneration);

        if (heartToggle != null)
            heartToggle.SetIsOnWithoutNotify(nm.ShouldPlayHeart);

        if (completeAnatomyToggle != null)
            completeAnatomyToggle.SetIsOnWithoutNotify(nm.CompleteAnatomyMode);

        // Master volume slider: server doesn't track a SyncVar for this (it's fire-and-forget),
        // so we leave the slider at its default Inspector value rather than guessing.
    }

    private void WireListeners()
    {
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

    // ── Complete Anatomy Toggle ────────────────────────────────────────────────

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