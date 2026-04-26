using System;
using BellRinger.Hardware;
using UnityEngine;

namespace BellRinger.Debug
{
    [DisallowMultipleComponent]
    public sealed class BellRingerHardwareConnectionProbe : MonoBehaviour
    {
        public const string DisableProbeEnvName = "BELL_RINGER_DISABLE_CONNECTION_PROBE";

        [SerializeField] private bool enabledOnStart = true;
        [SerializeField] private float pulseIntervalSeconds = 1f;
        [SerializeField] private float pulseDurationSeconds = 0.15f;
        [SerializeField] [Range(0f, 1f)] private float pulseBrightness = 0.12f;

        private bool _probeEnabled;
        private bool _pulseActive;
        private bool _wasConnected;
        private float _nextPulseTime;
        private float _clearPulseAt;

        private void Start()
        {
            _probeEnabled = enabledOnStart && !ProbeDisabledByEnvironment();
            _nextPulseTime = Time.unscaledTime;
            _clearPulseAt = -1f;
        }

        private void Update()
        {
            if (!_probeEnabled)
            {
                return;
            }

            HardwareBridge hardwareBridge = HardwareBridge.Instance;
            if (hardwareBridge == null)
            {
                return;
            }

            bool isConnected = hardwareBridge.IsConnected;
            if (!isConnected)
            {
                _pulseActive = false;
                _wasConnected = false;
                _clearPulseAt = -1f;
                return;
            }

            if (!_wasConnected)
            {
                _nextPulseTime = Time.unscaledTime;
            }

            _wasConnected = true;

            if (!_pulseActive && Time.unscaledTime >= _nextPulseTime)
            {
                hardwareBridge.SendLedFill(pulseBrightness);
                _pulseActive = true;
                _clearPulseAt = Time.unscaledTime + Mathf.Max(0.02f, pulseDurationSeconds);
                _nextPulseTime = Time.unscaledTime + Mathf.Max(0.1f, pulseIntervalSeconds);
            }

            if (_pulseActive && Time.unscaledTime >= _clearPulseAt)
            {
                hardwareBridge.ClearLedDisplay();
                _pulseActive = false;
                _clearPulseAt = -1f;
            }
        }

        private void OnDisable()
        {
            TryClearDisplay();
        }

        private void OnDestroy()
        {
            TryClearDisplay();
        }

        private void TryClearDisplay()
        {
            if (!_pulseActive || HardwareBridge.Instance == null || !HardwareBridge.Instance.IsConnected)
            {
                return;
            }

            HardwareBridge.Instance.ClearLedDisplay();
            _pulseActive = false;
            _clearPulseAt = -1f;
        }

        private static bool ProbeDisabledByEnvironment()
        {
            string rawValue = Environment.GetEnvironmentVariable(DisableProbeEnvName);
            return !string.IsNullOrWhiteSpace(rawValue) &&
                   (rawValue == "1" || rawValue.Equals("true", StringComparison.OrdinalIgnoreCase));
        }
    }
}
