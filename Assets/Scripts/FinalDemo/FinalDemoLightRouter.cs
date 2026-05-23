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
        [SerializeField] private Color rainColor = new Color(0.02f, 0.08f, 1f);
        [SerializeField] private Color tinnitusColor = new Color(0.55f, 0.08f, 1f);
        [SerializeField] private Color wallNoiseColor = new Color(0.06f, 0.78f, 0.9f);
        [SerializeField] private float highPriorityHoldSeconds = 0.35f;
        [SerializeField] private float defaultMaxDistance = 6f;

        private FinalDemoFeedbackPriority _heldPriority = FinalDemoFeedbackPriority.Rain;
        private float _holdUntilRealtime;
        private int _seed;
        private string _lastAction = "(idle)";

        public bool OutputToHardware => outputToHardware;
        public string LastAction => _lastAction;
        public FinalDemoFeedbackPriority HeldPriority => Time.realtimeSinceStartup <= _holdUntilRealtime ? _heldPriority : FinalDemoFeedbackPriority.Rain;

        public void Initialize(Transform listener, HardwareBridge bridge)
        {
            listenerTransform = listener;
            hardwareBridge = bridge;
        }

        public void ShowBellPoint(Vector3 worldPosition, float intensity = 1f, bool miniRipple = true)
        {
            if (!TryMapWorldPosition(worldPosition, intensity, out BellRingerLedDotFrame frame))
            {
                frame = new BellRingerLedDotFrame(8, 4, Mathf.Clamp01(intensity));
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
                    bridge.SendLedRipple(frame.x, frame.y, 1.7f, 0.75f, bellColor, frame.brightnessNormalized);
                }
                else
                {
                    bridge.SendLedPulseCore(frame.x, frame.y, 0.55f, 0.8f, 0.6f, bellColor, frame.brightnessNormalized, 1.4f);
                }
            }

            HoldPriority(FinalDemoFeedbackPriority.Bell);
            _lastAction = $"Bell light x={frame.x} y={frame.y} b={frame.brightnessNormalized:0.00}";
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

            _lastAction = $"Rain floor band b={Mathf.Clamp01(intensity):0.00}";
        }

        public void ShowTinnitusPoint(Vector3 worldPosition, float intensity = 0.65f, bool boss = false)
        {
            if (!TryMapWorldPosition(worldPosition, intensity, out BellRingerLedDotFrame frame))
            {
                frame = new BellRingerLedDotFrame(8, 4, Mathf.Clamp01(intensity));
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
                    frame.x,
                    frame.y,
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

            HoldPriority(priority);
            _lastAction = $"{(boss ? "Boss" : "Tinnitus")} light x={frame.x} y={frame.y} b={frame.brightnessNormalized:0.00}";
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
            _lastAction = "LED clear.";
        }

        private bool TryMapWorldPosition(Vector3 worldPosition, float intensity, out BellRingerLedDotFrame frame)
        {
            Transform listener = listenerTransform != null ? listenerTransform : Camera.main != null ? Camera.main.transform : null;
            return BellRingerAudioLedMapper.TryMap(
                listener,
                worldPosition,
                0.25f,
                defaultMaxDistance,
                Mathf.Clamp01(intensity),
                1f,
                0.8f,
                0.1f,
                85f,
                55f,
                out frame);
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

        private HardwareBridge ResolveBridge()
        {
            hardwareBridge ??= HardwareBridge.Instance ?? FindFirstObjectByType<HardwareBridge>();
            return hardwareBridge;
        }
    }
}
