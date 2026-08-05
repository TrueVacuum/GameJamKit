using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameJamKit.Palettes
{
    [Serializable]
    public sealed class PaletteColor
    {
        [SerializeField] private string _key = string.Empty;
        [SerializeField] private Color _color = Color.white;

        public string Key => _key;
        public Color Color => _color;

        public PaletteColor(string key, Color color)
        {
            _key = key;
            _color = color;
        }

        internal void SetColor(Color color)
        {
            _color = color;
        }
    }

    [CreateAssetMenu(
        fileName = "ColorPalette",
        menuName = "Game Jam Kit/Color Palette")]
    public sealed class ColorPalette : ScriptableObject
    {
        [SerializeField] private List<PaletteColor> _colors = new List<PaletteColor>();

        private Dictionary<string, Color> _lookup;

        public event Action Changed;

        public IReadOnlyList<PaletteColor> Colors => _colors;
        public int Count => _colors.Count;

        private void OnEnable()
        {
            RebuildLookup();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            RebuildLookup();
            Changed?.Invoke();
        }
#endif

        public bool TryGetColor(string key, out Color color)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                color = default;
                return false;
            }

            EnsureLookup();
            return _lookup.TryGetValue(key, out color);
        }

        public Color GetColor(string key, Color fallback)
        {
            return TryGetColor(key, out Color color) ? color : fallback;
        }

        public bool Contains(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            EnsureLookup();
            return _lookup.ContainsKey(key);
        }

        public void SetColor(string key, Color color)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Palette color keys cannot be empty.", nameof(key));
            }

            for (int i = 0; i < _colors.Count; i++)
            {
                PaletteColor entry = _colors[i];
                if (entry != null && string.Equals(entry.Key, key, StringComparison.Ordinal))
                {
                    entry.SetColor(color);
                    RebuildLookup();
                    Changed?.Invoke();
                    return;
                }
            }

            _colors.Add(new PaletteColor(key, color));
            RebuildLookup();
            Changed?.Invoke();
        }

        public bool RemoveColor(string key)
        {
            for (int i = _colors.Count - 1; i >= 0; i--)
            {
                PaletteColor entry = _colors[i];
                if (entry == null || !string.Equals(entry.Key, key, StringComparison.Ordinal))
                {
                    continue;
                }

                _colors.RemoveAt(i);
                RebuildLookup();
                Changed?.Invoke();
                return true;
            }

            return false;
        }

        private void EnsureLookup()
        {
            if (_lookup == null)
            {
                RebuildLookup();
            }
        }

        private void RebuildLookup()
        {
            _lookup = new Dictionary<string, Color>(StringComparer.Ordinal);

            for (int i = 0; i < _colors.Count; i++)
            {
                PaletteColor entry = _colors[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.Key))
                {
                    continue;
                }

                // The final duplicate entry wins. The custom inspector reports duplicates.
                _lookup[entry.Key] = entry.Color;
            }
        }
    }
}
