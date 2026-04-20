using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Replaces TabManagement. Owns the two-view (Content / Info) model for all 11 parts.
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

    public int  CurrentPart => _currentPart;
    public View CurrentView => _currentView;

    private int  _currentPart = -1;
    private View _currentView = View.Content;

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
    }

    private void Start()
    {
        infoToggleButton.onClick.AddListener(ToggleView);
        SetPart(0);
    }

    private void OnDestroy()
    {
        infoToggleButton.onClick.RemoveListener(ToggleView);
    }

    // -------------------------------------------------------------------------

    /// <summary>
    /// Called by the server/network layer to advance to a part.
    /// Always lands on Content view.
    /// </summary>
    public void SetPart(int part)
    {
        _currentPart = Mathf.Clamp(part, 0, contentViews.Length - 1);
        _currentView = View.Content;
        ApplyCurrentView();
    }

    /// <summary>
    /// Bound to the Information Icon button — toggles Content ↔ Info.
    /// </summary>
    public void ToggleView()
    {
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
}