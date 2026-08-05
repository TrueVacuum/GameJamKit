using System.Collections.Generic;
using UnityEngine;

namespace GameJamKit.Display
{
    [CreateAssetMenu(
        fileName = "DisplaySettingsProfile",
        menuName = "Game Jam Kit/Display Settings Profile")]
    public sealed class DisplaySettingsProfile : ScriptableObject
    {
        [Header("Defaults")]
        [SerializeField] private FullScreenMode _defaultMode = FullScreenMode.Windowed;
        [SerializeField] private Vector2Int _defaultWindowSize = new Vector2Int(1280, 720);

        [Header("Allowed Modes")]
        [SerializeField] private bool _allowWindowed = true;
        [SerializeField] private bool _allowBorderlessFullscreen = true;
        [SerializeField] private bool _allowExclusiveFullscreen;

        [Header("Aspect Ratio")]
        [SerializeField] private bool _preserveContentAspectRatio = true;
        [SerializeField] private Vector2Int _targetAspectRatio = new Vector2Int(16, 9);

        [Header("Resolution Filtering")]
        [SerializeField] private Vector2Int _minimumResolution = new Vector2Int(960, 540);
        [Tooltip("Hide driver-provided virtual resolutions above the current desktop size.")]
        [SerializeField] private bool _limitToDesktopResolution = true;
        [SerializeField] private bool _filterByAspectRatio;
        [SerializeField, Min(0f)] private float _aspectRatioTolerance = 0.02f;

        [Header("Windowed Presets")]
        [SerializeField] private List<Vector2Int> _windowedPresets = new List<Vector2Int>
        {
            new Vector2Int(960, 540),
            new Vector2Int(1280, 720),
            new Vector2Int(1600, 900)
        };

        [Header("Persistence")]
        [SerializeField] private string _playerPrefsKey = "GameJamKit.DisplaySettings";

        public FullScreenMode DefaultMode => _defaultMode;
        public Vector2Int DefaultWindowSize => _defaultWindowSize;
        public bool PreserveContentAspectRatio => _preserveContentAspectRatio;
        public Vector2Int TargetAspectRatio => _targetAspectRatio;
        public Vector2Int MinimumResolution => _minimumResolution;
        public bool LimitToDesktopResolution => _limitToDesktopResolution;
        public bool FilterByAspectRatio => _filterByAspectRatio;
        public float AspectRatioTolerance => _aspectRatioTolerance;
        public IReadOnlyList<Vector2Int> WindowedPresets => _windowedPresets;
        public string PlayerPrefsKey => _playerPrefsKey;

        public DisplaySettingsData DefaultSettings => new DisplaySettingsData(
            _defaultWindowSize.x,
            _defaultWindowSize.y,
            GetAllowedModeOrFallback(_defaultMode));

        public bool IsModeAllowed(FullScreenMode mode)
        {
            return mode switch
            {
                FullScreenMode.Windowed => _allowWindowed,
                FullScreenMode.FullScreenWindow => _allowBorderlessFullscreen,
                FullScreenMode.ExclusiveFullScreen => _allowExclusiveFullscreen,
                _ => false
            };
        }

        public FullScreenMode GetAllowedModeOrFallback(FullScreenMode requestedMode)
        {
            if (IsModeAllowed(requestedMode))
            {
                return requestedMode;
            }

            if (_allowWindowed)
            {
                return FullScreenMode.Windowed;
            }

            if (_allowBorderlessFullscreen)
            {
                return FullScreenMode.FullScreenWindow;
            }

            return FullScreenMode.ExclusiveFullScreen;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _defaultWindowSize.x = Mathf.Max(1, _defaultWindowSize.x);
            _defaultWindowSize.y = Mathf.Max(1, _defaultWindowSize.y);
            _minimumResolution.x = Mathf.Max(1, _minimumResolution.x);
            _minimumResolution.y = Mathf.Max(1, _minimumResolution.y);
            _targetAspectRatio.x = Mathf.Max(1, _targetAspectRatio.x);
            _targetAspectRatio.y = Mathf.Max(1, _targetAspectRatio.y);
            _aspectRatioTolerance = Mathf.Max(0f, _aspectRatioTolerance);

            if (!_allowWindowed && !_allowBorderlessFullscreen && !_allowExclusiveFullscreen)
            {
                _allowWindowed = true;
            }

            _defaultMode = GetAllowedModeOrFallback(_defaultMode);
            _defaultWindowSize.x = Mathf.Max(_minimumResolution.x, _defaultWindowSize.x);
            _defaultWindowSize.y = Mathf.Max(_minimumResolution.y, _defaultWindowSize.y);

            if (string.IsNullOrWhiteSpace(_playerPrefsKey))
            {
                _playerPrefsKey = "GameJamKit.DisplaySettings";
            }
        }
#endif
    }
}
