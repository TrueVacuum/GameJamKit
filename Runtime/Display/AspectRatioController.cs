using System;
using UnityEngine;

namespace GameJamKit.Display
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class AspectRatioController : MonoBehaviour
    {
        [SerializeField] private DisplaySettingsManager _displaySettingsManager;
        [Tooltip("Optional. When assigned, this profile is used instead of the manager's profile.")]
        [SerializeField] private DisplaySettingsProfile _profileOverride;
        [SerializeField] private Camera _targetCamera;

        private int _lastScreenWidth = -1;
        private int _lastScreenHeight = -1;
        private Vector2Int _lastTargetAspect;
        private bool _lastPreserveAspect;

        public event Action<Rect> ViewportChanged;

        public DisplaySettingsProfile ActiveProfile => _profileOverride != null
            ? _profileOverride
            : _displaySettingsManager != null
                ? _displaySettingsManager.Profile
                : null;

        public Camera TargetCamera => _targetCamera;
        public Rect CurrentViewport { get; private set; } = new Rect(0f, 0f, 1f, 1f);

        private void OnEnable()
        {
            ResolveReferences();
            RefreshViewport(true);
        }

        private void LateUpdate()
        {
            DisplaySettingsProfile profile = ActiveProfile;
            Vector2Int targetAspect = profile != null
                ? profile.TargetAspectRatio
                : Vector2Int.zero;
            bool preserveAspect = profile != null && profile.PreserveContentAspectRatio;

            if (_lastScreenWidth != Screen.width ||
                _lastScreenHeight != Screen.height ||
                _lastTargetAspect != targetAspect ||
                _lastPreserveAspect != preserveAspect)
            {
                RefreshViewport();
            }
        }

        private void OnDisable()
        {
            ApplyViewport(new Rect(0f, 0f, 1f, 1f), true);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            ResolveReferences();
            RefreshViewport(true);
        }
#endif

        public void SetDisplaySettingsManager(DisplaySettingsManager manager)
        {
            _displaySettingsManager = manager;
            RefreshViewport(true);
        }

        public void SetProfileOverride(DisplaySettingsProfile profile)
        {
            _profileOverride = profile;
            RefreshViewport(true);
        }

        public void SetTargetCamera(Camera targetCamera)
        {
            if (_targetCamera != null && _targetCamera != targetCamera)
            {
                _targetCamera.rect = new Rect(0f, 0f, 1f, 1f);
            }

            _targetCamera = targetCamera;
            RefreshViewport(true);
        }

        public void RefreshViewport(bool forceNotification = false)
        {
            ResolveReferences();

            DisplaySettingsProfile profile = ActiveProfile;
            Rect viewport = new Rect(0f, 0f, 1f, 1f);

            if (profile != null && profile.PreserveContentAspectRatio)
            {
                Vector2Int aspect = profile.TargetAspectRatio;
                viewport = AspectRatioUtility.CalculateViewport(
                    Screen.width,
                    Screen.height,
                    aspect.x,
                    aspect.y);
            }

            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
            _lastTargetAspect = profile != null ? profile.TargetAspectRatio : Vector2Int.zero;
            _lastPreserveAspect = profile != null && profile.PreserveContentAspectRatio;

            ApplyViewport(viewport, forceNotification);
        }

        private void ApplyViewport(Rect viewport, bool forceNotification)
        {
            bool changed = CurrentViewport != viewport;
            CurrentViewport = viewport;

            if (_targetCamera != null)
            {
                _targetCamera.rect = viewport;
            }

            if (changed || forceNotification)
            {
                ViewportChanged?.Invoke(viewport);
            }
        }

        private void ResolveReferences()
        {
            if (_displaySettingsManager == null)
            {
                _displaySettingsManager = FindFirstObjectByType<DisplaySettingsManager>();
            }

            if (_targetCamera == null)
            {
                _targetCamera = GetComponent<Camera>();
            }

            if (_targetCamera == null)
            {
                _targetCamera = Camera.main;
            }
        }
    }
}
