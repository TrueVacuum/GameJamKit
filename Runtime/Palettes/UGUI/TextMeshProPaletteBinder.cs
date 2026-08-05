using TMPro;
using UnityEngine;

namespace GameJamKit.Palettes
{
    [AddComponentMenu("Game Jam Kit/Palettes/TextMesh Pro Palette Binder")]
    public sealed class TextMeshProPaletteBinder : PaletteColorBinder
    {
        [SerializeField] private TMP_Text _target;

        protected override void Reset()
        {
            _target = GetComponent<TMP_Text>();
            base.Reset();
        }

        protected override void ApplyColor(Color color)
        {
            if (_target == null)
            {
                _target = GetComponent<TMP_Text>();
            }

            if (_target != null)
            {
                _target.color = color;
            }
        }
    }
}
