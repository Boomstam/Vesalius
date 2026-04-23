using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows the correct global slider overlay for the currently active audio mode,
/// routes slider changes back into the AudioManager, and bootstraps the overlay
/// layout from the existing tutorial slider visuals at runtime.
/// </summary>
public class GlobalAudioSliderOverlay : MonoBehaviour
{
    public static GlobalAudioSliderOverlay Instance { get; private set; }

    [Header("Dual Slider Overlay")]
    [SerializeField] private Slider dualPrimarySlider;
    [SerializeField] private Slider dualSecondarySlider;
    [SerializeField] private TMP_Text dualPrimaryMinLabel;
    [SerializeField] private TMP_Text dualPrimaryMaxLabel;
    [SerializeField] private TMP_Text dualSecondaryMinLabel;
    [SerializeField] private TMP_Text dualSecondaryMaxLabel;

    [Header("Single Slider Overlay")]
    [SerializeField] private Slider singleSlider;
    [SerializeField] private TMP_Text singleMinLabel;
    [SerializeField] private TMP_Text singleMaxLabel;

    private AudioManager audioManager;
    private AudioManager.AudioOverlayKind currentKind = AudioManager.AudioOverlayKind.None;
    private bool suppressCallbacks;
    private bool subscribed;
    private bool syncingFromImageFader;

    private static readonly Vector2 DualLeftMin = new(0.15f, 0.23f);
    private static readonly Vector2 DualLeftMax = new(0.45f, 0.80f);
    private static readonly Vector2 DualRightMin = new(0.59f, 0.23f);
    private static readonly Vector2 DualRightMax = new(0.88f, 0.80f);
    private static readonly Vector2 SingleMin = new(0.33f, 0.22f);
    private static readonly Vector2 SingleMax = new(0.67f, 0.81f);

    private static readonly Vector2 DualLeftTopMin = new(0.15f, 0.84f);
    private static readonly Vector2 DualLeftTopMax = new(0.45f, 0.91f);
    private static readonly Vector2 DualLeftBottomMin = new(0.15f, 0.11f);
    private static readonly Vector2 DualLeftBottomMax = new(0.45f, 0.18f);
    private static readonly Vector2 DualRightTopMin = new(0.59f, 0.84f);
    private static readonly Vector2 DualRightTopMax = new(0.88f, 0.91f);
    private static readonly Vector2 DualRightBottomMin = new(0.59f, 0.11f);
    private static readonly Vector2 DualRightBottomMax = new(0.88f, 0.18f);
    private static readonly Vector2 SingleTopMin = new(0.33f, 0.84f);
    private static readonly Vector2 SingleTopMax = new(0.67f, 0.91f);
    private static readonly Vector2 SingleBottomMin = new(0.33f, 0.11f);
    private static readonly Vector2 SingleBottomMax = new(0.67f, 0.18f);

    private void Awake()
    {
        Instance = this;
        EnsureOverlayStructure();
        audioManager = FindAnyObjectByType<AudioManager>();
        HideAll();
    }

    private void OnEnable()
    {
        EnsureOverlayStructure();
        EnsureSubscriptions();
        RefreshState();
    }

    private void OnDisable()
    {
        RemoveSubscriptions();
        UpdateImageFaderBinding();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void EnsureSubscriptions()
    {
        if (dualPrimarySlider == null || dualSecondarySlider == null || singleSlider == null)
            return;

        if (!subscribed)
        {
            dualPrimarySlider.onValueChanged.AddListener(OnDualPrimarySliderChanged);
            dualSecondarySlider.onValueChanged.AddListener(OnDualSecondarySliderChanged);
            singleSlider.onValueChanged.AddListener(OnSingleSliderChanged);
            subscribed = true;
        }

        if (audioManager == null)
            audioManager = FindAnyObjectByType<AudioManager>();

        if (audioManager != null)
            audioManager.OverlayStateChanged -= ApplyOverlayState;

        if (audioManager != null)
            audioManager.OverlayStateChanged += ApplyOverlayState;
    }

    private void RemoveSubscriptions()
    {
        if (!subscribed) return;

        if (dualPrimarySlider != null)
            dualPrimarySlider.onValueChanged.RemoveListener(OnDualPrimarySliderChanged);
        if (dualSecondarySlider != null)
            dualSecondarySlider.onValueChanged.RemoveListener(OnDualSecondarySliderChanged);
        if (singleSlider != null)
            singleSlider.onValueChanged.RemoveListener(OnSingleSliderChanged);

        if (audioManager != null)
            audioManager.OverlayStateChanged -= ApplyOverlayState;

        subscribed = false;
    }

    private void RefreshState()
    {
        if (audioManager == null)
            audioManager = FindAnyObjectByType<AudioManager>();

        if (audioManager == null)
        {
            HideAll();
            return;
        }

        ApplyOverlayState(audioManager.CurrentOverlayState);
    }

    private void EnsureOverlayStructure()
    {
        List<Slider> topLevelSliders = transform.Cast<Transform>()
            .Select(child => child.GetComponent<Slider>())
            .Where(slider => slider != null)
            .ToList();

        if (topLevelSliders.Count < 2)
            return;

        dualPrimarySlider = topLevelSliders.OrderBy(slider => slider.transform.position.x).First();
        dualSecondarySlider = topLevelSliders.OrderBy(slider => slider.transform.position.x).Last();

        dualPrimarySlider.name = "Global Dual Slider Left";
        dualSecondarySlider.name = "Global Dual Slider Right";
        RemoveCrossfadePlayer(dualPrimarySlider.gameObject);
        RemoveCrossfadePlayer(dualSecondarySlider.gameObject);

        if (singleSlider == null)
            singleSlider = EnsureSingleSlider(dualPrimarySlider);

        singleSlider.name = "Global Single Slider";
        RemoveCrossfadePlayer(singleSlider.gameObject);

        dualPrimaryMaxLabel = CreateOrFindLabel("Global Dual Left Max Label");
        dualPrimaryMinLabel = CreateOrFindLabel("Global Dual Left Min Label");
        dualSecondaryMaxLabel = CreateOrFindLabel("Global Dual Right Max Label");
        dualSecondaryMinLabel = CreateOrFindLabel("Global Dual Right Min Label");
        singleMaxLabel = CreateOrFindLabel("Global Single Max Label");
        singleMinLabel = CreateOrFindLabel("Global Single Min Label");

        SetLabelText(dualPrimaryMaxLabel, "HIGH");
        SetLabelText(dualPrimaryMinLabel, "LOW");
        SetLabelText(dualSecondaryMaxLabel, "LONG");
        SetLabelText(dualSecondaryMinLabel, "SHORT");
        SetLabelText(singleMaxLabel, "HIGH");
        SetLabelText(singleMinLabel, "LOW");

        SetAnchoredRect((RectTransform)dualPrimarySlider.transform, DualLeftMin, DualLeftMax);
        SetAnchoredRect((RectTransform)dualSecondarySlider.transform, DualRightMin, DualRightMax);
        SetAnchoredRect((RectTransform)singleSlider.transform, SingleMin, SingleMax);

        SetAnchoredRectIfValid(dualPrimaryMaxLabel, DualLeftTopMin, DualLeftTopMax);
        SetAnchoredRectIfValid(dualPrimaryMinLabel, DualLeftBottomMin, DualLeftBottomMax);
        SetAnchoredRectIfValid(dualSecondaryMaxLabel, DualRightTopMin, DualRightTopMax);
        SetAnchoredRectIfValid(dualSecondaryMinLabel, DualRightBottomMin, DualRightBottomMax);
        SetAnchoredRectIfValid(singleMaxLabel, SingleTopMin, SingleTopMax);
        SetAnchoredRectIfValid(singleMinLabel, SingleBottomMin, SingleBottomMax);
    }

    private Slider EnsureSingleSlider(Slider template)
    {
        Transform existing = transform.Find("Global Single Slider");
        if (existing != null && existing.TryGetComponent(out Slider existingSlider))
            return existingSlider;

        Slider duplicate = Instantiate(template, transform);
        duplicate.name = "Global Single Slider";
        return duplicate;
    }

    private TMP_Text CreateOrFindLabel(string objectName)
    {
        Transform existing = transform.Find(objectName);
        if (existing != null && existing.TryGetComponent(out TMP_Text existingText))
            return existingText;

        GameObject labelObject = new(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(transform, false);

        TMP_Text label = labelObject.GetComponent<TMP_Text>();
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 36f;
        label.enableAutoSizing = true;
        label.fontSizeMin = 18f;
        label.fontSizeMax = 36f;
        label.color = Color.white;
        return label;
    }

    private void RemoveCrossfadePlayer(GameObject sliderObject)
    {
        CrossfadePlayer player = sliderObject.GetComponent<CrossfadePlayer>();
        if (player != null)
            player.enabled = false;
    }

    private void SetAnchoredRect(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax)
    {
        if (rectTransform == null) return;

        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
    }

    private void SetAnchoredRectIfValid(TMP_Text label, Vector2 anchorMin, Vector2 anchorMax)
    {
        if (label == null) return;
        SetAnchoredRect((RectTransform)label.transform, anchorMin, anchorMax);
    }

    private void OnDualPrimarySliderChanged(float value)
    {
        if (suppressCallbacks || audioManager == null) return;

        if (currentKind == AudioManager.AudioOverlayKind.HeartDual)
            audioManager.SetHeartBandFade(value);
    }

    private void OnDualSecondarySliderChanged(float value)
    {
        if (suppressCallbacks || audioManager == null) return;

        if (currentKind == AudioManager.AudioOverlayKind.HeartDual)
            audioManager.SetHeartDelay(value);
    }

    private void OnSingleSliderChanged(float value)
    {
        if (suppressCallbacks || audioManager == null) return;

        switch (currentKind)
        {
            case AudioManager.AudioOverlayKind.NutritionSingle:
                audioManager.SetOrgansOfNutritionFade(value);
                break;
            case AudioManager.AudioOverlayKind.GenerationSingle:
                audioManager.SetOrgansOfGenerationFade(value);
                break;
        }
    }

    private void ApplyOverlayState(AudioManager.AudioOverlayState state)
    {
        AudioManager.AudioOverlayKind previousKind = currentKind;

        if (!syncingFromImageFader && ShouldSyncFromImageFader(previousKind, state.Kind))
        {
            syncingFromImageFader = true;
            SyncAudioStateFromImageFader(state.Kind);
            syncingFromImageFader = false;

            if (audioManager != null)
                state = audioManager.CurrentOverlayState;
        }

        currentKind = state.Kind;
        suppressCallbacks = true;

        bool showDual = state.Kind == AudioManager.AudioOverlayKind.HeartDual;
        bool showSingle = state.Kind == AudioManager.AudioOverlayKind.NutritionSingle
                       || state.Kind == AudioManager.AudioOverlayKind.GenerationSingle;

        SetDualOverlayActive(showDual);
        SetSingleOverlayActive(showSingle);

        if (showDual)
        {
            SetSliderValue(dualPrimarySlider, state.PrimaryValue);
            SetSliderValue(dualSecondarySlider, state.SecondaryValue);
            SetLabelText(dualPrimaryMinLabel, state.PrimaryMinLabel);
            SetLabelText(dualPrimaryMaxLabel, state.PrimaryMaxLabel);
            SetLabelText(dualSecondaryMinLabel, state.SecondaryMinLabel);
            SetLabelText(dualSecondaryMaxLabel, state.SecondaryMaxLabel);
        }

        if (showSingle)
        {
            SetSliderValue(singleSlider, state.PrimaryValue);
            SetLabelText(singleMinLabel, state.PrimaryMinLabel);
            SetLabelText(singleMaxLabel, state.PrimaryMaxLabel);
        }

        suppressCallbacks = false;
        UpdateImageFaderBinding();
    }

    private void HideAll()
    {
        SetDualOverlayActive(false);
        SetSingleOverlayActive(false);
    }

    private void SetDualOverlayActive(bool isActive)
    {
        SetComponentGameObjectActive(dualPrimarySlider, isActive);
        SetComponentGameObjectActive(dualSecondarySlider, isActive);
        SetComponentGameObjectActive(dualPrimaryMinLabel, isActive);
        SetComponentGameObjectActive(dualPrimaryMaxLabel, isActive);
        SetComponentGameObjectActive(dualSecondaryMinLabel, isActive);
        SetComponentGameObjectActive(dualSecondaryMaxLabel, isActive);
    }

    private void SetSingleOverlayActive(bool isActive)
    {
        SetComponentGameObjectActive(singleSlider, isActive);
        SetComponentGameObjectActive(singleMinLabel, isActive);
        SetComponentGameObjectActive(singleMaxLabel, isActive);
    }

    private void SetSliderValue(Slider slider, float value)
    {
        if (slider == null) return;
        slider.SetValueWithoutNotify(value);
    }

    private void SetLabelText(TMP_Text label, string text)
    {
        if (label == null) return;
        label.text = text;
    }

    private void SetComponentGameObjectActive(Component component, bool isActive)
    {
        if (component == null) return;
        component.gameObject.SetActive(isActive);
    }

    public bool TryGetImageFaderDriveState(out bool isDriven, out float value)
    {
        isDriven = IsDrivingKind(currentKind);
        value = GetDrivenImageFadeValue();
        return true;
    }

    private bool ShouldSyncFromImageFader(AudioManager.AudioOverlayKind previousKind, AudioManager.AudioOverlayKind nextKind)
    {
        return previousKind != nextKind && IsDrivingKind(nextKind);
    }

    private void SyncAudioStateFromImageFader(AudioManager.AudioOverlayKind overlayKind)
    {
        ImageFader activeFader = GetActiveImageFader();
        if (activeFader == null || audioManager == null)
            return;

        float currentImageFade = activeFader.CurrentFadeValue;

        switch (overlayKind)
        {
            case AudioManager.AudioOverlayKind.NutritionSingle:
                audioManager.SetOrgansOfNutritionFade(currentImageFade);
                break;
            case AudioManager.AudioOverlayKind.GenerationSingle:
                audioManager.SetOrgansOfGenerationFade(currentImageFade);
                break;
            case AudioManager.AudioOverlayKind.HeartDual:
                float halfValue = currentImageFade * 0.5f;
                audioManager.SetHeartBandFade(halfValue);
                audioManager.SetHeartDelay(halfValue);
                break;
        }
    }

    private void UpdateImageFaderBinding()
    {
        bool isDriven = IsDrivingKind(currentKind) && isActiveAndEnabled;
        float drivenValue = GetDrivenImageFadeValue();

        foreach (ImageFader fader in EnumerateSceneImageFaders())
        {
            if (!fader.gameObject.activeInHierarchy)
                continue;

            fader.SetOverlayDriven(isDriven);

            if (isDriven)
                fader.ApplyOverlayDrivenValue(drivenValue);
        }
    }

    private float GetDrivenImageFadeValue()
    {
        return currentKind switch
        {
            AudioManager.AudioOverlayKind.NutritionSingle => singleSlider != null ? singleSlider.value : 0f,
            AudioManager.AudioOverlayKind.GenerationSingle => singleSlider != null ? singleSlider.value : 0f,
            AudioManager.AudioOverlayKind.HeartDual => GetDualAverageValue(),
            _ => 0f,
        };
    }

    private float GetDualAverageValue()
    {
        if (dualPrimarySlider == null || dualSecondarySlider == null)
            return 0f;

        return (dualPrimarySlider.value + dualSecondarySlider.value) * 0.5f;
    }

    private bool IsDrivingKind(AudioManager.AudioOverlayKind kind)
    {
        return kind == AudioManager.AudioOverlayKind.NutritionSingle
            || kind == AudioManager.AudioOverlayKind.GenerationSingle
            || kind == AudioManager.AudioOverlayKind.HeartDual;
    }

    private ImageFader GetActiveImageFader()
    {
        return EnumerateSceneImageFaders().FirstOrDefault(fader => fader.gameObject.activeInHierarchy);
    }

    private IEnumerable<ImageFader> EnumerateSceneImageFaders()
    {
        return Resources.FindObjectsOfTypeAll<ImageFader>()
            .Where(fader =>
                fader != null &&
                fader.gameObject.scene.IsValid() &&
                (fader.hideFlags & HideFlags.HideAndDontSave) == 0);
    }
}
