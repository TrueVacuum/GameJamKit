using System;

namespace GameJamKit.Display
{
    [Serializable]
    public readonly struct DisplayResolution : IEquatable<DisplayResolution>
    {
        public int Width { get; }
        public int Height { get; }

        public DisplayResolution(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public bool Equals(DisplayResolution other)
        {
            return Width == other.Width && Height == other.Height;
        }

        public override bool Equals(object obj)
        {
            return obj is DisplayResolution other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Width, Height);
        }

        public override string ToString()
        {
            return $"{Width} x {Height}";
        }
    }
}
