using UnityEngine;
using UnityEngine.UI;

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

    private void Start()
    {
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
        if (_tutorialActive)
            HideTutorial();
        else
            ShowTutorial();
    }

    private void ShowTutorial()
    {
        if (tabManagement.CurrentTab != TutorialTabIndex)
            _previousTab = tabManagement.CurrentTab;

        _tutorialActive = true;

        // Cancel any pending unlock from a previous HideTutorial call —
        // if Show is called before the next frame, the deferred unlock must
        // not fire and undo the lock we're about to set.
        _pendingUnlock = false;

        infoManager.ResetToStart();
        infoManager.GoToChapter(0);

        tabManagement.LockTabZero();
        tabManagement.ShowTab(TutorialTabIndex);
        tabManagement.DisableAllTabsButTheFirst();
    }

    private void HideTutorial()
    {
        StopAnimatorsForChapter(infoManager.CurrentIndex);
        _tutorialActive = false;

        tabManagement.ShowTab(_previousTab);
        tabManagement.EnableAllTabsButTheFirst();

        _pendingUnlock = true;
    }

    private void OnChapterChanged(int index)
    {
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