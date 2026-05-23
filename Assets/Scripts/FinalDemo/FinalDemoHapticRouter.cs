using UnityEngine;
using UnityEngine.InputSystem;

namespace BellRinger.FinalDemo
{
    [DisallowMultipleComponent]
    public sealed class FinalDemoHapticRouter : MonoBehaviour
    {
        [SerializeField] private float refreshIntervalSeconds = 0.06f;

        private FinalDemoFeedbackPriority _activePriority = FinalDemoFeedbackPriority.Rain;
        private float _activeUntilRealtime;
        private float _nextRefreshRealtime;
        private float _lowMotor;
        private float _highMotor;
        private bool _continuous;
        private string _lastAction = "(idle)";

        public string LastAction => _lastAction;
        public bool HapticsActive => Time.realtimeSinceStartup <= _activeUntilRealtime;
        public bool Continuous => _continuous;

        private void Update()
        {
            if (!HapticsActive)
            {
                if (_continuous)
                {
                    StopAllHaptics();
                }

                return;
            }

            if (Time.realtimeSinceStartup >= _nextRefreshRealtime)
            {
                ApplyHaptics(_lowMotor, _highMotor);
                _nextRefreshRealtime = Time.realtimeSinceStartup + Mathf.Max(0.01f, refreshIntervalSeconds);
            }
        }

        private void OnDisable()
        {
            StopAllHaptics();
        }

        private void OnDestroy()
        {
            StopAllHaptics();
        }

        public void TriggerBellAssistPulse()
        {
            Pulse(0.22f, 0.42f, 0.16f, FinalDemoFeedbackPriority.Bell, "bell assist pulse");
        }

        public void TriggerBellAcquisitionPulse()
        {
            Pulse(0.55f, 0.82f, 0.32f, FinalDemoFeedbackPriority.CriticalPad, "bell acquisition pulse");
        }

        public void TriggerTinnitusLockPulse()
        {
            Pulse(0.78f, 0.95f, 0.18f, FinalDemoFeedbackPriority.CriticalPad, "tinnitus lock pulse");
        }

        public void StartTinnitusCleanseHum(float intensity = 0.55f, float seconds = 1.8f)
        {
            float clampedIntensity = Mathf.Clamp01(intensity);
            Pulse(0.18f + clampedIntensity * 0.25f, 0.26f + clampedIntensity * 0.32f, seconds, FinalDemoFeedbackPriority.CriticalPad, "tinnitus cleanse hum");
            _continuous = true;
        }

        public void TriggerBossTrackingPulse()
        {
            Pulse(0.42f, 0.62f, 0.22f, FinalDemoFeedbackPriority.CriticalPad, "boss tracking pulse");
        }

        public void TriggerBossFailurePulse()
        {
            Pulse(0.12f, 0.88f, 0.14f, FinalDemoFeedbackPriority.CriticalPad, "boss failure pulse");
        }

        public void TriggerBossHitPulse()
        {
            Pulse(0.85f, 1f, 0.26f, FinalDemoFeedbackPriority.CriticalPad, "boss hit pulse");
        }

        public void StopAllHaptics()
        {
            try
            {
                InputSystem.ResetHaptics();
            }
            catch
            {
            }

            _activeUntilRealtime = 0f;
            _continuous = false;
            _lowMotor = 0f;
            _highMotor = 0f;
            _lastAction = "haptics stopped";
        }

        private void Pulse(float lowMotor, float highMotor, float seconds, FinalDemoFeedbackPriority priority, string label)
        {
            if (HapticsActive && priority < _activePriority)
            {
                _lastAction = $"{label} ignored by higher priority {_activePriority}";
                return;
            }

            _activePriority = priority;
            _activeUntilRealtime = Time.realtimeSinceStartup + Mathf.Max(0.01f, seconds);
            _lowMotor = Mathf.Clamp01(lowMotor);
            _highMotor = Mathf.Clamp01(highMotor);
            _continuous = false;
            ApplyHaptics(_lowMotor, _highMotor);
            _lastAction = $"{label} {_lowMotor:0.00}/{_highMotor:0.00}";
        }

        private void ApplyHaptics(float lowMotor, float highMotor)
        {
            Gamepad gamepad = Gamepad.current ?? (Gamepad.all.Count > 0 ? Gamepad.all[0] : null);
            if (gamepad == null)
            {
                _lastAction = "no gamepad for haptics";
                return;
            }

            try
            {
                InputSystem.ResumeHaptics();
                gamepad.SetMotorSpeeds(Mathf.Clamp01(lowMotor), Mathf.Clamp01(highMotor));
            }
            catch (System.Exception exception)
            {
                _lastAction = $"haptics failed: {exception.Message}";
            }
        }
    }
}
