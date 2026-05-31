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
        [SerializeField] private bool clearWhenMappedOutsideBoard = true;

        private FinalDemoFeedbackPriority _heldPriority = FinalDemoFeedbackPriority.Rain;
        private float _holdUntilRealtime;
        private int _seed;
        private string _lastAction = "(idle)";
        private readonly Color[] _logicalFrame = new Color[BellRingerAudioLedMapper.DisplayWidth * BellRingerAudioLedMapper.DisplayHeight];
        private int _logicalFrameVersion;

        public bool OutputToHardware => outputToHardware;
        public string LastAction => _lastAction;
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

            RenderBellLogicalFrame(frame, miniRipple, bellColor);
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

            HardwareBridge bridge = ResolveBridge();
            if (outputToHardware && bridge != null)
            {
                bridge.SendLedPulseCore(frame.centerX, frame.centerY, 0.18f, 0.45f, 0.48f, bellColor, frame.brightnessNormalized, 1.15f);
            }

            RenderBellLogicalFrame(frame, false, bellColor);
            HoldPriority(FinalDemoFeedbackPriority.Bell);
            _lastAction = $"Bell anchor x={frame.centerX:0.00} y={frame.centerY:0.00} b={frame.brightnessNormalized:0.00}";
        }

        public void ShowPadAnchor(Vector3 worldPosition, float intensity = 0.22f)
        {
            if (!TryMapWorldPosition(worldPosition, intensity, out BellRingerLedDotFrame frame, padAnchorBrightnessBoost))
            {
                ClearMappedOutput(FinalDemoFeedbackPriority.Pad, "Pad anchor out of board.");
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

            HardwareBridge bridge = ResolveBridge();
            if (outputToHardware && bridge != null)
            {
                bridge.SendLedPulseCore(frame.centerX, frame.centerY, radius, core, width, bellColor, brightness, 0.62f + onset * 0.04f);
            }

            RenderBellWaveLogicalFrame(frame, radius, width, bellColor, brightness);
            HoldPriority(FinalDemoFeedbackPriority.Bell);
            _lastAction = $"Bell wave x={frame.x} y={frame.y} env={envelope:0.00} b={brightness:0.00}";
        }

        public void ShowRainFloorBand(float intensity = 0.45f)
        {
            if (!TryEmit(FinalDemoFeedbackPriority.Rain))
            {
                _lastAction = $"Rain yielded to {HeldPriority}.";
                return;
            }

            HardwareBridge bridge = ResolveBridge();
            if (outputToHardware && bridge != null)
            {
                bridge.SendLedRain(rainColor, Mathf.Clamp01(intensity), ++_seed, 7.5f, 0.65f, 16f, 2.4f, Time.realtimeSinceStartup, 0.9f, 1.75f);
            }

            RenderRainLogicalFrame(Mathf.Clamp01(intensity));
            _lastAction = $"Rain floor band b={Mathf.Clamp01(intensity):0.00}";
        }

        public void ShowTinnitusPoint(Vector3 worldPosition, float intensity = 0.65f, bool boss = false)
        {
            float scaledIntensity = intensity * (boss ? bossTinnitusLightScale : tinnitusLightScale);
            if (!TryMapWorldPosition(worldPosition, scaledIntensity, out BellRingerLedDotFrame frame))
            {
                ClearMappedOutput(boss ? FinalDemoFeedbackPriority.CriticalPad : FinalDemoFeedbackPriority.Tinnitus, $"{(boss ? "Boss" : "Tinnitus")} point out of board.");
                return;
            }

            FinalDemoFeedbackPriority priority = boss ? FinalDemoFeedbackPriority.CriticalPad : FinalDemoFeedbackPriority.Tinnitus;
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
                    boss ? 1.65f : 1.05f,
                    boss ? 3.2f : 1.6f,
                    boss ? 0.65f : 1f,
                    boss ? 0.35f : 0f,
                    tinnitusColor,
                    frame.brightnessNormalized,
                    ++_seed,
                    boss ? 0.78f : 0.45f,
                    boss ? 2.6f : 1.6f,
                    boss ? 1.85f : 1.35f);
            }

            RenderTinnitusLogicalFrame(frame, boss, tinnitusColor);
            HoldPriority(priority);
            _lastAction = $"{(boss ? "Boss" : "Tinnitus")} light x={frame.x} y={frame.y} b={frame.brightnessNormalized:0.00}";
        }

        public void ShowTinnitusWave(Vector3 worldPosition, float envelope01, float onset01, float intensityScale = 1f, bool boss = false)
        {
            float envelope = Mathf.Clamp01(envelope01);
            float onset = Mathf.Clamp01(onset01);
            float brightness = Mathf.Clamp01((0.12f + envelope * 0.62f + onset * 0.06f) * Mathf.Clamp01(intensityScale));
            brightness *= boss ? bossTinnitusLightScale : tinnitusLightScale;
            if (!TryMapWorldPosition(worldPosition, brightness, out BellRingerLedDotFrame frame))
            {
                ClearMappedOutput(boss ? FinalDemoFeedbackPriority.CriticalPad : FinalDemoFeedbackPriority.Tinnitus, $"{(boss ? "Boss" : "Tinnitus")} wave out of board.");
                return;
            }
            brightness = frame.brightnessNormalized;

            FinalDemoFeedbackPriority priority = boss ? FinalDemoFeedbackPriority.CriticalPad : FinalDemoFeedbackPriority.Tinnitus;
            if (!TryEmit(priority))
            {
                return;
            }

            float coreSize = boss
                ? Mathf.Lerp(1.2f, 2.35f, envelope)
                : Mathf.Lerp(0.65f, 1.28f, envelope);
            float tearAmount = boss
                ? Mathf.Lerp(0.9f, 3.15f, envelope) + onset * 0.35f
                : Mathf.Lerp(0.35f, 1.55f, envelope) + onset * 0.22f;
            float instability = Mathf.Clamp01(envelope * 0.55f + onset * 0.16f);
            float axisX = Mathf.Sin((_seed + 1) * 0.73f);
            float axisY = boss ? Mathf.Cos((_seed + 3) * 0.47f) * 0.55f : 0f;
            float smear = boss ? Mathf.Lerp(1.7f, 3.4f, envelope) : Mathf.Lerp(1.1f, 2.2f, envelope);

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

            RenderTinnitusLogicalFrame(frame, boss, tinnitusColor);
            HoldPriority(priority);
            _lastAction = $"{(boss ? "Boss" : "Tinnitus")} wave x={frame.x} y={frame.y} env={envelope:0.00} b={brightness:0.00}";
        }

        public void ShowTinnitusPattern(Vector3 worldPosition, float intensity = 0.65f, float cleanseStability = 0f, bool boss = false, float rangeScale = 1f)
        {
            float scaledIntensity = Mathf.Clamp01(intensity) * (boss ? bossTinnitusLightScale : tinnitusLightScale);
            if (!TryMapWorldPosition(worldPosition, scaledIntensity, out BellRingerLedDotFrame frame, 1f, rangeScale))
            {
                ClearMappedOutput(boss ? FinalDemoFeedbackPriority.CriticalPad : FinalDemoFeedbackPriority.Tinnitus, $"{(boss ? "Boss" : "Tinnitus")} pattern out of board.");
                return;
            }

            FinalDemoFeedbackPriority priority = boss ? FinalDemoFeedbackPriority.CriticalPad : FinalDemoFeedbackPriority.Tinnitus;
            if (!TryEmit(priority))
            {
                return;
            }

            float stability = Mathf.Clamp01(cleanseStability);
            float pulse = EvaluateTinnitusPulse(Time.realtimeSinceStartup, boss ? 0.72f : 0.95f);
            float softLevel = Mathf.Clamp01(0.48f + pulse * 0.52f);
            Vector2 axis = boss ? new Vector2(1f, 0.42f).normalized : Vector2.right;
            float coreSize = (boss ? 1.56f : 1.08f) * Mathf.Lerp(1f, 0.82f, stability);
            float tearStrength = (boss ? 2.35f : 1.42f) * (1f - stability) * Mathf.Lerp(0.38f, 1f, pulse);
            float level = Mathf.Clamp01(frame.brightnessNormalized * softLevel);
            float instability = Mathf.Clamp01((1f - stability) * (boss ? 0.52f : 0.36f));
            float smear = boss ? 2.35f : 1.55f;

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

            HardwareBridge bridge = ResolveBridge();
            if (outputToHardware && bridge != null)
            {
                bridge.ClearLedDisplay();
            }

            ClearLogicalFrame();
            HoldPriority(priority);
            _lastAction = reason;
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
            for (int i = 0; i < _logicalFrame.Length; i++)
            {
                _logicalFrame[i] = Color.black;
            }

            _logicalFrameVersion++;
        }

        private void RenderBellLogicalFrame(BellRingerLedDotFrame frame, bool miniRipple, Color color)
        {
            ClearLogicalFrame();
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

        private void RenderBellWaveLogicalFrame(BellRingerLedDotFrame frame, float radius, float width, Color color, float brightness)
        {
            ClearLogicalFrame();
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

        private void RenderRainLogicalFrame(float intensity)
        {
            ClearLogicalFrame();
            for (int y = 0; y < LogicalFrameHeight; y++)
            {
                float rowWeight = y switch
                {
                    0 => 1f,
                    1 => 0.9f,
                    2 => 0.62f,
                    3 => 0.35f,
                    _ => 0f,
                };
                if (rowWeight <= 0f)
                {
                    continue;
                }

                for (int x = 0; x < LogicalFrameWidth; x++)
                {
                    float streak = 0.7f + Mathf.Abs(Mathf.Sin((_seed * 0.37f) + x * 0.75f + y * 1.2f)) * 0.3f;
                    SetPixel(x, y, rainColor, intensity * rowWeight * streak);
                }
            }
        }

        private void RenderTinnitusLogicalFrame(BellRingerLedDotFrame frame, bool boss, Color color)
        {
            ClearLogicalFrame();
            float center = frame.brightnessNormalized;
            SetPixel(frame.x, frame.y, color, center);
            AddPixel(frame.x - 1, frame.y, color, center * (boss ? 0.55f : 0.35f));
            AddPixel(frame.x + 1, frame.y, color, center * (boss ? 0.55f : 0.35f));
            AddPixel(frame.x, frame.y - 1, color, center * (boss ? 0.45f : 0.25f));
            AddPixel(frame.x, frame.y + 1, color, center * (boss ? 0.45f : 0.25f));
            if (!boss)
            {
                return;
            }

            AddPixel(frame.x - 2, frame.y + 1, color, center * 0.32f);
            AddPixel(frame.x + 2, frame.y - 1, color, center * 0.32f);
            AddPixel(frame.x, frame.y + 2, color, center * 0.24f);
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
