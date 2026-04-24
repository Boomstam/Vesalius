using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Owns the two-view (Content / Info) model for all 11 parts.
/// Part switches are server-driven (call SetPart from a network handler).
/// The Information Icon button toggles between Content and Info within the current part.
/// </summary>
public class ViewManager : MonoBehaviour
{
    public enum View { Content, Info }

    [Header("Part Views — index 0-10, one entry per part")]
    [SerializeField] private GameObject[] contentViews;
    [SerializeField] private GameObject[] infoViews;

    [Header("Info Managers for the Info view — index 0-10")]
    [SerializeField] private InfoManager[] infoManagers;

    [Header("Part 0 only: Info Manager for the Content view (the tutorial)")]
    [SerializeField] private InfoManager part0ContentInfoManager;

    [Header("UI")]
    [SerializeField] private Button infoToggleButton;
    [SerializeField] private Button languageButton;
    [SerializeField] private Image connectedImage;
    [SerializeField] private GameObject contentImage;
    [SerializeField] private Color infoButtonHighlightColor = Color.white;
    [SerializeField] private float infoButtonFlashDuration = 5f;
    [SerializeField] private float infoButtonFlashSpeed = 3.5f;
    
    public int  CurrentPart => _currentPart;
    public View CurrentView => _currentView;

    private int  _currentPart = -1;
    private View _currentView = View.Content;
    private Color _defaultButtonColor;
    private bool _isLockedToContent;
    private float _infoButtonFlashEndTime;

    // -------------------------------------------------------------------------

    public int partToTest = 0;
    public bool testPart = false;

    private void Update()
    {
        if (testPart)
        {
            SetPart(partToTest);
            
            testPart = false;
        }

        RefreshContentLockState();
        UpdateInfoButtonFlash();
    }

    private void Awake()
    {
        if (infoToggleButton != null)
            _defaultButtonColor = infoToggleButton.image.color;
    }

    private void Start()
    {
        contentImage.SetActive(false);
        infoToggleButton.onClick.AddListener(ToggleView);
        SetPart(0);
    }

    private void OnDestroy()
    {
        infoToggleButton.onClick.RemoveListener(ToggleView);
    }

    // -------------------------------------------------------------------------

    /// <summary>
    /// Called by the server/network layer to set completeAnatomyMode on all
    /// ImageFader components in the content views of parts 1-10.
    /// Part 0 is intentionally excluded (it has no ImageFader).
    /// </summary>
    public void SetCompleteAnatomyMode(bool enabled)
    {
        for (int i = 1; i < contentViews.Length; i++)
        {
            if (contentViews[i] == null) continue;

            ImageFader[] faders = contentViews[i].GetComponentsInChildren<ImageFader>(true);
            foreach (ImageFader fader in faders)
                fader.alternateMode = enabled;
        }
    }

    public void SetParticipationMode(bool enabled)
    {
        if (part0ContentInfoManager != null)
            part0ContentInfoManager.SetParticipationMode(enabled);
    }

    /// <summary>
    /// Called by the server/network layer to advance to a part.
    /// Always lands on Content view.
    /// </summary>
    public void SetPart(int part)
    {
        contentImage.SetActive(false);

        int previousPart = _currentPart;
        _currentPart = Mathf.Clamp(part, 0, contentViews.Length - 1);
        _currentView = View.Content;
        ApplyCurrentView();

        if (_currentPart == 0)
        {
            _infoButtonFlashEndTime = 0f;
            RefreshInfoButtonState();
        }
        else if (previousPart != _currentPart)
            StartInfoButtonFlash();
    }

    /// <summary>
    /// Bound to the Information Icon button — toggles Content ↔ Info.
    /// </summary>
    public void ToggleView()
    {
        if (_isLockedToContent)
            return;

        contentImage.SetActive(false);
        _currentView = (_currentView == View.Content) ? View.Info : View.Content;
        ApplyCurrentView();
    }

    // -------------------------------------------------------------------------

    private void ApplyCurrentView()
    {
        for (int i = 0; i < contentViews.Length; i++)
        {
            bool isActive = i == _currentPart;

            if (contentViews[i] != null)
                contentViews[i].SetActive(isActive && _currentView == View.Content);

            if (infoViews[i] != null)
                infoViews[i].SetActive(isActive && _currentView == View.Info);
        }

        RefreshActiveInfoManager();
        RefreshInfoButtonState();
    }

    /// <summary>
    /// Tints the info toggle button white (editor-defined color) when we're on
    /// the Content view of any part other than part 0.
    /// Resets to normal color otherwise.
    /// Hides the language button and connected image in that same case.
    /// </summary>
    private void RefreshInfoButtonState()
    {
        if (infoToggleButton == null) return;

        bool showInfoButton = !_isLockedToContent;
        if (infoToggleButton.gameObject.activeSelf != showInfoButton)
            infoToggleButton.gameObject.SetActive(showInfoButton);

        if (!showInfoButton)
            return;

        bool useHighlight = _currentView == View.Content && _currentPart != 0;
        Color baseColor = useHighlight ? infoButtonHighlightColor : _defaultButtonColor;
        infoToggleButton.image.color = GetDisplayedInfoButtonColor(baseColor);

        if (languageButton != null)
            languageButton.gameObject.SetActive(!useHighlight);

        if (connectedImage != null)
            connectedImage.gameObject.SetActive(!useHighlight);
    }

    /// <summary>
    /// Pokes the relevant InfoManager so it redraws its current chapter.
    /// Needed because GoToChapter is a no-op when the GameObject is inactive,
    /// so we call it once after the GameObject is activated.
    /// </summary>
    private void RefreshActiveInfoManager()
    {
        if (_currentView == View.Info)
        {
            if (_currentPart < infoManagers.Length && infoManagers[_currentPart] != null)
            {
                InfoManager im = infoManagers[_currentPart];
                im.GoToChapter(im.CurrentIndex);
            }
        }
        else if (_currentPart == 0 && part0ContentInfoManager != null)
        {
            part0ContentInfoManager.GoToChapter(part0ContentInfoManager.CurrentIndex);
        }
    }

    private void RefreshContentLockState()
    {
        bool shouldLock = IsBlockingOverlayActive();
        if (_isLockedToContent == shouldLock)
            return;

        _isLockedToContent = shouldLock;

        if (_isLockedToContent && _currentView != View.Content)
        {
            _currentView = View.Content;
            ApplyCurrentView();
            return;
        }

        RefreshInfoButtonState();
    }

    private bool IsBlockingOverlayActive()
    {
        if (GlobalAudioSliderOverlay.Instance != null && GlobalAudioSliderOverlay.Instance.IsOverlayVisible)
            return true;

        if (MessageOverlay.Instance != null && MessageOverlay.Instance.IsVisible)
            return true;

        if (Instances.ColorOverlay != null && Instances.ColorOverlay.IsBlockingOverlayVisible)
            return true;

        if (Instances.GroupColorOverlay != null && Instances.GroupColorOverlay.IsVisible)
            return true;

        return false;
    }

    private void StartInfoButtonFlash()
    {
        _infoButtonFlashEndTime = Time.unscaledTime + Mathf.Max(0f, infoButtonFlashDuration);
        RefreshInfoButtonState();
    }

    private void UpdateInfoButtonFlash()
    {
        if (_infoButtonFlashEndTime <= 0f)
            return;

        if (Time.unscaledTime >= _infoButtonFlashEndTime)
        {
            _infoButtonFlashEndTime = 0f;
            RefreshInfoButtonState();
            return;
        }

        if (!_isLockedToContent && infoToggleButton != null && infoToggleButton.gameObject.activeInHierarchy)
            RefreshInfoButtonState();
    }

    private Color GetDisplayedInfoButtonColor(Color baseColor)
    {
        if (_infoButtonFlashEndTime <= Time.unscaledTime)
            return baseColor;

        float pulse = Mathf.PingPong(Time.unscaledTime * Mathf.Max(0.01f, infoButtonFlashSpeed), 1f);
        Color dimmedColor = new(baseColor.r, baseColor.g, baseColor.b, baseColor.a * 0.2f);
        return Color.Lerp(dimmedColor, baseColor, pulse);
    }
}
