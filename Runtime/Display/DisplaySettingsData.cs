using System;
using UnityEngine;

namespace GameJamKit.Display
{
    [Serializable]
    public struct DisplaySettingsData : IEquatable<DisplaySettingsData>
    {
        [SerializeField] private int _width;
        [SerializeField] private int _height;
        [SerializeField] private FullScreenMode _fullScreenMode;

        public int Width => _width;
        public int Height => _height;
        public FullScreenMode FullScreenMode => _fullScreenMode;

        public DisplaySettingsData(int width, int height, FullScreenMode fullScreenMode)
        {
            _width = width;
            _height = height;
            _fullScreenMode = fullScreenMode;
        }

        public bool Equals(DisplaySettingsData other)
        {
            return _width == other._width &&
                   _height == other._height &&
                   _fullScreenMode == other._fullScreenMode;
        }

        public override bool Equals(object obj)
        {
            return obj is DisplaySettingsData other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_width, _height, (int)_fullScreenMode);
        }

        public override string ToString()
        {
            return $"{_width} x {_height} ({_fullScreenMode})";
        }
    }
}
