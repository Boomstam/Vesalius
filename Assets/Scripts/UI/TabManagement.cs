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

    [Header("Tabs")]
    public Tab[] tabs;

    [Header("Colors")]
    public Color activeColor = Color.white;
    public Color inactiveColor = new Color(0.7f, 0.7f, 0.7f);

    public int CurrentTab => currentTab;

    private int currentTab = 1;

    private void Start()
    {
        for (int i = 0; i < tabs.Length; i++)
        {
            int index = i;
            tabs[i].button.onClick.AddListener(() => ShowTab(index));
        }

        ShowTab(0);
    }

    public void ShowTab(int index)
    {
        Debug.Log("Showing tab " + index);
        
        if (index == currentTab) return;
        
        currentTab = index;
        
        Debug.Log("Change tab");

        for (int i = 0; i < tabs.Length; i++)
        {
            if (tabs[i].panel != null)
                tabs[i].panel.SetActive(i == index);

            if (tabs[i].button != null)
            {
                // tabs[i].button.transition = Selectable.Transition.None;
                tabs[i].button.GetComponent<Image>().color = (i == index) ? activeColor : inactiveColor;
            }
        }
    }

    public void DisableAllTabsButTheFirst()
    {
        for (int i = 1; i < tabs.Length; i++)
            if (tabs[i].button != null)
            {
                tabs[i].button.interactable = false;
                Debug.Log("Disable tab " + i);
            }
    }

    public void EnableAllTabsButTheFirst()
    {
        for (int i = 1; i < tabs.Length; i++)
            if (tabs[i].button != null)
                tabs[i].button.interactable = true;
    }
}