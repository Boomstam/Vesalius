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
    [Header("Dependencies")]
    [SerializeField] private InfoManager infoManager;

    [Header("UI")]
    [SerializeField] private GameObject soundsPanel;

    [Header("Tutorial Styling")]
    [SerializeField] private TMP_FontAsset tutorialFont;
    [SerializeField] private Sprite navigationArrowSprite;

    private bool styleApplied;
    private bool backgroundOverlayCreated;

    private void Start()
    {
        ApplyTutorialStyling();
        EnsureTutorialBackgroundOverlay();

        infoManager.OnChapterChanged += OnChapterChanged;
        Refresh(infoManager.CurrentIndex);
    }

    private void OnDestroy()
    {
        if (infoManager != null)
            infoManager.OnChapterChanged -= OnChapterChanged;
    }

    private void OnChapterChanged(int index) => Refresh(index);

    private void Refresh(int index)
    {
        string slotType = infoManager.GetSlotType(index);
        bool isSoundsSlot =
            string.Equals(slotType, "sounds", System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(slotType, "sound", System.StringComparison.OrdinalIgnoreCase);

        soundsPanel.SetActive(isSoundsSlot);
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

    private void EnsureTutorialBackgroundOverlay()
    {
        if (backgroundOverlayCreated)
            return;

        Transform existing = transform.Find("Tutorial Color Overlay Stack");
        if (existing != null)
        {
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
        duplicate.name = "Tutorial Color Overlay Stack";
        duplicate.SetActive(true);
        duplicate.transform.SetSiblingIndex(0);

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
            overlay.RegisterAsSharedInstance = false;

        ColorOverlay sourceOverlay = source.GetComponent<ColorOverlay>();
        if (sourceOverlay != null && sourceOverlay.RegisterAsSharedInstance)
            Instances.ColorOverlay = sourceOverlay;

        foreach (Graphic graphic in duplicate.GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = false;

        backgroundOverlayCreated = true;
    }
}
