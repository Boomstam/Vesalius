using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ImageFader : MonoBehaviour
{
    public Image[] images;
    public Sprite[] alternateImages;
    public Slider fadeSlider;

    public bool alternateMode;

    public float fadeVal;

    private Sprite[] _originalSprites;
    private Image[]  _activeImages;
    private Slider _runtimeBoundSlider;
    private bool _initialized;
    private bool _lastAlternateMode;
    private float _lastFadeVal;
    private bool _overlayDriven;

    private int CurrentNumImages => _activeImages?.Length ?? 0;
    public float CurrentFadeValue => fadeVal;

    private void Awake()
    {
        EnsureInitialized();
    }

    private void Start()
    {
        BindSlider();
        SyncWithOverlayIfActive();
        SetFadeVal(GetPreferredFadeValue());
    }

    private void OnEnable()
    {
        EnsureInitialized();
        BindSlider();
        SyncAlternateMode();
        SyncWithOverlayIfActive();
        SetFadeVal(GetPreferredFadeValue());
    }

    private void OnDisable()
    {
        if (_runtimeBoundSlider == null) return;

        _runtimeBoundSlider.onValueChanged.RemoveListener(SetFadeVal);
        _runtimeBoundSlider = null;
    }

    private void Update()
    {
        bool modeChanged = SyncAlternateMode();

        if (modeChanged || !Mathf.Approximately(_lastFadeVal, fadeVal))
        {
            _lastFadeVal = fadeVal;
            SetFadeVal(fadeVal);
        }
    }

    public void SetFadeVal(float fadeVal)
    {
        EnsureInitialized();

        this.fadeVal = Mathf.Clamp01(fadeVal);
        _lastFadeVal = this.fadeVal;

        if (CurrentNumImages == 0)
        {
            Debug.LogWarning("No images in active set, can't fade");
            return;
        }
        if (CurrentNumImages == 1)
        {
            Debug.LogWarning("Only 1 image in active set, can't fade");
            return;
        }

        float percentagePerSource = 1f / (float)(CurrentNumImages - 1);

        int startSample = Mathf.FloorToInt(this.fadeVal / percentagePerSource);
        if (startSample >= CurrentNumImages - 1)
            startSample = CurrentNumImages - 2;

        float remainder = this.fadeVal - (percentagePerSource * startSample);
        float remainderPercentage = remainder / percentagePerSource;

        for (int i = 0; i < CurrentNumImages; i++)
        {
            Image image = _activeImages[i];

            float alpha = 0;

            if (i == startSample)
                alpha = 1 - remainderPercentage;
            if (i == startSample + 1)
                alpha = remainderPercentage;

            Color c = image.color;
            image.color = new Color(c.r, c.g, c.b, alpha);
        }
    }

    private void SwapSprites(bool useAlternate)
    {
        EnsureInitialized();

        if (_originalSprites == null)
        {
            Debug.LogWarning("ImageFader: cache was empty on SwapSprites, caching now.");
            _originalSprites = images.Select(img => img.sprite).ToArray();
        }

        Sprite[] source = useAlternate && alternateImages != null && alternateImages.Length > 0
            ? alternateImages
            : _originalSprites;

        if (source == null)
        {
            Debug.LogWarning("ImageFader: no sprites to swap to.");
            return;
        }

        int activeCount = Mathf.Min(images.Length, source.Length);

        for (int i = 0; i < images.Length; i++)
        {
            if (i >= activeCount)
            {
                // This image is outside the active set — hide it
                Color c = images[i].color;
                images[i].color = new Color(c.r, c.g, c.b, 0f);
                continue;
            }

            images[i].sprite = source[i];
        }

        _activeImages = images.Take(activeCount).ToArray();
    }

    private void InitImageArray(ref Image[] imageArray)
    {
        if (imageArray == null || imageArray.Length < 3)
            return;

        if (imageArray[2].sprite == null)
        {
            imageArray[2].gameObject.SetActive(false);
            imageArray = imageArray.Take(2).ToArray();
        }
    }

    public void SetOverlayDriven(bool isDriven)
    {
        _overlayDriven = isDriven;

        if (fadeSlider != null)
            fadeSlider.interactable = !isDriven;
    }

    public void ApplyOverlayDrivenValue(float value)
    {
        float clampedValue = Mathf.Clamp01(value);
        SetHiddenSliderValue(clampedValue);
        SetFadeVal(clampedValue);
    }

    private void EnsureInitialized()
    {
        if (_initialized && CurrentNumImages > 0)
            return;

        if (images == null || images.Length == 0)
            return;

        InitImageArray(ref images);

        if (images == null || images.Length == 0)
            return;

        _originalSprites = images.Select(img => img.sprite).ToArray();
        _activeImages = images;

        _initialized = true;

        if (alternateMode)
            SwapSprites(true);

        _lastAlternateMode = alternateMode;
    }

    private bool SyncAlternateMode()
    {
        if (_lastAlternateMode == alternateMode)
            return false;

        _lastAlternateMode = alternateMode;
        SwapSprites(alternateMode);
        return true;
    }

    private void BindSlider()
    {
        if (fadeSlider == null)
            fadeSlider = GetComponentInChildren<Slider>(true);

        if (fadeSlider == null ||
            _runtimeBoundSlider == fadeSlider ||
            HasPersistentSetFadeValBinding(fadeSlider))
        {
            return;
        }

        fadeSlider.onValueChanged.AddListener(SetFadeVal);
        _runtimeBoundSlider = fadeSlider;
    }

    private void SyncWithOverlayIfActive()
    {
        if (GlobalAudioSliderOverlay.Instance == null)
            return;

        if (GlobalAudioSliderOverlay.Instance.TryGetImageFaderDriveState(out bool isDriven, out float drivenValue))
        {
            SetOverlayDriven(isDriven);

            if (isDriven)
                SetHiddenSliderValue(drivenValue);
        }
    }

    private float GetPreferredFadeValue()
    {
        if (_overlayDriven &&
            GlobalAudioSliderOverlay.Instance != null &&
            GlobalAudioSliderOverlay.Instance.TryGetImageFaderDriveState(out _, out float drivenValue))
        {
            return drivenValue;
        }

        if (fadeSlider != null)
            return fadeSlider.value;

        return fadeVal;
    }

    private void SetHiddenSliderValue(float value)
    {
        if (fadeSlider == null)
            return;

        fadeSlider.SetValueWithoutNotify(value);
    }

    private bool HasPersistentSetFadeValBinding(Slider slider)
    {
        int eventCount = slider.onValueChanged.GetPersistentEventCount();

        for (int i = 0; i < eventCount; i++)
        {
            if (slider.onValueChanged.GetPersistentTarget(i) == this &&
                slider.onValueChanged.GetPersistentMethodName(i) == nameof(SetFadeVal))
            {
                return true;
            }
        }

        return false;
    }
}
