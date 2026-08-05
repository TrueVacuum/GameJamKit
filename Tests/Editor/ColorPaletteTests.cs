using NUnit.Framework;
using UnityEngine;

namespace GameJamKit.Palettes.Tests
{
    public sealed class ColorPaletteTests
    {
        private ColorPalette _palette;
        private GameObject _controllerObject;
        private ColorPaletteController _controller;

        [SetUp]
        public void SetUp()
        {
            _palette = ScriptableObject.CreateInstance<ColorPalette>();
            _controllerObject = new GameObject("ColorPaletteController");
            _controller = _controllerObject.AddComponent<ColorPaletteController>();
            _controller.SetPalette(_palette);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_controllerObject);
            Object.DestroyImmediate(_palette);
        }

        [Test]
        public void PaletteReturnsStoredColor()
        {
            Color expected = new Color(0.1f, 0.2f, 0.3f, 1f);

            _palette.SetColor("Primary", expected);

            Assert.That(_palette.TryGetColor("Primary", out Color actual), Is.True);
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void ControllerOverrideTakesPriorityAndCanBeRemoved()
        {
            Color paletteColor = Color.blue;
            Color overrideColor = Color.red;
            _palette.SetColor("Primary", paletteColor);

            _controller.SetOverride("Primary", overrideColor);
            Assert.That(_controller.GetColor("Primary", Color.clear), Is.EqualTo(overrideColor));

            Assert.That(_controller.RemoveOverride("Primary"), Is.True);
            Assert.That(_controller.GetColor("Primary", Color.clear), Is.EqualTo(paletteColor));
        }

        [Test]
        public void SpriteBinderRefreshesWhenOverrideChanges()
        {
            _palette.SetColor("Primary", Color.green);

            GameObject spriteObject = new GameObject("Sprite");
            SpriteRenderer renderer = spriteObject.AddComponent<SpriteRenderer>();
            SpriteRendererPaletteBinder binder =
                spriteObject.AddComponent<SpriteRendererPaletteBinder>();

            binder.SetController(_controller);
            binder.SetColorKey("Primary");
            Assert.That(renderer.color, Is.EqualTo(Color.green));

            _controller.SetOverride("Primary", Color.magenta);
            Assert.That(renderer.color, Is.EqualTo(Color.magenta));

            Object.DestroyImmediate(spriteObject);
        }

        [Test]
        public void SpriteBinderCanOverridePaletteAlpha()
        {
            _palette.SetColor("Primary", new Color(0.2f, 0.4f, 0.6f, 0.8f));

            GameObject spriteObject = new GameObject("Sprite");
            SpriteRenderer renderer = spriteObject.AddComponent<SpriteRenderer>();
            SpriteRendererPaletteBinder binder =
                spriteObject.AddComponent<SpriteRendererPaletteBinder>();

            binder.SetController(_controller);
            binder.SetColorKey("Primary");
            binder.SetAlphaOverride(true, 0.25f);

            Assert.That(renderer.color.r, Is.EqualTo(0.2f).Within(0.001f));
            Assert.That(renderer.color.g, Is.EqualTo(0.4f).Within(0.001f));
            Assert.That(renderer.color.b, Is.EqualTo(0.6f).Within(0.001f));
            Assert.That(renderer.color.a, Is.EqualTo(0.25f).Within(0.001f));

            Object.DestroyImmediate(spriteObject);
        }
    }
}
