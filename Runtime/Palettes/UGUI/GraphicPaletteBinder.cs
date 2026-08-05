using UnityEngine;
using UnityEngine.UI;

namespace GameJamKit.Palettes
{
    [AddComponentMenu("Game Jam Kit/Palettes/UI Graphic Palette Binder")]
    public sealed class GraphicPaletteBinder : PaletteColorBinder
    {
        [SerializeField] private Graphic _target;

        protected override void Reset()
        {
            _target = GetComponent<Graphic>();
            base.Reset();
        }

        protected override void ApplyColor(Color color)
        {
            if (_target == null)
            {
                _target = GetComponent<Graphic>();
            }

            if (_target != null)
            {
                _target.color = color;
            }
        }
    }
}
