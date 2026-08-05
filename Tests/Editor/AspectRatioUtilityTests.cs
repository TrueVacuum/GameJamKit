using NUnit.Framework;
using UnityEngine;

namespace GameJamKit.Display.Tests
{
    public sealed class AspectRatioUtilityTests
    {
        [Test]
        public void MatchingAspectUsesFullViewport()
        {
            Rect viewport = AspectRatioUtility.CalculateViewport(1920, 1080, 16, 9);

            AssertRect(viewport, 0f, 0f, 1f, 1f);
        }

        [Test]
        public void WiderScreenProducesSideBars()
        {
            Rect viewport = AspectRatioUtility.CalculateViewport(2560, 1080, 16, 9);

            AssertRect(viewport, 0.125f, 0f, 0.75f, 1f);
        }

        [Test]
        public void TallerScreenProducesTopAndBottomBars()
        {
            Rect viewport = AspectRatioUtility.CalculateViewport(1280, 1024, 16, 9);

            AssertRect(viewport, 0f, 0.1484375f, 1f, 0.703125f);
        }

        [Test]
        public void InvalidDimensionsUseFullViewport()
        {
            Rect viewport = AspectRatioUtility.CalculateViewport(0, 1080, 16, 9);

            AssertRect(viewport, 0f, 0f, 1f, 1f);
        }

        private static void AssertRect(
            Rect actual,
            float x,
            float y,
            float width,
            float height)
        {
            Assert.That(actual.x, Is.EqualTo(x).Within(0.0001f));
            Assert.That(actual.y, Is.EqualTo(y).Within(0.0001f));
            Assert.That(actual.width, Is.EqualTo(width).Within(0.0001f));
            Assert.That(actual.height, Is.EqualTo(height).Within(0.0001f));
        }
    }
}
