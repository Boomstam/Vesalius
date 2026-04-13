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

    [Header("Per-Chapter Animators (parallel arrays, indexed by chapter)")]
    [SerializeField] private TextColorAnimator[] textAnimators;
    [SerializeField] private ImageColorAnimator[] imageAnimators;

    public bool TutorialActive = false;
    
    private int _previousTab = 1;

    // Unity fires all onClick listeners for a button in the same EventSystem update.
    // When HideTutorial runs, it switches to _previousTab and locks tab 0 — but
    // TabManagement's own ShowTab(0) lambda fires in the same update tick, after
    // HideTutorial returns, which would immediately switch back to tab 0.
    // The lock in TabManagement suppresses that lambda, but we can only safely
    // unlock it on the next frame, once that same-frame lambda has already been ignored.
    // A coroutine can't be used here because the tutorial panel GameObject (which this
    // component may live on) becomes inactive during HideTutorial, which would kill
    // the coroutine before it runs. Update() is safe because TutorialManager itself
    // remains active.
    private bool _pendingUnlock = false;

    private const int TutorialTabIndex = 0;

    // Using Awake so listeners are registered before Start runs on any object.
    // This ensures ToggleTutorial is wired up before TabManagement.Awake adds
    // its own lambda to the same button.
    private void Awake()
    {
        infoToggleButton.onClick.AddListener(ToggleTutorial);
        quitTutorialButton.onClick.AddListener(HideTutorial);
        
        infoManager.OnChapterChanged += OnChapterChanged;
    }

    private void Update()
    {
        if (_pendingUnlock)
        {
            _pendingUnlock = false;
            tabManagement.UnlockTabZero();
        }
    }

    private void ToggleTutorial()
    {
        if (TutorialActive)
            HideTutorial();
        else
            ShowTutorial();
    }

    private void ShowTutorial()
    {
        Debug.Log("ShowTutorial");

        if (tabManagement.CurrentTab != TutorialTabIndex)
            _previousTab = tabManagement.CurrentTab;

        TutorialActive = true;
        _pendingUnlock = false;

        infoManager.ResetToStart();
        infoManager.GoToChapter(0);

        // Chapter 0 is never the last chapter, so hide immediately.
        quitTutorialButton.gameObject.SetActive(false);

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

    

    private void OnChapterChanged(int index)
    {
        StopAllAnimators();
        StartAnimatorsForChapter(index);

        // Show the quit button only on the final chapter.
        bool isLastChapter = index == infoManager.ChapterCount - 1;
        quitTutorialButton.gameObject.SetActive(isLastChapter);
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