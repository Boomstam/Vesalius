using System;
using System.Collections.Generic;
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
    private class LocalizedEntry
    {
        public string type;
        public string key;
        public string title;
        public string content;
    }

    [System.Serializable]
    private class LocalizedEntryCollection
    {
        public string locale;
        public LocalizedEntry[] entries;
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
    private ChapterData[] _defaultChapters;
    private string _localizedResourceBasePath;

    private void Awake()
    {
        _defaultChapters = CloneChapters(chapters);
        _localizedResourceBasePath = ExtractLocalizedResourceBasePath(chaptersJsonResourcePath);
    }

    private void Start()
    {
        LoadChapterTextForCurrentLanguage();

        previousInfoButton.onClick.AddListener(OnPrevious);
        nextInfoButton.onClick.AddListener(OnNext);

        if (LanguageManager.Instance != null)
        {
            LanguageManager.Instance.LanguageChanged += OnLanguageChanged;
        }

        if (ChapterCount > 0)
        {
            DisplayChapter(_currentIndex);
        }
    }

    private void OnDestroy()
    {
        previousInfoButton.onClick.RemoveListener(OnPrevious);
        nextInfoButton.onClick.RemoveListener(OnNext);

        if (LanguageManager.Instance != null)
        {
            LanguageManager.Instance.LanguageChanged -= OnLanguageChanged;
        }
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
        chapters = CloneChapters(_defaultChapters);

        string resourcePath = ResolveLocalizedResourcePath();
        if (string.IsNullOrWhiteSpace(resourcePath)) return;

        TextAsset jsonAsset = Resources.Load<TextAsset>(resourcePath);
        if (jsonAsset == null)
        {
            Debug.LogWarning($"InfoManager on '{gameObject.name}' could not load JSON at Resources path '{resourcePath}'.");
            return;
        }

        LocalizedEntryCollection localizedData = JsonUtility.FromJson<LocalizedEntryCollection>(jsonAsset.text);
        if (localizedData == null || localizedData.entries == null || localizedData.entries.Length == 0)
        {
            Debug.LogWarning($"InfoManager on '{gameObject.name}' found no entries in '{resourcePath}'.");
            return;
        }

        // Build full slot list from JSON: chapters get merged text, images become blank placeholders.
        var result = new List<ChapterData>();
        int chapterIndex = 0;

        foreach (LocalizedEntry entry in localizedData.entries)
        {
            if (entry == null) continue;

            if (string.Equals(entry.type, "chapter", StringComparison.OrdinalIgnoreCase))
            {
                ChapterData chapter = chapterIndex < chapters.Length
                    ? chapters[chapterIndex]
                    : new ChapterData();

                if (!string.IsNullOrWhiteSpace(entry.title))   chapter.title   = entry.title;
                if (!string.IsNullOrWhiteSpace(entry.content)) chapter.content = entry.content;

                result.Add(chapter);
                chapterIndex++;
            }
            else if (string.Equals(entry.type, "image", StringComparison.OrdinalIgnoreCase))
            {
                // Blank placeholder — title will be set by DisplayChapter via the image entry's title,
                // but we still need a slot so indices stay in sync with ContentImageManager.
                result.Add(new ChapterData { title = entry.title ?? string.Empty });
            }
        }

        chapters = result.ToArray();
    }

    private void LoadChapterTextForCurrentLanguage()
    {
        LoadChapterTextFromJson();

        if (ChapterCount == 0)
        {
            return;
        }

        _currentIndex = Mathf.Clamp(_currentIndex, 0, ChapterCount - 1);
        DisplayChapter(_currentIndex);
    }

    private void OnLanguageChanged(LanguageManager.AppLanguage _)
    {
        LoadChapterTextForCurrentLanguage();
    }

    private string ResolveLocalizedResourcePath()
    {
        if (!string.IsNullOrWhiteSpace(_localizedResourceBasePath))
        {
            string preferredPath = $"{_localizedResourceBasePath}.{LanguageManager.CurrentLanguageCode}";
            if (Resources.Load<TextAsset>(preferredPath) != null)
            {
                return preferredPath;
            }
        }

        return chaptersJsonResourcePath;
    }

    private static string ExtractLocalizedResourceBasePath(string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            return resourcePath;
        }

        int separatorIndex = resourcePath.LastIndexOf('.');
        if (separatorIndex < 0 || separatorIndex >= resourcePath.Length - 1)
        {
            return resourcePath;
        }

        string suffix = resourcePath.Substring(separatorIndex + 1);
        if (suffix.Equals("en", StringComparison.OrdinalIgnoreCase) ||
            suffix.Equals("nl", StringComparison.OrdinalIgnoreCase) ||
            suffix.Equals("default", StringComparison.OrdinalIgnoreCase))
        {
            return resourcePath.Substring(0, separatorIndex);
        }

        return resourcePath;
    }

    private static ChapterData[] CloneChapters(ChapterData[] source)
    {
        if (source == null)
        {
            return Array.Empty<ChapterData>();
        }

        ChapterData[] clone = new ChapterData[source.Length];
        Array.Copy(source, clone, source.Length);
        return clone;
    }
}