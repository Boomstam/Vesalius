using UnityEngine;

/// <summary>
/// Sits on the Part 0 Content GameObject alongside its InfoManager.
/// Shows the sounds panel whenever the active chapter slot is of type "sounds",
/// and hides it for every other slot type.
/// </summary>
public class SoundsSlotManager : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private InfoManager infoManager;

    [Header("UI")]
    [SerializeField] private GameObject soundsPanel;

    // -------------------------------------------------------------------------

    private void Start()
    {
        infoManager.OnChapterChanged += OnChapterChanged;
        Refresh(infoManager.CurrentIndex);
    }

    private void OnDestroy()
    {
        if (infoManager != null)
            infoManager.OnChapterChanged -= OnChapterChanged;
    }

    // -------------------------------------------------------------------------

    private void OnChapterChanged(int index) => Refresh(index);

    private void Refresh(int index)
    {
        bool isSoundsSlot = string.Equals(
            infoManager.GetSlotType(index), "sounds",
            System.StringComparison.OrdinalIgnoreCase);

        soundsPanel.SetActive(isSoundsSlot);
    }
}