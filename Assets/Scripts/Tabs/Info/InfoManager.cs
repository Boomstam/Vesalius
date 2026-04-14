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

    [System.Serializable]
    private class LocalizedChapter
    {
        public string title;
        public string content;
    }

    [System.Serializable]
    private class LocalizedChapterCollection
    {
        public string locale;
        public LocalizedChapter[] chapters;
    }

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI infoTitleText;
    [SerializeField] private TextMeshProUGUI infoContentText;
    [SerializeField] private Button previousInfoButton;
    [SerializeField] private Button nextInfoButton;
    [SerializeField] private Image infoBackground;

    [Header("Chapters")]
    [SerializeField] private string chaptersJsonResourcePath;
    [SerializeField] private ChapterData[] chapters;

    // Fired whenever the displayed chapter changes. Payload: new index.
    public event Action<int> OnChapterChanged;

    public int CurrentIndex => _currentIndex;
    public int ChapterCount => chapters?.Length ?? 0;

    private int _currentIndex;

    private void Start()
    {
        LoadChapterTextFromJson();

        previousInfoButton.onClick.AddListener(OnPrevious);
        nextInfoButton.onClick.AddListener(OnNext);

        if (ChapterCount > 0)
        {
            DisplayChapter(_currentIndex);
        }
    }

    private void OnDestroy()
    {
        previousInfoButton.onClick.RemoveListener(OnPrevious);
        nextInfoButton.onClick.RemoveListener(OnNext);
    }

    // Resets to chapter 0 without firing the event, used by TutorialManager
    // on show so animators don't double-fire with the subsequent GoToChapter(0).
    public void ResetToStart()
    {
        if (ChapterCount == 0)
        {
            return;
        }

        _currentIndex = 0;
        DisplayChapter(_currentIndex);
        // Intentionally no event, TutorialManager calls GoToChapter(0) right after.
    }

    public void GoToChapter(int index)
    {
        if (ChapterCount == 0)
        {
            return;
        }

        index = Mathf.Clamp(index, 0, ChapterCount - 1);
        _currentIndex = index;
        DisplayChapter(_currentIndex);
        OnChapterChanged?.Invoke(_currentIndex);
    }

    private void OnPrevious()
    {
        if (ChapterCount == 0)
        {
            return;
        }

        _currentIndex = (_currentIndex - 1 + ChapterCount) % ChapterCount;
        DisplayChapter(_currentIndex);
        OnChapterChanged?.Invoke(_currentIndex);
    }

    private void OnNext()
    {
        if (ChapterCount == 0)
        {
            return;
        }

        _currentIndex = (_currentIndex + 1) % ChapterCount;
        DisplayChapter(_currentIndex);
        OnChapterChanged?.Invoke(_currentIndex);
    }

    private void DisplayChapter(int index)
    {
        infoTitleText.text = chapters[index].title;
        infoContentText.text = chapters[index].content;
        infoBackground.sprite = chapters[index].backgroundImage;
    }

    private void LoadChapterTextFromJson()
    {
        if (string.IsNullOrWhiteSpace(chaptersJsonResourcePath))
        {
            return;
        }

        TextAsset jsonAsset = Resources.Load<TextAsset>(chaptersJsonResourcePath);
        if (jsonAsset == null)
        {
            Debug.LogWarning($"InfoManager on '{gameObject.name}' could not load JSON at Resources path '{chaptersJsonResourcePath}'.");
            return;
        }

        LocalizedChapterCollection localizedData = JsonUtility.FromJson<LocalizedChapterCollection>(jsonAsset.text);
        if (localizedData == null || localizedData.chapters == null || localizedData.chapters.Length == 0)
        {
            Debug.LogWarning($"InfoManager on '{gameObject.name}' found no chapters in '{chaptersJsonResourcePath}'.");
            return;
        }

        if (chapters == null || chapters.Length == 0)
        {
            Debug.LogWarning($"InfoManager on '{gameObject.name}' has no serialized chapters to merge JSON text into.");
            return;
        }

        if (localizedData.chapters.Length != chapters.Length)
        {
            Debug.LogWarning(
                $"InfoManager on '{gameObject.name}' found {localizedData.chapters.Length} JSON chapters in '{chaptersJsonResourcePath}' but has {chapters.Length} serialized chapters. Applying the overlapping chapter count.");
        }

        int chapterCount = Mathf.Min(chapters.Length, localizedData.chapters.Length);
        for (int i = 0; i < chapterCount; i++)
        {
            LocalizedChapter localizedChapter = localizedData.chapters[i];
            if (localizedChapter == null)
            {
                continue;
            }

            ChapterData chapter = chapters[i];

            if (!string.IsNullOrWhiteSpace(localizedChapter.title))
            {
                chapter.title = localizedChapter.title;
            }

            if (!string.IsNullOrWhiteSpace(localizedChapter.content))
            {
                chapter.content = localizedChapter.content;
            }

            chapters[i] = chapter;
        }
    }
}
