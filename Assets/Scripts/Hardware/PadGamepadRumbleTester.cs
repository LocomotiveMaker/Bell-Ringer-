using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BellRinger.Hardware
{
    [DisallowMultipleComponent]
    public sealed class PadGamepadRumbleTester : MonoBehaviour
    {
        [SerializeField] private float lightLowMotor = 0.18f;
        [SerializeField] private float lightHighMotor = 0.34f;
        [SerializeField] private float lightDurationSeconds = 0.12f;
        [SerializeField] private float heavyLowMotor = 0.65f;
        [SerializeField] private float heavyHighMotor = 0.88f;
        [SerializeField] private float heavyDurationSeconds = 0.22f;

        private float _rumbleStopAtRealtime;
        private bool _rumbleActive;
        private string _lastError = string.Empty;

        public bool HasGamepad => Gamepad.current != null;
        public string DeviceName => Gamepad.current == null ? "(none)" : $"{Gamepad.current.displayName} / {Gamepad.current.description.product}";
        public bool RumbleActive => _rumbleActive;
        public string LastError => _lastError;

        private void Update()
        {
            if (_rumbleActive && Time.realtimeSinceStartup >= _rumbleStopAtRealtime)
            {
                StopRumble();
            }
        }

        private void OnDisable()
        {
            StopRumble();
        }

        private void OnDestroy()
        {
            StopRumble();
        }

        public void TriggerLightPulse()
        {
            TriggerRumble(lightLowMotor, lightHighMotor, lightDurationSeconds);
        }

        public void TriggerHeavyPulse()
        {
            TriggerRumble(heavyLowMotor, heavyHighMotor, heavyDurationSeconds);
        }

        public void TriggerRumble(float lowFrequency, float highFrequency, float durationSeconds)
        {
            Gamepad gamepad = Gamepad.current;
            if (gamepad == null)
            {
                _lastError = "No Gamepad.current device is available for rumble.";
                return;
            }

            try
            {
                gamepad.SetMotorSpeeds(Mathf.Clamp01(lowFrequency), Mathf.Clamp01(highFrequency));
                _rumbleStopAtRealtime = Time.realtimeSinceStartup + Mathf.Max(0.01f, durationSeconds);
                _rumbleActive = true;
                _lastError = string.Empty;
            }
            catch (Exception exception)
            {
                _lastError = $"Rumble failed: {exception.Message}";
                _rumbleActive = false;
            }
        }

        public void StopRumble()
        {
            Gamepad gamepad = Gamepad.current;
            if (gamepad != null)
            {
                try
                {
                    gamepad.ResetHaptics();
                }
                catch (Exception exception)
                {
                    _lastError = $"Stop rumble failed: {exception.Message}";
                }
            }

            _rumbleActive = false;
        }
    }
}
