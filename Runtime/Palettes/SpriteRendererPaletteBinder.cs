using UnityEngine;

namespace GameJamKit.Palettes
{
    [RequireComponent(typeof(SpriteRenderer))]
    [AddComponentMenu("Game Jam Kit/Palettes/Sprite Renderer Palette Binder")]
    public sealed class SpriteRendererPaletteBinder : PaletteColorBinder
    {
        [SerializeField] private SpriteRenderer _target;

        protected override void Reset()
        {
            _target = GetComponent<SpriteRenderer>();
            base.Reset();
        }

        protected override void ApplyColor(Color color)
        {
            if (_target == null)
            {
                _target = GetComponent<SpriteRenderer>();
            }

            if (_target != null)
            {
                _target.color = color;
            }
        }
    }
}
