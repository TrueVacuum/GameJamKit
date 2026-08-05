using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace GameJamKit.Audio
{
    [DisallowMultipleComponent]
    public sealed class AudioService : MonoBehaviour
    {
        [SerializeField] private AudioPlaybackProfile _profile;
        [SerializeField] private AudioSource _musicSourceA;
        [SerializeField] private AudioSource _musicSourceB;

        private readonly List<AudioSource> _soundEffectsPool = new List<AudioSource>();
        private AudioSource _activeMusicSource;
        private Coroutine _musicFadeRoutine;
        private int _nextPoolIndex;
        private bool _initialized;
        private bool _musicPaused;

        public static AudioService Instance { get; private set; }

        public AudioPlaybackProfile Profile => _profile;
        public AudioClip CurrentMusicClip => _activeMusicSource != null
            ? _activeMusicSource.clip
            : null;
        public bool IsMusicPlaying =>
            !_musicPaused && _activeMusicSource != null && _activeMusicSource.isPlaying;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("A duplicate Audio Service was ignored.", this);
                Destroy(this);
                return;
            }

            Instance = this;
            Initialize();

            if (_profile != null && _profile.PersistBetweenScenes)
            {
                DontDestroyOnLoad(gameObject);
            }
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
            if (_initialized || _profile == null)
            {
                return;
            }

            _initialized = true;
            CreateMusicSourcesIfNeeded();
            ConfigureMusicSource(_musicSourceA);
            ConfigureMusicSource(_musicSourceB);
            _activeMusicSource = _musicSourceA;

            for (int i = 0; i < _soundEffectsPool.Count; i++)
            {
                _soundEffectsPool[i].outputAudioMixerGroup =
                    _profile.SoundEffectsOutputGroup;
            }

            for (int i = _soundEffectsPool.Count; i < _profile.InitialPoolSize; i++)
            {
                _soundEffectsPool.Add(CreateSoundEffectsSource(i));
            }
        }

        public void SetProfile(AudioPlaybackProfile profile, bool reinitialize = true)
        {
            _profile = profile;
            _initialized = false;

            if (reinitialize)
            {
                Initialize();
            }
        }

        public void PlayMusic(AudioClip clip, bool loop = true, float fadeDuration = -1f)
        {
            if (clip == null || !EnsureInitialized())
            {
                return;
            }

            if (_activeMusicSource != null && _activeMusicSource.clip == clip)
            {
                if (_musicPaused)
                {
                    ResumeMusic();
                }

                if (_activeMusicSource.isPlaying)
                {
                    return;
                }
            }

            AudioSource from = NormalizeMusicSources();
            AudioSource to = from == _musicSourceA ? _musicSourceB : _musicSourceA;
            StopAndClear(to);

            to.clip = clip;
            to.loop = loop;
            to.volume = 0f;
            to.Play();
            _musicPaused = false;
            _activeMusicSource = to;

            float duration = ResolveFadeDuration(fadeDuration);
            if (from == null || !from.isPlaying || duration <= 0f)
            {
                StopAndClear(from);
                to.volume = 1f;
                return;
            }

            _musicFadeRoutine = StartCoroutine(CrossfadeMusic(from, to, duration));
        }

        public void StopMusic(float fadeDuration = -1f)
        {
            if (!EnsureInitialized())
            {
                return;
            }

            AudioSource source = NormalizeMusicSources();
            _musicPaused = false;

            if (source == null)
            {
                return;
            }

            float duration = ResolveFadeDuration(fadeDuration);
            if (duration <= 0f || !source.isPlaying)
            {
                StopAndClear(source);
                return;
            }

            _musicFadeRoutine = StartCoroutine(FadeOutMusic(source, duration));
        }

        public void PauseMusic()
        {
            if (!EnsureInitialized() || _musicPaused)
            {
                return;
            }

            _musicSourceA.Pause();
            _musicSourceB.Pause();
            _musicPaused = true;
        }

        public void ResumeMusic()
        {
            if (!EnsureInitialized() || !_musicPaused)
            {
                return;
            }

            _musicSourceA.UnPause();
            _musicSourceB.UnPause();
            _musicPaused = false;
        }

        public AudioSource PlaySfx(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            return PlaySfxInternal(clip, Vector3.zero, false, volume, pitch);
        }

        public AudioSource PlaySfxAtPoint(
            AudioClip clip,
            Vector3 worldPosition,
            float volume = 1f,
            float pitch = 1f)
        {
            return PlaySfxInternal(clip, worldPosition, true, volume, pitch);
        }

        public void StopAllSoundEffects()
        {
            for (int i = 0; i < _soundEffectsPool.Count; i++)
            {
                StopAndClear(_soundEffectsPool[i]);
            }
        }

        private AudioSource PlaySfxInternal(
            AudioClip clip,
            Vector3 position,
            bool spatial,
            float volume,
            float pitch)
        {
            if (clip == null || !EnsureInitialized())
            {
                return null;
            }

            AudioSource source = GetSoundEffectsSource();
            source.transform.position = spatial ? position : transform.position;
            source.spatialBlend = spatial ? 1f : 0f;
            source.clip = clip;
            source.loop = false;
            source.volume = Mathf.Clamp01(volume);
            source.pitch = Mathf.Clamp(pitch, -3f, 3f);
            source.Play();
            return source;
        }

        private bool EnsureInitialized()
        {
            Initialize();

            if (_initialized)
            {
                return true;
            }

            Debug.LogWarning("Audio Service requires an Audio Playback Profile.", this);
            return false;
        }

        private void CreateMusicSourcesIfNeeded()
        {
            if (_musicSourceA == null)
            {
                _musicSourceA = CreateSource("Music Source A");
            }

            if (_musicSourceB == null)
            {
                _musicSourceB = CreateSource("Music Source B");
            }
        }

        private AudioSource CreateSoundEffectsSource(int index)
        {
            AudioSource source = CreateSource($"SFX Source {index + 1}");
            source.outputAudioMixerGroup = _profile.SoundEffectsOutputGroup;
            source.loop = false;
            return source;
        }

        private AudioSource CreateSource(string objectName)
        {
            GameObject sourceObject = new GameObject(objectName);
            sourceObject.transform.SetParent(transform, false);
            AudioSource source = sourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            return source;
        }

        private void ConfigureMusicSource(AudioSource source)
        {
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.outputAudioMixerGroup = _profile.MusicOutputGroup;
        }

        private AudioSource GetSoundEffectsSource()
        {
            for (int i = 0; i < _soundEffectsPool.Count; i++)
            {
                if (!_soundEffectsPool[i].isPlaying)
                {
                    return _soundEffectsPool[i];
                }
            }

            if (_soundEffectsPool.Count < _profile.MaximumPoolSize)
            {
                AudioSource created = CreateSoundEffectsSource(_soundEffectsPool.Count);
                _soundEffectsPool.Add(created);
                return created;
            }

            AudioSource reused = _soundEffectsPool[_nextPoolIndex % _soundEffectsPool.Count];
            _nextPoolIndex = (_nextPoolIndex + 1) % _soundEffectsPool.Count;
            reused.Stop();
            return reused;
        }

        private AudioSource NormalizeMusicSources()
        {
            CancelMusicFade();

            bool aActive = _musicSourceA != null && _musicSourceA.clip != null;
            bool bActive = _musicSourceB != null && _musicSourceB.clip != null;

            if (aActive && bActive)
            {
                AudioSource dominant = _musicSourceA.volume >= _musicSourceB.volume
                    ? _musicSourceA
                    : _musicSourceB;
                AudioSource other = dominant == _musicSourceA ? _musicSourceB : _musicSourceA;
                StopAndClear(other);
                _activeMusicSource = dominant;
                return dominant;
            }

            AudioSource active = aActive ? _musicSourceA : bActive ? _musicSourceB : null;
            _activeMusicSource = active ?? _musicSourceA;
            return active;
        }

        private IEnumerator CrossfadeMusic(AudioSource from, AudioSource to, float duration)
        {
            double startTime = Time.realtimeSinceStartupAsDouble;
            float fromStartVolume = from.volume;

            while (true)
            {
                float elapsed = (float)(Time.realtimeSinceStartupAsDouble - startTime);
                float progress = Mathf.Clamp01(elapsed / duration);
                from.volume = Mathf.Lerp(fromStartVolume, 0f, progress);
                to.volume = Mathf.Lerp(0f, 1f, progress);

                if (progress >= 1f)
                {
                    break;
                }

                yield return null;
            }

            StopAndClear(from);
            to.volume = 1f;
            _activeMusicSource = to;
            _musicFadeRoutine = null;
        }

        private IEnumerator FadeOutMusic(AudioSource source, float duration)
        {
            double startTime = Time.realtimeSinceStartupAsDouble;
            float startVolume = source.volume;

            while (true)
            {
                float elapsed = (float)(Time.realtimeSinceStartupAsDouble - startTime);
                float progress = Mathf.Clamp01(elapsed / duration);
                source.volume = Mathf.Lerp(startVolume, 0f, progress);

                if (progress >= 1f)
                {
                    break;
                }

                yield return null;
            }

            StopAndClear(source);
            _musicFadeRoutine = null;
        }

        private float ResolveFadeDuration(float requestedDuration)
        {
            return requestedDuration >= 0f
                ? requestedDuration
                : _profile.DefaultMusicFadeDuration;
        }

        private void CancelMusicFade()
        {
            if (_musicFadeRoutine == null)
            {
                return;
            }

            StopCoroutine(_musicFadeRoutine);
            _musicFadeRoutine = null;
        }

        private static void StopAndClear(AudioSource source)
        {
            if (source == null)
            {
                return;
            }

            source.Stop();
            source.clip = null;
            source.volume = 1f;
        }
    }
}
