using UnityEngine;
using UnityEngine.UI;

namespace GameJamKit.Audio.UGUI
{
    [DisallowMultipleComponent]
    public sealed class AudioSettingsMenu : MonoBehaviour
    {
        [SerializeField] private AudioSettingsManager _manager;
        [SerializeField] private Slider _masterVolumeSlider;
        [SerializeField] private Slider _musicVolumeSlider;
        [SerializeField] private Slider _soundEffectsVolumeSlider;
        [SerializeField] private Toggle _muteToggle;
        [SerializeField] private bool _applyImmediately;
        [SerializeField] private Button _applyButton;
        [SerializeField] private Button _resetButton;

        private void OnEnable()
        {
            if (_manager == null)
            {
                _manager = FindFirstObjectByType<AudioSettingsManager>();
            }

            if (_manager == null)
            {
                Debug.LogWarning("Audio Settings Menu requires an Audio Settings Manager.", this);
                return;
            }

            ConfigureSlider(_masterVolumeSlider);
            ConfigureSlider(_musicVolumeSlider);
            ConfigureSlider(_soundEffectsVolumeSlider);

            _manager.Initialize();
            _manager.SettingsChanged += HandleSettingsChanged;

            AddValueChangedListeners();

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

            RemoveValueChangedListeners();

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
            if (_manager == null)
            {
                return;
            }

            AudioSettingsData current = _manager.CurrentSettings;
            _manager.Apply(new AudioSettingsData(
                GetSliderValue(_masterVolumeSlider, current.MasterVolume),
                GetSliderValue(_musicVolumeSlider, current.MusicVolume),
                GetSliderValue(_soundEffectsVolumeSlider, current.SoundEffectsVolume),
                _muteToggle != null ? _muteToggle.isOn : current.Muted));
        }

        public void ResetToDefaults()
        {
            if (_manager == null)
            {
                return;
            }

            _manager.ResetToDefaults();
        }

        private void Refresh(AudioSettingsData settings)
        {
            SetSliderValue(_masterVolumeSlider, settings.MasterVolume);
            SetSliderValue(_musicVolumeSlider, settings.MusicVolume);
            SetSliderValue(_soundEffectsVolumeSlider, settings.SoundEffectsVolume);

            if (_muteToggle != null)
            {
                _muteToggle.SetIsOnWithoutNotify(settings.Muted);
            }
        }

        private void HandleSettingsChanged(AudioSettingsData settings)
        {
            Refresh(settings);
        }

        private void AddValueChangedListeners()
        {
            if (_masterVolumeSlider != null)
            {
                _masterVolumeSlider.onValueChanged.AddListener(HandleValueChanged);
            }

            if (_musicVolumeSlider != null)
            {
                _musicVolumeSlider.onValueChanged.AddListener(HandleValueChanged);
            }

            if (_soundEffectsVolumeSlider != null)
            {
                _soundEffectsVolumeSlider.onValueChanged.AddListener(HandleValueChanged);
            }

            if (_muteToggle != null)
            {
                _muteToggle.onValueChanged.AddListener(HandleValueChanged);
            }
        }

        private void RemoveValueChangedListeners()
        {
            if (_masterVolumeSlider != null)
            {
                _masterVolumeSlider.onValueChanged.RemoveListener(HandleValueChanged);
            }

            if (_musicVolumeSlider != null)
            {
                _musicVolumeSlider.onValueChanged.RemoveListener(HandleValueChanged);
            }

            if (_soundEffectsVolumeSlider != null)
            {
                _soundEffectsVolumeSlider.onValueChanged.RemoveListener(HandleValueChanged);
            }

            if (_muteToggle != null)
            {
                _muteToggle.onValueChanged.RemoveListener(HandleValueChanged);
            }
        }

        private void HandleValueChanged(float value)
        {
            ApplyIfImmediate();
        }

        private void HandleValueChanged(bool value)
        {
            ApplyIfImmediate();
        }

        private void ApplyIfImmediate()
        {
            if (_applyImmediately)
            {
                ApplySelection();
            }
        }

        private static void ConfigureSlider(Slider slider)
        {
            if (slider == null)
            {
                return;
            }

            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
        }

        private static float GetSliderValue(Slider slider, float fallback)
        {
            return slider != null ? slider.value : fallback;
        }

        private static void SetSliderValue(Slider slider, float value)
        {
            if (slider != null)
            {
                slider.SetValueWithoutNotify(value);
            }
        }
    }
}
