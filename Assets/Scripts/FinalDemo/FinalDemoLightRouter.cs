using BellRinger.Audio;
using BellRinger.Hardware;
using UnityEngine;

namespace BellRinger.FinalDemo
{
    [DisallowMultipleComponent]
    public sealed class FinalDemoLightRouter : MonoBehaviour
    {
        [SerializeField] private Transform listenerTransform;
        [SerializeField] private HardwareBridge hardwareBridge;
        [SerializeField] private bool outputToHardware = true;
        [SerializeField] private Color bellColor = new Color(0.08f, 1f, 0.15f);
        [SerializeField] private Color padColor = new Color(1f, 0.54f, 0.05f);
        [SerializeField] private Color rainColor = new Color(0.02f, 0.08f, 1f);
        [SerializeField] private Color tinnitusColor = new Color(0.55f, 0.08f, 1f);
        [SerializeField] private Color wallNoiseColor = new Color(0.06f, 0.78f, 0.9f);
        [SerializeField] private float highPriorityHoldSeconds = 0.35f;
        [SerializeField] private float defaultMaxDistance = 20.8f;
        [SerializeField] private float horizontalPositionWeight = 1.18f;
        [Header("LED / Final Demo Output")]
        [SerializeField, Range(0.25f, 3f)] private float finalDemoBrightnessBoost = 1.45f;
        [SerializeField, Range(0.25f, 3f)] private float bellBrightnessBoost = 0.87f;
        [SerializeField, Range(0.05f, 1f)] private float padAnchorBrightnessBoost = 0.36f;
        [SerializeField, Range(0.25f, 3f)] private float bellWaveBrightnessBoost = 0.43f;
        [SerializeField, Range(0.05f, 1f)] private float bellWaveMaximumBrightness = 0.21f;
        [SerializeField, Range(0.05f, 1f)] private float tinnitusLightScale = 0.3f;
        [SerializeField, Range(0.05f, 1f)] private float bossTinnitusLightScale = 0.3f;
        [SerializeField, Range(0.05f, 1f)] private float tinnitusIntensityMultiplier = 0.5f;
        [SerializeField, Range(0.05f, 1f)] private float bossTinnitusIntensityMultiplier = 0.5f;
        [SerializeField, Range(0.05f, 1f)] private float tinnitusBoardRangeScale = 0.5f;
        [SerializeField, Range(0.05f, 1f)] private float bossTinnitusBoardRangeScale = 0.5f;
        [SerializeField, Range(0.05f, 1f)] private float tinnitusPatternSizeScale = 0.5f;
        [SerializeField, Range(0.05f, 1f)] private float bossTinnitusPatternSizeScale = 0.5f;
        [Header("Rain LED / Sample Grammar")]
        [SerializeField, Range(0.01f, 0.35f)] private float rainBrightness = 0.055f;
        [SerializeField, Range(0f, 1f)] private float rainDensity = 0.2f;
        [SerializeField, Range(0.6f, 4f)] private float rainNeutralRows = 2f;
        [SerializeField, Range(1f, 6f)] private float rainLookDownRows = 4.4f;
        [SerializeField, Range(0.05f, 1f)] private float rainHardwareBrightnessMultiplier = 0.25f;
        [SerializeField, Range(0.05f, 1f)] private float rainHardwareColorMultiplier = 0.25f;
        [SerializeField, Range(0.02f, 0.35f)] private float rainHardwareMaximumBrightness = 0.055f;
        [SerializeField, Range(0f, 0.1f)] private float rainHardwareMinimumVisibleBrightness = 0.067f;
        [SerializeField, Range(0.1f, 3f)] private float rainPreviewBrightnessBoost = 1.8f;
        [SerializeField, Range(0.1f, 2f)] private float rainSeedRate = 0.22f;
        [SerializeField, Range(0.1f, 2.5f)] private float rainPhaseSpeed = 0.9f;
        [SerializeField, Range(0.5f, 2f)] private float rainPeakContrast = 1.1f;
        [SerializeField] private bool clearWhenMappedOutsideBoard = true;

        private FinalDemoFeedbackPriority _heldPriority = FinalDemoFeedbackPriority.Rain;
        private float _holdUntilRealtime;
        private int _seed;
        private string _lastAction = "(idle)";
        private readonly Color[] _logicalFrame = new Color[BellRingerAudioLedMapper.DisplayWidth * BellRingerAudioLedMapper.DisplayHeight];
        private readonly Color[] _rainFrame = new Color[BellRingerAudioLedMapper.DisplayWidth * BellRingerAudioLedMapper.DisplayHeight];
        private readonly Color[] _compositeFrame = new Color[BellRingerAudioLedMapper.DisplayWidth * BellRingerAudioLedMapper.DisplayHeight];
        private readonly Color[] _hardwareFrame = new Color[BellRingerAudioLedMapper.DisplayWidth * BellRingerAudioLedMapper.DisplayHeight];
        private int _logicalFrameVersion;
        private float _rainFrameActiveUntilRealtime;
        private string _lastRainDebug = "(no rain)";

        public bool OutputToHardware => outputToHardware;
        public string LastAction => _lastAction;
        public string LastRainDebug => _lastRainDebug;
        public FinalDemoFeedbackPriority HeldPriority => Time.realtimeSinceStartup <= _holdUntilRealtime ? _heldPriority : FinalDemoFeedbackPriority.Rain;
        public int LogicalFrameWidth => BellRingerAudioLedMapper.DisplayWidth;
        public int LogicalFrameHeight => BellRingerAudioLedMapper.DisplayHeight;
        public int LogicalFrameVersion => _logicalFrameVersion;

        public void Initialize(Transform listener, HardwareBridge bridge)
        {
            listenerTransform = listener;
            hardwareBridge = bridge;
        }

        public void ShowBellPoint(Vector3 worldPosition, float intensity = 1f, bool miniRipple = true)
        {
            if (!TryMapWorldPosition(worldPosition, intensity, out BellRingerLedDotFrame frame, bellBrightnessBoost))
            {
                ClearMappedOutput(FinalDemoFeedbackPriority.Bell, "Bell point out of board.");
                return;
            }

            if (!TryEmit(FinalDemoFeedbackPriority.Bell))
            {
                return;
            }

            RenderBellLogicalFrame(frame, miniRipple, bellColor);
            if (!TrySendCurrentFrameWithRainComposite(FinalDemoFeedbackPriority.Bell))
            {
                HardwareBridge bridge = ResolveBridge();
                if (outputToHardware && bridge != null)
                {
                    if (miniRipple)
                    {
                        bridge.SendLedRipple(frame.centerX, frame.centerY, 1.7f, 0.75f, bellColor, frame.brightnessNormalized);
                    }
                    else
                    {
                        bridge.SendLedPulseCore(frame.centerX, frame.centerY, 0.55f, 0.8f, 0.6f, bellColor, frame.brightnessNormalized, 1.4f);
                    }
                }
            }

            HoldPriority(FinalDemoFeedbackPriority.Bell);
            _lastAction = $"Bell light x={frame.x} y={frame.y} b={frame.brightnessNormalized:0.00}";
        }

        public void ShowBellAnchor(Vector3 worldPosition, float intensity = 0.35f)
        {
            if (!TryMapWorldPosition(worldPosition, intensity, out BellRingerLedDotFrame frame, bellBrightnessBoost))
            {
                ClearMappedOutput(FinalDemoFeedbackPriority.Bell, "Bell anchor out of board.");
                return;
            }

            if (!TryEmit(FinalDemoFeedbackPriority.Bell))
            {
                return;
            }

            RenderBellLogicalFrame(frame, false, bellColor);
            if (!TrySendCurrentFrameWithRainComposite(FinalDemoFeedbackPriority.Bell))
            {
                HardwareBridge bridge = ResolveBridge();
                if (outputToHardware && bridge != null)
                {
                    bridge.SendLedPulseCore(frame.centerX, frame.centerY, 0.18f, 0.45f, 0.48f, bellColor, frame.brightnessNormalized, 1.15f);
                }
            }

            HoldPriority(FinalDemoFeedbackPriority.Bell);
            _lastAction = $"Bell anchor x={frame.centerX:0.00} y={frame.centerY:0.00} b={frame.brightnessNormalized:0.00}";
        }

        public void ShowPadAnchor(Vector3 worldPosition, float intensity = 0.22f)
        {
            if (!TryMapWorldPosition(worldPosition, intensity, out BellRingerLedDotFrame frame, padAnchorBrightnessBoost))
            {
                ClearPadAnchor();
                return;
            }

            if (!TryEmit(FinalDemoFeedbackPriority.Pad))
            {
                return;
            }

            HardwareBridge bridge = ResolveBridge();
            if (outputToHardware && bridge != null)
            {
                bridge.SendLedPulseCore(frame.centerX, frame.centerY, 0.22f, 0.34f, 0.46f, padColor, frame.brightnessNormalized, 1.05f);
            }

            RenderPadLogicalFrame(frame, padColor);
            HoldPriority(FinalDemoFeedbackPriority.Pad);
            _lastAction = $"Pad anchor x={frame.centerX:0.00} y={frame.centerY:0.00} b={frame.brightnessNormalized:0.00}";
        }

        public void ShowPadAnchorLocal(Vector3 listenerLocalPosition, float intensity = 0.22f)
        {
            if (!TryMapLocalPosition(listenerLocalPosition, intensity, out BellRingerLedDotFrame frame, padAnchorBrightnessBoost))
            {
                ClearPadAnchor();
                return;
            }

            if (!TryEmit(FinalDemoFeedbackPriority.Pad))
            {
                return;
            }

            HardwareBridge bridge = ResolveBridge();
            if (outputToHardware && bridge != null)
            {
                bridge.SendLedPulseCore(frame.centerX, frame.centerY, 0.24f, 0.34f, 0.42f, padColor, frame.brightnessNormalized, 1.08f);
            }

            RenderPadLogicalFrame(frame, padColor);
            HoldPriority(FinalDemoFeedbackPriority.Pad);
            _lastAction = $"Pad anchor local x={frame.centerX:0.00} y={frame.centerY:0.00} b={frame.brightnessNormalized:0.00}";
        }

        public void ClearPadAnchor()
        {
            bool higherPriorityHeld = Time.realtimeSinceStartup <= _holdUntilRealtime && _heldPriority > FinalDemoFeedbackPriority.Pad;
            if (higherPriorityHeld)
            {
                return;
            }

            HardwareBridge bridge = ResolveBridge();
            if (outputToHardware && bridge != null)
            {
                bridge.ClearLedDisplay();
            }

            _holdUntilRealtime = 0f;
            ClearLogicalFrame();
            _lastAction = "Pad anchor cleared.";
        }

        public void ShowBellWave(Vector3 worldPosition, float envelope01, float onset01, float intensityScale = 1f)
        {
            float envelope = Mathf.Clamp01(envelope01);
            float onset = Mathf.Clamp01(onset01);
            float brightness = Mathf.Clamp01((0.03f + envelope * 0.28f + onset * 0.015f) * Mathf.Clamp01(intensityScale));
            if (!TryMapWorldPosition(worldPosition, brightness, out BellRingerLedDotFrame frame, bellWaveBrightnessBoost))
            {
                ClearMappedOutput(FinalDemoFeedbackPriority.Bell, "Bell wave out of board.");
                return;
            }
            brightness = Mathf.Min(frame.brightnessNormalized, bellWaveMaximumBrightness);

            if (!TryEmit(FinalDemoFeedbackPriority.Bell))
            {
                return;
            }

            float radius = Mathf.Lerp(0.42f, 2.1f, Mathf.SmoothStep(0f, 1f, envelope)) + onset * 0.04f;
            float core = Mathf.Lerp(0.28f, 0.52f, envelope);
            float width = Mathf.Lerp(1.15f, 2.15f, Mathf.Clamp01(envelope + onset * 0.04f));

            RenderBellWaveLogicalFrame(frame, radius, width, bellColor, brightness);
            if (!TrySendCurrentFrameWithRainComposite(FinalDemoFeedbackPriority.Bell))
            {
                HardwareBridge bridge = ResolveBridge();
                if (outputToHardware && bridge != null)
                {
                    bridge.SendLedPulseCore(frame.centerX, frame.centerY, radius, core, width, bellColor, brightness, 0.62f + onset * 0.04f);
                }
            }

            HoldPriority(FinalDemoFeedbackPriority.Bell);
            _lastAction = $"Bell wave x={frame.x} y={frame.y} env={envelope:0.00} b={brightness:0.00}";
        }

        public void ShowRainFloorBand(float intensity = 0.45f)
        {
            if (!TryBuildRainFrame(
                    Mathf.Clamp01(intensity),
                    _rainFrame,
                    out float centerX,
                    out float centerY,
                    out float width,
                    out float height,
                    out float hardwareLevel,
                    out int seed,
                    out float phase,
                    out float density,
                    out string rainDebug))
            {
                _rainFrameActiveUntilRealtime = 0f;
                _lastRainDebug = rainDebug;
                ClearMappedOutput(FinalDemoFeedbackPriority.Rain, rainDebug);
                return;
            }

            _rainFrameActiveUntilRealtime = 0f;
            _lastRainDebug = rainDebug;
            if (!TryEmit(FinalDemoFeedbackPriority.Rain))
            {
                _lastAction = $"Rain blocked by {HeldPriority}. {rainDebug}";
                return;
            }

            CopyFrame(_rainFrame, _logicalFrame);
            HardwareBridge bridge = ResolveBridge();
            if (outputToHardware && bridge != null)
            {
                bridge.SendLedRain(rainColor, hardwareLevel, seed, centerX, centerY, width, height, phase, density, rainPeakContrast);
            }

            _logicalFrameVersion++;
            _lastAction = $"Rain floor frame. {rainDebug}";
        }

        public void ShowTinnitusPoint(Vector3 worldPosition, float intensity = 0.65f, bool boss = false)
        {
            float lightScale = boss ? bossTinnitusLightScale : tinnitusLightScale;
            float intensityMultiplier = boss ? bossTinnitusIntensityMultiplier : tinnitusIntensityMultiplier;
            float boardRangeScale = boss ? bossTinnitusBoardRangeScale : tinnitusBoardRangeScale;
            float sizeScale = boss ? bossTinnitusPatternSizeScale : tinnitusPatternSizeScale;
            float scaledIntensity = intensity * lightScale * intensityMultiplier;
            FinalDemoFeedbackPriority priority = boss ? FinalDemoFeedbackPriority.CriticalPad : FinalDemoFeedbackPriority.Tinnitus;
            if (!TryMapWorldPosition(worldPosition, scaledIntensity, out BellRingerLedDotFrame frame, 1f, boardRangeScale))
            {
                ClearMappedOutput(priority, $"{(boss ? "Boss" : "Tinnitus")} point out of board.");
                return;
            }

            if (!TryEmit(priority))
            {
                return;
            }

            HardwareBridge bridge = ResolveBridge();
            if (outputToHardware && bridge != null)
            {
                bridge.SendLedTinnitus(
                    frame.centerX,
                    frame.centerY,
                    (boss ? 1.65f : 1.05f) * sizeScale,
                    (boss ? 3.2f : 1.6f) * sizeScale,
                    boss ? 0.65f : 1f,
                    boss ? 0.35f : 0f,
                    tinnitusColor,
                    frame.brightnessNormalized,
                    ++_seed,
                    boss ? 0.78f : 0.45f,
                    (boss ? 2.6f : 1.6f) * sizeScale,
                    boss ? 1.85f : 1.35f);
            }

            RenderTinnitusLogicalFrame(frame, boss, tinnitusColor, sizeScale);
            HoldPriority(priority);
            _lastAction = $"{(boss ? "Boss" : "Tinnitus")} light x={frame.x} y={frame.y} b={frame.brightnessNormalized:0.00}";
        }

        public void ShowTinnitusWave(Vector3 worldPosition, float envelope01, float onset01, float intensityScale = 1f, bool boss = false)
        {
            float envelope = Mathf.Clamp01(envelope01);
            float onset = Mathf.Clamp01(onset01);
            float brightness = Mathf.Clamp01((0.12f + envelope * 0.62f + onset * 0.06f) * Mathf.Clamp01(intensityScale));
            brightness *= (boss ? bossTinnitusLightScale : tinnitusLightScale) * (boss ? bossTinnitusIntensityMultiplier : tinnitusIntensityMultiplier);
            float boardRangeScale = boss ? bossTinnitusBoardRangeScale : tinnitusBoardRangeScale;
            float sizeScale = boss ? bossTinnitusPatternSizeScale : tinnitusPatternSizeScale;
            FinalDemoFeedbackPriority priority = boss ? FinalDemoFeedbackPriority.CriticalPad : FinalDemoFeedbackPriority.Tinnitus;
            if (!TryMapWorldPosition(worldPosition, brightness, out BellRingerLedDotFrame frame, 1f, boardRangeScale))
            {
                ClearMappedOutput(priority, $"{(boss ? "Boss" : "Tinnitus")} wave out of board.");
                return;
            }
            brightness = frame.brightnessNormalized;

            if (!TryEmit(priority))
            {
                return;
            }

            float coreSize = (boss
                ? Mathf.Lerp(1.2f, 2.35f, envelope)
                : Mathf.Lerp(0.65f, 1.28f, envelope)) * sizeScale;
            float tearAmount = (boss
                ? Mathf.Lerp(0.9f, 3.15f, envelope) + onset * 0.35f
                : Mathf.Lerp(0.35f, 1.55f, envelope) + onset * 0.22f) * sizeScale;
            float instability = Mathf.Clamp01(envelope * 0.55f + onset * 0.16f);
            float axisX = Mathf.Sin((_seed + 1) * 0.73f);
            float axisY = boss ? Mathf.Cos((_seed + 3) * 0.47f) * 0.55f : 0f;
            float smear = (boss ? Mathf.Lerp(1.7f, 3.4f, envelope) : Mathf.Lerp(1.1f, 2.2f, envelope)) * sizeScale;

            HardwareBridge bridge = ResolveBridge();
            if (outputToHardware && bridge != null)
            {
                bridge.SendLedTinnitus(
                    frame.centerX,
                    frame.centerY,
                    coreSize,
                    tearAmount,
                    axisX,
                    axisY,
                    tinnitusColor,
                    brightness,
                    ++_seed,
                    instability,
                    smear,
                    boss ? 1.85f : 1.35f);
            }

            RenderTinnitusLogicalFrame(frame, boss, tinnitusColor, sizeScale);
            HoldPriority(priority);
            _lastAction = $"{(boss ? "Boss" : "Tinnitus")} wave x={frame.x} y={frame.y} env={envelope:0.00} b={brightness:0.00}";
        }

        public void ShowTinnitusPattern(Vector3 worldPosition, float intensity = 0.65f, float cleanseStability = 0f, bool boss = false, float rangeScale = 1f)
        {
            float lightScale = boss ? bossTinnitusLightScale : tinnitusLightScale;
            float intensityMultiplier = boss ? bossTinnitusIntensityMultiplier : tinnitusIntensityMultiplier;
            float boardRangeScale = Mathf.Max(0.05f, rangeScale * (boss ? bossTinnitusBoardRangeScale : tinnitusBoardRangeScale));
            float sizeScale = boss ? bossTinnitusPatternSizeScale : tinnitusPatternSizeScale;
            float scaledIntensity = Mathf.Clamp01(intensity) * lightScale * intensityMultiplier;
            FinalDemoFeedbackPriority priority = boss ? FinalDemoFeedbackPriority.CriticalPad : FinalDemoFeedbackPriority.Tinnitus;
            if (!TryMapWorldPosition(worldPosition, scaledIntensity, out BellRingerLedDotFrame frame, 1f, boardRangeScale))
            {
                ClearMappedOutput(priority, $"{(boss ? "Boss" : "Tinnitus")} pattern out of board.");
                return;
            }

            if (!TryEmit(priority))
            {
                return;
            }

            float stability = Mathf.Clamp01(cleanseStability);
            float pulse = EvaluateTinnitusPulse(Time.realtimeSinceStartup, boss ? 0.72f : 0.95f);
            float softLevel = Mathf.Clamp01(0.48f + pulse * 0.52f);
            Vector2 axis = boss ? new Vector2(1f, 0.42f).normalized : Vector2.right;
            float coreSize = (boss ? 1.56f : 1.08f) * Mathf.Lerp(1f, 0.82f, stability) * sizeScale;
            float tearStrength = (boss ? 2.35f : 1.42f) * (1f - stability) * Mathf.Lerp(0.38f, 1f, pulse) * sizeScale;
            float level = Mathf.Clamp01(frame.brightnessNormalized * softLevel);
            float instability = Mathf.Clamp01((1f - stability) * (boss ? 0.52f : 0.36f));
            float smear = (boss ? 2.35f : 1.55f) * sizeScale;

            HardwareBridge bridge = ResolveBridge();
            if (outputToHardware && bridge != null)
            {
                bridge.SendLedTinnitus(
                    frame.centerX,
                    frame.centerY,
                    coreSize,
                    tearStrength,
                    axis.x,
                    axis.y,
                    tinnitusColor,
                    level,
                    ++_seed,
                    instability,
                    smear,
                    boss ? 1.45f : 1.18f);
            }

            RenderTinnitusPatternLogicalFrame(new Vector2(frame.x, frame.y), coreSize, tearStrength, axis, tinnitusColor, level, instability, smear);
            HoldPriority(priority);
            _lastAction = $"{(boss ? "Boss" : "Tinnitus")} pattern x={frame.x} y={frame.y} b={level:0.00}";
        }

        public void ShowWallNoise(float intensity = 0.3f)
        {
            if (!TryEmit(FinalDemoFeedbackPriority.WallNoise))
            {
                return;
            }

            HardwareBridge bridge = ResolveBridge();
            if (outputToHardware && bridge != null)
            {
                bridge.SendLedWallNoise(7.5f, 3.2f, 15.5f, 5.8f, wallNoiseColor, Mathf.Clamp01(intensity), ++_seed, 0.55f, 1.6f);
            }

            RenderWallNoiseLogicalFrame(Mathf.Clamp01(intensity));
            _lastAction = $"Wall noise b={Mathf.Clamp01(intensity):0.00}";
        }

        public void Clear()
        {
            HardwareBridge bridge = ResolveBridge();
            if (outputToHardware && bridge != null)
            {
                bridge.ClearLedDisplay();
            }

            _holdUntilRealtime = 0f;
            ClearLogicalFrame();
            _lastAction = "LED clear.";
        }

        public void CopyLogicalLedFrame(Color[] destination)
        {
            if (destination == null)
            {
                return;
            }

            int copyLength = Mathf.Min(destination.Length, _logicalFrame.Length);
            for (int i = 0; i < copyLength; i++)
            {
                destination[i] = _logicalFrame[i];
            }
        }

        public Color GetLogicalLedPixel(int x, int y)
        {
            if (x < 0 || x >= LogicalFrameWidth || y < 0 || y >= LogicalFrameHeight)
            {
                return Color.black;
            }

            return _logicalFrame[(y * LogicalFrameWidth) + x];
        }

        private bool TryMapWorldPosition(Vector3 worldPosition, float intensity, out BellRingerLedDotFrame frame, float brightnessBoost = 1f, float maxDistanceScale = 1f)
        {
            Transform listener = listenerTransform != null ? listenerTransform : Camera.main != null ? Camera.main.transform : null;
            if (listener == null)
            {
                frame = default;
                return false;
            }

            Vector3 localTargetPosition = listener.InverseTransformPoint(worldPosition);
            return TryMapLocalPosition(localTargetPosition, intensity, out frame, brightnessBoost, maxDistanceScale);
        }

        private bool TryMapLocalPosition(Vector3 localTargetPosition, float intensity, out BellRingerLedDotFrame frame, float brightnessBoost = 1f, float maxDistanceScale = 1f)
        {
            localTargetPosition.x *= Mathf.Max(0.1f, horizontalPositionWeight);
            float boost = finalDemoBrightnessBoost * Mathf.Max(0.1f, brightnessBoost);

            return BellRingerAudioLedMapper.TryMapFromLocalPosition(
                localTargetPosition,
                0.25f,
                defaultMaxDistance * Mathf.Max(0.1f, maxDistanceScale),
                Mathf.Clamp01(intensity),
                boost,
                1f,
                0.04f,
                85f,
                55f,
                out frame);
        }

        private static float EvaluateTinnitusPulse(float time, float rate)
        {
            float phase = Mathf.Repeat(time * Mathf.Clamp(rate, 0.1f, 3f), 1f);
            float wave = 0.5f - Mathf.Cos(phase * Mathf.PI * 2f) * 0.5f;
            return Mathf.SmoothStep(0f, 1f, wave);
        }

        private void ClearMappedOutput(FinalDemoFeedbackPriority priority, string reason)
        {
            if (!clearWhenMappedOutsideBoard || !TryEmit(priority))
            {
                _lastAction = reason;
                return;
            }

            if (priority > FinalDemoFeedbackPriority.Rain && TryRestoreRainFrame(reason))
            {
                return;
            }

            HardwareBridge bridge = ResolveBridge();
            if (outputToHardware && bridge != null)
            {
                bridge.ClearLedDisplay();
            }

            ClearLogicalFrame();
            HoldPriority(priority);
            _lastAction = reason;
        }

        private bool TryRestoreRainFrame(string reason)
        {
            if (Time.realtimeSinceStartup > _rainFrameActiveUntilRealtime || !FrameHasVisiblePixels(_rainFrame))
            {
                return false;
            }

            CopyFrame(_rainFrame, _logicalFrame);
            SendFrameToHardware(_logicalFrame, _rainFrame);
            _logicalFrameVersion++;
            _lastAction = $"Rain restored. {reason}";
            return true;
        }

        private bool TryEmit(FinalDemoFeedbackPriority priority)
        {
            return Time.realtimeSinceStartup > _holdUntilRealtime || priority >= _heldPriority;
        }

        private void HoldPriority(FinalDemoFeedbackPriority priority)
        {
            _heldPriority = priority;
            _holdUntilRealtime = Time.realtimeSinceStartup + highPriorityHoldSeconds;
        }

        private void ClearLogicalFrame()
        {
            ClearFrame(_logicalFrame);
            _logicalFrameVersion++;
        }

        private static void ClearFrame(Color[] frame)
        {
            if (frame == null)
            {
                return;
            }

            for (int i = 0; i < frame.Length; i++)
            {
                frame[i] = Color.black;
            }
        }

        private static void CopyFrame(Color[] source, Color[] destination)
        {
            if (source == null || destination == null)
            {
                return;
            }

            int copyLength = Mathf.Min(source.Length, destination.Length);
            for (int i = 0; i < copyLength; i++)
            {
                destination[i] = source[i];
            }
        }

        private bool TrySendCurrentFrameWithRainComposite(FinalDemoFeedbackPriority priority)
        {
            _ = priority;
            return false;
        }

        private void SendFrameToHardware(Color[] frame, Color[] rainReference)
        {
            HardwareBridge bridge = ResolveBridge();
            if (!outputToHardware || bridge == null || frame == null)
            {
                return;
            }

            int copyLength = Mathf.Min(frame.Length, _hardwareFrame.Length);
            for (int i = 0; i < copyLength; i++)
            {
                Color pixel = frame[i];
                bool rainOnly = rainReference != null &&
                                i < rainReference.Length &&
                                IsVisiblePixel(rainReference[i]) &&
                                ApproximatelySameColor(pixel, rainReference[i]);
                _hardwareFrame[i] = rainOnly ? ScaleRainHardwarePixel(pixel) : ClampPixel(pixel);
            }

            for (int i = copyLength; i < _hardwareFrame.Length; i++)
            {
                _hardwareFrame[i] = Color.black;
            }

            bridge.SendLedFrame(_hardwareFrame);
        }

        private Color ScaleRainHardwarePixel(Color pixel)
        {
            Color scaled = pixel * rainHardwareBrightnessMultiplier;
            scaled.r *= rainHardwareColorMultiplier;
            scaled.g *= rainHardwareColorMultiplier;
            scaled.b *= rainHardwareColorMultiplier;
            scaled.a = 1f;

            float maxComponent = Mathf.Max(scaled.r, Mathf.Max(scaled.g, scaled.b));
            if (maxComponent <= 0.001f)
            {
                return Color.black;
            }

            float cap = Mathf.Max(0.001f, rainHardwareMaximumBrightness);
            if (maxComponent > cap)
            {
                scaled *= cap / maxComponent;
                scaled.a = 1f;
            }

            float minimum = Mathf.Clamp(rainHardwareMinimumVisibleBrightness, 0f, cap);
            maxComponent = Mathf.Max(scaled.r, Mathf.Max(scaled.g, scaled.b));
            if (minimum > 0f && maxComponent > 0.001f && maxComponent < minimum)
            {
                scaled *= minimum / maxComponent;
                scaled.a = 1f;
            }

            return ClampPixel(scaled);
        }

        private static Color ClampPixel(Color pixel)
        {
            pixel.r = Mathf.Clamp01(pixel.r);
            pixel.g = Mathf.Clamp01(pixel.g);
            pixel.b = Mathf.Clamp01(pixel.b);
            pixel.a = 1f;
            return pixel;
        }

        private static bool IsVisiblePixel(Color pixel)
        {
            return Mathf.Max(pixel.r, Mathf.Max(pixel.g, pixel.b)) > 0.01f;
        }

        private static bool ApproximatelySameColor(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) < 0.002f &&
                   Mathf.Abs(a.g - b.g) < 0.002f &&
                   Mathf.Abs(a.b - b.b) < 0.002f;
        }

        private static bool FrameHasVisiblePixels(Color[] frame)
        {
            if (frame == null)
            {
                return false;
            }

            for (int i = 0; i < frame.Length; i++)
            {
                if (IsVisiblePixel(frame[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private void RenderBellLogicalFrame(BellRingerLedDotFrame frame, bool miniRipple, Color color, bool clear = true)
        {
            if (clear)
            {
                ClearLogicalFrame();
            }

            SetPixel(frame.x, frame.y, color, frame.brightnessNormalized);
            if (!miniRipple)
            {
                AddPixel(frame.x, frame.y + 1, color, frame.brightnessNormalized * 0.45f);
                AddPixel(frame.x, frame.y - 1, color, frame.brightnessNormalized * 0.25f);
                return;
            }

            AddPixel(frame.x - 1, frame.y, color, frame.brightnessNormalized * 0.38f);
            AddPixel(frame.x + 1, frame.y, color, frame.brightnessNormalized * 0.38f);
            AddPixel(frame.x, frame.y - 1, color, frame.brightnessNormalized * 0.24f);
            AddPixel(frame.x, frame.y + 1, color, frame.brightnessNormalized * 0.24f);
        }

        private void RenderBellWaveLogicalFrame(BellRingerLedDotFrame frame, float radius, float width, Color color, float brightness, bool clear = true)
        {
            if (clear)
            {
                ClearLogicalFrame();
            }

            float halfWidth = Mathf.Max(0.12f, width * 0.5f);
            for (int y = 0; y < LogicalFrameHeight; y++)
            {
                for (int x = 0; x < LogicalFrameWidth; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(frame.x, frame.y));
                    float core = Mathf.Exp(-(distance * distance) / 0.9f);
                    float ring = Mathf.Clamp01(1f - Mathf.Abs(distance - radius) / halfWidth);
                    float alpha = Mathf.Max(core * 0.42f, ring * 0.72f);
                    if (alpha <= 0.01f)
                    {
                        continue;
                    }

                    AddPixel(x, y, color, brightness * alpha);
                }
            }
        }

        private void RenderPadLogicalFrame(BellRingerLedDotFrame frame, Color color)
        {
            ClearLogicalFrame();
            SetPixel(frame.x, frame.y, color, frame.brightnessNormalized);
            AddPixel(frame.x - 1, frame.y, color, frame.brightnessNormalized * 0.18f);
            AddPixel(frame.x + 1, frame.y, color, frame.brightnessNormalized * 0.18f);
        }

        private bool TryBuildRainFrame(
            float intensity,
            Color[] destination,
            out float centerX,
            out float centerY,
            out float width,
            out float minimalHeight,
            out float hardwareLevel,
            out int seed,
            out float phase,
            out float density,
            out string debug)
        {
            centerX = 7.5f;
            centerY = 0.5f;
            width = 16f;
            minimalHeight = 0f;
            hardwareLevel = 0f;
            seed = 0;
            phase = 0f;
            density = 0f;
            ClearFrame(destination);
            if (destination == null || destination.Length == 0)
            {
                debug = "Rain skipped: frame missing.";
                return false;
            }

            if (!TryProjectRainBand(out centerX, out centerY, out width, out float height, out float visibility))
            {
                debug = "Rain skipped: outside rain band.";
                return false;
            }

            seed = Mathf.FloorToInt(Time.realtimeSinceStartup * rainSeedRate);
            _seed = seed;
            phase = Time.realtimeSinceStartup * rainPhaseSpeed;
            density = Mathf.Clamp01(rainDensity);
            hardwareLevel = Mathf.Clamp01(rainBrightness * Mathf.Clamp01(intensity) * visibility);
            float previewLevel = Mathf.Clamp01(hardwareLevel * rainPreviewBrightnessBoost);
            minimalHeight = Mathf.Max(0.6f, height * Mathf.Lerp(0.55f, 1f, density));
            centerY = Mathf.Clamp((minimalHeight - 1f) * 0.5f, 0f, 7f);
            float halfWidth = width * 0.5f;
            float halfHeight = minimalHeight * 0.5f;
            int dropCount = Mathf.Clamp(Mathf.RoundToInt(2f + density * 5f), 2, 7);

            for (int y = 0; y < LogicalFrameHeight; y++)
            {
                for (int x = 0; x < LogicalFrameWidth; x++)
                {
                    float dxArea = Mathf.Abs(x - centerX);
                    float dyArea = Mathf.Abs(y - centerY);
                    if (dxArea > halfWidth || dyArea > halfHeight)
                    {
                        continue;
                    }

                    float alpha = 0f;
                    for (int drop = 0; drop < dropCount; drop++)
                    {
                        float dropX = Mathf.Lerp(centerX - halfWidth, centerX + halfWidth, Hash01(seed + drop * 19, 3, 11));
                        float dropY = Mathf.Lerp(centerY - halfHeight, centerY + halfHeight, Hash01(seed + drop * 23, 7, 5));
                        float radius = Mathf.Repeat((phase * 1.15f) + Hash01(seed + drop * 29, 13, 17) * 2.8f, 2.8f);
                        float distance = Vector2.Distance(new Vector2(x, y), new Vector2(dropX, dropY));
                        alpha = Mathf.Max(alpha, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(1f - Mathf.Abs(distance - radius) / 1.15f)));
                    }

                    float edgeX = width >= 15.9f ? 1f : Mathf.Clamp01((halfWidth - dxArea) / 1.5f);
                    float edgeY = Mathf.Clamp01(((centerY + halfHeight) - y) / 1.25f);
                    float brightness = BellRingerLightStyle.ContrastAlpha(alpha, rainPeakContrast) * edgeX * edgeY * previewLevel;
                    AddFramePixel(destination, x, y, rainColor, brightness);
                }
            }

            debug = $"rain level={hardwareLevel:0.000} density={density:0.00} drops={dropCount} h={minimalHeight:0.00} vis={visibility:0.00} seed={seed}";
            return true;
        }

        private bool TryProjectRainBand(out float centerX, out float centerY, out float width, out float height, out float visibility)
        {
            centerX = 7.5f;
            centerY = 0.5f;
            width = 16f;
            height = 0f;
            visibility = 0f;

            Transform listener = listenerTransform != null ? listenerTransform : Camera.main != null ? Camera.main.transform : null;
            if (listener == null)
            {
                return false;
            }

            float forwardY = listener.forward.y;
            float skySuppression = Mathf.Clamp01(1f - Mathf.InverseLerp(0.18f, 0.7f, forwardY));
            if (skySuppression <= 0.01f)
            {
                return false;
            }

            float lookDown = Mathf.Clamp01(-forwardY);
            height = Mathf.Lerp(Mathf.Max(1f, rainNeutralRows), Mathf.Max(rainNeutralRows, rainLookDownRows), lookDown) * skySuppression;
            if (height < 0.6f)
            {
                return false;
            }

            height = Mathf.Clamp(height, 0.6f, 5f);
            centerY = Mathf.Clamp((height - 1f) * 0.5f, 0f, 7f);
            visibility = Mathf.Clamp01(skySuppression * Mathf.Lerp(0.72f, 1f, lookDown));
            return visibility > 0.01f && height > 0.25f;
        }

        private void RenderTinnitusLogicalFrame(BellRingerLedDotFrame frame, bool boss, Color color, float sizeScale)
        {
            ClearLogicalFrame();
            float center = frame.brightnessNormalized;
            SetPixel(frame.x, frame.y, color, center);
            if (sizeScale <= 0.01f)
            {
                return;
            }

            AddPixel(frame.x - 1, frame.y, color, center * (boss ? 0.55f : 0.35f) * sizeScale);
            AddPixel(frame.x + 1, frame.y, color, center * (boss ? 0.55f : 0.35f) * sizeScale);
            AddPixel(frame.x, frame.y - 1, color, center * (boss ? 0.45f : 0.25f) * sizeScale);
            AddPixel(frame.x, frame.y + 1, color, center * (boss ? 0.45f : 0.25f) * sizeScale);
            if (!boss || sizeScale < 0.75f)
            {
                return;
            }

            AddPixel(frame.x - 2, frame.y + 1, color, center * 0.32f * sizeScale);
            AddPixel(frame.x + 2, frame.y - 1, color, center * 0.32f * sizeScale);
            AddPixel(frame.x, frame.y + 2, color, center * 0.24f * sizeScale);
        }

        private void RenderTinnitusPatternLogicalFrame(Vector2 center, float size, float tearStrength, Vector2 axis, Color color, float level, float effectiveInstability, float smearDecay)
        {
            ClearLogicalFrame();
            Vector2 normalizedAxis = axis.sqrMagnitude <= 0.001f ? Vector2.right : axis.normalized;
            Vector2 perpendicular = new Vector2(-normalizedAxis.y, normalizedAxis.x);
            float coreSigma = Mathf.Max(0.18f, size);
            float lobeSigma = Mathf.Lerp(0.35f, 0.7f, effectiveInstability);
            float smear = Mathf.Max(0.1f, smearDecay);

            for (int y = 0; y < LogicalFrameHeight; y++)
            {
                for (int x = 0; x < LogicalFrameWidth; x++)
                {
                    Vector2 offset = new Vector2(x, y) - center;
                    float distanceSquared = offset.sqrMagnitude;
                    float core = Mathf.Exp(-distanceSquared / (2f * coreSigma * coreSigma));
                    float along = Vector2.Dot(offset, normalizedAxis);
                    float across = Mathf.Abs(Vector2.Dot(offset, perpendicular));
                    float splitDistance = Mathf.Abs(Mathf.Abs(along) - tearStrength);
                    float tear = Mathf.Exp(-(splitDistance * splitDistance) / (2f * lobeSigma * lobeSigma)) *
                                 Mathf.Exp(-(across * across) / (2f * 0.32f * 0.32f)) *
                                 effectiveInstability;
                    float residualSmear = Mathf.Exp(-Mathf.Abs(along) / Mathf.Max(0.1f, tearStrength + smear)) *
                                          Mathf.Exp(-(across * across) / (2f * 0.75f * 0.75f)) *
                                          effectiveInstability * 0.12f;
                    float alpha = BellRingerLightStyle.ContrastAlpha(Mathf.Max(core, Mathf.Max(tear, residualSmear)), 1.2f) * level;
                    AddPixel(x, y, color, alpha);
                }
            }
        }

        private void RenderWallNoiseLogicalFrame(float intensity)
        {
            ClearLogicalFrame();
            for (int y = 2; y < LogicalFrameHeight; y++)
            {
                for (int x = 0; x < LogicalFrameWidth; x++)
                {
                    float noise = Mathf.Abs(Mathf.Sin((_seed * 0.61f) + x * 1.37f + y * 2.21f));
                    if (noise < 0.48f)
                    {
                        continue;
                    }

                    SetPixel(x, y, wallNoiseColor, intensity * noise * 0.75f);
                }
            }
        }

        private void SetPixel(int x, int y, Color color, float brightness)
        {
            if (x < 0 || x >= LogicalFrameWidth || y < 0 || y >= LogicalFrameHeight)
            {
                return;
            }

            Color pixel = color * Mathf.Clamp01(brightness);
            pixel.a = 1f;
            _logicalFrame[(y * LogicalFrameWidth) + x] = pixel;
        }

        private void AddPixel(int x, int y, Color color, float brightness)
        {
            if (x < 0 || x >= LogicalFrameWidth || y < 0 || y >= LogicalFrameHeight)
            {
                return;
            }

            int index = (y * LogicalFrameWidth) + x;
            _logicalFrame[index] += color * Mathf.Clamp01(brightness);
            _logicalFrame[index].r = Mathf.Clamp01(_logicalFrame[index].r);
            _logicalFrame[index].g = Mathf.Clamp01(_logicalFrame[index].g);
            _logicalFrame[index].b = Mathf.Clamp01(_logicalFrame[index].b);
            _logicalFrame[index].a = 1f;
        }

        private void AddFramePixel(Color[] frame, int x, int y, Color color, float brightness)
        {
            if (frame == null || x < 0 || x >= LogicalFrameWidth || y < 0 || y >= LogicalFrameHeight)
            {
                return;
            }

            int index = (y * LogicalFrameWidth) + x;
            frame[index] += color * Mathf.Clamp01(brightness);
            frame[index].r = Mathf.Clamp01(frame[index].r);
            frame[index].g = Mathf.Clamp01(frame[index].g);
            frame[index].b = Mathf.Clamp01(frame[index].b);
            frame[index].a = 1f;
        }

        private static float Hash01(int seed, int x, int y)
        {
            uint value = (uint)seed;
            value ^= (uint)(x + 37) * 1103515245u;
            value ^= (uint)(y + 101) * 12345u;
            value ^= value >> 16;
            value *= 2246822519u;
            value ^= value >> 13;
            return (value & 0x00FFFFFF) / 16777215f;
        }

        private HardwareBridge ResolveBridge()
        {
            if (hardwareBridge == null ||
                (!hardwareBridge.IsConnected && HardwareBridge.Instance != null && HardwareBridge.Instance.IsConnected))
            {
                hardwareBridge = HardwareBridge.Instance ?? FindFirstObjectByType<HardwareBridge>();
            }

            return hardwareBridge;
        }
    }
}
