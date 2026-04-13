using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Decorates InfoManager with tutorial-specific behaviour:
/// animator side-effects per chapter, info-toggle wiring,
/// quit button, first-launch auto-show, and tab restore on dismiss.
/// 
/// Sits as a sibling or parent component — does not touch InfoManager internals
/// beyond subscribing to OnChapterChanged and calling GoToChapter / ResetToStart.
/// </summary>
public class TutorialManager : MonoBehaviour
{
    private const string PlayerPrefsKey = "TutorialShown";

    [Header("Dependencies")]
    [SerializeField] private InfoManager infoManager;
    [SerializeField] private TabManagement tabManagement;

    [Header("Buttons")]
    [SerializeField] private Button infoToggleButton;
    [SerializeField] private Button quitTutorialButton;

    [Header("Per-Chapter Animators (parallel arrays, indexed by chapter)")]
    [SerializeField] private TextColorAnimator[] textAnimators;
    [SerializeField] private ImageColorAnimator[] imageAnimators;

    private bool _tutorialActive = false;
    private int _previousTab = 1; // tab to restore on dismiss; 1 = first real tab

    // Tab index reserved for the tutorial panel in TabManagement (the "0th" tab).
    private const int TutorialTabIndex = 0;

    private void Start()
    {
        infoToggleButton.onClick.AddListener(ToggleTutorial);
        quitTutorialButton.onClick.AddListener(HideTutorial);

        infoManager.OnChapterChanged += OnChapterChanged;
        
        if (!PlayerPrefs.HasKey(PlayerPrefsKey) || Application.isEditor)
        {
            PlayerPrefs.SetInt(PlayerPrefsKey, 1);
            PlayerPrefs.Save();
            
            ShowTutorial();
        }
    } 

    private void OnDestroy()
    {
        infoToggleButton.onClick.RemoveListener(ToggleTutorial);
        quitTutorialButton.onClick.RemoveListener(HideTutorial);

        if (infoManager != null)
            infoManager.OnChapterChanged -= OnChapterChanged;
    }

    private void ToggleTutorial()
    {
        if (_tutorialActive)
            HideTutorial();
        else
            ShowTutorial();
    }

    private void ShowTutorial()
    {
        // Remember where we came from, unless we're already on the tutorial tab.
        if (tabManagement.CurrentTab != TutorialTabIndex)
            _previousTab = tabManagement.CurrentTab;

        _tutorialActive = true;

        // Reset InfoManager silently, then GoToChapter fires the event
        // so animators initialise cleanly for chapter 0.
        infoManager.ResetToStart();
        infoManager.GoToChapter(0);

        tabManagement.ShowTab(TutorialTabIndex);
        
        tabManagement.DisableAllTabsButTheFirst();

    }

    private void HideTutorial()
    {
        StopAnimatorsForChapter(infoManager.CurrentIndex);

        _tutorialActive = false;

        tabManagement.ShowTab(_previousTab);
        tabManagement.EnableAllTabsButTheFirst();
    }

    private void OnChapterChanged(int index)
    {
        // Stop all animators first, then start only the ones for the new chapter.
        StopAllAnimators();
        StartAnimatorsForChapter(index);
    }

    private void StartAnimatorsForChapter(int index)
    {
        if (textAnimators != null && index < textAnimators.Length && textAnimators[index] != null)
            textAnimators[index].StartAnimation();

        if (imageAnimators != null && index < imageAnimators.Length && imageAnimators[index] != null)
            imageAnimators[index].StartAnimation();
    }

    private void StopAnimatorsForChapter(int index)
    {
        if (textAnimators != null && index < textAnimators.Length && textAnimators[index] != null)
            textAnimators[index].StopAnimation();

        if (imageAnimators != null && index < imageAnimators.Length && imageAnimators[index] != null)
            imageAnimators[index].StopAnimation();
    }

    private void StopAllAnimators()
    {
        if (textAnimators != null)
            foreach (var a in textAnimators)
                if (a != null) a.StopAnimation();

        if (imageAnimators != null)
            foreach (var a in imageAnimators)
                if (a != null) a.StopAnimation();
    }
}
