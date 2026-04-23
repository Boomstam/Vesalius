using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Monitor-side UI. Communicates exclusively through NetworkedMonitor —
/// never touches AudioManager directly (AudioManager lives on the client).
///
/// Initialisation is driven by NetworkedMonitor.OnStartClient(), which calls
/// Init() once the NetworkObject is fully spawned and SyncVar values are valid.
/// This guarantees a late-connecting Monitor sees the correct UI state immediately
/// without polling via a coroutine.
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

    private bool _initialised;

    // ── Unity Lifecycle ────────────────────────────────────────────────────────

    private void Start()
    {
        Debug.Log("[MonitorUI] Start() called.");
        completeAnatomyToggle = ResolveCompleteAnatomyToggle();
        Debug.Log($"[MonitorUI] After Start(), completeAnatomyToggle={(completeAnatomyToggle != null ? completeAnatomyToggle.name : "NULL")}");
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
    /// Called by NetworkedMonitor.OnStartClient() once the NetworkObject is spawned
    /// and SyncVar values are authoritative. Syncs all controls to current server
    /// state, then wires change listeners.
    /// Safe to call more than once — subsequent calls are ignored.
    /// </summary>
    public void Init(NetworkedMonitor nm)
    {
        Debug.Log($"[MonitorUI] Init() called. Already initialised: {_initialised}");

        if (_initialised) return;
        _initialised = true;

        // completeAnatomyToggle may not be resolved yet if OnStartClient fires
        // before Start() on this MonoBehaviour (edge case on the same frame).
        if (completeAnatomyToggle == null)
        {
            Debug.Log("[MonitorUI] completeAnatomyToggle was null at Init() — resolving now.");
            completeAnatomyToggle = ResolveCompleteAnatomyToggle();
        }

        Debug.Log($"[MonitorUI] References check —" +
            $"\n  masterVolumeSlider={masterVolumeSlider != null}" +
            $"\n  fadeInButton={fadeInButton != null}" +
            $"\n  fadeOutButton={fadeOutButton != null}" +
            $"\n  muteButton={muteButton != null}" +
            $"\n  resetButton={resetButton != null}" +
            $"\n  organsOfNutritionToggle={organsOfNutritionToggle != null}" +
            $"\n  organsOfGenerationToggle={organsOfGenerationToggle != null}" +
            $"\n  heartToggle={heartToggle != null}" +
            $"\n  completeAnatomyToggle={completeAnatomyToggle != null}");

        SyncStateFromServer(nm);
        WireListeners();
    }

    /// <summary>
    /// Pushes current SyncVar values into controls without notifying listeners,
    /// so the UI reflects server state without sending redundant RPCs.
    /// </summary>
    private void SyncStateFromServer(NetworkedMonitor nm)
    {
        Debug.Log($"[MonitorUI] SyncStateFromServer —" +
            $"\n  ShouldPlayOrgansOfNutrition={nm.ShouldPlayOrgansOfNutrition}" +
            $"\n  ShouldPlayOrgansOfGeneration={nm.ShouldPlayOrgansOfGeneration}" +
            $"\n  ShouldPlayHeart={nm.ShouldPlayHeart}" +
            $"\n  CompleteAnatomyMode={nm.CompleteAnatomyMode}");

        if (organsOfNutritionToggle != null)
            organsOfNutritionToggle.SetIsOnWithoutNotify(nm.ShouldPlayOrgansOfNutrition);
        else
            Debug.LogWarning("[MonitorUI] organsOfNutritionToggle is null — skipping sync.");

        if (organsOfGenerationToggle != null)
            organsOfGenerationToggle.SetIsOnWithoutNotify(nm.ShouldPlayOrgansOfGeneration);
        else
            Debug.LogWarning("[MonitorUI] organsOfGenerationToggle is null — skipping sync.");

        if (heartToggle != null)
            heartToggle.SetIsOnWithoutNotify(nm.ShouldPlayHeart);
        else
            Debug.LogWarning("[MonitorUI] heartToggle is null — skipping sync.");

        if (completeAnatomyToggle != null)
            completeAnatomyToggle.SetIsOnWithoutNotify(nm.CompleteAnatomyMode);
        else
            Debug.LogWarning("[MonitorUI] completeAnatomyToggle is null — skipping sync.");

        // Master volume slider: server doesn't track a SyncVar for this (it's fire-and-forget),
        // so we leave the slider at its default Inspector value rather than guessing.
    }

    private void WireListeners()
    {
        Debug.Log("[MonitorUI] WireListeners() — wiring all UI callbacks.");

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
        else
            Debug.LogWarning("[MonitorUI] completeAnatomyToggle is null — its listener was not wired.");

        Debug.Log("[MonitorUI] WireListeners() complete.");
    }

    // ── Master Volume ──────────────────────────────────────────────────────────

    private void OnMasterVolumeChanged(float value)
    {
        Debug.Log($"[MonitorUI] OnMasterVolumeChanged({value})");
        Instances.NetworkedMonitor.SetMasterVolume(value);
    }

    private void OnFadeInClicked()
    {
        Debug.Log("[MonitorUI] OnFadeInClicked()");
        Instances.NetworkedMonitor.TriggerMasterFadeIn();
    }

    private void OnFadeOutClicked()
    {
        Debug.Log("[MonitorUI] OnFadeOutClicked()");
        Instances.NetworkedMonitor.TriggerMasterFadeOut();
    }

    private void OnMuteClicked()
    {
        Debug.Log("[MonitorUI] OnMuteClicked()");
        Instances.NetworkedMonitor.TriggerMasterMute();
    }

    private void OnResetClicked()
    {
        Debug.Log("[MonitorUI] OnResetClicked()");
        Instances.NetworkedMonitor.TriggerMasterReset();
    }

    // ── Audio Mode Toggles ─────────────────────────────────────────────────────

    private void OnOrgansOfNutritionToggled(bool value)
    {
        Debug.Log($"[MonitorUI] OnOrgansOfNutritionToggled({value})");
        Instances.NetworkedMonitor.SetShouldPlayOrgansOfNutrition(value);
    }

    private void OnOrgansOfGenerationToggled(bool value)
    {
        Debug.Log($"[MonitorUI] OnOrgansOfGenerationToggled({value})");
        Instances.NetworkedMonitor.SetShouldPlayOrgansOfGeneration(value);
    }

    private void OnHeartToggled(bool value)
    {
        Debug.Log($"[MonitorUI] OnHeartToggled({value})");
        Instances.NetworkedMonitor.SetShouldPlayHeart(value);
    }

    private void OnCompleteAnatomyToggled(bool value)
    {
        Debug.Log($"[MonitorUI] OnCompleteAnatomyToggled({value})");
        Instances.NetworkedMonitor.SetCompleteAnatomyMode(value);
    }

    // ── Complete Anatomy Toggle ────────────────────────────────────────────────

    private Toggle ResolveCompleteAnatomyToggle()
    {
        if (completeAnatomyToggle != null)
        {
            Debug.Log("[MonitorUI] ResolveCompleteAnatomyToggle — already assigned in Inspector.");
            return completeAnatomyToggle;
        }

        GameObject existing = GameObject.Find("Complete Anatomy");
        if (existing != null && existing.TryGetComponent(out Toggle existingToggle))
        {
            Debug.Log("[MonitorUI] ResolveCompleteAnatomyToggle — found existing 'Complete Anatomy' GameObject.");
            return existingToggle;
        }

        Debug.Log("[MonitorUI] ResolveCompleteAnatomyToggle — no existing toggle found, creating new one.");
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

        Debug.Log("[MonitorUI] CreateCompleteAnatomyToggle — toggle created successfully.");
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