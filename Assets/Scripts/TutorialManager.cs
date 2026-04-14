using System;
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

    [Header("Chapter Animations")]
    [SerializeField] private ChapterAnimationEntry[] chapterAnimations;

    [System.Serializable]
    public class ChapterAnimationEntry
    {
        public int Chapter;
        public TextColorAnimator[] TextAnimators;
        public ImageColorAnimator[] ImageAnimators;
    }
    
    public bool TutorialActive = false;

    private int _previousTab = 1;

    private bool _pendingUnlock = false;

    private const int TutorialTabIndex = 0;

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
        if (tabManagement.CurrentTab != TutorialTabIndex)
            _previousTab = tabManagement.CurrentTab;

        TutorialActive = true;
        _pendingUnlock = false;

        infoManager.ResetToStart();
        infoManager.GoToChapter(0);

        quitTutorialButton.gameObject.SetActive(false);
        tutorialActiveLabel.SetActive(true);

        tabManagement.LockTabZero();
        tabManagement.ShowTab(TutorialTabIndex);
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
        
        StartAnimatorsForChapter(index);

        bool isLastChapter = index == infoManager.ChapterCount - 1;
        
        quitTutorialButton.gameObject.SetActive(isLastChapter);
        tutorialActiveLabel.SetActive(!isLastChapter);
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
}