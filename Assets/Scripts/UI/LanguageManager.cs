using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LanguageManager : MonoBehaviour
{
    public enum AppLanguage
    {
        English,
        Dutch
    }

    [Header("UI")]
    [SerializeField] private Button languageToggleButton;
    [SerializeField] private TextMeshProUGUI languageButtonLabel;

    [Header("Settings")]
    [SerializeField] private AppLanguage defaultLanguage = AppLanguage.English;
    [SerializeField] private bool useSystemLanguageOnFirstLaunch = true;

    public static LanguageManager Instance { get; private set; }
    public static AppLanguage CurrentLanguage { get; private set; } = AppLanguage.English;
    public static string CurrentLanguageCode => CurrentLanguage == AppLanguage.Dutch ? "nl" : "en";

    public event Action<AppLanguage> LanguageChanged;

    private const string PlayerPrefsKey = "Language";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        CurrentLanguage = LoadInitialLanguage();
        UpdateLanguageButtonLabel();
    }

    private void OnEnable()
    {
        if (languageToggleButton != null)
        {
            languageToggleButton.onClick.AddListener(ToggleLanguage);
        }
    }

    private void OnDisable()
    {
        if (languageToggleButton != null)
        {
            languageToggleButton.onClick.RemoveListener(ToggleLanguage);
        }
    }

    public void ToggleLanguage()
    {
        SetLanguage(CurrentLanguage == AppLanguage.English ? AppLanguage.Dutch : AppLanguage.English);
    }

    public void SetLanguage(AppLanguage language)
    {
        if (CurrentLanguage == language)
        {
            UpdateLanguageButtonLabel();
            return;
        }

        CurrentLanguage = language;
        PlayerPrefs.SetString(PlayerPrefsKey, CurrentLanguageCode);
        PlayerPrefs.Save();

        UpdateLanguageButtonLabel();
        LanguageChanged?.Invoke(CurrentLanguage);
    }

    private AppLanguage LoadInitialLanguage()
    {
        if (PlayerPrefs.HasKey(PlayerPrefsKey))
        {
            return FromCode(PlayerPrefs.GetString(PlayerPrefsKey));
        }

        if (useSystemLanguageOnFirstLaunch)
        {
            return (Application.systemLanguage == SystemLanguage.Dutch)
                ? AppLanguage.Dutch
                : AppLanguage.English;
        }

        return defaultLanguage;
    }

    private void UpdateLanguageButtonLabel()
    {
        if (languageButtonLabel == null)
        {
            return;
        }

        languageButtonLabel.text = (CurrentLanguage == AppLanguage.English) ? "EN" : "NL";
        
        Debug.Log("languageButtonLabel.text now: "  + languageButtonLabel.text);
    }

    private static AppLanguage FromCode(string languageCode)
    {
        return string.Equals(languageCode, "nl", StringComparison.OrdinalIgnoreCase)
            ? AppLanguage.Dutch
            : AppLanguage.English;
    }
}
