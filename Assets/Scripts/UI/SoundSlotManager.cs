using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sits on the Part 0 Content GameObject alongside its InfoManager.
/// Shows the sounds panel whenever the active chapter slot is of type "sounds",
/// and hides it for every other slot type.
/// </summary>
public class SoundsSlotManager : MonoBehaviour
{
    private const string DoNotDisturbObjectName = "Do Not Disturb Icons";
    private const string TutorialOverlayObjectName = "Tutorial Color Overlay Stack";
    private const string TutorialImageFaderObjectName = "Tutorial Sounds ImageFader Background";
    private const float TutorialOverlayFadeTime = 10f;

    [Header("Dependencies")]
    [SerializeField] private InfoManager infoManager;

    [Header("UI")]
    [SerializeField] private GameObject soundsPanel;
    [SerializeField] private GameObject doNotDisturbIcons;
    [SerializeField] private Slider tutorialSlider1;

    [Header("Tutorial Styling")]
    [SerializeField] private TMP_FontAsset tutorialFont;
    [SerializeField] private Sprite navigationArrowSprite;

    private bool styleApplied;
    private bool backgroundOverlayCreated;
    private GameObject tutorialBackgroundOverlayObject;
    private GameObject tutorialBackgroundImageFaderObject;

    private void Start()
    {
        ApplyTutorialStyling();
        EnsureTutorialBackgrounds();
        ResolveOptionalObjects();
        HideSliderLabels();

        infoManager.OnChapterChanged += OnChapterChanged;
        if (LanguageManager.Instance != null)
            LanguageManager.Instance.LanguageChanged += OnLanguageChanged;

        Refresh(infoManager.CurrentIndex);
    }

    private void OnDestroy()
    {
        if (infoManager != null)
            infoManager.OnChapterChanged -= OnChapterChanged;

        if (LanguageManager.Instance != null)
            LanguageManager.Instance.LanguageChanged -= OnLanguageChanged;
    }

    private void OnChapterChanged(int index) => Refresh(index);

    private void OnLanguageChanged(LanguageManager.AppLanguage _) => Refresh(infoManager.CurrentIndex);

    private void Refresh(int index)
    {
        string slotType = infoManager.GetSlotType(index);
        bool isSoundsSlot =
            string.Equals(slotType, InfoManager.SoundsType, System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(slotType, InfoManager.SoundType, System.StringComparison.OrdinalIgnoreCase);
        bool showDoNotDisturbIcons =
            string.Equals(slotType, InfoManager.DoNotDisturbType, System.StringComparison.OrdinalIgnoreCase);

        if (soundsPanel != null)
            soundsPanel.SetActive(isSoundsSlot);

        if (tutorialBackgroundOverlayObject != null)
            tutorialBackgroundOverlayObject.SetActive(!isSoundsSlot);

        if (tutorialBackgroundImageFaderObject != null)
            tutorialBackgroundImageFaderObject.SetActive(isSoundsSlot);

        if (doNotDisturbIcons != null)
            doNotDisturbIcons.SetActive(showDoNotDisturbIcons);
    }

    private void ApplyTutorialStyling()
    {
        if (styleApplied || infoManager == null)
            return;

        infoManager.SetDisplayedTitleOverride("TUTORIAL");

        if (tutorialFont != null)
        {
            if (infoManager.InfoTitleText != null)
                infoManager.InfoTitleText.font = tutorialFont;

            if (infoManager.InfoContentText != null)
                infoManager.InfoContentText.font = tutorialFont;
        }

        if (infoManager.InfoTitleText != null)
            infoManager.InfoTitleText.color = Color.white;

        if (infoManager.InfoContentText != null)
            infoManager.InfoContentText.color = Color.white;

        if (infoManager.InfoBackground != null)
            infoManager.InfoBackground.gameObject.SetActive(false);

        Image topBarImage = infoManager.InfoTitleText != null
            ? infoManager.InfoTitleText.transform.parent.GetComponent<Image>()
            : null;

        if (topBarImage != null)
        {
            Color color = topBarImage.color;
            color.a = 0f;
            topBarImage.color = color;
            topBarImage.raycastTarget = false;
        }

        ConfigureNavigationButton(infoManager.PreviousInfoButton, 90f);
        ConfigureNavigationButton(infoManager.NextInfoButton, -90f);

        styleApplied = true;
    }

    private void ConfigureNavigationButton(Button button, float zRotation)
    {
        if (button == null || navigationArrowSprite == null)
            return;

        Transform iconTransform = button.transform.childCount > 0 ? button.transform.GetChild(0) : null;
        if (iconTransform == null)
            return;

        if (iconTransform is RectTransform iconRect)
            iconRect.localEulerAngles = new Vector3(0f, 0f, zRotation);

        TextMeshProUGUI textComponent = iconTransform.GetComponent<TextMeshProUGUI>();
        if (textComponent != null)
            textComponent.enabled = false;

        Image image = iconTransform.GetComponent<Image>();
        if (image == null)
            image = iconTransform.gameObject.AddComponent<Image>();

        image.sprite = navigationArrowSprite;
        image.color = Color.white;
        image.preserveAspect = true;
        image.raycastTarget = false;
        button.targetGraphic = image;
    }

    private void EnsureTutorialBackgrounds()
    {
        EnsureTutorialBackgroundOverlay();
        EnsureTutorialBackgroundImageFader();
    }

    private void EnsureTutorialBackgroundOverlay()
    {
        if (backgroundOverlayCreated)
            return;

        Transform existing = transform.Find(TutorialOverlayObjectName);
        if (existing != null)
        {
            tutorialBackgroundOverlayObject = existing.gameObject;
            ConfigureTutorialOverlay(existing.GetComponent<ColorOverlay>());
            backgroundOverlayCreated = true;
            return;
        }

        Transform root = transform.root;
        if (root == null)
            return;

        Transform source = root.Find("Color Overlay Stack");
        if (source == null)
            return;

        GameObject duplicate = Instantiate(source.gameObject, transform);
        duplicate.name = TutorialOverlayObjectName;
        duplicate.SetActive(true);
        duplicate.transform.SetSiblingIndex(0);
        tutorialBackgroundOverlayObject = duplicate;

        if (duplicate.TryGetComponent(out RectTransform duplicateRect))
        {
            duplicateRect.anchorMin = Vector2.zero;
            duplicateRect.anchorMax = Vector2.one;
            duplicateRect.offsetMin = Vector2.zero;
            duplicateRect.offsetMax = Vector2.zero;
            duplicateRect.anchoredPosition = Vector2.zero;
        }

        ColorOverlay overlay = duplicate.GetComponent<ColorOverlay>();
        if (overlay != null)
        {
            overlay.RegisterAsSharedInstance = false;
            ConfigureTutorialOverlay(overlay);
        }

        ColorOverlay sourceOverlay = source.GetComponent<ColorOverlay>();
        if (sourceOverlay != null && sourceOverlay.RegisterAsSharedInstance)
            Instances.ColorOverlay = sourceOverlay;

        foreach (Graphic graphic in duplicate.GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = false;

        backgroundOverlayCreated = true;
    }

    private void EnsureTutorialBackgroundImageFader()
    {
        if (tutorialBackgroundImageFaderObject != null)
            return;

        Transform existing = transform.Find(TutorialImageFaderObjectName);
        if (existing != null)
        {
            tutorialBackgroundImageFaderObject = existing.gameObject;
            return;
        }

        Slider slider1 = ResolveTutorialSlider1();
        Sprite[] backgroundSprites = ResolveTutorialBackgroundSprites();
        if (backgroundSprites == null || backgroundSprites.Length < 2)
        {
            Debug.LogWarning("SoundsSlotManager: could not resolve the nerves alternate images for the tutorial sounds background.");
            return;
        }

        GameObject root = new(TutorialImageFaderObjectName, typeof(RectTransform));
        root.transform.SetParent(transform, false);
        root.transform.SetSiblingIndex(0);
        root.SetActive(false);
        tutorialBackgroundImageFaderObject = root;

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        rootRect.anchoredPosition = Vector2.zero;

        GameObject underlayObject = new("Black Underlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        underlayObject.transform.SetParent(root.transform, false);

        RectTransform underlayRect = underlayObject.GetComponent<RectTransform>();
        underlayRect.anchorMin = Vector2.zero;
        underlayRect.anchorMax = Vector2.one;
        underlayRect.offsetMin = Vector2.zero;
        underlayRect.offsetMax = Vector2.zero;
        underlayRect.anchoredPosition = Vector2.zero;

        Image underlayImage = underlayObject.GetComponent<Image>();
        underlayImage.color = Color.black;
        underlayImage.raycastTarget = false;

        Image[] images = new Image[backgroundSprites.Length];
        for (int i = 0; i < backgroundSprites.Length; i++)
        {
            GameObject imageObject = new($"Image {i + 1}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(root.transform, false);

            RectTransform imageRect = imageObject.GetComponent<RectTransform>();
            imageRect.anchorMin = Vector2.zero;
            imageRect.anchorMax = Vector2.one;
            imageRect.offsetMin = Vector2.zero;
            imageRect.offsetMax = Vector2.zero;
            imageRect.anchoredPosition = Vector2.zero;

            Image image = imageObject.GetComponent<Image>();
            image.sprite = backgroundSprites[i];
            image.color = new Color(1f, 1f, 1f, i == 0 ? 1f : 0f);
            image.raycastTarget = false;
            images[i] = image;
        }

        ImageFader imageFader = root.AddComponent<ImageFader>();
        imageFader.images = images;
        imageFader.alternateImages = Array.Empty<Sprite>();
        imageFader.fadeSlider = slider1;
        imageFader.alternateMode = false;
        imageFader.fadeVal = 0f;
    }

    private Slider ResolveTutorialSlider1()
    {
        if (tutorialSlider1 != null)
            return tutorialSlider1;

        if (soundsPanel == null)
            return null;

        tutorialSlider1 = soundsPanel.GetComponentsInChildren<Slider>(true)
            .FirstOrDefault(slider => string.Equals(slider.gameObject.name, "Tutorial Slider 1", StringComparison.Ordinal));

        return tutorialSlider1;
    }

    private static Sprite[] ResolveTutorialBackgroundSprites()
    {
        ImageFader nervesFader = Resources.FindObjectsOfTypeAll<ImageFader>()
            .FirstOrDefault(IsNervesAlternateImageFader);

        if (nervesFader?.alternateImages == null || nervesFader.alternateImages.Length < 2)
            return null;

        return nervesFader.alternateImages
            .Where(sprite => sprite != null)
            .Take(2)
            .ToArray();
    }

    private static bool IsNervesAlternateImageFader(ImageFader fader)
    {
        if (fader == null ||
            !fader.gameObject.scene.IsValid() ||
            fader.alternateImages == null ||
            fader.alternateImages.Length < 2)
        {
            return false;
        }

        return fader.alternateImages.Any(sprite =>
            sprite != null &&
            sprite.name.IndexOf("Book IV The Nerves", StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private static void ConfigureTutorialOverlay(ColorOverlay overlay)
    {
        if (overlay != null)
            overlay.ConfigureColorCycle(TutorialOverlayFadeTime, initializeWithFirstColor: true);
    }

    private void ResolveOptionalObjects()
    {
        if (doNotDisturbIcons == null)
        {
            Transform child = transform.Find(DoNotDisturbObjectName);
            if (child != null)
                doNotDisturbIcons = child.gameObject;
        }
    }

    private void HideSliderLabels()
    {
        if (soundsPanel == null)
            return;

        foreach (TMP_Text text in soundsPanel.GetComponentsInChildren<TMP_Text>(true))
            text.gameObject.SetActive(false);

        foreach (Text text in soundsPanel.GetComponentsInChildren<Text>(true))
            text.gameObject.SetActive(false);
    }
}
