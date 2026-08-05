using UnityEngine;
using UnityEngine.Audio;

namespace GameJamKit.Audio
{
    [CreateAssetMenu(
        fileName = "AudioPlaybackProfile",
        menuName = "Game Jam Kit/Audio Playback Profile")]
    public sealed class AudioPlaybackProfile : ScriptableObject
    {
        [Header("Mixer Routing")]
        [SerializeField] private AudioMixerGroup _musicOutputGroup;
        [SerializeField] private AudioMixerGroup _soundEffectsOutputGroup;

        [Header("Music")]
        [SerializeField, Min(0f)] private float _defaultMusicFadeDuration = 0.5f;

        [Header("Sound Effects Pool")]
        [SerializeField, Min(1)] private int _initialPoolSize = 8;
        [SerializeField, Min(1)] private int _maximumPoolSize = 24;

        [Header("Lifetime")]
        [SerializeField] private bool _persistBetweenScenes = true;

        public AudioMixerGroup MusicOutputGroup => _musicOutputGroup;
        public AudioMixerGroup SoundEffectsOutputGroup => _soundEffectsOutputGroup;
        public float DefaultMusicFadeDuration => _defaultMusicFadeDuration;
        public int InitialPoolSize => Mathf.Max(1, _initialPoolSize);
        public int MaximumPoolSize => Mathf.Max(InitialPoolSize, _maximumPoolSize);
        public bool PersistBetweenScenes => _persistBetweenScenes;

#if UNITY_EDITOR
        private void OnValidate()
        {
            _defaultMusicFadeDuration = Mathf.Max(0f, _defaultMusicFadeDuration);
            _initialPoolSize = Mathf.Max(1, _initialPoolSize);
            _maximumPoolSize = Mathf.Max(_initialPoolSize, _maximumPoolSize);
        }
#endif
    }
}
