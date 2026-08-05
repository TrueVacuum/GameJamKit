using System;
using UnityEngine;

namespace GameJamKit.Audio
{
    [Serializable]
    public struct AudioSettingsData : IEquatable<AudioSettingsData>
    {
        [SerializeField] private float _masterVolume;
        [SerializeField] private float _musicVolume;
        [SerializeField] private float _soundEffectsVolume;
        [SerializeField] private bool _muted;

        public float MasterVolume => _masterVolume;
        public float MusicVolume => _musicVolume;
        public float SoundEffectsVolume => _soundEffectsVolume;
        public bool Muted => _muted;

        public AudioSettingsData(
            float masterVolume,
            float musicVolume,
            float soundEffectsVolume,
            bool muted)
        {
            _masterVolume = masterVolume;
            _musicVolume = musicVolume;
            _soundEffectsVolume = soundEffectsVolume;
            _muted = muted;
        }

        public AudioSettingsData Clamp()
        {
            return new AudioSettingsData(
                Mathf.Clamp01(_masterVolume),
                Mathf.Clamp01(_musicVolume),
                Mathf.Clamp01(_soundEffectsVolume),
                _muted);
        }

        public bool Equals(AudioSettingsData other)
        {
            return Mathf.Approximately(_masterVolume, other._masterVolume) &&
                   Mathf.Approximately(_musicVolume, other._musicVolume) &&
                   Mathf.Approximately(_soundEffectsVolume, other._soundEffectsVolume) &&
                   _muted == other._muted;
        }

        public override bool Equals(object obj)
        {
            return obj is AudioSettingsData other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                _masterVolume,
                _musicVolume,
                _soundEffectsVolume,
                _muted);
        }
    }
}
