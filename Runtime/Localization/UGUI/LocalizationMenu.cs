using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameJamKit.Localization.UGUI
{
    [DisallowMultipleComponent]
    public sealed class LocalizationMenu : MonoBehaviour
    {
        [SerializeField] private LocalizationManager _manager;
        [SerializeField] private TMP_Dropdown _languageDropdown;
        [SerializeField] private bool _applyImmediately = true;
        [SerializeField] private Button _applyButton;
        [SerializeField] private Button _resetButton;

        private readonly List<string> _localeCodes = new List<string>();
        private TMP_FontAsset _originalCaptionFont;
        private TMP_FontAsset _originalItemFont;
        private bool _fontsCached;

        private void OnEnable()
        {
            ResolveManager();
            if (_manager == null)
            {
                Debug.LogWarning("Localization Menu requires a Localization Manager.", this);
                return;
            }

            _manager.Initialize();
            _manager.LanguageChanged += HandleLanguageChanged;
            CacheOriginalFonts();
            PopulateLanguages();

            if (_languageDropdown != null)
            {
                _languageDropdown.onValueChanged.AddListener(HandleLanguageSelected);
            }

            if (_applyButton != null)
            {
                _applyButton.onClick.AddListener(ApplySelection);
            }

            if (_resetButton != null)
            {
                _resetButton.onClick.AddListener(ResetToDefault);
            }
        }

        private void OnDisable()
        {
            if (_manager != null)
            {
                _manager.LanguageChanged -= HandleLanguageChanged;
            }

            if (_languageDropdown != null)
            {
                _languageDropdown.onValueChanged.RemoveListener(HandleLanguageSelected);
            }

            if (_applyButton != null)
            {
                _applyButton.onClick.RemoveListener(ApplySelection);
            }

            if (_resetButton != null)
            {
                _resetButton.onClick.RemoveListener(ResetToDefault);
            }
        }

        public void ApplySelection()
        {
            if (_manager == null || _localeCodes.Count == 0)
            {
                return;
            }

            int index = Mathf.Clamp(
                _languageDropdown != null ? _languageDropdown.value : 0,
                0,
                _localeCodes.Count - 1);
            _manager.SetLocale(_localeCodes[index]);
        }

        public void ResetToDefault()
        {
            if (_manager != null)
            {
                _manager.ResetToDefault();
            }
        }

        public void Refresh()
        {
            PopulateLanguages();
        }

        private void PopulateLanguages()
        {
            _localeCodes.Clear();
            if (_manager == null || _manager.Profile == null)
            {
                return;
            }

            List<string> labels = new List<string>();
            IReadOnlyList<LocalizationLocale> locales = _manager.Profile.SupportedLocales;
            int selectedIndex = 0;

            for (int i = 0; i < locales.Count; i++)
            {
                LocalizationLocale locale = locales[i];
                if (locale == null || string.IsNullOrEmpty(locale.Code))
                {
                    continue;
                }

                if (string.Equals(
                        locale.Code,
                        _manager.CurrentLocaleCode,
                        System.StringComparison.OrdinalIgnoreCase))
                {
                    selectedIndex = _localeCodes.Count;
                }

                _localeCodes.Add(locale.Code);
                labels.Add(locale.DisplayName);
            }

            if (_languageDropdown == null)
            {
                return;
            }

            _languageDropdown.ClearOptions();
            _languageDropdown.AddOptions(labels);
            _languageDropdown.SetValueWithoutNotify(selectedIndex);
            ApplyLocalizedFonts();
            _languageDropdown.RefreshShownValue();
        }

        private void ResolveManager()
        {
            if (_manager == null)
            {
                _manager = LocalizationManager.Instance;
            }

            if (_manager == null)
            {
                _manager = FindFirstObjectByType<LocalizationManager>();
            }
        }

        private void HandleLanguageSelected(int index)
        {
            if (_applyImmediately)
            {
                ApplySelection();
            }
        }

        private void HandleLanguageChanged(string localeCode)
        {
            PopulateLanguages();
        }

        private void CacheOriginalFonts()
        {
            if (_fontsCached || _languageDropdown == null)
            {
                return;
            }

            _originalCaptionFont = _languageDropdown.captionText != null
                ? _languageDropdown.captionText.font
                : null;
            _originalItemFont = _languageDropdown.itemText != null
                ? _languageDropdown.itemText.font
                : null;
            _fontsCached = true;
        }

        private void ApplyLocalizedFonts()
        {
            if (_languageDropdown == null || _manager == null)
            {
                return;
            }

            CacheOriginalFonts();
            TMP_FontAsset localizedFont = null;
            if (_manager.Profile != null)
            {
                localizedFont = _manager.Profile.LanguageMenuFont != null
                    ? _manager.Profile.LanguageMenuFont
                    : _manager.Profile.GetFont(_manager.CurrentLocaleCode);
            }

            if (_languageDropdown.captionText != null)
            {
                _languageDropdown.captionText.font = localizedFont != null
                    ? localizedFont
                    : _originalCaptionFont;
            }

            if (_languageDropdown.itemText != null)
            {
                _languageDropdown.itemText.font = localizedFont != null
                    ? localizedFont
                    : _originalItemFont;
            }
        }
    }
}
