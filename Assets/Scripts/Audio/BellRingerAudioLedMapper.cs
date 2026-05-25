using UnityEngine;

namespace BellRinger.Audio
{
    public static class BellRingerAudioLedMapper
    {
        public const int DisplayWidth = 16;
        public const int DisplayHeight = 8;

        public static bool TryMap(
            Transform listenerTransform,
            Vector3 targetWorldPosition,
            float minDistance,
            float maxDistance,
            float sourceVolume,
            float intensityMultiplier,
            float maximumBrightness,
            float minimumVisibleBrightness,
            float horizontalAngleLimitDegrees,
            float verticalAngleLimitDegrees,
            out BellRingerLedDotFrame dotFrame)
        {
            if (listenerTransform == null)
            {
                dotFrame = default;
                return false;
            }

            Vector3 localTargetPosition = listenerTransform.InverseTransformPoint(targetWorldPosition);

            return TryMapFromLocalPosition(
                localTargetPosition,
                minDistance,
                maxDistance,
                sourceVolume,
                intensityMultiplier,
                maximumBrightness,
                minimumVisibleBrightness,
                horizontalAngleLimitDegrees,
                verticalAngleLimitDegrees,
                out dotFrame);
        }

        public static bool TryMapFromLocalPosition(
            Vector3 localTargetPosition,
            float minDistance,
            float maxDistance,
            float sourceVolume,
            float intensityMultiplier,
            float maximumBrightness,
            float minimumVisibleBrightness,
            float horizontalAngleLimitDegrees,
            float verticalAngleLimitDegrees,
            out BellRingerLedDotFrame dotFrame)
        {
            dotFrame = default;

            if (maxDistance <= 0f || maxDistance <= minDistance)
            {
                return false;
            }

            float distance = localTargetPosition.magnitude;
            if (distance > maxDistance)
            {
                return false;
            }

            float normalizedDistance = distance <= minDistance
                ? 1f
                : 1f - Mathf.InverseLerp(minDistance, maxDistance, distance);

            float brightness = Mathf.Lerp(minimumVisibleBrightness, maximumBrightness, normalizedDistance);
            brightness *= Mathf.Clamp01(sourceVolume) * Mathf.Max(0f, intensityMultiplier);
            brightness = Mathf.Clamp01(brightness);

            if (brightness <= 0.001f)
            {
                return false;
            }

            float planarDistance = new Vector2(localTargetPosition.x, localTargetPosition.z).magnitude;
            float horizontalAngle = Mathf.Atan2(localTargetPosition.x, localTargetPosition.z) * Mathf.Rad2Deg;
            float verticalAngle = Mathf.Atan2(localTargetPosition.y, planarDistance) * Mathf.Rad2Deg;

            float normalizedX = Mathf.Clamp(horizontalAngle / Mathf.Max(1f, horizontalAngleLimitDegrees), -1f, 1f);
            float normalizedY = Mathf.Clamp(verticalAngle / Mathf.Max(1f, verticalAngleLimitDegrees), -1f, 1f);

            float mappedXFloat = Mathf.Clamp(Mathf.Lerp(0f, DisplayWidth - 1, (normalizedX * 0.5f) + 0.5f), 0f, DisplayWidth - 1);
            float mappedYFloat = Mathf.Clamp(Mathf.Lerp(0f, DisplayHeight - 1, (normalizedY * 0.5f) + 0.5f), 0f, DisplayHeight - 1);
            int mappedX = Mathf.Clamp(Mathf.RoundToInt(mappedXFloat), 0, DisplayWidth - 1);
            int mappedY = Mathf.Clamp(Mathf.RoundToInt(mappedYFloat), 0, DisplayHeight - 1);

            dotFrame = new BellRingerLedDotFrame(mappedX, mappedY, mappedXFloat, mappedYFloat, brightness);
            return true;
        }
    }
}
