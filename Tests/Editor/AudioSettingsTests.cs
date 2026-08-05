using NUnit.Framework;

namespace GameJamKit.Audio.Tests
{
    public sealed class AudioSettingsTests
    {
        [Test]
        public void LinearVolumeConvertsToDecibels()
        {
            Assert.That(
                AudioVolumeUtility.LinearToDecibels(1f),
                Is.EqualTo(0f).Within(0.001f));
            Assert.That(
                AudioVolumeUtility.LinearToDecibels(0.5f),
                Is.EqualTo(-6.0206f).Within(0.001f));
            Assert.That(
                AudioVolumeUtility.LinearToDecibels(0f),
                Is.EqualTo(-80f).Within(0.001f));
        }

        [Test]
        public void SettingsClampVolumesToSliderRange()
        {
            AudioSettingsData settings = new AudioSettingsData(2f, -1f, 0.5f, true);

            AudioSettingsData clamped = settings.Clamp();

            Assert.That(clamped.MasterVolume, Is.EqualTo(1f));
            Assert.That(clamped.MusicVolume, Is.EqualTo(0f));
            Assert.That(clamped.SoundEffectsVolume, Is.EqualTo(0.5f));
            Assert.That(clamped.Muted, Is.True);
        }
    }
}
