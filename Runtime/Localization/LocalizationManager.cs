using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace GameJamKit.Localization
{
    [DisallowMultipleComponent]
    public sealed class LocalizationManager : MonoBehaviour
    {
        [SerializeField] private LocalizationProfile _profile;

        private LocalizationDatabase _database;
        private string _currentLocaleCode;
        private bool _initialized;

        public static LocalizationManager Instance { get; private set; }

        public LocalizationProfile Profile => _profile;
        public string CurrentLocaleCode => _currentLocaleCode;
        public LocalizationLocale CurrentLocale => _profile != null
            ? _profile.FindLocale(_currentLocaleCode)
            : null;

        public event Action<string> LanguageChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("More than one Localization Manager is active.", this);
            }

            Instance = this;
            Initialize();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            if (_profile == null)
            {
                Debug.LogWarning("Localization Manager requires a Localization Profile.", this);
                _database = new LocalizationDatabase();
                return;
            }

            List<string> issues = new List<string>();
            _database = LocalizationDatabase.Create(_profile.CsvSources, issues);
            for (int i = 0; i < issues.Count; i++)
            {
                Debug.LogWarning(issues[i], _profile);
            }

            _currentLocaleCode = ResolveInitialLocaleCode();
        }

        public bool SetLocale(string localeCode, bool save = true)
        {
            Initialize();
            string normalizedCode = LocalizationLocale.NormalizeCode(localeCode);
            if (!CanUseLocale(normalizedCode))
            {
                Debug.LogWarning($"Locale '{localeCode}' is not configured or has no CSV column.", this);
                return false;
            }

            bool changed = !string.Equals(
                _currentLocaleCode,
                normalizedCode,
                StringComparison.OrdinalIgnoreCase);
            _currentLocaleCode = normalizedCode;

            if (save && _profile != null)
            {
                PlayerPrefs.SetString(_profile.PlayerPrefsKey, normalizedCode);
                PlayerPrefs.Save();
            }

            if (changed)
            {
                LanguageChanged?.Invoke(_currentLocaleCode);
            }

            return true;
        }

        public void ResetToDefault()
        {
            if (_profile != null)
            {
                SetLocale(_profile.DefaultLocaleCode);
            }
        }

        public string GetText(string key, params object[] arguments)
        {
            Initialize();
            string text = TryGetText(key, out string localizedText)
                ? localizedText
                : GetMissingTranslation(key);
            return FormatText(key, text, arguments);
        }

        public string GetTextOrFallback(
            string key,
            string fallbackText,
            params object[] arguments)
        {
            Initialize();
            string text = TryGetText(key, out string localizedText)
                ? localizedText
                : fallbackText;
            return FormatText(key, text ?? string.Empty, arguments);
        }

        public bool TryGetText(string key, out string text)
        {
            Initialize();
            text = null;
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            if (_database != null && _database.TryGetText(_currentLocaleCode, key, out text))
            {
                return true;
            }

            return _profile != null &&
                   _database != null &&
                   _database.TryGetText(_profile.FallbackLocaleCode, key, out text);
        }

        private string FormatText(string key, string text, object[] arguments)
        {
            if (arguments == null || arguments.Length == 0)
            {
                return text;
            }

            try
            {
                return string.Format(GetCurrentCulture(), text, arguments);
            }
            catch (FormatException exception)
            {
                Debug.LogWarning(
                    $"Localization key '{key}' has invalid format arguments: {exception.Message}",
                    this);
                return text;
            }
        }

        private string GetMissingTranslation(string key)
        {
            return _profile != null
                ? _profile.FormatMissingTranslation(key)
                : $"[MISSING: {key}]";
        }

        public void ReloadSources()
        {
            if (_profile == null)
            {
                return;
            }

            List<string> issues = new List<string>();
            _database = LocalizationDatabase.Create(_profile.CsvSources, issues);
            for (int i = 0; i < issues.Count; i++)
            {
                Debug.LogWarning(issues[i], _profile);
            }

            _initialized = true;
            if (!CanUseLocale(_currentLocaleCode))
            {
                _currentLocaleCode = ResolveInitialLocaleCode();
            }

            LanguageChanged?.Invoke(_currentLocaleCode);
        }

        private string ResolveInitialLocaleCode()
        {
            if (PlayerPrefs.HasKey(_profile.PlayerPrefsKey))
            {
                string savedCode = PlayerPrefs.GetString(_profile.PlayerPrefsKey);
                if (CanUseLocale(savedCode))
                {
                    return LocalizationLocale.NormalizeCode(savedCode);
                }
            }

            if (_profile.DetectSystemLanguage)
            {
                LocalizationLocale systemLocale = _profile.FindSystemLocale(Application.systemLanguage);
                if (systemLocale != null && CanUseLocale(systemLocale.Code))
                {
                    return systemLocale.Code;
                }
            }

            if (CanUseLocale(_profile.DefaultLocaleCode))
            {
                return _profile.DefaultLocaleCode;
            }

            if (CanUseLocale(_profile.FallbackLocaleCode))
            {
                return _profile.FallbackLocaleCode;
            }

            IReadOnlyList<LocalizationLocale> locales = _profile.SupportedLocales;
            for (int i = 0; i < locales.Count; i++)
            {
                LocalizationLocale locale = locales[i];
                if (locale != null && CanUseLocale(locale.Code))
                {
                    return locale.Code;
                }
            }

            return string.Empty;
        }

        private bool CanUseLocale(string localeCode)
        {
            return _profile != null &&
                   _profile.FindLocale(localeCode) != null &&
                   _database != null &&
                   _database.ContainsLocale(localeCode);
        }

        private CultureInfo GetCurrentCulture()
        {
            try
            {
                return CultureInfo.GetCultureInfo(_currentLocaleCode);
            }
            catch (CultureNotFoundException)
            {
                return CultureInfo.InvariantCulture;
            }
        }
    }
}
