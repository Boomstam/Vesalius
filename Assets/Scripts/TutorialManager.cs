using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private InfoManager infoManager;
    [SerializeField] private TabManagement tabManagement;

    [Header("Buttons")]
    [SerializeField] private Button infoToggleButton;
    [SerializeField] private Button quitTutorialButton;
    [SerializeField] private GameObject tutorialActiveLabel;

    [Header("Tutorial Styling")]
    [SerializeField] private TMP_FontAsset tutorialFont;
    [SerializeField] private Sprite navigationArrowSprite;

    [Header("Chapter Animations")]
    [SerializeField] private ChapterAnimationEntry[] chapterAnimations;

    [System.Serializable]
    public class ChapterAnimationEntry
    {
        public int Chapter;
        public TextColorAnimator[] TextAnimators;
        public ImageColorAnimator[] ImageAnimators;
    }

    public InfoManager InfoManager => infoManager;
    public bool TutorialActive = false;

    private TabIndex _previousTab = TabIndex.Vesalius;
    private bool _pendingUnlock = false;
    private bool _styleApplied;
    private bool _backgroundOverlayCreated;

    private void Update()
    {
        if (_pendingUnlock)
        {
            _pendingUnlock = false;
            tabManagement.UnlockTabZero();
        }
    }

    public void Initialize()
    {
        ApplyTutorialStyling();
        EnsureTutorialBackgroundOverlay();

        infoToggleButton.onClick.AddListener(ToggleTutorial);
        quitTutorialButton.onClick.AddListener(HideTutorial);

        infoManager.OnChapterChanged += OnChapterChanged;
    }

    private void ToggleTutorial()
    {
        if (TutorialActive)
            HideTutorial();
        else
            ShowTutorial();
    }

    public void ShowTutorial()
    {
        if (tabManagement.CurrentTab != TabIndex.Tutorial)
            _previousTab = tabManagement.CurrentTab;

        TutorialActive = true;
        _pendingUnlock = false;

        infoManager.ResetToStart();
        infoManager.GoToChapter(0);

        quitTutorialButton.gameObject.SetActive(false);
        tutorialActiveLabel.SetActive(true);

        tabManagement.LockTabZero();
        tabManagement.ShowTab(TabIndex.Tutorial);
        tabManagement.DisableAllTabsButTheFirst();
    }

    private void HideTutorial()
    {
        StopAnimatorsForChapter(infoManager.CurrentIndex);
        TutorialActive = false;

        tabManagement.ShowTab(_previousTab);
        tabManagement.EnableAllTabsButTheFirst();

        _pendingUnlock = true;
    }

    public void OnChapterChanged(int index)
    {
        StopAllAnimators();

        bool isLastChapter = index == infoManager.ChapterCount - 1;
        quitTutorialButton.gameObject.SetActive(isLastChapter);
        tutorialActiveLabel.SetActive(!isLastChapter);

        StartAnimatorsForChapter(index);
    }

    private ChapterAnimationEntry FindEntry(int chapterIndex)
    {
        if (chapterAnimations == null) return null;
        foreach (var entry in chapterAnimations)
            if (entry.Chapter == chapterIndex) return entry;
        return null;
    }

    private void StartAnimatorsForChapter(int chapterIndex)
    {
        var entry = FindEntry(chapterIndex);
        if (entry == null) return;

        if (entry.TextAnimators != null)
            foreach (var a in entry.TextAnimators)
                if (a != null) a.StartAnimation();

        if (entry.ImageAnimators != null)
            foreach (var a in entry.ImageAnimators)
                if (a != null) a.StartAnimation();
    }

    private void StopAnimatorsForChapter(int chapterIndex)
    {
        var entry = FindEntry(chapterIndex);
        if (entry == null) return;

        if (entry.TextAnimators != null)
            foreach (var a in entry.TextAnimators)
                if (a != null) a.StopAnimation();

        if (entry.ImageAnimators != null)
            foreach (var a in entry.ImageAnimators)
                if (a != null) a.StopAnimation();
    }

    private void StopAllAnimators()
    {
        if (chapterAnimations == null) return;
        foreach (var entry in chapterAnimations)
        {
            if (entry.TextAnimators != null)
                foreach (var a in entry.TextAnimators)
                    if (a != null) a.StopAnimation();

            if (entry.ImageAnimators != null)
                foreach (var a in entry.ImageAnimators)
                    if (a != null) a.StopAnimation();
        }
    }

    private void ApplyTutorialStyling()
    {
        if (_styleApplied || infoManager == null)
            return;

        infoManager.SetDisplayedTitleOverride("Tutorial");

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

        _styleApplied = true;
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
        if (_backgroundOverlayCreated)
            return;

        Transform existing = transform.Find("Tutorial Color Overlay Stack");
        if (existing != null)
        {
            _backgroundOverlayCreated = true;
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

        _backgroundOverlayCreated = true;
    }
}
