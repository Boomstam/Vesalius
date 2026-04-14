using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ContentImageManager : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private InfoManager infoManager;

    [Header("UI")]
    [SerializeField] private Image displayImage;
    [SerializeField] private TextMeshProUGUI imageTitleText;

    [Header("Assets")]
    [SerializeField] private Sprite[] imageAssets;

    [Header("Resource Path")]
    [SerializeField] private string chaptersJsonResourcePath;

    // -------------------------------------------------------------------------

    [System.Serializable]
    private class LocalizedEntry
    {
        public string type;
        public string key;
        public string title;
    }

    [System.Serializable]
    private class LocalizedEntryCollection
    {
        public string locale;
        public LocalizedEntry[] entries;
    }

    private struct ImageEntry
    {
        public Sprite sprite;
        public string title;
    }

    // Maps chapter index (0-based) → image to show while that chapter is active.
    private readonly Dictionary<int, ImageEntry> _chapterImageMap = new Dictionary<int, ImageEntry>();

    private string _localizedResourceBasePath;

    // -------------------------------------------------------------------------

    private void Start()
    {
        _localizedResourceBasePath = ExtractLocalizedResourceBasePath(chaptersJsonResourcePath);

        BuildImageMap();

        infoManager.OnChapterChanged += OnChapterChanged;

        if (LanguageManager.Instance != null)
        {
            LanguageManager.Instance.LanguageChanged += OnLanguageChanged;
        }

        ApplyForChapter(infoManager.CurrentIndex);
    }

    private void OnDestroy()
    {
        if (infoManager != null)
        {
            infoManager.OnChapterChanged -= OnChapterChanged;
        }

        if (LanguageManager.Instance != null)
        {
            LanguageManager.Instance.LanguageChanged -= OnLanguageChanged;
        }
    }

    // -------------------------------------------------------------------------

    private void BuildImageMap()
    {
        _chapterImageMap.Clear();

        string resourcePath = ResolveLocalizedResourcePath();
        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            return;
        }

        TextAsset jsonAsset = Resources.Load<TextAsset>(resourcePath);
        if (jsonAsset == null)
        {
            Debug.LogWarning($"ContentImageManager on '{gameObject.name}' could not load JSON at Resources path '{resourcePath}'.");
            return;
        }

        LocalizedEntryCollection data = JsonUtility.FromJson<LocalizedEntryCollection>(jsonAsset.text);
        if (data == null || data.entries == null || data.entries.Length == 0)
        {
            return;
        }

        int chapterIndex = -1;

        foreach (LocalizedEntry entry in data.entries)
        {
            if (entry == null)
            {
                continue;
            }

            if (string.Equals(entry.type, "chapter", StringComparison.OrdinalIgnoreCase))
            {
                chapterIndex++;
                continue;
            }

            if (!string.Equals(entry.type, "image", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (chapterIndex < 0)
            {
                Debug.LogWarning(
                    $"ContentImageManager on '{gameObject.name}': Image entry with key '{entry.key}' " +
                    $"appears before any chapter entry in '{resourcePath}' and will be ignored.");
                continue;
            }

            if (_chapterImageMap.ContainsKey(chapterIndex))
            {
                throw new Exception(
                    $"ContentImageManager on '{gameObject.name}': Two image entries map to the same chapter index " +
                    $"{chapterIndex} in '{resourcePath}'. Fix the JSON entry order so no two image entries are " +
                    $"adjacent without a chapter entry between them.");
            }

            Sprite sprite = FindSprite(entry.key);
            if (sprite == null)
            {
                throw new Exception(
                    $"ContentImageManager on '{gameObject.name}': No sprite asset found with name '{entry.key}'. " +
                    $"Add it to the imageAssets array in the Inspector.");
            }

            _chapterImageMap[chapterIndex] = new ImageEntry
            {
                sprite = sprite,
                title  = entry.title ?? string.Empty
            };
        }
    }

    private void ApplyForChapter(int index)
    {
        if (_chapterImageMap.TryGetValue(index, out ImageEntry entry))
        {
            displayImage.sprite = entry.sprite;
            displayImage.gameObject.SetActive(true);
        }
        else
        {
            displayImage.gameObject.SetActive(false);
        }
    }

    // -------------------------------------------------------------------------

    private void OnChapterChanged(int index)
    {
        ApplyForChapter(index);
    }

    private void OnLanguageChanged(LanguageManager.AppLanguage _)
    {
        BuildImageMap();
        ApplyForChapter(infoManager.CurrentIndex);
    }

    // -------------------------------------------------------------------------

    private Sprite FindSprite(string key)
    {
        if (imageAssets == null || string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        foreach (Sprite sprite in imageAssets)
        {
            if (sprite != null && string.Equals(sprite.name, key, StringComparison.Ordinal))
            {
                return sprite;
            }
        }

        return null;
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
}
