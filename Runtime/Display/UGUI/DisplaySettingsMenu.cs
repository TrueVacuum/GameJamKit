using System.Collections.Generic;
using GameJamKit.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameJamKit.Display.UGUI
{
    [DisallowMultipleComponent]
    public sealed class DisplaySettingsMenu : MonoBehaviour
    {
        [SerializeField] private DisplaySettingsManager _manager;
        [SerializeField] private LocalizationManager _localizationManager;
        [SerializeField] private TMP_Dropdown _displayModeDropdown;
        [SerializeField] private TMP_Dropdown _resolutionDropdown;
        [SerializeField] private bool _applyImmediately;
        [SerializeField] private Button _applyButton;
        [SerializeField] private Button _resetButton;

        private readonly List<FullScreenMode> _modes = new List<FullScreenMode>();
        private readonly List<DisplayResolution> _resolutions = new List<DisplayResolution>();
        private TMP_FontAsset _originalModeCaptionFont;
        private TMP_FontAsset _originalModeItemFont;
        private bool _modeFontsCached;

        private void OnEnable()
        {
            if (_manager == null)
            {
                _manager = FindFirstObjectByType<DisplaySettingsManager>();
            }

            if (_manager == null)
            {
                Debug.LogWarning("Display Settings Menu requires a Display Settings Manager.", this);
                return;
            }

            _manager.Initialize();
            _manager.SettingsChanged += HandleSettingsChanged;
            ResolveLocalizationManager();
            CacheModeFonts();

            if (_localizationManager != null)
            {
                _localizationManager.LanguageChanged += HandleLanguageChanged;
            }

            if (_displayModeDropdown != null)
            {
                _displayModeDropdown.onValueChanged.AddListener(HandleModeSelected);
            }

            if (_resolutionDropdown != null)
            {
                _resolutionDropdown.onValueChanged.AddListener(HandleResolutionSelected);
            }

            if (_applyButton != null)
            {
                _applyButton.onClick.AddListener(ApplySelection);
            }

            if (_resetButton != null)
            {
                _resetButton.onClick.AddListener(ResetToDefaults);
            }

            Refresh(_manager.CurrentSettings);
        }

        private void OnDisable()
        {
            if (_manager != null)
            {
                _manager.SettingsChanged -= HandleSettingsChanged;
            }

            if (_localizationManager != null)
            {
                _localizationManager.LanguageChanged -= HandleLanguageChanged;
            }

            if (_displayModeDropdown != null)
            {
                _displayModeDropdown.onValueChanged.RemoveListener(HandleModeSelected);
            }

            if (_resolutionDropdown != null)
            {
                _resolutionDropdown.onValueChanged.RemoveListener(HandleResolutionSelected);
            }

            if (_applyButton != null)
            {
                _applyButton.onClick.RemoveListener(ApplySelection);
            }

            if (_resetButton != null)
            {
                _resetButton.onClick.RemoveListener(ResetToDefaults);
            }
        }

        public void Refresh()
        {
            if (_manager != null)
            {
                Refresh(_manager.CurrentSettings);
            }
        }

        public void ApplySelection()
        {
            if (_manager == null || _modes.Count == 0 || _resolutions.Count == 0)
            {
                return;
            }

            int modeIndex = Mathf.Clamp(
                _displayModeDropdown != null ? _displayModeDropdown.value : 0,
                0,
                _modes.Count - 1);
            int resolutionIndex = Mathf.Clamp(
                _resolutionDropdown != null ? _resolutionDropdown.value : 0,
                0,
                _resolutions.Count - 1);

            DisplayResolution resolution = _resolutions[resolutionIndex];
            _manager.Apply(new DisplaySettingsData(
                resolution.Width,
                resolution.Height,
                _modes[modeIndex]));
        }

        public void ResetToDefaults()
        {
            if (_manager == null)
            {
                return;
            }

            _manager.ResetToDefaults();
            Refresh(_manager.CurrentSettings);
        }

        private void Refresh(DisplaySettingsData selectedSettings)
        {
            PopulateModes(selectedSettings.FullScreenMode);
            PopulateResolutions(
                GetSelectedMode(selectedSettings.FullScreenMode),
                selectedSettings.Width,
                selectedSettings.Height);
        }

        private void PopulateModes(FullScreenMode selectedMode)
        {
            _modes.Clear();
            _modes.AddRange(_manager.AvailableModes);

            if (_displayModeDropdown == null)
            {
                return;
            }

            _displayModeDropdown.ClearOptions();
            List<string> labels = new List<string>(_modes.Count);
            for (int i = 0; i < _modes.Count; i++)
            {
                labels.Add(GetModeLabel(_modes[i]));
            }

            _displayModeDropdown.AddOptions(labels);
            int selectedIndex = _modes.IndexOf(selectedMode);
            _displayModeDropdown.SetValueWithoutNotify(Mathf.Max(0, selectedIndex));
            ApplyLocalizedModeFonts();
            _displayModeDropdown.RefreshShownValue();
        }

        private void PopulateResolutions(FullScreenMode mode, int selectedWidth, int selectedHeight)
        {
            _resolutions.Clear();
            _resolutions.AddRange(_manager.GetAvailableResolutions(mode));

            if (_resolutionDropdown == null)
            {
                return;
            }

            _resolutionDropdown.ClearOptions();
            List<string> labels = new List<string>(_resolutions.Count);
            int selectedIndex = 0;

            for (int i = 0; i < _resolutions.Count; i++)
            {
                DisplayResolution resolution = _resolutions[i];
                labels.Add(resolution.ToString());

                if (resolution.Width == selectedWidth && resolution.Height == selectedHeight)
                {
                    selectedIndex = i;
                }
            }

            _resolutionDropdown.AddOptions(labels);
            _resolutionDropdown.SetValueWithoutNotify(selectedIndex);
            _resolutionDropdown.RefreshShownValue();
        }

        private void HandleModeSelected(int index)
        {
            if (index < 0 || index >= _modes.Count)
            {
                return;
            }

            DisplaySettingsData current = _manager.CurrentSettings;
            PopulateResolutions(_modes[index], current.Width, current.Height);

            if (_applyImmediately)
            {
                ApplySelection();
            }
        }

        private void HandleResolutionSelected(int index)
        {
            if (_applyImmediately)
            {
                ApplySelection();
            }
        }

        private void HandleSettingsChanged(DisplaySettingsData settings)
        {
            Refresh(settings);
        }

        private void HandleLanguageChanged(string localeCode)
        {
            if (_manager != null)
            {
                Refresh(_manager.CurrentSettings);
            }
        }

        private FullScreenMode GetSelectedMode(FullScreenMode fallback)
        {
            if (_modes.Count == 0)
            {
                return fallback;
            }

            int index = _displayModeDropdown != null ? _displayModeDropdown.value : 0;
            return _modes[Mathf.Clamp(index, 0, _modes.Count - 1)];
        }

        private void ResolveLocalizationManager()
        {
            if (_localizationManager == null)
            {
                _localizationManager = LocalizationManager.Instance;
            }

            if (_localizationManager == null)
            {
                _localizationManager = FindFirstObjectByType<LocalizationManager>();
            }
        }

        private void CacheModeFonts()
        {
            if (_modeFontsCached || _displayModeDropdown == null)
            {
                return;
            }

            _originalModeCaptionFont = _displayModeDropdown.captionText != null
                ? _displayModeDropdown.captionText.font
                : null;
            _originalModeItemFont = _displayModeDropdown.itemText != null
                ? _displayModeDropdown.itemText.font
                : null;
            _modeFontsCached = true;
        }

        private void ApplyLocalizedModeFonts()
        {
            if (_displayModeDropdown == null)
            {
                return;
            }

            CacheModeFonts();
            TMP_FontAsset localizedFont = _localizationManager != null &&
                                          _localizationManager.Profile != null
                ? _localizationManager.Profile.GetFont(_localizationManager.CurrentLocaleCode)
                : null;

            if (_displayModeDropdown.captionText != null)
            {
                _displayModeDropdown.captionText.font = localizedFont != null
                    ? localizedFont
                    : _originalModeCaptionFont;
            }

            if (_displayModeDropdown.itemText != null)
            {
                _displayModeDropdown.itemText.font = localizedFont != null
                    ? localizedFont
                    : _originalModeItemFont;
            }
        }

        private string GetModeLabel(FullScreenMode mode)
        {
            string fallback = mode switch
            {
                FullScreenMode.Windowed => "Windowed",
                FullScreenMode.FullScreenWindow => "Borderless Fullscreen",
                FullScreenMode.ExclusiveFullScreen => "Exclusive Fullscreen",
                _ => mode.ToString()
            };

            if (_localizationManager == null)
            {
                return fallback;
            }

            string key = mode switch
            {
                FullScreenMode.Windowed => "settings.mode.windowed",
                FullScreenMode.FullScreenWindow => "settings.mode.borderless",
                FullScreenMode.ExclusiveFullScreen => "settings.mode.exclusive",
                _ => string.Empty
            };

            return string.IsNullOrEmpty(key)
                ? fallback
                : _localizationManager.GetTextOrFallback(key, fallback);
        }
    }
}
