using System;
using System.Text;
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
        [SerializeField] private int selectedGamepadIndex = -1;

        private float _rumbleStopAtRealtime;
        private bool _rumbleActive;
        private string _lastError = string.Empty;
        private string _lastAction = string.Empty;

        public bool HasGamepad => ResolveSelectedGamepad() != null;
        public string DeviceName
        {
            get
            {
                Gamepad gamepad = ResolveSelectedGamepad();
                int resolvedIndex = selectedGamepadIndex >= 0 ? selectedGamepadIndex : ResolveIndex(gamepad);
                return gamepad == null ? "(none)" : DescribeGamepad(gamepad, resolvedIndex);
            }
        }

        public string CurrentDeviceName => Gamepad.current == null ? "(none)" : DescribeGamepad(Gamepad.current, ResolveCurrentIndex());
        public string AvailableGamepadsSummary => BuildAvailableGamepadsSummary();
        public bool RumbleActive => _rumbleActive;
        public string LastError => _lastError;
        public string LastAction => _lastAction;

        private void Update()
        {
            ClampSelectedGamepadIndex();

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

        public void SelectNextGamepad()
        {
            if (Gamepad.all.Count == 0)
            {
                selectedGamepadIndex = -1;
                _lastAction = "No gamepad is connected.";
                return;
            }

            selectedGamepadIndex = (selectedGamepadIndex + 1 + Gamepad.all.Count) % Gamepad.all.Count;
            _lastAction = $"Selected gamepad #{selectedGamepadIndex}: {DescribeGamepad(Gamepad.all[selectedGamepadIndex], selectedGamepadIndex)}";
        }

        public void UseCurrentGamepad()
        {
            if (Gamepad.current == null)
            {
                _lastAction = "Gamepad.current is null.";
                return;
            }

            selectedGamepadIndex = ResolveCurrentIndex();
            _lastAction = $"Selected current gamepad: {DescribeGamepad(Gamepad.current, selectedGamepadIndex)}";
        }

        public void TriggerLightPulse()
        {
            TriggerRumble(lightLowMotor, lightHighMotor, lightDurationSeconds);
        }

        public void TriggerHeavyPulse()
        {
            TriggerRumble(heavyLowMotor, heavyHighMotor, heavyDurationSeconds);
        }

        public void TriggerAllGamepads()
        {
            if (Gamepad.all.Count == 0)
            {
                _lastError = "No connected gamepad was found.";
                return;
            }

            try
            {
                InputSystem.ResumeHaptics();
                foreach (Gamepad gamepad in Gamepad.all)
                {
                    gamepad.SetMotorSpeeds(Mathf.Clamp01(heavyLowMotor), Mathf.Clamp01(heavyHighMotor));
                }

                _rumbleStopAtRealtime = Time.realtimeSinceStartup + Mathf.Max(0.01f, heavyDurationSeconds);
                _rumbleActive = true;
                _lastError = string.Empty;
                _lastAction = $"SetMotorSpeeds sent to all {Gamepad.all.Count} gamepad(s).";
            }
            catch (Exception exception)
            {
                _lastError = $"Broadcast rumble failed: {exception.Message}";
                _rumbleActive = false;
            }
        }

        public void TriggerRumble(float lowFrequency, float highFrequency, float durationSeconds)
        {
            Gamepad gamepad = ResolveSelectedGamepad();
            if (gamepad == null)
            {
                _lastError = "No selected gamepad is available for rumble.";
                return;
            }

            try
            {
                InputSystem.ResumeHaptics();
                gamepad.SetMotorSpeeds(Mathf.Clamp01(lowFrequency), Mathf.Clamp01(highFrequency));
                _rumbleStopAtRealtime = Time.realtimeSinceStartup + Mathf.Max(0.01f, durationSeconds);
                _rumbleActive = true;
                _lastError = string.Empty;
                _lastAction = $"SetMotorSpeeds ok on {DescribeGamepad(gamepad, selectedGamepadIndex)}";
            }
            catch (Exception exception)
            {
                _lastError = $"Rumble failed: {exception.Message}";
                _rumbleActive = false;
            }
        }

        public void StopRumble()
        {
            try
            {
                InputSystem.ResetHaptics();
                _lastAction = "ResetHaptics sent.";
            }
            catch (Exception exception)
            {
                _lastError = $"Stop rumble failed: {exception.Message}";
            }

            _rumbleActive = false;
        }

        private Gamepad ResolveSelectedGamepad()
        {
            ClampSelectedGamepadIndex();

            if (selectedGamepadIndex >= 0 && selectedGamepadIndex < Gamepad.all.Count)
            {
                return Gamepad.all[selectedGamepadIndex];
            }

            return Gamepad.current ?? (Gamepad.all.Count > 0 ? Gamepad.all[0] : null);
        }

        private void ClampSelectedGamepadIndex()
        {
            if (Gamepad.all.Count == 0)
            {
                selectedGamepadIndex = -1;
                return;
            }

            if (selectedGamepadIndex >= Gamepad.all.Count)
            {
                selectedGamepadIndex = Gamepad.all.Count - 1;
            }
        }

        private int ResolveCurrentIndex()
        {
            return ResolveIndex(Gamepad.current);
        }

        private int ResolveIndex(Gamepad gamepad)
        {
            if (gamepad == null)
            {
                return -1;
            }

            for (int index = 0; index < Gamepad.all.Count; index++)
            {
                if (Gamepad.all[index] == gamepad)
                {
                    return index;
                }
            }

            return -1;
        }

        private string BuildAvailableGamepadsSummary()
        {
            if (Gamepad.all.Count == 0)
            {
                return "(none)";
            }

            StringBuilder builder = new StringBuilder();
            int currentIndex = ResolveCurrentIndex();
            for (int index = 0; index < Gamepad.all.Count; index++)
            {
                if (index > 0)
                {
                    builder.Append(" | ");
                }

                Gamepad gamepad = Gamepad.all[index];
                builder.Append(index);
                builder.Append(':');
                if (index == selectedGamepadIndex)
                {
                    builder.Append("[selected]");
                }

                if (index == currentIndex)
                {
                    builder.Append("[current]");
                }

                builder.Append(gamepad.displayName);
                if (!string.IsNullOrWhiteSpace(gamepad.description.interfaceName))
                {
                    builder.Append('/');
                    builder.Append(gamepad.description.interfaceName);
                }
            }

            return builder.ToString();
        }

        private static string DescribeGamepad(Gamepad gamepad, int index)
        {
            return $"#{index} {gamepad.displayName} / {gamepad.description.product} / {gamepad.description.interfaceName} / {gamepad.layout}";
        }
    }
}
