using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace GameJamKit.Display.Tests
{
    public sealed class DisplayResolutionUtilityTests
    {
        private DisplaySettingsProfile _profile;

        [SetUp]
        public void SetUp()
        {
            _profile = ScriptableObject.CreateInstance<DisplaySettingsProfile>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_profile);
        }

        [Test]
        public void FullscreenListRemovesDuplicatesAndSmallResolutions()
        {
            Resolution[] supported =
            {
                CreateResolution(3840, 2160),
                CreateResolution(1920, 1080),
                CreateResolution(1920, 1080),
                CreateResolution(1280, 720),
                CreateResolution(800, 450)
            };

            List<DisplayResolution> result = DisplayResolutionUtility.BuildResolutionList(
                _profile,
                supported,
                FullScreenMode.FullScreenWindow,
                1920,
                1080);

            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result.Contains(new DisplayResolution(3840, 2160)), Is.False);
            Assert.That(result[0], Is.EqualTo(new DisplayResolution(1920, 1080)));
            Assert.That(result[1], Is.EqualTo(new DisplayResolution(1280, 720)));
        }

        [Test]
        public void WindowedListIncludesProfilePresetsAndExcludesDesktopSizedWindow()
        {
            Resolution[] supported =
            {
                CreateResolution(1920, 1080),
                CreateResolution(1280, 720)
            };

            List<DisplayResolution> result = DisplayResolutionUtility.BuildResolutionList(
                _profile,
                supported,
                FullScreenMode.Windowed,
                1920,
                1080);

            Assert.That(result.Contains(new DisplayResolution(1920, 1080)), Is.False);
            Assert.That(result.Contains(new DisplayResolution(1600, 900)), Is.True);
            Assert.That(result.Contains(new DisplayResolution(1280, 720)), Is.True);
            Assert.That(result.Contains(new DisplayResolution(960, 540)), Is.True);
        }

        [Test]
        public void FindClosestReturnsNearestWidthAndHeight()
        {
            DisplayResolution[] available =
            {
                new DisplayResolution(1920, 1080),
                new DisplayResolution(1600, 900),
                new DisplayResolution(1280, 720)
            };

            DisplayResolution result = DisplayResolutionUtility.FindClosest(
                available,
                1500,
                850);

            Assert.That(result, Is.EqualTo(new DisplayResolution(1600, 900)));
        }

        [Test]
        public void ProfileFallsBackToAnAllowedMode()
        {
            FullScreenMode result = _profile.GetAllowedModeOrFallback(
                FullScreenMode.ExclusiveFullScreen);

            Assert.That(result, Is.EqualTo(FullScreenMode.Windowed));
        }

        private static Resolution CreateResolution(int width, int height)
        {
            return new Resolution
            {
                width = width,
                height = height
            };
        }
    }
}
