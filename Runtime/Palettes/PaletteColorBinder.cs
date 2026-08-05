using UnityEngine;

namespace GameJamKit.Palettes
{
    [ExecuteAlways]
    public abstract class PaletteColorBinder : MonoBehaviour
    {
        [SerializeField] private ColorPaletteController _controller;
        [SerializeField] private string _colorKey = string.Empty;
        [SerializeField] private Color _fallbackColor = Color.white;
        [SerializeField] private bool _overrideAlpha;
        [SerializeField, Range(0f, 1f)] private float _alpha = 1f;
        [SerializeField] private bool _applyOnEnable = true;

        private ColorPaletteController _subscribedController;

        public ColorPaletteController Controller => _controller;
        public string ColorKey => _colorKey;
        public Color FallbackColor => _fallbackColor;
        public bool OverrideAlpha => _overrideAlpha;
        public float Alpha => _alpha;

        protected virtual void OnEnable()
        {
            ResolveController();
            SubscribeToController();

            if (_applyOnEnable)
            {
                RefreshColor();
            }
        }

        protected virtual void OnDisable()
        {
            UnsubscribeFromController();
        }

        protected virtual void Reset()
        {
            ResolveController();
            SubscribeToController();
            RefreshColor();
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            ResolveController();
            SubscribeToController();
            RefreshColor();
        }
#endif

        public void SetController(ColorPaletteController controller)
        {
            if (_controller == controller)
            {
                return;
            }

            UnsubscribeFromController();
            _controller = controller;
            SubscribeToController();
            RefreshColor();
        }

        public void SetColorKey(string colorKey)
        {
            _colorKey = colorKey;
            RefreshColor();
        }

        public void SetFallbackColor(Color fallbackColor)
        {
            _fallbackColor = fallbackColor;
            RefreshColor();
        }

        public void SetAlphaOverride(bool enabled, float alpha)
        {
            _overrideAlpha = enabled;
            _alpha = Mathf.Clamp01(alpha);
            RefreshColor();
        }

        public void RefreshColor()
        {
            ResolveController();

            Color color = _controller != null
                ? _controller.GetColor(_colorKey, _fallbackColor)
                : _fallbackColor;

            if (_overrideAlpha)
            {
                color.a = _alpha;
            }

            ApplyColor(color);
        }

        protected abstract void ApplyColor(Color color);

        private void ResolveController()
        {
            if (_controller != null)
            {
                return;
            }

            _controller = GetComponentInParent<ColorPaletteController>(true);
            if (_controller == null)
            {
                _controller = FindFirstObjectByType<ColorPaletteController>(
                    FindObjectsInactive.Include);
            }
        }

        private void SubscribeToController()
        {
            if (_subscribedController == _controller)
            {
                return;
            }

            UnsubscribeFromController();
            _subscribedController = _controller;

            if (_subscribedController != null)
            {
                _subscribedController.PaletteChanged += RefreshColor;
            }
        }

        private void UnsubscribeFromController()
        {
            if (_subscribedController != null)
            {
                _subscribedController.PaletteChanged -= RefreshColor;
            }

            _subscribedController = null;
        }
    }
}
