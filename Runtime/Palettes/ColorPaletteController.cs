using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameJamKit.Palettes
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("Game Jam Kit/Palettes/Color Palette Controller")]
    public sealed class ColorPaletteController : MonoBehaviour
    {
        [SerializeField] private ColorPalette _activePalette;

        private readonly Dictionary<string, Color> _overrides =
            new Dictionary<string, Color>(StringComparer.Ordinal);

        private ColorPalette _subscribedPalette;

        public event Action PaletteChanged;

        public ColorPalette ActivePalette => _activePalette;
        public int OverrideCount => _overrides.Count;

        private void OnEnable()
        {
            SubscribeToActivePalette();
            NotifyPaletteChanged();
        }

        private void OnDisable()
        {
            UnsubscribeFromPalette();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            SubscribeToActivePalette();
            NotifyPaletteChanged();
        }
#endif

        public void SetPalette(ColorPalette palette)
        {
            if (_activePalette == palette)
            {
                return;
            }

            _activePalette = palette;
            SubscribeToActivePalette();
            NotifyPaletteChanged();
        }

        public bool TryGetColor(string key, out Color color)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                color = default;
                return false;
            }

            if (_overrides.TryGetValue(key, out color))
            {
                return true;
            }

            if (_activePalette != null)
            {
                return _activePalette.TryGetColor(key, out color);
            }

            color = default;
            return false;
        }

        public Color GetColor(string key, Color fallback)
        {
            return TryGetColor(key, out Color color) ? color : fallback;
        }

        public void SetOverride(string key, Color color)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Palette override keys cannot be empty.", nameof(key));
            }

            _overrides[key] = color;
            NotifyPaletteChanged();
        }

        public bool RemoveOverride(string key)
        {
            if (!_overrides.Remove(key))
            {
                return false;
            }

            NotifyPaletteChanged();
            return true;
        }

        public void ClearOverrides()
        {
            if (_overrides.Count == 0)
            {
                return;
            }

            _overrides.Clear();
            NotifyPaletteChanged();
        }

        private void SubscribeToActivePalette()
        {
            if (_subscribedPalette == _activePalette)
            {
                return;
            }

            UnsubscribeFromPalette();
            _subscribedPalette = _activePalette;

            if (_subscribedPalette != null)
            {
                _subscribedPalette.Changed += HandlePaletteAssetChanged;
            }
        }

        private void UnsubscribeFromPalette()
        {
            if (_subscribedPalette != null)
            {
                _subscribedPalette.Changed -= HandlePaletteAssetChanged;
            }

            _subscribedPalette = null;
        }

        private void HandlePaletteAssetChanged()
        {
            NotifyPaletteChanged();
        }

        private void NotifyPaletteChanged()
        {
            PaletteChanged?.Invoke();
        }
    }
}
