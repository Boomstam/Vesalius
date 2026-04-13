using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InfoManager : MonoBehaviour
{
    [System.Serializable]
    public struct ChapterData
    {
        public string title;
        [TextArea(4, 12)]
        public string content;
        public Sprite backgroundImage;
    }

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI infoTitleText;
    [SerializeField] private TextMeshProUGUI infoContentText;
    [SerializeField] private Button previousInfoButton;
    [SerializeField] private Button nextInfoButton;
    [SerializeField] private Image infoBackground;

    [Header("Chapters")] 
    [SerializeField] private ChapterData[] chapters;

    // Fired whenever the displayed chapter changes. Payload: new index.
    public event Action<int> OnChapterChanged;

    public int CurrentIndex => _currentIndex;
    public int ChapterCount => chapters.Length;

    private int _currentIndex = 0;

    private void Start()
    {
        previousInfoButton.onClick.AddListener(OnPrevious);
        nextInfoButton.onClick.AddListener(OnNext);
        DisplayChapter(_currentIndex);
    }

    private void OnDestroy()
    {
        previousInfoButton.onClick.RemoveListener(OnPrevious);
        nextInfoButton.onClick.RemoveListener(OnNext);
    }

    // Resets to chapter 0 without firing the event — used by TutorialManager
    // on show so animators don't double-fire with the subsequent GoToChapter(0).
    public void ResetToStart()
    {
        _currentIndex = 0;
        DisplayChapter(_currentIndex);
        // Intentionally no event — TutorialManager calls GoToChapter(0) right after.
    }

    public void GoToChapter(int index)
    {
        Debug.Log("GoToChapter "  + index);
        
        index = Mathf.Clamp(index, 0, chapters.Length - 1);
        _currentIndex = index;
        DisplayChapter(_currentIndex);
        OnChapterChanged?.Invoke(_currentIndex);
    }

    private void OnPrevious()
    {
        _currentIndex = (_currentIndex - 1 + chapters.Length) % chapters.Length;
        DisplayChapter(_currentIndex);
        OnChapterChanged?.Invoke(_currentIndex);
    }

    private void OnNext()
    {
        _currentIndex = (_currentIndex + 1) % chapters.Length;
        DisplayChapter(_currentIndex);
        OnChapterChanged?.Invoke(_currentIndex);
    }

    private void DisplayChapter(int index)
    {
        infoTitleText.text = chapters[index].title;
        infoContentText.text = chapters[index].content;
        infoBackground.sprite = chapters[index].backgroundImage;
    }
}