using UnityEngine;
using UnityEngine.UI;

public class TabManagement : MonoBehaviour
{
    [Header("Tab Buttons")]
    public Button btnInfo;
    public Button btnVisuals;
    public Button btnSounds;

    [Header("Tab Panels")]
    public GameObject[] tabPanels;

    [Header("Tab Button Colors")]
    public Color activeColor = Color.white;
    public Color inactiveColor = new Color(0.7f, 0.7f, 0.7f);

    private Button[] tabButtons;
    private int currentTab = -1;

    private void Start()
    {
        tabButtons = new Button[] { btnInfo, btnVisuals, btnSounds };

        btnInfo.onClick.AddListener(() => ShowTab(0));
        btnVisuals.onClick.AddListener(() => ShowTab(1));
        btnSounds.onClick.AddListener(() => ShowTab(2));
        ShowTab(0);
    }

    public void ShowTab(int index)
    {
        if (index == currentTab) return;
        currentTab = index;

        // Toggle panels
        for (int i = 0; i < tabPanels.Length; i++)
        {
            if (tabPanels[i] != null)
                tabPanels[i].SetActive(i == index);
        }

        // Update button visuals
        for (int i = 0; i < tabButtons.Length; i++)
        {
            if (tabButtons[i] != null)
            {
                ColorBlock colors = tabButtons[i].colors;
                colors.normalColor = (i == index) ? activeColor : inactiveColor;
                tabButtons[i].colors = colors;
            }
        }
    }
}