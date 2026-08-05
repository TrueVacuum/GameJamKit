using UnityEngine;
using UnityEngine.Audio;

namespace GameJamKit.Audio
{
    [CreateAssetMenu(
        fileName = "AudioSettingsProfile",
        menuName = "Game Jam Kit/Audio Settings Profile")]
    public sealed class AudioSettingsProfile : ScriptableObject
    {
        [Header("Mixer")]
        [SerializeField] private AudioMixer _audioMixer;
        [SerializeField] private string _masterVolumeParameter = "MasterVolume";
        [SerializeField] private string _musicVolumeParameter = "MusicVolume";
        [SerializeField] private string _soundEffectsVolumeParameter = "SfxVolume";
        [SerializeField] private float _minimumDecibels = -80f;

        [Header("Defaults")]
        [SerializeField, Range(0f, 1f)] private float _defaultMasterVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float _defaultMusicVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float _defaultSoundEffectsVolume = 1f;
        [SerializeField] private bool _defaultMuted;

        [Header("Persistence")]
        [SerializeField] private string _playerPrefsKey = "GameJamKit.AudioSettings";

        public AudioMixer AudioMixer => _audioMixer;
        public string MasterVolumeParameter => _masterVolumeParameter;
        public string MusicVolumeParameter => _musicVolumeParameter;
        public string SoundEffectsVolumeParameter => _soundEffectsVolumeParameter;
        public float MinimumDecibels => _minimumDecibels;
        public string PlayerPrefsKey => _playerPrefsKey;

        public AudioSettingsData DefaultSettings => new AudioSettingsData(
            _defaultMasterVolume,
            _defaultMusicVolume,
            _defaultSoundEffectsVolume,
            _defaultMuted);

#if UNITY_EDITOR
        private void OnValidate()
        {
            _minimumDecibels = Mathf.Min(0f, _minimumDecibels);
            _defaultMasterVolume = Mathf.Clamp01(_defaultMasterVolume);
            _defaultMusicVolume = Mathf.Clamp01(_defaultMusicVolume);
            _defaultSoundEffectsVolume = Mathf.Clamp01(_defaultSoundEffectsVolume);

            if (string.IsNullOrWhiteSpace(_playerPrefsKey))
            {
                _playerPrefsKey = "GameJamKit.AudioSettings";
            }
        }
#endif
    }
}
