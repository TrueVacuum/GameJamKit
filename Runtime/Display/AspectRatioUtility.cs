using UnityEngine;

namespace GameJamKit.Display
{
    public static class AspectRatioUtility
    {
        public static Rect CalculateViewport(
            int screenWidth,
            int screenHeight,
            int aspectWidth,
            int aspectHeight)
        {
            if (screenWidth <= 0 || screenHeight <= 0 ||
                aspectWidth <= 0 || aspectHeight <= 0)
            {
                return new Rect(0f, 0f, 1f, 1f);
            }

            float screenAspect = screenWidth / (float)screenHeight;
            float targetAspect = aspectWidth / (float)aspectHeight;

            if (Mathf.Approximately(screenAspect, targetAspect))
            {
                return new Rect(0f, 0f, 1f, 1f);
            }

            if (screenAspect > targetAspect)
            {
                float normalizedWidth = targetAspect / screenAspect;
                return new Rect(
                    (1f - normalizedWidth) * 0.5f,
                    0f,
                    normalizedWidth,
                    1f);
            }

            float normalizedHeight = screenAspect / targetAspect;
            return new Rect(
                0f,
                (1f - normalizedHeight) * 0.5f,
                1f,
                normalizedHeight);
        }
    }
}
