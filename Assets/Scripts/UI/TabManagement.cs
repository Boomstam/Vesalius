using UnityEngine;
using UnityEngine.UI;
using System;

public class TabManagement : MonoBehaviour
{
    [Serializable]
    public struct Tab
    {
        public Button button;
        public GameObject panel;
    }
    [Header("Info Managers")]
    [SerializeField] private InfoManager[] tabInfoManagers;
    
    [Header("Tabs")]
    public Tab[] tabs;

    [Header("Colors")]
    public Color activeColor = Color.white;
    public Color inactiveColor = new Color(0.7f, 0.7f, 0.7f);
    
    [Header("Tutorial Manager")]
    public TutorialManager tutorialManager;
    
    private const string PlayerPrefsKey = "TutorialShown";
    
    public int CurrentTab => currentTab;

    private int currentTab = 1;

    // When the tutorial is active, tab 0's own onClick lambda must be suppressed.
    // Tab 0's button is the same GameObject as the info toggle button, so both
    // TutorialManager's ToggleTutorial listener and this tab's ShowTab(0) lambda
    // fire on every click. Without the lock, HideTutorial correctly switches away
    // from tab 0, but then the lambda immediately switches back to it.
    private bool _tabZeroLocked = false;

    // Using Awake (not Start) so listeners are registered before TutorialManager.Start
    // runs and potentially calls ShowTutorial, which relies on the lock mechanism.
    private void Awake()
    {
        for (int i = 0; i < tabs.Length; i++)
        {
            int index = i;
            tabs[i].button.onClick.AddListener(() =>
            {
                if (index == 0 && _tabZeroLocked)
                {
                    return;
                }
                ShowTab(index);
            });
        }

        tutorialManager.Initialize();
        
        if (!PlayerPrefs.HasKey(PlayerPrefsKey))
        {
            PlayerPrefs.SetInt(PlayerPrefsKey, 1);
            PlayerPrefs.Save();
            
            tutorialManager.TutorialActive = true;
            
            ShowTab(0);
            
            tutorialManager.OnChapterChanged(0);

        }
        else
        {
            tutorialManager.TutorialActive = false;

            ShowTab(1);
        }
    }

    public void LockTabZero()
    {
        _tabZeroLocked = true;
    }

    public void UnlockTabZero()
    {
        _tabZeroLocked = false;
    }

    public void ShowTab(int index)
    {
        currentTab = index;
        
        for (int i = 0; i < tabs.Length; i++)
        {
            tabs[i].panel.SetActive(i == index);
            
            if(i != 0) // Don't change the buttons for the first tab
            {
                tabs[i].button.transition = Selectable.Transition.None;
                tabs[i].button.GetComponent<Image>().color = (i == index) ? activeColor : inactiveColor;
            }
        }
        if (index == 0 && !tutorialManager.TutorialActive)
        {
            tutorialManager.InfoManager.GoToChapter(tutorialManager.InfoManager.CurrentIndex);
        }
        if (index < tabInfoManagers.Length && tabInfoManagers[index] != null)
        {
            tabInfoManagers[index].GoToChapter(tabInfoManagers[index].CurrentIndex);
        }
    }

    public void DisableAllTabsButTheFirst()
    {
        for (int i = 1; i < tabs.Length; i++)
            tabs[i].button.interactable = false;
    }

    public void EnableAllTabsButTheFirst()
    {
        for (int i = 1; i < tabs.Length; i++)
            tabs[i].button.interactable = true;
    }
}