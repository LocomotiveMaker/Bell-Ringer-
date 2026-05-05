using UnityEngine;

namespace BellRinger.Audio
{
    public static class BellRingerLightStyle
    {
        public static readonly Color PadOrange = new Color(1f, 0.28f, 0.02f, 1f);
        public static readonly Color BellGreen = new Color(0.02f, 1f, 0.12f, 1f);
        public static readonly Color WallCyan = new Color(0.02f, 0.85f, 1f, 1f);
        public static readonly Color RainDeepBlue = new Color(0.02f, 0.08f, 1f, 1f);
        public static readonly Color TinnitusViolet = new Color(0.46f, 0.05f, 1f, 1f);

        public static float ScaleLevel(float level, float averageLightScale, float peakIntensityScale)
        {
            return Mathf.Clamp01(level * Mathf.Clamp(averageLightScale, 0.05f, 1.5f) * Mathf.Clamp(peakIntensityScale, 0.5f, 3f));
        }

        public static float ContrastAlpha(float alpha, float contrast)
        {
            return Mathf.Pow(Mathf.Clamp01(alpha), Mathf.Max(0.1f, contrast));
        }

        public static float DensityFromLitScale(float litPixelScale)
        {
            return Mathf.Lerp(0.08f, 0.75f, Mathf.Clamp01(litPixelScale));
        }
    }
}
