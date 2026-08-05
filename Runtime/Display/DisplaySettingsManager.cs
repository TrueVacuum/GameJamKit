using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameJamKit.Display
{
    [DisallowMultipleComponent]
    public sealed class DisplaySettingsManager : MonoBehaviour
    {
        [SerializeField] private DisplaySettingsProfile _profile;
        [SerializeField] private bool _loadSavedSettingsOnStart = true;
        [SerializeField] private bool _saveAfterApply = true;

        private readonly List<FullScreenMode> _availableModes = new List<FullScreenMode>();
        private bool _initialized;

        public event Action<DisplaySettingsData> SettingsChanged;

        public DisplaySettingsProfile Profile => _profile;
        public DisplaySettingsData CurrentSettings { get; private set; }
        public IReadOnlyList<FullScreenMode> AvailableModes => _availableModes;

        private void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (_initialized || _profile == null)
            {
                return;
            }

            _initialized = true;
            RebuildAvailableModes();

            DisplaySettingsData initial = _loadSavedSettingsOnStart && TryLoad(out var saved)
                ? saved
                : _profile.DefaultSettings;

            Apply(initial, false);
        }

        public void SetProfile(DisplaySettingsProfile profile, bool applyDefaults = true)
        {
            _profile = profile;
            _initialized = false;

            if (_profile == null)
            {
                _availableModes.Clear();
                return;
            }

            RebuildAvailableModes();
            _initialized = true;

            if (applyDefaults)
            {
                Apply(_profile.DefaultSettings);
            }
        }

        public List<DisplayResolution> GetAvailableResolutions(FullScreenMode mode)
        {
            if (_profile == null)
            {
                return new List<DisplayResolution>();
            }

            return DisplayResolutionUtility.BuildResolutionList(
                _profile,
                Screen.resolutions,
                NormalizeMode(mode),
                UnityEngine.Display.main.systemWidth,
                UnityEngine.Display.main.systemHeight);
        }

        public bool Apply(DisplaySettingsData requestedSettings, bool save)
        {
            if (_profile == null)
            {
                Debug.LogWarning("Cannot apply display settings without a profile.", this);
                return false;
            }

            FullScreenMode mode = NormalizeMode(requestedSettings.FullScreenMode);
            List<DisplayResolution> available = GetAvailableResolutions(mode);
            DisplayResolution resolution = DisplayResolutionUtility.FindClosest(
                available,
                requestedSettings.Width,
                requestedSettings.Height);

            DisplaySettingsData applied = new DisplaySettingsData(
                resolution.Width,
                resolution.Height,
                mode);

            Screen.SetResolution(applied.Width, applied.Height, applied.FullScreenMode);
            CurrentSettings = applied;

            if (save && _saveAfterApply)
            {
                Save(applied);
            }

            SettingsChanged?.Invoke(applied);
            return true;
        }

        public bool Apply(DisplaySettingsData requestedSettings)
        {
            return Apply(requestedSettings, true);
        }

        public void ResetToDefaults()
        {
            if (_profile == null)
            {
                return;
            }

            DeleteSavedSettings();
            Apply(_profile.DefaultSettings, false);
        }

        public void Save(DisplaySettingsData settings)
        {
            if (_profile == null)
            {
                return;
            }

            string key = _profile.PlayerPrefsKey;
            PlayerPrefs.SetInt($"{key}.Width", settings.Width);
            PlayerPrefs.SetInt($"{key}.Height", settings.Height);
            PlayerPrefs.SetInt($"{key}.Mode", (int)settings.FullScreenMode);
            PlayerPrefs.Save();
        }

        public bool TryLoad(out DisplaySettingsData settings)
        {
            settings = default;
            if (_profile == null)
            {
                return false;
            }

            string key = _profile.PlayerPrefsKey;
            if (!PlayerPrefs.HasKey($"{key}.Width") ||
                !PlayerPrefs.HasKey($"{key}.Height") ||
                !PlayerPrefs.HasKey($"{key}.Mode"))
            {
                return false;
            }

            settings = new DisplaySettingsData(
                PlayerPrefs.GetInt($"{key}.Width"),
                PlayerPrefs.GetInt($"{key}.Height"),
                (FullScreenMode)PlayerPrefs.GetInt($"{key}.Mode"));
            return true;
        }

        public void DeleteSavedSettings()
        {
            if (_profile == null)
            {
                return;
            }

            string key = _profile.PlayerPrefsKey;
            PlayerPrefs.DeleteKey($"{key}.Width");
            PlayerPrefs.DeleteKey($"{key}.Height");
            PlayerPrefs.DeleteKey($"{key}.Mode");
            PlayerPrefs.Save();
        }

        private void RebuildAvailableModes()
        {
            _availableModes.Clear();
            AddModeIfSupported(FullScreenMode.Windowed);
            AddModeIfSupported(FullScreenMode.FullScreenWindow);
            AddModeIfSupported(FullScreenMode.ExclusiveFullScreen);

            if (_availableModes.Count == 0)
            {
                _availableModes.Add(FullScreenMode.FullScreenWindow);
            }
        }

        private void AddModeIfSupported(FullScreenMode mode)
        {
            if (_profile.IsModeAllowed(mode) && IsModeSupportedOnCurrentPlatform(mode))
            {
                _availableModes.Add(mode);
            }
        }

        private FullScreenMode NormalizeMode(FullScreenMode requestedMode)
        {
            if (_availableModes.Count == 0)
            {
                RebuildAvailableModes();
            }

            if (_availableModes.Contains(requestedMode))
            {
                return requestedMode;
            }

            FullScreenMode profileFallback = _profile.GetAllowedModeOrFallback(requestedMode);
            return _availableModes.Contains(profileFallback)
                ? profileFallback
                : _availableModes[0];
        }

        private static bool IsModeSupportedOnCurrentPlatform(FullScreenMode mode)
        {
            if (mode == FullScreenMode.ExclusiveFullScreen)
            {
                return Application.platform == RuntimePlatform.WindowsPlayer ||
                       Application.platform == RuntimePlatform.WindowsEditor;
            }

            if (mode == FullScreenMode.Windowed)
            {
                return !Application.isMobilePlatform &&
                       Application.platform != RuntimePlatform.WebGLPlayer;
            }

            return mode == FullScreenMode.FullScreenWindow;
        }
    }
}
