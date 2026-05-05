using BellRinger.Hardware;
using UnityEngine;

namespace BellRinger.Audio
{
    [DisallowMultipleComponent]
    public sealed class BellRingerLightTexturePlayer : MonoBehaviour
    {
        [SerializeField] private Transform listenerTransform;
        [SerializeField] private BellRingerLightTexturePreset[] presets = new BellRingerLightTexturePreset[0];
        [SerializeField] private int selectedPresetIndex;
        [SerializeField] private bool outputToHardware = true;
        [SerializeField] private bool showRuntimeControls = true;
        [SerializeField] private float serialRefreshRate = 20f;
        [SerializeField] [Range(0f, 0.35f)] private float globalMaximumBrightness = 0.2f;
        [SerializeField] private float maxBrightnessChangePerSecond = 3.5f;
        [SerializeField] private float horizontalAngleLimitDegrees = 110f;
        [SerializeField] private float verticalAngleLimitDegrees = 55f;
        [SerializeField] private Color bellColor = BellRingerLightStyle.BellGreen;
        [SerializeField] [Range(0.2f, 1f)] private float averageLightScale = 0.58f;
        [SerializeField] [Range(1f, 3f)] private float peakContrast = 1.85f;
        [SerializeField] [Range(0.25f, 1f)] private float litPixelScale = 0.55f;
        [SerializeField] [Range(1f, 2f)] private float peakIntensityScale = 1.25f;
        [SerializeField] [Range(2f, 4f)] private float bellMaxRadiusPixels = 3.4f;
        [SerializeField] [Range(0.2f, 1.4f)] private float bellCoreSizePixels = 0.58f;

        private Vector2 _rippleCenter = new Vector2(7.5f, 3.5f);
        private float _rippleAgeSeconds;
        private float _currentBrightness;
        private float _triggerIntensity = 1f;
        private float _nextSendTime;
        private bool _rippleActive;
        private bool _displayClear = true;

        public BellRingerLightTexturePreset ActivePreset
        {
            get
            {
                if (presets == null || presets.Length == 0)
                {
                    return null;
                }

                selectedPresetIndex = Mathf.Clamp(selectedPresetIndex, 0, presets.Length - 1);
                return presets[selectedPresetIndex];
            }
        }

        public void TriggerAtWorldPosition(Vector3 worldPosition)
        {
            TriggerAtWorldPosition(worldPosition, 1f);
        }

        public void TriggerAtWorldPosition(Vector3 worldPosition, float intensityNormalized)
        {
            ResolveListenerTransform();

            if (listenerTransform != null &&
                BellRingerAudioLedMapper.TryMap(
                    listenerTransform,
                    worldPosition,
                    0.1f,
                    16f,
                    1f,
                    1f,
                    1f,
                    0.01f,
                    horizontalAngleLimitDegrees,
                    verticalAngleLimitDegrees,
                    out BellRingerLedDotFrame dotFrame))
            {
                _rippleCenter = new Vector2(dotFrame.x, dotFrame.y);
            }

            _triggerIntensity = Mathf.Clamp01(intensityNormalized);
            _rippleAgeSeconds = 0f;
            _rippleActive = true;
            _displayClear = false;
        }

        public void SelectPreset(int presetIndex)
        {
            if (presets == null || presets.Length == 0)
            {
                selectedPresetIndex = 0;
                return;
            }

            selectedPresetIndex = Mathf.Clamp(presetIndex, 0, presets.Length - 1);
        }

        public void SelectNextPreset()
        {
            if (presets == null || presets.Length == 0)
            {
                return;
            }

            selectedPresetIndex = (selectedPresetIndex + 1) % presets.Length;
        }

        public void SelectPreviousPreset()
        {
            if (presets == null || presets.Length == 0)
            {
                return;
            }

            selectedPresetIndex = (selectedPresetIndex + presets.Length - 1) % presets.Length;
        }

        private void Awake()
        {
            ResolveListenerTransform();
        }

        private void Update()
        {
            BellRingerLightTexturePreset preset = ActivePreset;
            if (preset == null)
            {
                return;
            }

            float deltaTime = Time.unscaledDeltaTime;
            if (_rippleActive)
            {
                _rippleAgeSeconds += deltaTime;
            }

            float targetBrightness = EvaluateTargetBrightness(preset);
            _currentBrightness = Mathf.MoveTowards(
                _currentBrightness,
                targetBrightness,
                Mathf.Max(0.01f, maxBrightnessChangePerSecond) * deltaTime);

            if (_rippleActive && CurrentRadius(preset) > preset.MaxRadiusPixels + preset.RippleWidthPixels)
            {
                _rippleActive = false;
            }

            float sendInterval = serialRefreshRate <= 0f ? 0.05f : 1f / serialRefreshRate;
            if (Time.unscaledTime >= _nextSendTime)
            {
                _nextSendTime = Time.unscaledTime + sendInterval;
                SendCurrentFrame(preset);
            }
        }

        private void OnGUI()
        {
            if (!showRuntimeControls)
            {
                return;
            }

            BellRingerLightTexturePreset preset = ActivePreset;
            GUILayout.BeginArea(new Rect(16f, 252f, 280f, 176f), "Light Texture", GUI.skin.window);
            GUILayout.Label(preset == null ? "(no preset)" : preset.DisplayName);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Prev", GUILayout.Height(30f)))
            {
                SelectPreviousPreset();
            }

            if (GUILayout.Button("Trigger", GUILayout.Height(30f)))
            {
                TriggerAtWorldPosition(listenerTransform == null ? Vector3.forward * 4f : listenerTransform.position + listenerTransform.forward * 4f);
            }

            if (GUILayout.Button("Next", GUILayout.Height(30f)))
            {
                SelectNextPreset();
            }

            GUILayout.EndHorizontal();
            outputToHardware = GUILayout.Toggle(outputToHardware, "Hardware");
            showRuntimeControls = GUILayout.Toggle(showRuntimeControls, "Controls");
            averageLightScale = Slider("Average", averageLightScale, 0.2f, 1f);
            peakIntensityScale = Slider("Peak", peakIntensityScale, 1f, 2f);
            peakContrast = Slider("Contrast", peakContrast, 1f, 3f);
            litPixelScale = Slider("Lit Pixels", litPixelScale, 0.25f, 1f);
            GUILayout.EndArea();

            DrawPreview(new Rect(16f, 436f, 256f, 128f), preset);
        }

        private void DrawPreview(Rect rect, BellRingerLightTexturePreset preset)
        {
            Color previousColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.65f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);

            if (preset != null)
            {
                float cellWidth = rect.width / BellRingerAudioLedMapper.DisplayWidth;
                float cellHeight = rect.height / BellRingerAudioLedMapper.DisplayHeight;

                for (int y = 0; y < BellRingerAudioLedMapper.DisplayHeight; y++)
                {
                    for (int x = 0; x < BellRingerAudioLedMapper.DisplayWidth; x++)
                    {
                        float alpha = EvaluatePixelAlpha(preset, x, y);
                        GUI.color = bellColor * Mathf.Clamp01(alpha * _currentBrightness / Mathf.Max(0.01f, preset.MaximumBrightness));
                        GUI.DrawTexture(new Rect(rect.x + x * cellWidth + 1f, rect.y + y * cellHeight + 1f, cellWidth - 2f, cellHeight - 2f), Texture2D.whiteTexture);
                    }
                }
            }

            GUI.color = previousColor;
        }

        private void ResolveListenerTransform()
        {
            if (listenerTransform != null)
            {
                return;
            }

            AudioListener listener = GetComponent<AudioListener>();
            if (listener == null)
            {
                listener = Object.FindFirstObjectByType<AudioListener>();
            }

            if (listener != null)
            {
                listenerTransform = listener.transform;
            }
        }

        private void SendCurrentFrame(BellRingerLightTexturePreset preset)
        {
            if (!outputToHardware || HardwareBridge.Instance == null)
            {
                return;
            }

            if (!_rippleActive || _currentBrightness <= 0.001f)
            {
                if (!_displayClear)
                {
                    HardwareBridge.Instance.ClearLedDisplay();
                    _displayClear = true;
                }

                return;
            }

            HardwareBridge.Instance.SendLedPulseCore(
                _rippleCenter.x,
                _rippleCenter.y,
                Mathf.Min(CurrentRadius(preset), bellMaxRadiusPixels),
                bellCoreSizePixels * Mathf.Lerp(0.75f, 1.25f, litPixelScale),
                Mathf.Max(0.45f, preset.RippleWidthPixels * litPixelScale),
                bellColor,
                _currentBrightness,
                peakContrast);
        }

        private float EvaluateTargetBrightness(BellRingerLightTexturePreset preset)
        {
            if (!_rippleActive)
            {
                return 0f;
            }

            float maximumBrightness = Mathf.Min(globalMaximumBrightness, preset.MaximumBrightness);
            float fadeIn = preset.FadeInSeconds <= 0f ? 1f : Mathf.Clamp01(_rippleAgeSeconds / preset.FadeInSeconds);
            float fadeOutDistance = Mathf.Max(0.01f, preset.RippleSpeedPixelsPerSecond * Mathf.Max(0.01f, preset.FadeOutSeconds));
            float remainingDistance = (bellMaxRadiusPixels + preset.RippleWidthPixels) - CurrentRadius(preset);
            float fadeOut = Mathf.Clamp01(remainingDistance / fadeOutDistance);
            return BellRingerLightStyle.ScaleLevel(maximumBrightness * _triggerIntensity * Mathf.Min(fadeIn, fadeOut), averageLightScale, peakIntensityScale);
        }

        private float EvaluatePixelAlpha(BellRingerLightTexturePreset preset, int x, int y)
        {
            if (!_rippleActive)
            {
                return 0f;
            }

            float distance = Vector2.Distance(new Vector2(x, y), _rippleCenter);
            float radius = Mathf.Min(CurrentRadius(preset), bellMaxRadiusPixels);
            float coreSize = bellCoreSizePixels * Mathf.Lerp(0.75f, 1.25f, litPixelScale);
            float halfWidth = Mathf.Max(0.05f, preset.RippleWidthPixels * litPixelScale * 0.5f);
            float core = Mathf.Exp(-(distance * distance) / (2f * coreSize * coreSize));
            float ring = Mathf.Clamp01(1f - Mathf.Abs(distance - radius) / halfWidth) * 0.75f;
            return BellRingerLightStyle.ContrastAlpha(Mathf.Max(core, ring), peakContrast);
        }

        private float CurrentRadius(BellRingerLightTexturePreset preset)
        {
            return preset.StartRadiusPixels + _rippleAgeSeconds * preset.RippleSpeedPixelsPerSecond;
        }

        private static float Slider(string label, float value, float min, float max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{label}: {value:0.00}", GUILayout.Width(90f));
            float nextValue = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(150f));
            GUILayout.EndHorizontal();
            return nextValue;
        }
    }
}
