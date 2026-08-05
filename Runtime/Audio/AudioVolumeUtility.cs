using UnityEngine;

namespace GameJamKit.Audio
{
    public static class AudioVolumeUtility
    {
        public static float LinearToDecibels(float linearVolume, float minimumDecibels = -80f)
        {
            float clampedVolume = Mathf.Clamp01(linearVolume);
            if (clampedVolume <= 0.0001f)
            {
                return minimumDecibels;
            }

            return Mathf.Max(minimumDecibels, 20f * Mathf.Log10(clampedVolume));
        }
    }
}
