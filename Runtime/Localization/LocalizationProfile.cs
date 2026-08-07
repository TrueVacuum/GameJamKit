using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace GameJamKit.Localization
{
    [CreateAssetMenu(
        fileName = "LocalizationProfile",
        menuName = "Game Jam Kit/Localization Profile")]
    public sealed class LocalizationProfile : ScriptableObject
    {
        [SerializeField] private string _defaultLocaleCode = "en";
        [SerializeField] private string _fallbackLocaleCode = "en";
        [SerializeField] private bool _detectSystemLanguage = true;
        [SerializeField] private string _playerPrefsKey = "GameJamKit.Localization";
        [SerializeField] private string _missingTranslationFormat = "[MISSING: {0}]";
        [SerializeField] private TMP_FontAsset _defaultFont;
        [Tooltip("Font used by the language selector, which may show several writing systems at once.")]
        [SerializeField] private TMP_FontAsset _languageMenuFont;
        [SerializeField] private List<LocalizationLocale> _supportedLocales =
            new List<LocalizationLocale>();
        [SerializeField] private List<TextAsset> _csvSources = new List<TextAsset>();

        public string DefaultLocaleCode => LocalizationLocale.NormalizeCode(_defaultLocaleCode);
        public string FallbackLocaleCode => LocalizationLocale.NormalizeCode(_fallbackLocaleCode);
        public bool DetectSystemLanguage => _detectSystemLanguage;
        public TMP_FontAsset DefaultFont => _defaultFont;
        public TMP_FontAsset LanguageMenuFont => _languageMenuFont;
        public string PlayerPrefsKey => string.IsNullOrWhiteSpace(_playerPrefsKey)
            ? "GameJamKit.Localization"
            : _playerPrefsKey.Trim();
        public string MissingTranslationFormat => string.IsNullOrEmpty(_missingTranslationFormat)
            ? "[MISSING: {0}]"
            : _missingTranslationFormat;
        public IReadOnlyList<LocalizationLocale> SupportedLocales => _supportedLocales;
        public IReadOnlyList<TextAsset> CsvSources => _csvSources;

        public LocalizationLocale FindLocale(string localeCode)
        {
            string normalizedCode = LocalizationLocale.NormalizeCode(localeCode);
            if (_supportedLocales == null)
            {
                return null;
            }

            for (int i = 0; i < _supportedLocales.Count; i++)
            {
                LocalizationLocale locale = _supportedLocales[i];
                if (locale != null && string.Equals(
                        locale.Code,
                        normalizedCode,
                        System.StringComparison.OrdinalIgnoreCase))
                {
                    return locale;
                }
            }

            return null;
        }

        public LocalizationLocale FindSystemLocale(SystemLanguage systemLanguage)
        {
            if (_supportedLocales == null)
            {
                return null;
            }

            for (int i = 0; i < _supportedLocales.Count; i++)
            {
                LocalizationLocale locale = _supportedLocales[i];
                if (locale != null && locale.Matches(systemLanguage))
                {
                    return locale;
                }
            }

            return null;
        }

        public TMP_FontAsset GetFont(string localeCode)
        {
            LocalizationLocale locale = FindLocale(localeCode);
            return locale != null && locale.FontOverride != null
                ? locale.FontOverride
                : _defaultFont;
        }

        public string FormatMissingTranslation(string key)
        {
            return MissingTranslationFormat.Replace("{0}", key ?? string.Empty);
        }
    }
}
