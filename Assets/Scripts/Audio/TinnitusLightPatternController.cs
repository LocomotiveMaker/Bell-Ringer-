using BellRinger.Hardware;
using UnityEngine;

namespace BellRinger.Audio
{
    [DisallowMultipleComponent]
    public sealed class TinnitusLightPatternController : MonoBehaviour
    {
        [SerializeField] private Transform listenerTransform;
        [SerializeField] private Transform tinnitusTransform;
        [SerializeField] private TinnitusAudioController audioController;
        [SerializeField] private bool outputToHardware = true;
        [SerializeField] private float serialRefreshRate = 20f;
        [SerializeField] private float visibleRangeMeters = 10f;
        [SerializeField] [Range(0f, 0.35f)] private float coreIntensity = 0.25f;
        [SerializeField] [Range(0.2f, 2.2f)] private float coreSize = 1.12f;
        [SerializeField] [Range(0f, 5f)] private float tearAmount = 5f;
        [SerializeField] private Vector2 tearAxis = Vector2.right;
        [SerializeField] [Range(0f, 1.5f)] private float jitter;
        [SerializeField] [Range(0.1f, 4f)] private float smearDecay = 1.92f;
        [SerializeField] [Range(0.1f, 3f)] private float pulseRate = 0.947f;
        [SerializeField] [Range(0f, 1f)] private float instability = 0.425f;
        [SerializeField] [Range(0f, 1f)] private float cleanseStability;
        [SerializeField] private Color tinnitusColor = BellRingerLightStyle.TinnitusViolet;
        [SerializeField] [Range(0.2f, 1f)] private float averageLightScale = 0.898f;
        [SerializeField] [Range(1f, 3f)] private float peakContrast = 1.309f;
        [SerializeField] [Range(0.25f, 1f)] private float litPixelScale = 0.25f;
        [SerializeField] [Range(1f, 2f)] private float peakIntensityScale = 1.638f;

        private readonly Color[,] _previewPixels = new Color[BellRingerAudioLedMapper.DisplayWidth, BellRingerAudioLedMapper.DisplayHeight];
        private float _nextSendTime;
        private bool _displayClear = true;

        public bool OutputToHardware
        {
            get => outputToHardware;
            set => outputToHardware = value;
        }

        public float CoreIntensity
        {
            get => coreIntensity;
            set => coreIntensity = Mathf.Clamp(value, 0f, 0.35f);
        }

        public float CoreSize
        {
            get => coreSize;
            set => coreSize = Mathf.Clamp(value, 0.2f, 2.2f);
        }

        public float TearAmount
        {
            get => tearAmount;
            set => tearAmount = Mathf.Clamp(value, 0f, 5f);
        }

        public Vector2 TearAxis
        {
            get => tearAxis;
            set => tearAxis = value.sqrMagnitude <= 0.001f ? Vector2.right : value.normalized;
        }

        public float Jitter
        {
            get => jitter;
            set => jitter = Mathf.Clamp(value, 0f, 1.5f);
        }

        public float SmearDecay
        {
            get => smearDecay;
            set => smearDecay = Mathf.Clamp(value, 0.1f, 4f);
        }

        public float PulseRate
        {
            get => pulseRate;
            set => pulseRate = Mathf.Clamp(value, 0.1f, 3f);
        }

        public float Instability
        {
            get => instability;
            set => instability = Mathf.Clamp01(value);
        }

        public float CleanseStability
        {
            get => cleanseStability;
            set => cleanseStability = Mathf.Clamp01(value);
        }

        public float AverageLightScale
        {
            get => averageLightScale;
            set => averageLightScale = Mathf.Clamp(value, 0.2f, 1f);
        }

        public float PeakContrast
        {
            get => peakContrast;
            set => peakContrast = Mathf.Clamp(value, 1f, 3f);
        }

        public float LitPixelScale
        {
            get => litPixelScale;
            set => litPixelScale = Mathf.Clamp(value, 0.25f, 1f);
        }

        public float PeakIntensityScale
        {
            get => peakIntensityScale;
            set => peakIntensityScale = Mathf.Clamp(value, 1f, 2f);
        }

        public bool IsVisible { get; private set; }
        public Vector2 LastCorePosition { get; private set; } = new Vector2(7.5f, 3.5f);
        public float LastTearStrength { get; private set; }
        public float LastEffectiveInstability { get; private set; }

        public void Configure(Transform listener, Transform tinnitus, TinnitusAudioController audio, bool hardwareOutput)
        {
            listenerTransform = listener;
            tinnitusTransform = tinnitus;
            audioController = audio;
            outputToHardware = hardwareOutput;
        }

        public void ApplySharedSettings(TinnitusSharedSettingsData settings)
        {
            if (settings == null)
            {
                return;
            }

            CoreIntensity = settings.coreIntensity;
            CoreSize = settings.coreSize;
            TearAmount = settings.tearAmount;
            TearAxis = new Vector2(settings.tearAxisX, settings.tearAxisY);
            Jitter = settings.jitter;
            SmearDecay = settings.smearDecay;
            PulseRate = settings.pulseRate;
            Instability = settings.instability;
            AverageLightScale = settings.averageLightScale;
            PeakIntensityScale = settings.peakIntensityScale;
            PeakContrast = settings.peakContrast;
            LitPixelScale = settings.litPixelScale;
            CleanseStability = settings.cleanseStability;
        }

        public void FillSharedSettings(TinnitusSharedSettingsData settings)
        {
            if (settings == null)
            {
                return;
            }

            settings.coreIntensity = CoreIntensity;
            settings.coreSize = CoreSize;
            settings.tearAmount = TearAmount;
            settings.tearAxisX = TearAxis.x;
            settings.tearAxisY = TearAxis.y;
            settings.jitter = Jitter;
            settings.smearDecay = SmearDecay;
            settings.pulseRate = PulseRate;
            settings.instability = Instability;
            settings.averageLightScale = AverageLightScale;
            settings.peakIntensityScale = PeakIntensityScale;
            settings.peakContrast = PeakContrast;
            settings.litPixelScale = LitPixelScale;
            settings.cleanseStability = CleanseStability;
        }

        public Color GetPreviewPixel(int x, int y)
        {
            return _previewPixels[Mathf.Clamp(x, 0, BellRingerAudioLedMapper.DisplayWidth - 1), Mathf.Clamp(y, 0, BellRingerAudioLedMapper.DisplayHeight - 1)];
        }

        private void Awake()
        {
            LoadSharedSettingsIfPresent();
            ResolveReferences();
        }

        private void Update()
        {
            ResolveReferences();
            if (Time.unscaledTime < _nextSendTime)
            {
                return;
            }

            float interval = serialRefreshRate <= 0f ? 0.05f : 1f / serialRefreshRate;
            _nextSendTime = Time.unscaledTime + interval;
            UpdatePattern();
        }

        private void OnValidate()
        {
            serialRefreshRate = Mathf.Max(1f, serialRefreshRate);
            visibleRangeMeters = Mathf.Max(0.5f, visibleRangeMeters);
            coreIntensity = Mathf.Clamp(coreIntensity, 0f, 0.35f);
            coreSize = Mathf.Clamp(coreSize, 0.2f, 2.2f);
            tearAmount = Mathf.Clamp(tearAmount, 0f, 5f);
            if (tearAxis.sqrMagnitude <= 0.001f)
            {
                tearAxis = Vector2.right;
            }

            jitter = Mathf.Clamp(jitter, 0f, 1.5f);
            smearDecay = Mathf.Clamp(smearDecay, 0.1f, 4f);
            pulseRate = Mathf.Clamp(pulseRate, 0.1f, 3f);
            instability = Mathf.Clamp01(instability);
            cleanseStability = Mathf.Clamp01(cleanseStability);
            averageLightScale = Mathf.Clamp(averageLightScale, 0.2f, 1f);
            peakContrast = Mathf.Clamp(peakContrast, 1f, 3f);
            litPixelScale = Mathf.Clamp(litPixelScale, 0.25f, 1f);
            peakIntensityScale = Mathf.Clamp(peakIntensityScale, 1f, 2f);
        }

        private void ResolveReferences()
        {
            if (tinnitusTransform == null)
            {
                tinnitusTransform = transform;
            }

            if (audioController == null)
            {
                audioController = GetComponent<TinnitusAudioController>();
            }

            if (listenerTransform == null)
            {
                AudioListener listener = FindFirstObjectByType<AudioListener>();
                if (listener != null)
                {
                    listenerTransform = listener.transform;
                }
            }
        }

        private void LoadSharedSettingsIfPresent()
        {
            if (!Application.isPlaying || !TinnitusSharedSettingsStore.HasSavedSettings)
            {
                return;
            }

            ApplySharedSettings(TinnitusSharedSettingsStore.Load());
        }

        private void UpdatePattern()
        {
            ClearPreview();

            if (listenerTransform == null || tinnitusTransform == null ||
                !BellRingerAudioLedMapper.TryMap(
                    listenerTransform,
                    tinnitusTransform.position,
                    0.2f,
                    visibleRangeMeters,
                    1f,
                    1f,
                    1f,
                    0.01f,
                    95f,
                    50f,
                    out BellRingerLedDotFrame dotFrame))
            {
                IsVisible = false;
                ClearHardwareIfNeeded();
                return;
            }

            IsVisible = true;
            float stability = Mathf.Max(cleanseStability, audioController == null ? 0f : audioController.CleanseStability);
            float burst = audioController == null ? 0f : audioController.EffectiveBurstIntensity;
            float audioInstability = audioController == null ? 0f : audioController.EffectiveInstability;
            float effectiveInstability = Mathf.Clamp01(Mathf.Max(instability, audioInstability) * (1f - stability) + burst * 0.7f);
            float pulse = EvaluateSafePulse(Time.unscaledTime, pulseRate);
            float tearStrength = tearAmount * effectiveInstability * Mathf.Clamp01(0.15f + pulse * 0.85f);
            float jitterAmount = jitter * (1f - stability) * Mathf.Clamp01(0.25f + effectiveInstability * 0.75f);
            Vector2 axis = tearAxis.sqrMagnitude <= 0.001f ? Vector2.right : tearAxis.normalized;
            Vector2 jitterOffset = new Vector2(
                Mathf.PerlinNoise(Time.unscaledTime * 1.7f, 0.13f) - 0.5f,
                Mathf.PerlinNoise(0.71f, Time.unscaledTime * 1.9f) - 0.5f) * jitterAmount;

            Vector2 center = new Vector2(dotFrame.x, dotFrame.y) + jitterOffset;
            center.x = Mathf.Clamp(center.x, 0f, BellRingerAudioLedMapper.DisplayWidth - 1);
            center.y = Mathf.Clamp(center.y, 0f, BellRingerAudioLedMapper.DisplayHeight - 1);

            float effectiveCoreSize = coreSize * Mathf.Lerp(0.7f, 1.1f, litPixelScale);
            float effectiveTearStrength = tearStrength * Mathf.Lerp(0.65f, 1f, litPixelScale);
            float level = BellRingerLightStyle.ScaleLevel(
                coreIntensity * dotFrame.brightnessNormalized * Mathf.Lerp(1f, 0.65f, stability),
                averageLightScale,
                peakIntensityScale);
            int seed = Mathf.FloorToInt(Time.unscaledTime * 3f);
            DrawTinnitusPreview(center, effectiveCoreSize, effectiveTearStrength, axis, level, effectiveInstability);

            LastCorePosition = center;
            LastTearStrength = effectiveTearStrength;
            LastEffectiveInstability = effectiveInstability;
            _displayClear = false;

            if (outputToHardware && HardwareBridge.Instance != null)
            {
                HardwareBridge.Instance.SendLedTinnitus(center.x, center.y, effectiveCoreSize, effectiveTearStrength, axis.x, axis.y, tinnitusColor, level, seed, effectiveInstability, smearDecay, peakContrast);
            }
        }

        private void ClearHardwareIfNeeded()
        {
            if (!_displayClear && outputToHardware && HardwareBridge.Instance != null)
            {
                HardwareBridge.Instance.ClearLedDisplay();
            }

            _displayClear = true;
        }

        private void DrawTinnitusPreview(Vector2 center, float size, float tearStrength, Vector2 axis, float level, float effectiveInstability)
        {
            Vector2 perpendicular = new Vector2(-axis.y, axis.x);
            float coreSigma = Mathf.Max(0.18f, size);
            float lobeSigma = Mathf.Lerp(0.35f, 0.7f, effectiveInstability) * Mathf.Lerp(0.75f, 1f, litPixelScale);
            float smear = Mathf.Max(0.1f, smearDecay);
            float density = BellRingerLightStyle.DensityFromLitScale(litPixelScale);

            for (int y = 0; y < BellRingerAudioLedMapper.DisplayHeight; y++)
            {
                for (int x = 0; x < BellRingerAudioLedMapper.DisplayWidth; x++)
                {
                    Vector2 offset = new Vector2(x, y) - center;
                    float distanceSquared = offset.sqrMagnitude;
                    float core = Mathf.Exp(-distanceSquared / (2f * coreSigma * coreSigma));
                    float along = Vector2.Dot(offset, axis);
                    float across = Mathf.Abs(Vector2.Dot(offset, perpendicular));
                    float splitDistance = Mathf.Abs(Mathf.Abs(along) - tearStrength);
                    float tear = Mathf.Exp(-(splitDistance * splitDistance) / (2f * lobeSigma * lobeSigma)) *
                                 Mathf.Exp(-(across * across) / (2f * 0.32f * 0.32f)) *
                                 effectiveInstability;
                    float residualSmear = Mathf.Exp(-Mathf.Abs(along) / Mathf.Max(0.1f, tearStrength + smear)) *
                                          Mathf.Exp(-(across * across) / (2f * 0.75f * 0.75f)) *
                                          effectiveInstability * 0.22f * density;
                    float alpha = BellRingerLightStyle.ContrastAlpha(Mathf.Max(core, Mathf.Max(tear, residualSmear)), peakContrast) * level;
                    _previewPixels[x, y] = tinnitusColor * Mathf.Clamp01(alpha);
                }
            }
        }

        private void ClearPreview()
        {
            for (int y = 0; y < BellRingerAudioLedMapper.DisplayHeight; y++)
            {
                for (int x = 0; x < BellRingerAudioLedMapper.DisplayWidth; x++)
                {
                    _previewPixels[x, y] = Color.clear;
                }
            }
        }

        private static float EvaluateSafePulse(float time, float rate)
        {
            float clampedRate = Mathf.Clamp(rate, 0.1f, 3f);
            float phase = Mathf.Repeat(time * clampedRate, 1f);
            if (phase > 0.42f)
            {
                return 0f;
            }

            return Mathf.Sin((phase / 0.42f) * Mathf.PI);
        }
    }
}
