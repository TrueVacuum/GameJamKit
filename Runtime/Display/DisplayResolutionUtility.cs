using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameJamKit.Display
{
    public static class DisplayResolutionUtility
    {
        public static List<DisplayResolution> BuildResolutionList(
            DisplaySettingsProfile profile,
            IReadOnlyList<Resolution> supportedResolutions,
            FullScreenMode mode,
            int displayWidth,
            int displayHeight)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            HashSet<DisplayResolution> unique = new HashSet<DisplayResolution>();

            if (supportedResolutions != null)
            {
                for (int i = 0; i < supportedResolutions.Count; i++)
                {
                    Resolution resolution = supportedResolutions[i];
                    TryAdd(profile, unique, resolution.width, resolution.height,
                        mode, displayWidth, displayHeight);
                }
            }

            if (mode == FullScreenMode.Windowed)
            {
                IReadOnlyList<Vector2Int> presets = profile.WindowedPresets;
                for (int i = 0; i < presets.Count; i++)
                {
                    Vector2Int preset = presets[i];
                    TryAdd(profile, unique, preset.x, preset.y,
                        mode, displayWidth, displayHeight);
                }
            }

            if (unique.Count == 0)
            {
                Vector2Int fallback = mode == FullScreenMode.Windowed
                    ? profile.DefaultWindowSize
                    : new Vector2Int(displayWidth, displayHeight);

                if (fallback.x > 0 && fallback.y > 0)
                {
                    unique.Add(new DisplayResolution(fallback.x, fallback.y));
                }
            }

            List<DisplayResolution> result = new List<DisplayResolution>(unique);
            result.Sort((left, right) =>
            {
                int pixelComparison =
                    (right.Width * (long)right.Height).CompareTo(left.Width * (long)left.Height);
                return pixelComparison != 0
                    ? pixelComparison
                    : right.Width.CompareTo(left.Width);
            });
            return result;
        }

        public static DisplayResolution FindClosest(
            IReadOnlyList<DisplayResolution> resolutions,
            int width,
            int height)
        {
            if (resolutions == null || resolutions.Count == 0)
            {
                return new DisplayResolution(Mathf.Max(1, width), Mathf.Max(1, height));
            }

            DisplayResolution closest = resolutions[0];
            long closestDistance = DistanceSquared(closest, width, height);

            for (int i = 1; i < resolutions.Count; i++)
            {
                long distance = DistanceSquared(resolutions[i], width, height);
                if (distance >= closestDistance)
                {
                    continue;
                }

                closest = resolutions[i];
                closestDistance = distance;
            }

            return closest;
        }

        private static void TryAdd(
            DisplaySettingsProfile profile,
            HashSet<DisplayResolution> resolutions,
            int width,
            int height,
            FullScreenMode mode,
            int displayWidth,
            int displayHeight)
        {
            if (width < profile.MinimumResolution.x || height < profile.MinimumResolution.y)
            {
                return;
            }

            if ((mode == FullScreenMode.Windowed || profile.LimitToDesktopResolution) &&
                displayWidth > 0 && displayHeight > 0 &&
                (mode == FullScreenMode.Windowed
                    ? width >= displayWidth || height >= displayHeight
                    : width > displayWidth || height > displayHeight))
            {
                return;
            }

            if (profile.FilterByAspectRatio)
            {
                float targetAspect =
                    profile.TargetAspectRatio.x / (float)profile.TargetAspectRatio.y;
                float actualAspect = width / (float)height;
                if (Mathf.Abs(actualAspect - targetAspect) > profile.AspectRatioTolerance)
                {
                    return;
                }
            }

            resolutions.Add(new DisplayResolution(width, height));
        }

        private static long DistanceSquared(DisplayResolution resolution, int width, int height)
        {
            long widthDelta = resolution.Width - (long)width;
            long heightDelta = resolution.Height - (long)height;
            return widthDelta * widthDelta + heightDelta * heightDelta;
        }
    }
}
