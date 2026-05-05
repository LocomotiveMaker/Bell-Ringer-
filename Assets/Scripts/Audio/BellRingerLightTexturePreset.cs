using UnityEngine;

namespace BellRinger.Audio
{
    [CreateAssetMenu(menuName = "Bell Ringer/Light Texture Preset", fileName = "BellRingerLightTexturePreset")]
    public sealed class BellRingerLightTexturePreset : ScriptableObject
    {
        [SerializeField] private string displayName = "Green Bell Ripple";
        [SerializeField] private Color color = BellRingerLightStyle.BellGreen;
        [SerializeField] [Range(0f, 0.35f)] private float maximumBrightness = 0.16f;
        [SerializeField] private float fadeInSeconds = 0.05f;
        [SerializeField] private float fadeOutSeconds = 0.2f;
        [SerializeField] private float rippleSpeedPixelsPerSecond = 5f;
        [SerializeField] private float rippleWidthPixels = 1.35f;
        [SerializeField] private float startRadiusPixels = 0f;
        [SerializeField] private float maxRadiusPixels = 11f;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public Color Color => color;
        public float MaximumBrightness => Mathf.Clamp(maximumBrightness, 0f, 0.35f);
        public float FadeInSeconds => Mathf.Max(0f, fadeInSeconds);
        public float FadeOutSeconds => Mathf.Max(0f, fadeOutSeconds);
        public float RippleSpeedPixelsPerSecond => Mathf.Max(0.01f, rippleSpeedPixelsPerSecond);
        public float RippleWidthPixels => Mathf.Max(0.1f, rippleWidthPixels);
        public float StartRadiusPixels => Mathf.Max(0f, startRadiusPixels);
        public float MaxRadiusPixels => Mathf.Max(StartRadiusPixels, maxRadiusPixels);
    }
}
