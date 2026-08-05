using System;
using UnityEngine;
using UnityEngine.Audio;

namespace GameJamKit.Audio
{
    [DisallowMultipleComponent]
    public sealed class AudioSettingsManager : MonoBehaviour
    {
        [SerializeField] private AudioSettingsProfile _profile;
        [SerializeField] private bool _loadSavedSettingsOnStart = true;
        [SerializeField] private bool _saveAfterApply = true;

        private bool _initialized;

        public event Action<AudioSettingsData> SettingsChanged;

        public AudioSettingsProfile Profile => _profile;
        public AudioSettingsData CurrentSettings { get; private set; }

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
            AudioSettingsData initial = _loadSavedSettingsOnStart && TryLoad(out var saved)
                ? saved
                : _profile.DefaultSettings;
            Apply(initial, false);
        }

        public void SetProfile(AudioSettingsProfile profile, bool applyDefaults = true)
        {
            _profile = profile;
            _initialized = _profile != null;

            if (applyDefaults && _profile != null)
            {
                Apply(_profile.DefaultSettings);
            }
        }

        public bool Apply(AudioSettingsData requestedSettings)
        {
            return Apply(requestedSettings, true);
        }

        public bool Apply(AudioSettingsData requestedSettings, bool save)
        {
            if (_profile == null)
            {
                Debug.LogWarning("Cannot apply audio settings without a profile.", this);
                return false;
            }

            AudioMixer mixer = _profile.AudioMixer;
            if (mixer == null)
            {
                Debug.LogWarning("Cannot apply audio settings without an Audio Mixer.", this);
                return false;
            }

            AudioSettingsData applied = requestedSettings.Clamp();
            bool succeeded = true;
            succeeded &= SetMixerVolume(
                mixer,
                _profile.MasterVolumeParameter,
                applied.Muted ? 0f : applied.MasterVolume);
            succeeded &= SetMixerVolume(
                mixer,
                _profile.MusicVolumeParameter,
                applied.MusicVolume);
            succeeded &= SetMixerVolume(
                mixer,
                _profile.SoundEffectsVolumeParameter,
                applied.SoundEffectsVolume);

            CurrentSettings = applied;

            if (save && _saveAfterApply)
            {
                Save(applied);
            }

            SettingsChanged?.Invoke(applied);
            return succeeded;
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

        public void Save(AudioSettingsData settings)
        {
            if (_profile == null)
            {
                return;
            }

            string key = _profile.PlayerPrefsKey;
            PlayerPrefs.SetFloat($"{key}.Master", settings.MasterVolume);
            PlayerPrefs.SetFloat($"{key}.Music", settings.MusicVolume);
            PlayerPrefs.SetFloat($"{key}.Sfx", settings.SoundEffectsVolume);
            PlayerPrefs.SetInt($"{key}.Muted", settings.Muted ? 1 : 0);
            PlayerPrefs.Save();
        }

        public bool TryLoad(out AudioSettingsData settings)
        {
            settings = default;
            if (_profile == null)
            {
                return false;
            }

            string key = _profile.PlayerPrefsKey;
            if (!PlayerPrefs.HasKey($"{key}.Master") ||
                !PlayerPrefs.HasKey($"{key}.Music") ||
                !PlayerPrefs.HasKey($"{key}.Sfx") ||
                !PlayerPrefs.HasKey($"{key}.Muted"))
            {
                return false;
            }

            settings = new AudioSettingsData(
                PlayerPrefs.GetFloat($"{key}.Master"),
                PlayerPrefs.GetFloat($"{key}.Music"),
                PlayerPrefs.GetFloat($"{key}.Sfx"),
                PlayerPrefs.GetInt($"{key}.Muted") != 0);
            return true;
        }

        public void DeleteSavedSettings()
        {
            if (_profile == null)
            {
                return;
            }

            string key = _profile.PlayerPrefsKey;
            PlayerPrefs.DeleteKey($"{key}.Master");
            PlayerPrefs.DeleteKey($"{key}.Music");
            PlayerPrefs.DeleteKey($"{key}.Sfx");
            PlayerPrefs.DeleteKey($"{key}.Muted");
            PlayerPrefs.Save();
        }

        private bool SetMixerVolume(AudioMixer mixer, string parameterName, float linearVolume)
        {
            if (string.IsNullOrWhiteSpace(parameterName))
            {
                return true;
            }

            float decibels = AudioVolumeUtility.LinearToDecibels(
                linearVolume,
                _profile.MinimumDecibels);
            bool succeeded = mixer.SetFloat(parameterName, decibels);

            if (!succeeded)
            {
                Debug.LogWarning(
                    $"Audio Mixer parameter '{parameterName}' is not exposed or does not exist.",
                    this);
            }

            return succeeded;
        }
    }
}
