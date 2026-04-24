using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InfoManager : MonoBehaviour
{
    public const string ChapterType = "chapter";
    public const string ImageType = "image";
    public const string SoundType = "sound";
    public const string SoundsType = "sounds";
    public const string DoNotDisturbType = "do_not_disturb";

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
    public string chaptersJsonResourcePath;
    [SerializeField] private ChapterData[] chapters;

    [Header("Participation Mode")]
    [Tooltip("When participation mode is enabled, this 0-based chapter index will be skipped. Use -1 to disable.")]
    [SerializeField] private int participationSkippedChapterIndex = -1;

    public event Action<int> OnChapterChanged;

    public int CurrentIndex => _currentIndex;
    public int ChapterCount => chapters?.Length ?? 0;
    public TextMeshProUGUI InfoTitleText => infoTitleText;
    public TextMeshProUGUI InfoContentText => infoContentText;
    public Button PreviousInfoButton => previousInfoButton;
    public Button NextInfoButton => nextInfoButton;
    public Image InfoBackground => infoBackground;

    public string GetSlotType(int index)
    {
        if (_slotTypes == null || index < 0 || index >= _slotTypes.Length)
            return string.Empty;

        return _slotTypes[index];
    }

    private int _currentIndex;
    private ChapterData[] _defaultChapters;
    private string[] _slotTypes;
    private string _localizedResourceBasePath;
    private string _displayTitleOverride;
    private int _skippedChapterIndex = -1;

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
            LanguageManager.Instance.LanguageChanged += OnLanguageChanged;

        if (ChapterCount > 0)
            DisplayChapter(_currentIndex);
    }

    private void OnDestroy()
    {
        previousInfoButton.onClick.RemoveListener(OnPrevious);
        nextInfoButton.onClick.RemoveListener(OnNext);

        if (LanguageManager.Instance != null)
            LanguageManager.Instance.LanguageChanged -= OnLanguageChanged;
    }

    public void ResetToStart()
    {
        if (ChapterCount == 0) return;
        _currentIndex = GetFirstAccessibleChapterIndex();
        DisplayChapter(_currentIndex);
    }

    public void GoToChapter(int index)
    {
        if (ChapterCount == 0) return;

        index = Mathf.Clamp(index, 0, ChapterCount - 1);
        index = GetNearestAccessibleChapterIndex(index, 1);
        _currentIndex = index;
        DisplayChapter(_currentIndex);
        OnChapterChanged?.Invoke(_currentIndex);
    }

    public void SetSkippedChapterIndex(int index)
    {
        int newIndex = index >= 0 ? index : -1;
        if (_skippedChapterIndex == newIndex)
            return;

        _skippedChapterIndex = newIndex;

        if (ChapterCount == 0)
            return;

        int adjustedIndex = GetNearestAccessibleChapterIndex(Mathf.Clamp(_currentIndex, 0, ChapterCount - 1), 1);
        _currentIndex = adjustedIndex;
        DisplayChapter(_currentIndex);
        OnChapterChanged?.Invoke(_currentIndex);
    }

    public void SetParticipationMode(bool enabled)
    {
        SetSkippedChapterIndex(enabled ? participationSkippedChapterIndex : -1);
    }

    public int GetLastAccessibleChapterIndex()
    {
        if (ChapterCount == 0)
            return -1;

        for (int i = ChapterCount - 1; i >= 0; i--)
        {
            if (!IsChapterSkipped(i))
                return i;
        }

        return Mathf.Clamp(_currentIndex, 0, ChapterCount - 1);
    }

    public void SetDisplayedTitleOverride(string title)
    {
        _displayTitleOverride = title;

        if (ChapterCount > 0)
            DisplayChapter(_currentIndex);
    }

    private void OnPrevious()
    {
        if (ChapterCount == 0) return;
        _currentIndex = GetAdjacentAccessibleChapterIndex(_currentIndex, -1);
        DisplayChapter(_currentIndex);
        OnChapterChanged?.Invoke(_currentIndex);
    }

    private void OnNext()
    {
        if (ChapterCount == 0) return;
        _currentIndex = GetAdjacentAccessibleChapterIndex(_currentIndex, 1);
        DisplayChapter(_currentIndex);
        OnChapterChanged?.Invoke(_currentIndex);
    }

    private void DisplayChapter(int index)
    {
        infoTitleText.text = string.IsNullOrWhiteSpace(_displayTitleOverride)
            ? chapters[index].title
            : _displayTitleOverride;
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

        var result = new List<ChapterData>();
        var slotTypes = new List<string>();
        int chapterIndex = 0;

        foreach (LocalizedEntry entry in localizedData.entries)
        {
            if (entry == null) continue;

            if (string.Equals(entry.type, ChapterType, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(entry.type, DoNotDisturbType, StringComparison.OrdinalIgnoreCase))
            {
                ChapterData chapter = chapterIndex < chapters.Length
                    ? chapters[chapterIndex]
                    : new ChapterData();

                if (!string.IsNullOrWhiteSpace(entry.title))
                    chapter.title = entry.title;
                if (!string.IsNullOrWhiteSpace(entry.content))
                    chapter.content = entry.content;

                result.Add(chapter);
                slotTypes.Add(entry.type);
                chapterIndex++;
            }
            else if (string.Equals(entry.type, ImageType, StringComparison.OrdinalIgnoreCase))
            {
                result.Add(new ChapterData { title = entry.title ?? string.Empty });
                slotTypes.Add(ImageType);
            }
            else if (string.Equals(entry.type, SoundType, StringComparison.OrdinalIgnoreCase))
            {
                result.Add(new ChapterData { title = entry.title ?? string.Empty });
                slotTypes.Add(SoundType);
            }
            else if (string.Equals(entry.type, SoundsType, StringComparison.OrdinalIgnoreCase))
            {
                result.Add(new ChapterData { title = entry.title ?? string.Empty });
                slotTypes.Add(SoundsType);
            }
        }

        chapters = result.ToArray();
        _slotTypes = slotTypes.ToArray();
    }

    private void LoadChapterTextForCurrentLanguage()
    {
        LoadChapterTextFromJson();

        if (ChapterCount == 0) return;

        _currentIndex = GetNearestAccessibleChapterIndex(Mathf.Clamp(_currentIndex, 0, ChapterCount - 1), 1);
        DisplayChapter(_currentIndex);
    }

    private int GetFirstAccessibleChapterIndex()
    {
        return GetNearestAccessibleChapterIndex(0, 1);
    }

    private int GetNearestAccessibleChapterIndex(int index, int direction)
    {
        if (ChapterCount == 0)
            return 0;

        index = Mathf.Clamp(index, 0, ChapterCount - 1);
        if (!IsChapterSkipped(index))
            return index;

        int candidate = index;
        for (int i = 0; i < ChapterCount; i++)
        {
            candidate = (candidate + direction + ChapterCount) % ChapterCount;
            if (!IsChapterSkipped(candidate))
                return candidate;
        }

        return index;
    }

    private int GetAdjacentAccessibleChapterIndex(int currentIndex, int direction)
    {
        if (ChapterCount == 0)
            return 0;

        int candidate = Mathf.Clamp(currentIndex, 0, ChapterCount - 1);
        for (int i = 0; i < ChapterCount; i++)
        {
            candidate = (candidate + direction + ChapterCount) % ChapterCount;
            if (!IsChapterSkipped(candidate))
                return candidate;
        }

        return candidate;
    }

    private bool IsChapterSkipped(int index)
    {
        return ChapterCount > 1 && _skippedChapterIndex >= 0 && index == _skippedChapterIndex;
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
                return preferredPath;
        }

        return chaptersJsonResourcePath;
    }

    private static string ExtractLocalizedResourceBasePath(string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath))
            return resourcePath;

        int separatorIndex = resourcePath.LastIndexOf('.');
        if (separatorIndex < 0 || separatorIndex >= resourcePath.Length - 1)
            return resourcePath;

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
        if (source == null) return Array.Empty<ChapterData>();
        ChapterData[] clone = new ChapterData[source.Length];
        Array.Copy(source, clone, source.Length);
        return clone;
    }
}
